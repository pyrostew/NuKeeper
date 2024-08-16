using NuKeeper.Abstractions;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Formats;
using NuKeeper.Abstractions.Git;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Engine;
using NuKeeper.Inspection.Files;

using System;
using System.Threading.Tasks;

namespace NuKeeper.Collaboration
{
    public class CollaborationEngine : ICollaborationEngine
    {
        private readonly ICollaborationFactory _collaborationFactory;
        private readonly IGitRepositoryEngine _repositoryEngine;
        private readonly IFolderFactory _folderFactory;
        private readonly INuKeeperLogger _logger;

        public CollaborationEngine(
            ICollaborationFactory collaborationFactory,
            IGitRepositoryEngine repositoryEngine,
            IFolderFactory folderFactory,
            INuKeeperLogger logger)
        {
            _collaborationFactory = collaborationFactory;
            _repositoryEngine = repositoryEngine;
            _folderFactory = folderFactory;
            _logger = logger;
        }

        public async Task<int> Run(SettingsContainer settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _logger.Detailed($"{Now()}: Started");
            _folderFactory.DeleteExistingTempDirs();

            Abstractions.CollaborationModels.User user = await _collaborationFactory.CollaborationPlatform.GetCurrentUser();
            GitUsernamePasswordCredentials credentials = new()
            {
                Username = user.Login,
                Password = _collaborationFactory.Settings.Token
            };

            System.Collections.Generic.IEnumerable<RepositorySettings> repositories = await _collaborationFactory.RepositoryDiscovery.GetRepositories(settings.SourceControlServerSettings);

            int reposUpdated = 0;
            (bool Happened, Exception Value) unhandledEx = (false, null);

            foreach (RepositorySettings repository in repositories)
            {
                if (reposUpdated >= settings.UserSettings.MaxRepositoriesChanged)
                {
                    _logger.Detailed($"Reached max of {reposUpdated} repositories changed");
                    break;
                }
                try
                {

                    int updatesInThisRepo = await _repositoryEngine.Run(repository,
                        credentials, settings, user);

                    if (updatesInThisRepo > 0)
                    {
                        reposUpdated++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed on repo {repository.RepositoryName}", ex);
                    SetOrUpdateUnhandledException(ref unhandledEx, ex);
                }
            }

            if (reposUpdated > 1)
            {
                _logger.Detailed($"{reposUpdated} repositories were updated");
            }

            _logger.Detailed($"Done at {Now()}");

            ThrowIfUnhandledException(unhandledEx);

            return reposUpdated;
        }

        private static string Now()
        {
            return DateFormat.AsUtcIso8601(DateTimeOffset.Now);
        }

        private static void SetOrUpdateUnhandledException(
            ref (bool Happened, Exception Value) unhandledEx,
            Exception ex
        )
        {
            unhandledEx.Happened = true;
            unhandledEx.Value = unhandledEx.Value == null ? ex : new AggregateException(unhandledEx.Value, ex);
        }

        private static void ThrowIfUnhandledException(
            (bool Happened, Exception Value) unhandledEx
        )
        {
            if (unhandledEx.Happened)
            {
                Exception exception = unhandledEx.Value;
                if (exception is AggregateException aggregateException)
                {
                    exception = aggregateException.Flatten();
                }
                throw new NuKeeperException("One or multiple repositories failed to update.", exception);
            }
        }
    }
}
