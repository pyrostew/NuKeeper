using LibGit2Sharp;

using NuKeeper.Abstractions;
using NuKeeper.Abstractions.CollaborationModels;
using NuKeeper.Abstractions.Git;
using NuKeeper.Abstractions.Inspections.Files;
using NuKeeper.Abstractions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using GitCommands = LibGit2Sharp.Commands;
using Repository = LibGit2Sharp.Repository;

namespace NuKeeper.Git
{
    public class LibGit2SharpDriver : IGitDriver
    {
        private readonly INuKeeperLogger _logger;
        private readonly Credentials _gitCredentials;
        private readonly Identity _identity;
        private bool _fetchFinished;

        public IFolder WorkingFolder { get; }

        public LibGit2SharpDriver(INuKeeperLogger logger,
            IFolder workingFolder, GitUsernamePasswordCredentials credentials, User user)
        {
            if (credentials == null)
            {
                throw new ArgumentNullException(nameof(credentials));
            }

            _logger = logger;
            WorkingFolder = workingFolder ?? throw new ArgumentNullException(nameof(workingFolder));
            _gitCredentials = new UsernamePasswordCredentials
            { Password = credentials.Password, Username = credentials.Username };
            _identity = GetUserIdentity(user);
        }

        public async Task Clone(Uri pullEndpoint)
        {
            await Clone(pullEndpoint, null);
        }

        public Task Clone(Uri pullEndpoint, string branchName)
        {
            return Task.Run(() =>
            {
                _logger.Normal($"Git clone {pullEndpoint}, branch {branchName ?? "default"}, to {WorkingFolder.FullPath}");

                CloneOptions opts = new()
                {
                    BranchName = branchName
                };
                opts.FetchOptions.CredentialsProvider = UsernamePasswordCredentials;
                opts.FetchOptions.OnTransferProgress = OnTransferProgress;

                _ = Repository.Clone(pullEndpoint.AbsoluteUri, WorkingFolder.FullPath, opts);

                _logger.Detailed("Git clone complete");
            });
        }

        private bool OnTransferProgress(TransferProgress progress)
        {
            if (progress.ReceivedObjects % Math.Max(progress.TotalObjects / 10, 1) == 0 && !_fetchFinished)
            {
                _logger.Detailed($"{progress.ReceivedObjects} / {progress.TotalObjects}");
                _fetchFinished = progress.ReceivedObjects == progress.TotalObjects;
            }

            return true;
        }

        public Task AddRemote(string name, Uri endpoint)
        {
            return Task.Run(() =>
            {
                using Repository repo = MakeRepo();
                _ = repo.Network.Remotes.Add(name, endpoint.AbsoluteUri);
            });
        }

        public Task Checkout(string branchName)
        {
            return Task.Run(() =>
            {
                _logger.Detailed($"Git checkout '{branchName}'");
                using Repository repo = MakeRepo();
                if (BranchExists(branchName))
                {
                    _logger.Normal($"Git checkout local branch '{branchName}'");
                    _ = GitCommands.Checkout(repo, repo.Branches[branchName]);
                }
                else
                {
                    throw new NuKeeperException(
                        $"Git Cannot checkout branch: the branch named '{branchName}' doesn't exist");
                }
            });
        }

        public Task CheckoutRemoteToLocal(string branchName)
        {
            return Task.Run(() =>
            {
                string qualifiedBranchName = "origin/" + branchName;

                _logger.Detailed($"Git checkout '{qualifiedBranchName}'");
                using Repository repo = MakeRepo();
                if (!BranchExists(qualifiedBranchName))
                {
                    throw new NuKeeperException(
                        $"Git Cannot checkout branch: the branch named '{qualifiedBranchName}' doesn't exist");
                }

                if (BranchExists(branchName))
                {
                    throw new NuKeeperException(
                        $"Git Cannot checkout branch '{qualifiedBranchName}' to '{branchName}': the branch named '{branchName}' does already exist");
                }

                _logger.Normal($"Git checkout existing branch '{qualifiedBranchName}' to '{branchName}'");

                // Get a reference on the remote tracking branch
                Branch trackedBranch = repo.Branches[qualifiedBranchName];
                // Create a local branch pointing at the same Commit
                Branch branch = repo.CreateBranch(branchName, trackedBranch.Tip);
                // Configure the local branch to track the remote one.
                _ = repo.Branches.Update(branch, b => b.TrackedBranch = trackedBranch.CanonicalName);

                // go to the just created branch
                _ = Checkout(branchName);
            });
        }

        public Task<bool> CheckoutNewBranch(string branchName)
        {
            return Task.Run(() =>
            {
                _logger.Detailed($"Git checkout new branch '{branchName}'");
                string qualifiedBranchName = "origin/" + branchName;
                if (BranchExists(qualifiedBranchName))
                {
                    _logger.Normal($"Git Cannot checkout new branch: a branch named '{qualifiedBranchName}' already exists");
                    return false;
                }

                _logger.Detailed($"Git checkout new branch '{branchName}'");
                using Repository repo = MakeRepo();
                Branch branch = repo.CreateBranch(branchName);
                _ = GitCommands.Checkout(repo, branch);
                return true;
            });
        }

        private bool BranchExists(string branchName)
        {
            using Repository repo = MakeRepo();
            bool branchFound = repo.Branches.Any(
                br => string.Equals(br.FriendlyName, branchName, StringComparison.Ordinal));
            return branchFound;
        }

        public Task Commit(string message)
        {
            return Task.Run(() =>
            {
                _logger.Detailed($"Git commit with message '{message}'");
                using Repository repo = MakeRepo();
                Signature signature = GetSignature(repo);
                GitCommands.Stage(repo, "*");
                _ = repo.Commit(message, signature, signature);
            });
        }

        private Signature GetSignature(Repository repo)
        {
            if (_identity != null)
            {
                return new Signature(_identity, DateTimeOffset.Now);
            }

            Signature repoSignature = repo.Config.BuildSignature(DateTimeOffset.Now);

            return repoSignature ?? throw new NuKeeperException(
                    "Failed to build signature, did not get valid git user identity from token or from repo config");
        }

        public Task Push(string remoteName, string branchName)
        {
            return Task.Run(() =>
            {
                _logger.Detailed($"Git push to {remoteName}/{branchName}");

                using Repository repo = MakeRepo();
                Branch localBranch = repo.Branches
                    .Single(b => b.CanonicalName.EndsWith(branchName, StringComparison.OrdinalIgnoreCase) && !b.IsRemote);
                Remote remote = repo.Network.Remotes
                    .Single(r => r.Name.EndsWith(remoteName, StringComparison.OrdinalIgnoreCase));

                _ = repo.Branches.Update(localBranch,
                    b => b.Remote = remote.Name,
                    b => b.UpstreamBranch = localBranch.CanonicalName);

                repo.Network.Push(localBranch, new PushOptions
                {
                    CredentialsProvider = UsernamePasswordCredentials
                });
            });
        }

        public Task<string> GetCurrentHead()
        {
            return Task.Run(() =>
            {
                using Repository repo = MakeRepo();
                return repo.Branches.Single(b => b.IsCurrentRepositoryHead).FriendlyName;
            });
        }

        private Repository MakeRepo()
        {
            return new Repository(WorkingFolder.FullPath);
        }

        private Credentials UsernamePasswordCredentials(
            string url, string usernameFromUrl, SupportedCredentialTypes types)
        {
            return _gitCredentials;
        }


        private Identity GetUserIdentity(User user)
        {
            if (string.IsNullOrWhiteSpace(user?.Name))
            {
                _logger.Minimal("User name missing from profile, falling back to .gitconfig");
                return null;
            }
            if (string.IsNullOrWhiteSpace(user?.Email))
            {
                _logger.Minimal("Email missing from profile, falling back to .gitconfig");
                return null;
            }

            return new Identity(user.Name, user.Email);
        }

        public Task<IReadOnlyCollection<string>> GetNewCommitMessages(string baseBranchName, string headBranchName)
        {
            return Task.Run(() =>
            {
                if (!BranchExists(baseBranchName))
                {
                    throw new NuKeeperException(
                        $"Git Cannot compare branches: the branch named '{baseBranchName}' doesn't exist");
                }
                if (!BranchExists(headBranchName))
                {
                    throw new NuKeeperException(
                        $"Git Cannot compare branches: the branch named '{headBranchName}' doesn't exist");
                }

                using Repository repo = MakeRepo();
                Branch baseBranch = repo.Branches[baseBranchName];
                Branch headBranch = repo.Branches[headBranchName];

                CommitFilter filter = new()
                {
                    SortBy = CommitSortStrategies.Time,
                    ExcludeReachableFrom = baseBranch,
                    IncludeReachableFrom = headBranch
                };
                return (IReadOnlyCollection<string>)repo.Commits.QueryBy(filter).Select(c => c.MessageShort.TrimEnd(new[] { '\r', '\n' })).ToList().AsReadOnly();
            });
        }
    }
}
