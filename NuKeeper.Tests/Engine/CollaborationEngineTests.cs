using NSubstitute;

using NuKeeper.Abstractions;
using NuKeeper.Abstractions.CollaborationModels;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Git;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Collaboration;
using NuKeeper.Engine;
using NuKeeper.Inspection.Files;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NuKeeper.Tests.Engine
{
    [TestFixture]
    public class CollaborationEngineTests
    {
        private IGitRepositoryEngine _repoEngine;
        private ICollaborationFactory _collaborationFactory;
        private IFolderFactory _folderFactory;
        private INuKeeperLogger _logger;
        private List<RepositorySettings> _disoverableRepositories;

        [SetUp]
        public void Initialize()
        {
            _repoEngine = Substitute.For<IGitRepositoryEngine>();
            _collaborationFactory = Substitute.For<ICollaborationFactory>();
            _folderFactory = Substitute.For<IFolderFactory>();
            _logger = Substitute.For<INuKeeperLogger>();
            _disoverableRepositories = [];

            _ = _collaborationFactory.CollaborationPlatform.GetCurrentUser().Returns(new User("", "", ""));
            _ = _collaborationFactory.Settings.Returns(new CollaborationPlatformSettings());
            _ = _collaborationFactory
                .RepositoryDiscovery
                .GetRepositories(Arg.Any<SourceControlServerSettings>())
                .Returns(_disoverableRepositories);
        }

        [Test]
        public async Task Run_ExceptionWhenUpdatingRepository_StillTreatsOtherRepositories()
        {
            SettingsContainer settings = MakeSettings();
            RepositorySettings repoSettingsOne = MakeRepositorySettingsAndAddAsDiscoverable();
            RepositorySettings repoSettingsTwo = MakeRepositorySettingsAndAddAsDiscoverable();
            _repoEngine
                .When(
                    r => r.Run(
                        repoSettingsOne,
                        Arg.Any<GitUsernamePasswordCredentials>(),
                        Arg.Any<SettingsContainer>(),
                        Arg.Any<User>()
                    )
                )
                .Do(r => throw new Exception());
            ICollaborationEngine engine = MakeCollaborationEngine();

            try
            {
                _ = await engine.Run(settings);
            }
            catch (Exception) { }

            _ = await _repoEngine
                .Received()
                .Run(
                    repoSettingsTwo,
                    Arg.Any<GitUsernamePasswordCredentials>(),
                    Arg.Any<SettingsContainer>(),
                    Arg.Any<User>()
                );
        }

        [Test]
        public void Run_ExceptionWhenUpdatingRepository_RethrowsException()
        {
            string exceptionMessage = "Try again later";
            SettingsContainer settings = MakeSettings();
            RepositorySettings repoSettingsOne = MakeRepositorySettingsAndAddAsDiscoverable();
            RepositorySettings repoSettingsTwo = MakeRepositorySettingsAndAddAsDiscoverable();
            _repoEngine
                .When(
                    r => r.Run(
                        repoSettingsOne,
                        Arg.Any<GitUsernamePasswordCredentials>(),
                        Arg.Any<SettingsContainer>(),
                        Arg.Any<User>()
                    )
                )
                .Do(r => throw new NuKeeperException(exceptionMessage));
            ICollaborationEngine engine = MakeCollaborationEngine();

            NuKeeperException ex = Assert.ThrowsAsync<NuKeeperException>(() => engine.Run(settings));

            NuKeeperException innerEx = ex.InnerException as NuKeeperException;
            Assert.That(innerEx, Is.Not.Null);
            Assert.That(innerEx.Message, Is.EqualTo(exceptionMessage));
        }

        [Test]
        public void Run_MultipleExceptionsWhenUpdatingRepositories_AreFlattened()
        {
            SettingsContainer settings = MakeSettings();
            RepositorySettings repoSettingsOne = MakeRepositorySettingsAndAddAsDiscoverable();
            RepositorySettings repoSettingsTwo = MakeRepositorySettingsAndAddAsDiscoverable();
            RepositorySettings repoSettingsThree = MakeRepositorySettingsAndAddAsDiscoverable();
            _repoEngine
                .When(
                    r => r.Run(
                        repoSettingsOne,
                        Arg.Any<GitUsernamePasswordCredentials>(),
                        Arg.Any<SettingsContainer>(),
                        Arg.Any<User>()
                    )
                )
                .Do(r => throw new InvalidOperationException("Repo 1 failed!"));
            _repoEngine
                .When(
                    r => r.Run(
                        repoSettingsThree,
                        Arg.Any<GitUsernamePasswordCredentials>(),
                        Arg.Any<SettingsContainer>(),
                        Arg.Any<User>()
                    )
                )
                .Do(r => throw new TaskCanceledException("Repo 3 failed!"));
            ICollaborationEngine engine = MakeCollaborationEngine();

            NuKeeperException ex = Assert.ThrowsAsync<NuKeeperException>(() => engine.Run(settings));

            AggregateException aggregateEx = ex.InnerException as AggregateException;
            System.Collections.ObjectModel.ReadOnlyCollection<Exception> exceptions = aggregateEx?.InnerExceptions;
            Assert.That(aggregateEx, Is.Not.Null);
            Assert.That(exceptions, Is.Not.Null);
            Assert.That(exceptions.Count, Is.EqualTo(2));
            Assert.That(exceptions, Has.One.InstanceOf(typeof(InvalidOperationException)));
            Assert.That(exceptions, Has.One.InstanceOf(typeof(TaskCanceledException)));
        }

        [Test]
        public async Task SuccessCaseWithNoRepos()
        {
            CollaborationEngine engine = MakeCollaborationEngine(
                []);

            int count = await engine.Run(MakeSettings());

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public async Task SuccessCaseWithOneRepo()
        {
            List<RepositorySettings> oneRepo =
            [
                new RepositorySettings()
            ];
            CollaborationEngine engine = MakeCollaborationEngine(oneRepo);

            int count = await engine.Run(MakeSettings());

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public async Task SuccessCaseWithTwoRepos()
        {
            List<RepositorySettings> repos =
            [
                new RepositorySettings(),
                new RepositorySettings()
            ];
            CollaborationEngine engine = MakeCollaborationEngine(repos);

            int count = await engine.Run(MakeSettings());

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public async Task SuccessCaseWithTwoReposAndTwoPrsPerRepo()
        {
            List<RepositorySettings> repos =
            [
                new RepositorySettings(),
                new RepositorySettings()
            ];
            CollaborationEngine engine = MakeCollaborationEngine(2, repos);

            int count = await engine.Run(MakeSettings());

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public async Task CountIsNotIncrementedWhenRepoEngineFails()
        {
            List<RepositorySettings> repos =
            [
                new RepositorySettings(),
                new RepositorySettings(),
                new RepositorySettings()
            ];
            CollaborationEngine engine = MakeCollaborationEngine(0, repos);

            int count = await engine.Run(MakeSettings());

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public async Task WhenThereIsAMaxNumberOfRepos()
        {
            List<RepositorySettings> repos =
            [
                new RepositorySettings(),
                new RepositorySettings(),
                new RepositorySettings()
            ];

            CollaborationEngine engine = MakeCollaborationEngine(1, repos);

            SettingsContainer settings = new()
            {
                UserSettings = new UserSettings
                {
                    MaxRepositoriesChanged = 1
                },
                SourceControlServerSettings = MakeServerSettings()
            };

            int count = await engine.Run(settings);

            Assert.That(count, Is.EqualTo(1));
        }

        private ICollaborationEngine MakeCollaborationEngine()
        {
            return new CollaborationEngine(
                _collaborationFactory,
                _repoEngine,
                _folderFactory,
                _logger
            );
        }

        private RepositorySettings MakeRepositorySettingsAndAddAsDiscoverable()
        {
            RepositorySettings repositorySettings = new();
            _disoverableRepositories.Add(repositorySettings);
            return repositorySettings;
        }

        private static CollaborationEngine MakeCollaborationEngine(
            List<RepositorySettings> repos)
        {
            return MakeCollaborationEngine(1, repos);
        }

        private static CollaborationEngine MakeCollaborationEngine(
            int repoEngineResult,
            List<RepositorySettings> repos)
        {
            ICollaborationFactory collaborationFactory = Substitute.For<ICollaborationFactory>();
            IGitRepositoryEngine repoEngine = Substitute.For<IGitRepositoryEngine>();
            IFolderFactory folders = Substitute.For<IFolderFactory>();

            User user = new("testUser", "Testy", "testuser@test.com");
            _ = collaborationFactory.CollaborationPlatform.GetCurrentUser().Returns(user);

            _ = collaborationFactory.Settings.Returns(new CollaborationPlatformSettings());

            _ = collaborationFactory.RepositoryDiscovery.GetRepositories(Arg.Any<SourceControlServerSettings>()).Returns(repos);

            _ = repoEngine.Run(null, null, null, null).ReturnsForAnyArgs(repoEngineResult);

            CollaborationEngine engine = new(collaborationFactory, repoEngine,
                folders, Substitute.For<INuKeeperLogger>());

            return engine;
        }

        private static SettingsContainer MakeSettings()
        {
            return new SettingsContainer
            {
                UserSettings = MakeUserSettings(),
                SourceControlServerSettings = MakeServerSettings()
            };
        }

        private static SourceControlServerSettings MakeServerSettings()
        {
            return new SourceControlServerSettings
            {
                Scope = ServerScope.Repository
            };
        }

        private static UserSettings MakeUserSettings()
        {
            return new UserSettings { MaxRepositoriesChanged = int.MaxValue };
        }
    }
}
