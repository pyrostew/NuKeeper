using NuKeeper.Abstractions;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Git;

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NuKeeper.Gitea
{
    public class GiteaSettingsReader : ISettingsReader
    {
        private readonly IEnvironmentVariablesProvider _environmentVariablesProvider;
        private const string GiteaTokenEnvironmentVariableName = "NuKeeper_gitea_token";
        private const string UrlPattern = "https://yourgiteaserver/{owner}/{repo}.git";
        private const string ApiBaseAdress = "api/v1/";
        private readonly IGitDiscoveryDriver _gitDriver;
        private readonly IHttpClientFactory _clientFactory;

        public GiteaSettingsReader(IGitDiscoveryDriver gitDiscoveryDriver, IEnvironmentVariablesProvider environmentVariablesProvider,
            IHttpClientFactory clientFactory)
        {
            _environmentVariablesProvider = environmentVariablesProvider;
            _clientFactory = clientFactory;
            _gitDriver = gitDiscoveryDriver;
        }

        public Platform Platform => Platform.Gitea;

        public async Task<bool> CanRead(Uri repositoryUri)
        {
            if (repositoryUri == null || repositoryUri.Segments == null || repositoryUri.Segments.Length < 3)
            {
                return false;
            }

            try
            {
                // There is no real identifier for gitea repos so try to get the gitea swagger json
                HttpClient client = _clientFactory.CreateClient();
                client.BaseAddress = GetBaseAddress(repositoryUri);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync($"swagger.v1.json");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("No valid Gitea repo during repo check\n\r{0}", ex.Message);
            }

            return false;
        }

        public void UpdateCollaborationPlatformSettings(CollaborationPlatformSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            string envToken = _environmentVariablesProvider.GetEnvironmentVariable(GiteaTokenEnvironmentVariableName);

            settings.Token = Concat.FirstValue(envToken, settings.Token);
        }

        public async Task<RepositorySettings> RepositorySettings(Uri repositoryUri, bool setAutoMerge, string targetBranch = null)
        {
            if (repositoryUri == null)
            {
                throw new NuKeeperException(
                    $"The provided uri was is not in the correct format. Provided null and format should be {UrlPattern}");
            }

            RepositorySettings settings = repositoryUri.IsFile
                ? await CreateSettingsFromLocal(repositoryUri, targetBranch)
                : await CreateSettingsFromRemote(repositoryUri, targetBranch);

            return settings;
        }

        private static Task<RepositorySettings> CreateSettingsFromRemote(Uri repositoryUri, string targetBranch)
        {
            // Assumption - url should look like https://yourgiteaUrl/{username}/{projectname}.git";
            string path = repositoryUri.AbsolutePath;
            System.Collections.Generic.List<string> pathParts = path.Split('/')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (pathParts.Count < 2)
            {
                throw new NuKeeperException(
                    $"The provided uri was is not in the correct format. Provided {repositoryUri} and format should be {UrlPattern}");
            }

            string repoOwner = pathParts[^2];
            string repoName = pathParts[^1].Replace(".git", string.Empty);

            // [ Base URL: /api/v1 ] https://try.gitea.io/api/swagger#
            Uri baseAddress = GetBaseAddress(repositoryUri);
            Uri apiUri = new(baseAddress, ApiBaseAdress);

            return InternalCreateRepositorySettings(apiUri, repositoryUri, repoName, repoOwner, targetBranch == null ? null : new RemoteInfo { BranchName = targetBranch });
        }

        private async Task<RepositorySettings> CreateSettingsFromLocal(Uri repositoryUri, string targetBranch)
        {
            RemoteInfo remoteInfo = new();

            Uri localFolder = repositoryUri;
            if (await _gitDriver.IsGitRepo(repositoryUri))
            {
                // Check the origin remotes
                GitRemote origin = (await _gitDriver.GetRemotes(repositoryUri)).FirstOrDefault();

                if (origin != null)
                {
                    Uri repo = await _gitDriver.DiscoverRepo(repositoryUri); // Set to the folder, because we found a remote git repository
                    if (repo != null && (repo.Segments.Last() == @".git/"))
                    {
                        string[] newSegments = repo.Segments.Take(repo.Segments.Length - 1).ToArray();
                        newSegments[^1] =
                            newSegments[^1].TrimEnd('/');
                        UriBuilder ub = new(repo)
                        {
                            Path = string.Concat(newSegments)
                        };
                        //ub.Query=string.Empty;  //maybe?
                        repo = ub.Uri;
                    }

                    remoteInfo.LocalRepositoryUri = repo;
                    repositoryUri = origin.Url;
                    remoteInfo.BranchName = targetBranch ?? await _gitDriver.GetCurrentHead(remoteInfo.LocalRepositoryUri);
                    remoteInfo.RemoteName = origin.Name;
                    remoteInfo.WorkingFolder = localFolder;
                }
            }
            else
            {
                throw new NuKeeperException("No git repository found");
            }

            RepositorySettings remoteSettings = await CreateSettingsFromRemote(repositoryUri, targetBranch);


            return await InternalCreateRepositorySettings(remoteSettings.ApiUri, remoteSettings.RepositoryUri, remoteSettings.RepositoryName, remoteSettings.RepositoryOwner, remoteInfo);
        }

        private static Task<RepositorySettings> InternalCreateRepositorySettings(Uri apiUri, Uri repositoryUri, string repoName, string repoOwner, RemoteInfo remoteInfo = null)
        {
            return Task.FromResult(new RepositorySettings
            {
                ApiUri = apiUri,
                RepositoryUri = repositoryUri,
                RepositoryName = repoName,
                RepositoryOwner = repoOwner,
                RemoteInfo = remoteInfo
            });
        }

        private static Uri GetBaseAddress(Uri repoUri)
        {
            string[] newSegments = repoUri.Segments.Take(repoUri.Segments.Length - 2).ToArray();
            UriBuilder ub = new(repoUri)
            {
                Path = string.Concat(newSegments)
            };

            return ub.Uri;
        }
    }
}
