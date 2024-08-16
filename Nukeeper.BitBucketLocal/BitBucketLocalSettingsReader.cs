using NuKeeper.Abstractions;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace NuKeeper.BitBucketLocal
{
    public class BitBucketLocalSettingsReader : ISettingsReader
    {
        private readonly IEnvironmentVariablesProvider _environmentVariablesProvider;

        public BitBucketLocalSettingsReader(IEnvironmentVariablesProvider environmentVariablesProvider)
        {
            _environmentVariablesProvider = environmentVariablesProvider;
        }

        public Platform Platform { get; } = Platform.BitbucketLocal;

        private string Username { get; set; }

        public Task<bool> CanRead(Uri repositoryUri)
        {
            return Task.FromResult(repositoryUri?.Host.Contains("bitbucket", StringComparison.OrdinalIgnoreCase) == true &&
                   repositoryUri.Host.Contains("bitbucket.org", StringComparison.OrdinalIgnoreCase) == false);
        }

        public Task<RepositorySettings> RepositorySettings(Uri repositoryUri, bool setAutoMerge, string targetBranch = null)
        {
            if (repositoryUri == null)
            {
                return Task.FromResult<RepositorySettings>(null);
            }

            string path = repositoryUri.AbsolutePath;
            System.Collections.Generic.List<string> pathParts = path.Split('/')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            Username = Concat.FirstValue(repositoryUri.UserInfo, _environmentVariablesProvider.GetUserName());

            if (pathParts.Count < 2)
            {
                return Task.FromResult<RepositorySettings>(null);
            }

            string repoName = pathParts[^1].ToLower(CultureInfo.CurrentCulture).Replace(".git", string.Empty);
            string project = pathParts[^2];

            return Task.FromResult(new RepositorySettings
            {
                ApiUri = new Uri($"{repositoryUri.Scheme}://{repositoryUri.Authority}"),
                RepositoryUri = repositoryUri,
                RepositoryName = repoName,
                RepositoryOwner = project,
                RemoteInfo = targetBranch != null ? new RemoteInfo { BranchName = targetBranch } : null
            });
        }

        public void UpdateCollaborationPlatformSettings(CollaborationPlatformSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            settings.Username = Concat.FirstValue(Username, _environmentVariablesProvider.GetUserName());

            string envToken = _environmentVariablesProvider.GetEnvironmentVariable("NuKeeper_bitbucketlocal_token");
            settings.Token = Concat.FirstValue(envToken, settings.Token);
            settings.ForkMode ??= ForkMode.SingleRepositoryOnly;
        }
    }
}


