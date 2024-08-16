using NuKeeper.Abstractions.CollaborationModels;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Logging;
using NuKeeper.BitBucketLocal.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Repository = NuKeeper.Abstractions.CollaborationModels.Repository;

namespace NuKeeper.BitBucketLocal
{
    public class BitBucketLocalPlatform : ICollaborationPlatform
    {
        private readonly INuKeeperLogger _logger;
        private readonly IHttpClientFactory _clientFactory;
        private AuthSettings _settings;
        private BitbucketLocalRestClient _client;

        public BitBucketLocalPlatform(INuKeeperLogger nuKeeperLogger, IHttpClientFactory clientFactory)
        {
            _logger = nuKeeperLogger;
            _clientFactory = clientFactory;
        }

        public void Initialise(AuthSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _client = new BitbucketLocalRestClient(_clientFactory, _logger, settings.Username, settings.Token, settings.ApiBase);
        }

        public Task<User> GetCurrentUser()
        {
            return Task.FromResult(new User(_settings.Username, "", ""));
        }

        public async Task<bool> PullRequestExists(ForkData target, string headBranch, string baseBranch)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            IEnumerable<Models.Repository> repositories = await _client.GetGitRepositories(target.Owner);
            Models.Repository targetRepository = repositories.FirstOrDefault(x => x.Name.Equals(target.Name, StringComparison.InvariantCultureIgnoreCase));

            IEnumerable<PullRequest> pullRequests = await _client.GetPullRequests(target.Owner, targetRepository.Name, headBranch, baseBranch);

            return pullRequests.Any();
        }

        public async Task OpenPullRequest(ForkData target, PullRequestRequest request, IEnumerable<string> labels)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            IEnumerable<Models.Repository> repositories = await _client.GetGitRepositories(target.Owner);
            Models.Repository targetRepository = repositories.FirstOrDefault(x => x.Name.Equals(target.Name, StringComparison.InvariantCultureIgnoreCase));

            IEnumerable<PullRequestReviewer> reviewers = await _client.GetBitBucketReviewers(target.Owner, targetRepository.Name, targetRepository.Id, request.Head, request.BaseRef);

            PullRequest pullReq = new()
            {
                Title = request.Title,
                Description = request.Body,
                FromRef = new Ref
                {
                    Id = request.Head
                },
                ToRef = new Ref
                {
                    Id = request.BaseRef
                },
                Reviewers = reviewers.ToList()
            };

            _ = await _client.CreatePullRequest(pullReq, target.Owner, targetRepository.Name);
        }

        public async Task<IReadOnlyList<Organization>> GetOrganizations()
        {
            IEnumerable<Models.Repository> projects = await _client.GetProjects();
            return projects
                .Select(project => new Organization(project.Name))
                .ToList();
        }

        public async Task<IReadOnlyList<Repository>> GetRepositoriesForOrganisation(string projectName)
        {
            IEnumerable<Models.Repository> repos = await _client.GetGitRepositories(projectName);

            return repos.Select(repo =>
                    new Repository(repo.Name, false,
                        new UserPermissions(true, true, true),
                        new Uri(repo.Links.Clone.First(x => x.Name.StartsWith("http", StringComparison.InvariantCultureIgnoreCase)).Href),
                        null, false, null))
                .ToList();
        }

        public async Task<Repository> GetUserRepository(string projectName, string repositoryName)
        {
            string sanitisedRepositoryName = SanitizeRepositoryName(repositoryName);
            IReadOnlyList<Repository> repos = await GetRepositoriesForOrganisation(projectName);
            return repos.Single(x => string.Equals(SanitizeRepositoryName(x.Name), sanitisedRepositoryName, StringComparison.OrdinalIgnoreCase));
        }

        private static string SanitizeRepositoryName(string repositoryName)
        {
            return string.IsNullOrWhiteSpace(repositoryName) ? string.Empty : repositoryName.Replace("-", " ");
        }

        public Task<Repository> MakeUserFork(string owner, string repositoryName)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> RepositoryBranchExists(string projectName, string repositoryName, string branchName)
        {
            IEnumerable<Branch> branches = await _client.GetGitRepositoryBranches(projectName, repositoryName);

            int count = branches.Count(x => x.DisplayId.Equals(branchName, StringComparison.OrdinalIgnoreCase));
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

        public Task<int> GetNumberOfOpenPullRequests(string projectName, string repositoryName)
        {
            return Task.FromResult(0);
        }
    }
}
