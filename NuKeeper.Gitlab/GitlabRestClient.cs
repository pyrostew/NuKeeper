using Newtonsoft.Json;

using NuKeeper.Abstractions;
using NuKeeper.Abstractions.Logging;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace NuKeeper.Gitlab
{
    public class GitlabRestClient
    {
        private readonly HttpClient _client;
        private readonly INuKeeperLogger _logger;

        public GitlabRestClient(IHttpClientFactory clientFactory, string token, INuKeeperLogger logger, Uri apiBaseAddress)
        {
            _logger = logger;

            _client = clientFactory.CreateClient();
            _client.BaseAddress = apiBaseAddress;
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Add("Private-Token", token);
        }

        // https://docs.gitlab.com/ee/api/users.html#for-normal-users-1
        // GET /user
        public async Task<Model.User> GetCurrentUser()
        {
            return await GetResource<Model.User>("user");
        }

        // https://docs.gitlab.com/ee/api/projects.html#get-single-project
        // GET /projects/:id
        public async Task<Model.Project> GetProject(string projectName, string repositoryName)
        {
            string encodedProjectName = HttpUtility.UrlEncode($"{projectName}/{repositoryName}");
            return await GetResource<Model.Project>($"projects/{encodedProjectName}");
        }

        // https://docs.gitlab.com/ee/api/branches.html#get-single-repository-branch
        // GET /projects/:id/repository/branches/:branch
        public async Task<Model.Branch> CheckExistingBranch(string projectName, string repositoryName,
            string branchName)
        {
            string encodedProjectName = HttpUtility.UrlEncode($"{projectName}/{repositoryName}");
            string encodedBranchName = HttpUtility.UrlEncode(branchName);

            return await GetResource(
                $"projects/{encodedProjectName}/repository/branches/{encodedBranchName}",
                statusCode => statusCode == HttpStatusCode.NotFound
                    ? Result<Model.Branch>.Success(null)
                    : Result<Model.Branch>.Failure());
        }

        // https://docs.gitlab.com/ee/api/merge_requests.html#list-merge-requests
        // GET GET /projects/:id/merge_requests?state=opened&target_branch=<head>&source_branch=<base>
        public async Task<IEnumerable<Model.MergeInfo>> GetMergeRequests(
            string projectName,
            string repositoryName,
            string headBranch,
            string baseBranch)
        {
            string encodedProjectName = HttpUtility.UrlEncode($"{projectName}/{repositoryName}");
            string encodedBaseBranch = HttpUtility.UrlEncode(baseBranch);
            string encodedHeadBranch = HttpUtility.UrlEncode(headBranch);

            return await GetResource(
                $"projects/{encodedProjectName}/merge_requests?state=opened&view=simple&source_branch={encodedHeadBranch}&target_branch={encodedBaseBranch}",
                statusCode => statusCode == HttpStatusCode.NotFound
                    ? Result<IEnumerable<Model.MergeInfo>>.Success(null)
                    : Result<IEnumerable<Model.MergeInfo>>.Failure());
        }

        // https://docs.gitlab.com/ee/api/merge_requests.html#create-mr
        // POST /projects/:id/merge_requests
        public Task<Model.MergeRequest> OpenMergeRequest(string projectName, string repositoryName, Model.MergeRequest mergeRequest)
        {
            string encodedProjectName = HttpUtility.UrlEncode($"{projectName}/{repositoryName}");

            StringContent content = new(JsonConvert.SerializeObject(mergeRequest), Encoding.UTF8,
                "application/json");
            return PostResource<Model.MergeRequest>($"projects/{encodedProjectName}/merge_requests", content);
        }

        private async Task<T> GetResource<T>(string url, Func<HttpStatusCode, Result<T>> customErrorHandling = null, [CallerMemberName] string caller = null)
        {
            Uri fullUrl = new(url, UriKind.Relative);
            _logger.Detailed($"{caller}: Requesting {fullUrl}");

            HttpResponseMessage response = await _client.GetAsync(fullUrl);
            return await HandleResponse(response, customErrorHandling, caller);
        }

        private async Task<T> PostResource<T>(string url, HttpContent content, Func<HttpStatusCode, Result<T>> customErrorHandling = null, [CallerMemberName] string caller = null)
        {
            _logger.Detailed($"{caller}: Requesting {url}");

            HttpResponseMessage response = await _client.PostAsync(url, content);

            return await HandleResponse(response, customErrorHandling, caller);
        }

        private async Task<T> HandleResponse<T>(HttpResponseMessage response,
            Func<HttpStatusCode, Result<T>> customErrorHandling,
            [CallerMemberName] string caller = null)
        {
            string msg;

            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.Detailed($"Response {response.StatusCode} is not success, body:\n{responseBody}");

                if (customErrorHandling != null)
                {
                    Result<T> result = customErrorHandling(response.StatusCode);

                    if (result.IsSuccessful)
                    {
                        return result.Value;
                    }
                }

                switch (response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        msg = $"{caller}: Unauthorised, ensure PAT has appropriate permissions";
                        _logger.Error(msg);
                        throw new NuKeeperException(msg);
                    case HttpStatusCode.Forbidden:
                        msg = $"{caller}: Forbidden, ensure PAT has appropriate permissions";
                        _logger.Error(msg);
                        throw new NuKeeperException(msg);
                    default:
                        msg = $"{caller}: Error {response.StatusCode}, {responseBody}";
                        _logger.Error(msg);
                        throw new NuKeeperException(msg);
                }
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(responseBody);
            }
            catch (JsonException ex)
            {
                msg = $"{caller} failed to parse json to {typeof(T)}: {ex.Message}";
                _logger.Error(msg);
                throw new NuKeeperException($"Failed to parse json to {typeof(T)}", ex);
            }
        }
    }
}
