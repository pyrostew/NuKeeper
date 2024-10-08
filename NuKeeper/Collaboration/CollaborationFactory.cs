using NuKeeper.Abstractions;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Formats;
using NuKeeper.Abstractions.Logging;
using NuKeeper.AzureDevOps;
using NuKeeper.BitBucket;
using NuKeeper.BitBucketLocal;
using NuKeeper.Engine;
using NuKeeper.Gitea;
using NuKeeper.GitHub;
using NuKeeper.Gitlab;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NuKeeper.Collaboration
{
    public class CollaborationFactory : ICollaborationFactory
    {
        private readonly IEnumerable<ISettingsReader> _settingReaders;
        private readonly INuKeeperLogger _nuKeeperLogger;
        private readonly IHttpClientFactory _httpClientFactory;
        private Platform? _platform;

        public IForkFinder ForkFinder { get; private set; }

        public ICommitWorder CommitWorder { get; private set; }

        public IRepositoryDiscovery RepositoryDiscovery { get; private set; }

        public ICollaborationPlatform CollaborationPlatform { get; private set; }

        public CollaborationPlatformSettings Settings { get; }

        public CollaborationFactory(IEnumerable<ISettingsReader> settingReaders,
            INuKeeperLogger nuKeeperLogger, IHttpClientFactory httpClientFactory)
        {
            _settingReaders = settingReaders;
            _nuKeeperLogger = nuKeeperLogger;
            _httpClientFactory = httpClientFactory;
            Settings = new CollaborationPlatformSettings();
        }

        public async Task<ValidationResult> Initialise(Uri apiEndpoint, string token,
            ForkMode? forkModeFromSettings, Platform? platformFromSettings)
        {
            ISettingsReader platformSettingsReader = await FindPlatformSettingsReader(platformFromSettings, apiEndpoint);
            if (platformSettingsReader != null)
            {
                _platform = platformSettingsReader.Platform;
            }
            else
            {
                return ValidationResult.Failure($"Unable to find collaboration platform for uri {apiEndpoint}");
            }

            Settings.BaseApiUrl = UriFormats.EnsureTrailingSlash(apiEndpoint);
            Settings.Token = token;
            Settings.ForkMode = forkModeFromSettings;
            platformSettingsReader.UpdateCollaborationPlatformSettings(Settings);

            ValidationResult result = ValidateSettings();
            if (!result.IsSuccess)
            {
                return result;
            }

            CreateForPlatform();

            return ValidationResult.Success;
        }

        private async Task<ISettingsReader> FindPlatformSettingsReader(
            Platform? platformFromSettings, Uri apiEndpoint)
        {
            if (platformFromSettings.HasValue)
            {
                ISettingsReader reader = _settingReaders
                    .FirstOrDefault(s => s.Platform == platformFromSettings.Value);

                if (reader != null)
                {
                    _nuKeeperLogger.Normal($"Collaboration platform specified as '{reader.Platform}'");
                }

                return reader;
            }
            else
            {
                ISettingsReader reader = await _settingReaders
                    .FirstOrDefaultAsync(s => s.CanRead(apiEndpoint));

                if (reader != null)
                {
                    _nuKeeperLogger.Normal($"Matched uri '{apiEndpoint}' to collaboration platform '{reader.Platform}'");
                }

                return reader;
            }
        }

        private ValidationResult ValidateSettings()
        {
            return !Settings.BaseApiUrl.IsWellFormedOriginalString()
                || (Settings.BaseApiUrl.Scheme != "http" && Settings.BaseApiUrl.Scheme != "https")
                ? ValidationResult.Failure(
                    $"Api is not of correct format {Settings.BaseApiUrl}")
                : !Settings.ForkMode.HasValue
                ? ValidationResult.Failure("Fork Mode was not set")
                : string.IsNullOrWhiteSpace(Settings.Token)
                ? ValidationResult.Failure("Token was not set")
                : !_platform.HasValue ? ValidationResult.Failure("Platform was not set") : ValidationResult.Success;
        }

        private void CreateForPlatform()
        {
            ForkMode forkMode = Settings.ForkMode.Value;

            switch (_platform.Value)
            {
                case Platform.AzureDevOps:
                    CollaborationPlatform = new AzureDevOpsPlatform(_nuKeeperLogger, _httpClientFactory);
                    RepositoryDiscovery = new AzureDevOpsRepositoryDiscovery(_nuKeeperLogger, CollaborationPlatform, Settings.Token);
                    ForkFinder = new AzureDevOpsForkFinder(CollaborationPlatform, _nuKeeperLogger, forkMode);

                    // We go for the specific platform version of ICommitWorder
                    // here since Azure DevOps has different commit message limits compared to other platforms.
                    CommitWorder = new AzureDevOpsCommitWorder();
                    break;

                case Platform.GitHub:
                    CollaborationPlatform = new OctokitClient(_nuKeeperLogger);
                    RepositoryDiscovery = new GitHubRepositoryDiscovery(_nuKeeperLogger, CollaborationPlatform);
                    ForkFinder = new GitHubForkFinder(CollaborationPlatform, _nuKeeperLogger, forkMode);
                    CommitWorder = new DefaultCommitWorder();
                    break;

                case Platform.Bitbucket:
                    CollaborationPlatform = new BitbucketPlatform(_nuKeeperLogger, _httpClientFactory);
                    RepositoryDiscovery = new BitbucketRepositoryDiscovery(_nuKeeperLogger);
                    ForkFinder = new BitbucketForkFinder(CollaborationPlatform, _nuKeeperLogger, forkMode);
                    CommitWorder = new BitbucketCommitWorder();
                    break;

                case Platform.BitbucketLocal:
                    CollaborationPlatform = new BitBucketLocalPlatform(_nuKeeperLogger, _httpClientFactory);
                    RepositoryDiscovery = new BitbucketLocalRepositoryDiscovery(_nuKeeperLogger, CollaborationPlatform, Settings);
                    ForkFinder = new BitbucketForkFinder(CollaborationPlatform, _nuKeeperLogger, forkMode);
                    CommitWorder = new DefaultCommitWorder();
                    break;

                case Platform.GitLab:
                    CollaborationPlatform = new GitlabPlatform(_nuKeeperLogger, _httpClientFactory);
                    RepositoryDiscovery = new GitlabRepositoryDiscovery(_nuKeeperLogger, CollaborationPlatform);
                    ForkFinder = new GitlabForkFinder(CollaborationPlatform, _nuKeeperLogger, forkMode);
                    CommitWorder = new DefaultCommitWorder();
                    break;

                case Platform.Gitea:
                    CollaborationPlatform = new GiteaPlatform(_nuKeeperLogger, _httpClientFactory);
                    RepositoryDiscovery = new GiteaRepositoryDiscovery(_nuKeeperLogger, CollaborationPlatform);
                    ForkFinder = new GiteaForkFinder(CollaborationPlatform, _nuKeeperLogger, forkMode);
                    CommitWorder = new DefaultCommitWorder();
                    break;

                default:
                    throw new NuKeeperException($"Unknown platform: {_platform}");
            }

            AuthSettings auth = new(Settings.BaseApiUrl, Settings.Token, Settings.Username);
            CollaborationPlatform.Initialise(auth);

            if (ForkFinder == null ||
                RepositoryDiscovery == null ||
                CollaborationPlatform == null)
            {
                throw new NuKeeperException($"Platform {_platform} could not be initialised");
            }
        }
    }
}
