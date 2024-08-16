using Newtonsoft.Json;

using NuKeeper.Abstractions.Logging;
using NuKeeper.BitBucket.Models;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace NuKeeper.BitBucket
{
    public class BitbucketRestClient
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        private readonly HttpClient _client;
        private readonly INuKeeperLogger _logger;

        public BitbucketRestClient(IHttpClientFactory clientFactory, INuKeeperLogger logger, string username,
            string appPassword, Uri apiBaseAddress)
        {
            _logger = logger;

            _client = clientFactory.CreateClient();
            _client.BaseAddress = apiBaseAddress;
            byte[] byteArray = Encoding.ASCII.GetBytes($"{username}:{appPassword}");
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private async Task<T> GetResourceOrEmpty<T>(string url)
        {
            _logger.Detailed($"Getting from BitBucket url {url}");
            HttpResponseMessage response = await _client.GetAsync(url);

            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.Detailed($"Response {response.StatusCode} is not success, body:\n{responseBody}");
                return default;
            }

            return JsonConvert.DeserializeObject<T>(responseBody);
        }

        public async Task<IEnumerable<ProjectInfo>> GetProjects(string account)
        {
            IteratorBasedPage<ProjectInfo> response = await GetResourceOrEmpty<IteratorBasedPage<ProjectInfo>>($"teams/{account}/projects/");
            return response.values;
        }

        public async Task<IEnumerable<Repository>> GetGitRepositories(string account)
        {
            IteratorBasedPage<Repository> response = await GetResourceOrEmpty<IteratorBasedPage<Repository>>($"repositories/{account}");
            return response.values;
        }

        public async Task<Repository> GetGitRepository(string account, string repositoryName)
        {
            Repository response = await GetResourceOrEmpty<Repository>($"repositories/{account}/{repositoryName}");
            return response;
        }

        public async Task<IEnumerable<Ref>> GetRepositoryRefs(string account, string repositoryId)
        {
            IteratorBasedPage<Ref> response = await GetResourceOrEmpty<IteratorBasedPage<Ref>>($"repositories/{account}/{repositoryId}/refs");
            return response.values;
        }

        // https://developer.atlassian.com/bitbucket/api/2/reference/meta/filtering#query-pullreq
        public async Task<PullRequestsInfo> GetPullRequests(
            string account,
            string repositoryName,
            string headBranch,
            string baseBranch)
        {
            string filter = $"state =\"open\" AND source.branch.name = \"{headBranch}\" AND destination.branch.name = \"{baseBranch}\"";

            PullRequestsInfo response = await GetResourceOrEmpty<PullRequestsInfo>($"repositories/{account}/{repositoryName}/pullrequests?q={HttpUtility.UrlEncode(filter)}");

            return response;
        }

        public async Task<PullRequest> CreatePullRequest(string account, string repositoryName, PullRequest request)
        {
            HttpResponseMessage response = await _client.PostAsync($"repositories/{account}/{repositoryName}/pullrequests",
                 new StringContent(JsonConvert.SerializeObject(request, Formatting.None, JsonSerializerSettings), Encoding.UTF8, "application/json"));

            string result = await response.Content.ReadAsStringAsync();
            PullRequest resource = JsonConvert.DeserializeObject<PullRequest>(result);
            return resource;
        }
    }
}
