using NuKeeper.Abstractions;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;

using System;
using System.Threading.Tasks;

namespace NuKeeper.AzureDevOps
{
    public abstract class BaseSettingsReader : ISettingsReader
    {
        private readonly IEnvironmentVariablesProvider _environmentVariablesProvider;

        public BaseSettingsReader(IEnvironmentVariablesProvider environmentVariablesProvider)
        {
            _environmentVariablesProvider = environmentVariablesProvider;
        }

        public Platform Platform => Platform.AzureDevOps;

        public abstract Task<bool> CanRead(Uri repositoryUri);

        public void UpdateCollaborationPlatformSettings(CollaborationPlatformSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            string envToken = _environmentVariablesProvider.GetEnvironmentVariable("NuKeeper_azure_devops_token");

            settings.Token = Concat.FirstValue(envToken, settings.Token);
            settings.ForkMode ??= ForkMode.SingleRepositoryOnly;
        }

        public abstract Task<RepositorySettings> RepositorySettings(Uri repositoryUri, bool setAutoMerge, string targetBranch = null);
    }
}
