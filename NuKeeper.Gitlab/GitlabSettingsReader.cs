using NuKeeper.Abstractions;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace NuKeeper.Gitlab
{
    public class GitlabSettingsReader : ISettingsReader
    {
        private readonly IEnvironmentVariablesProvider _environmentVariablesProvider;
        private const string GitLabTokenEnvironmentVariableName = "NuKeeper_gitlab_token";
        private const string UrlPattern = "https://gitlab.com/{username}/{projectname}.git";

        public GitlabSettingsReader(IEnvironmentVariablesProvider environmentVariablesProvider)
        {
            _environmentVariablesProvider = environmentVariablesProvider;
        }

        public Platform Platform => Platform.GitLab;

        public Task<bool> CanRead(Uri repositoryUri)
        {
            return repositoryUri == null
                ? Task.FromResult(false)
                : Task.FromResult(repositoryUri.Host.Contains("gitlab", StringComparison.OrdinalIgnoreCase));
        }

        public void UpdateCollaborationPlatformSettings(CollaborationPlatformSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            string envToken = _environmentVariablesProvider.GetEnvironmentVariable(GitLabTokenEnvironmentVariableName);

            settings.Token = Concat.FirstValue(envToken, settings.Token);
        }

        public Task<RepositorySettings> RepositorySettings(Uri repositoryUri, bool setAutoMerge, string targetBranch = null)
        {
            if (repositoryUri == null)
            {
                throw new NuKeeperException(
                    $"The provided uri was is not in the correct format. Provided null and format should be {UrlPattern}");
            }

            // Assumption - url should look like https://gitlab.com/{username}/{projectname}.git";
            string path = repositoryUri.AbsolutePath;
            System.Collections.Generic.List<string> pathParts = path.Split('/')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (pathParts.Count < 2)
            {
                throw new NuKeeperException(
                    $"The provided uri was is not in the correct format. Provided {repositoryUri} and format should be {UrlPattern}");
            }

            string repoOwner = string.Join("/", pathParts.Take(pathParts.Count - 1));
            string repoName = pathParts.Last().Replace(".git", string.Empty);

            UriBuilder uriBuilder = new(repositoryUri) { Path = "/api/v4/" };

            return Task.FromResult(new RepositorySettings
            {
                ApiUri = uriBuilder.Uri,
                RepositoryUri = repositoryUri,
                RepositoryName = repoName,
                RepositoryOwner = repoOwner,
                RemoteInfo = targetBranch == null
                    ? null
                    : new RemoteInfo { BranchName = targetBranch }
            });
        }
    }
}
