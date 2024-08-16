using LibGit2Sharp;

using NuKeeper.Abstractions;
using NuKeeper.Abstractions.Git;
using NuKeeper.Abstractions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NuKeeper.Git
{
    public class LibGit2SharpDiscoveryDriver : IGitDiscoveryDriver
    {
        private readonly INuKeeperLogger _logger;

        public LibGit2SharpDiscoveryDriver(INuKeeperLogger logger)
        {
            _logger = logger;
        }

        public async Task<bool> IsGitRepo(Uri repositoryUri)
        {
            Uri discovered = await DiscoverRepo(repositoryUri);
            return discovered != null && Repository.IsValid(Uri.UnescapeDataString(discovered.AbsolutePath));
        }

        public async Task<IEnumerable<GitRemote>> GetRemotes(Uri repositoryUri)
        {
            if (repositoryUri == null)
            {
                throw new ArgumentNullException(nameof(repositoryUri));
            }

            if (!await IsGitRepo(repositoryUri))
            {
                return Enumerable.Empty<GitRemote>();
            }

            string discover = Repository.Discover(Uri.UnescapeDataString(repositoryUri.AbsolutePath));

            List<GitRemote> gitRemotes = [];
            using Repository repo = new(discover);
            foreach (Remote remote in repo.Network.Remotes)
            {
                _ = Uri.TryCreate(remote.Url, UriKind.Absolute, out repositoryUri);

                if (repositoryUri != null)
                {
                    GitRemote gitRemote = new()
                    {
                        Name = remote.Name,
                        Url = repositoryUri
                    };
                    gitRemotes.Add(gitRemote);
                }
                else
                {
                    _logger.Normal($"Cannot parse {remote.Url} to URI. SSH remote is currently not supported");
                }
            }

            return gitRemotes;
        }

        public Task<Uri> DiscoverRepo(Uri repositoryUri)
        {
            return Task.Run(() =>
            {
                string discovery = Repository.Discover(Uri.UnescapeDataString(repositoryUri.AbsolutePath));

                return string.IsNullOrEmpty(discovery) ? null : new Uri(discovery);
            });
        }

        public async Task<string> GetCurrentHead(Uri repositoryUri)
        {
            string repoRoot = (await DiscoverRepo(repositoryUri)).AbsolutePath;
            using Repository repo = new(repoRoot);
            Branch repoHeadBranch = repo.Branches.
                SingleOrDefault(b => b.IsCurrentRepositoryHead);

            return repoHeadBranch == null
                ? throw new NuKeeperException($"Cannot find current head branch for repo at '{repoRoot}', with {repo.Branches.Count()} branches")
                : repoHeadBranch.FriendlyName;
        }

        public async Task<GitRemote> GetRemoteForPlatform(Uri repositoryUri, string platformHost)
        {
            IEnumerable<GitRemote> remotes = await GetRemotes(repositoryUri);
            return remotes
                .FirstOrDefault(rm => rm.Url.Host.Contains(platformHost, StringComparison.OrdinalIgnoreCase));
        }
    }
}
