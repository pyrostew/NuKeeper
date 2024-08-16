using NuKeeper.Abstractions;
using NuKeeper.Abstractions.CollaborationModels;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NuKeeper.AzureDevOps
{
    public class AzureDevOpsPlatform : ICollaborationPlatform
    {
        private readonly INuKeeperLogger _logger;
        private readonly IHttpClientFactory _clientFactory;
        private AzureDevOpsRestClient _client;

        public AzureDevOpsPlatform(INuKeeperLogger logger, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _clientFactory = clientFactory;
        }

        public void Initialise(AuthSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _client = new AzureDevOpsRestClient(_clientFactory, _logger, settings.Token, settings.ApiBase);
        }

        public async Task<User> GetCurrentUser()
        {
            try
            {
                Resource<Account> currentAccounts = await _client.GetCurrentUser();
                Account account = currentAccounts.value.FirstOrDefault();

                return account == null ? User.Default : new User(account.accountId, account.accountName, account.Mail);
            }
            catch (NuKeeperException)
            {
                return User.Default;
            }
        }

        public async Task<User> GetUserByMail(string email)
        {
            try
            {
                Resource<Account> currentAccounts = await _client.GetUserByMail(email);
                Account account = currentAccounts.value.FirstOrDefault();

                return account == null ? User.Default : new User(account.accountId, account.accountName, account.Mail);
            }
            catch (NuKeeperException)
            {
                return User.Default;
            }
        }

        public async Task<bool> PullRequestExists(ForkData target, string headBranch, string baseBranch)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            IEnumerable<AzureRepository> repos = await _client.GetGitRepositories(target.Owner);
            AzureRepository repo = repos.Single(x => x.name.Equals(target.Name, StringComparison.OrdinalIgnoreCase));

            IEnumerable<PullRequest> result = await _client.GetPullRequests(
                target.Owner,
                repo.id,
                $"refs/heads/{headBranch}",
                $"refs/heads/{baseBranch}");

            return result.Any();
        }

        public async Task OpenPullRequest(ForkData target, PullRequestRequest request, IEnumerable<string> labels)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (labels == null)
            {
                throw new ArgumentNullException(nameof(labels));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            IEnumerable<AzureRepository> repos = await _client.GetGitRepositories(target.Owner);
            AzureRepository repo = repos.Single(x => x.name.Equals(target.Name, StringComparison.OrdinalIgnoreCase));

            PRRequest req = new()
            {
                title = request.Title,
                sourceRefName = $"refs/heads/{request.Head}",
                description = request.Body,
                targetRefName = $"refs/heads/{request.BaseRef}",
                completionOptions = new GitPullRequestCompletionOptions
                {
                    deleteSourceBranch = request.DeleteBranchAfterMerge
                }
            };

            PullRequest pullRequest = await _client.CreatePullRequest(req, target.Owner, repo.id);

            if (request.SetAutoMerge)
            {
                _ = await _client.SetAutoComplete(new PRRequest()
                {
                    autoCompleteSetBy = new Creator()
                    {
                        id = pullRequest.CreatedBy.id
                    }
                }, target.Owner,
                    repo.id,
                    pullRequest.PullRequestId);
            }

            foreach (string label in labels)
            {
                _ = await _client.CreatePullRequestLabel(new LabelRequest { name = label }, target.Owner, repo.id, pullRequest.PullRequestId);
            }
        }

        public async Task<IReadOnlyList<Organization>> GetOrganizations()
        {
            IEnumerable<Project> projects = await _client.GetProjects();
            return projects
                .Select(project => new Organization(project.name))
                .ToList();
        }

        public async Task<IReadOnlyList<Repository>> GetRepositoriesForOrganisation(string projectName)
        {
            IEnumerable<AzureRepository> repos = await _client.GetGitRepositories(projectName);
            return repos.Select(x =>
                    new Repository(x.name, false,
                        new UserPermissions(true, true, true),
                        new Uri(x.remoteUrl),
                        null, false, null))
                .ToList();
        }

        public async Task<Repository> GetUserRepository(string projectName, string repositoryName)
        {
            IReadOnlyList<Repository> repos = await GetRepositoriesForOrganisation(projectName);
            return repos.Single(x => x.Name.Equals(repositoryName, StringComparison.OrdinalIgnoreCase));
        }

        public Task<Repository> MakeUserFork(string owner, string repositoryName)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> RepositoryBranchExists(string projectName, string repositoryName, string branchName)
        {
            IEnumerable<AzureRepository> repos = await _client.GetGitRepositories(projectName);
            AzureRepository repo = repos.Single(x => x.name.Equals(repositoryName, StringComparison.OrdinalIgnoreCase));
            IEnumerable<GitRefs> refs = await _client.GetRepositoryRefs(projectName, repo.id);
            int count = refs.Count(x => x.name.EndsWith(branchName, StringComparison.OrdinalIgnoreCase));
            if (count > 0)
            {
                _logger.Detailed($"Branch found for {projectName} / {repositoryName} / {branchName}");
                return true;
            }

            _logger.Detailed($"No branch found for {projectName} / {repositoryName} / {branchName}");
            return false;
        }

        public async Task<SearchCodeResult> Search(SearchCodeRequest searchRequest)
        {
            if (searchRequest == null)
            {
                throw new ArgumentNullException(nameof(searchRequest));
            }

            int totalCount = 0;
            List<string> repositoryFileNames = [];
            foreach (SearchRepo repo in searchRequest.Repos)
            {
                repositoryFileNames.AddRange(await _client.GetGitRepositoryFileNames(repo.Owner, repo.Name));
            }

            string[] searchStrings = searchRequest.Term
                .Replace("\"", string.Empty)
                .Split(new[] { "OR" }, StringSplitOptions.None);

            foreach (string searchString in searchStrings)
            {
                totalCount += repositoryFileNames.FindAll(x => x.EndsWith(searchString.Trim(), StringComparison.InvariantCultureIgnoreCase)).Count;
            }

            return new SearchCodeResult(totalCount);
        }

        public async Task<int> GetNumberOfOpenPullRequests(string projectName, string repositoryName)
        {
            User user = await GetCurrentUser();

            if (user == User.Default)
            {
                // TODO: allow this to be configurable
                user = await GetUserByMail("bot@nukeeper.com");
            }

            IEnumerable<PullRequest> prs = await GetPullRequestsForUser(
                projectName,
                repositoryName,
                user == User.Default ?
                    string.Empty
                    : user.Login
            );

            if (user == User.Default)
            {
                IEnumerable<PullRequest> relevantPrs = prs?
                    .Where(
                        pr => pr.labels
                            ?.FirstOrDefault(
                                l => l.name.Equals(
                                    "nukeeper",
                                    StringComparison.InvariantCultureIgnoreCase
                                )
                            )?.active ?? false
                    );

                return relevantPrs?.Count() ?? 0;
            }
            else
            {
                return prs?.Count() ?? 0;
            }
        }

        private async Task<IEnumerable<PullRequest>> GetPullRequestsForUser(string projectName, string repositoryName, string userName)
        {
            try
            {
                return await _client.GetPullRequests(projectName, repositoryName, userName);

            }
            catch (NuKeeperException ex)
            {
                _logger.Error($"Failed to get pull requests for name {userName}", ex);
                return Enumerable.Empty<PullRequest>();
            }
        }
    }
}
