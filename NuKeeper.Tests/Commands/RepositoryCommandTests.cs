using NSubstitute;

using NuKeeper.Abstractions.CollaborationModels;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Git;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.Output;
using NuKeeper.BitBucketLocal;
using NuKeeper.Collaboration;
using NuKeeper.Commands;
using NuKeeper.Engine;
using NuKeeper.GitHub;
using NuKeeper.Inspection.Files;
using NuKeeper.Inspection.Logging;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace NuKeeper.Tests.Commands
{
    [TestFixture]
    public class RepositoryCommandTests
    {
        private IEnvironmentVariablesProvider _environmentVariablesProvider;

        public static async Task<(SettingsContainer settingsContainer, CollaborationPlatformSettings platformSettings)> CaptureSettings(
            FileSettings settingsIn,
            bool addLabels = false,
            int? maxPackageUpdates = null,
            int? maxOpenPullRequests = null
        )
        {
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();
            IEnvironmentVariablesProvider environmentVariablesProvider = Substitute.For<IEnvironmentVariablesProvider>();
            _ = fileSettings.GetSettings().Returns(settingsIn);

            GitHubSettingsReader settingReader = new(new MockedGitDiscoveryDriver(), environmentVariablesProvider);
            List<ISettingsReader> settingsReaders = [settingReader];
            ICollaborationFactory collaborationFactory = GetCollaborationFactory(environmentVariablesProvider, settingsReaders);

            SettingsContainer settingsOut = null;
            ICollaborationEngine engine = Substitute.For<ICollaborationEngine>();
            _ = await engine.Run(Arg.Do<SettingsContainer>(x => settingsOut = x));

            RepositoryCommand command = new(engine, logger, fileSettings, collaborationFactory, settingsReaders)
            {
                PersonalAccessToken = "testToken",
                RepositoryUri = "http://github.com/test/test"
            };

            if (addLabels)
            {
                command.Label = ["runLabel1", "runLabel2"];
            }

            command.MaxPackageUpdates = maxPackageUpdates;
            command.MaxOpenPullRequests = maxOpenPullRequests;

            _ = await command.OnExecute();

            return (settingsOut, collaborationFactory.Settings);
        }

        [Test]
        public async Task EmptyFileResultsInDefaultSettings()
        {
            FileSettings fileSettings = FileSettings.Empty();

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Not.Null);

            Assert.That(settings.PackageFilters, Is.Not.Null);
            Assert.That(settings.PackageFilters.MinimumAge, Is.EqualTo(TimeSpan.FromDays(7)));
            Assert.That(settings.PackageFilters.Excludes, Is.Null);
            Assert.That(settings.PackageFilters.Includes, Is.Null);
            Assert.That(settings.PackageFilters.MaxPackageUpdates, Is.EqualTo(3));

            Assert.That(settings.UserSettings, Is.Not.Null);
            Assert.That(settings.UserSettings.AllowedChange, Is.EqualTo(VersionChange.Major));
            Assert.That(settings.UserSettings.NuGetSources, Is.Null);
            Assert.That(settings.UserSettings.OutputDestination, Is.EqualTo(OutputDestination.Console));
            Assert.That(settings.UserSettings.OutputFormat, Is.EqualTo(OutputFormat.Text));
            Assert.That(settings.UserSettings.MaxRepositoriesChanged, Is.EqualTo(1));
            Assert.That(settings.UserSettings.ConsolidateUpdatesInSinglePullRequest, Is.False);

            Assert.That(settings.BranchSettings, Is.Not.Null);
            Assert.That(settings.BranchSettings.BranchNameTemplate, Is.Null);
            Assert.That(settings.BranchSettings.DeleteBranchAfterMerge, Is.EqualTo(true));

            Assert.That(settings.SourceControlServerSettings.IncludeRepos, Is.Null);
            Assert.That(settings.SourceControlServerSettings.ExcludeRepos, Is.Null);
        }

        [Test]
        public async Task LabelsOnCommandLineWillReplaceFileLabels()
        {
            FileSettings fileSettings = new()
            {
                Label = ["testLabel"]
            };

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings, true);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings.Labels, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings.Labels, Has.Count.EqualTo(2));
            Assert.That(settings.SourceControlServerSettings.Labels, Does.Contain("runLabel1"));
            Assert.That(settings.SourceControlServerSettings.Labels, Does.Contain("runLabel2"));
            Assert.That(settings.SourceControlServerSettings.Labels, Does.Not.Contain("testLabel"));
        }

        [Test]
        public async Task MaxPackageUpdatesFromCommandLineOverridesFiles()
        {
            FileSettings fileSettings = new()
            {
                MaxPackageUpdates = 42
            };

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings, false, 101);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.PackageFilters, Is.Not.Null);
            Assert.That(settings.PackageFilters.MaxPackageUpdates, Is.EqualTo(101));
        }

        [Test]
        public async Task MaxOpenPullRequestsFromCommandLineOverridesFiles()
        {
            FileSettings fileSettings = new()
            {
                MaxOpenPullRequests = 10
            };

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings, false, null, 15);

            Assert.That(settings.UserSettings.MaxOpenPullRequests, Is.EqualTo(15));
        }

        [SetUp]
        public void Setup()
        {
            _environmentVariablesProvider = Substitute.For<IEnvironmentVariablesProvider>();
        }

        [Test]
        public async Task ShouldCallEngineAndNotSucceedWithoutParams()
        {
            ICollaborationEngine engine = Substitute.For<ICollaborationEngine>();
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();
            _ = fileSettings.GetSettings().Returns(FileSettings.Empty());

            GitHubSettingsReader settingReader = new(new MockedGitDiscoveryDriver(), _environmentVariablesProvider);
            List<ISettingsReader> settingsReaders = [settingReader];
            ICollaborationFactory collaborationFactory = GetCollaborationFactory(_environmentVariablesProvider, settingsReaders);

            RepositoryCommand command = new(engine, logger, fileSettings, collaborationFactory, settingsReaders);

            int status = await command.OnExecute();

            Assert.That(status, Is.EqualTo(-1));
            _ = await engine
                .DidNotReceive()
                .Run(Arg.Any<SettingsContainer>());
        }

        [Test]
        public async Task ShouldCallEngineAndSucceedWithRequiredGithubParams()
        {
            ICollaborationEngine engine = Substitute.For<ICollaborationEngine>();
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();
            _ = fileSettings.GetSettings().Returns(FileSettings.Empty());

            GitHubSettingsReader settingReader = new(new MockedGitDiscoveryDriver(), _environmentVariablesProvider);
            List<ISettingsReader> settingsReaders = [settingReader];
            ICollaborationFactory collaborationFactory = GetCollaborationFactory(_environmentVariablesProvider, settingsReaders);

            RepositoryCommand command = new(engine, logger, fileSettings, collaborationFactory, settingsReaders)
            {
                PersonalAccessToken = "abc",
                RepositoryUri = "http://github.com/abc/abc"
            };

            int status = await command.OnExecute();

            Assert.That(status, Is.EqualTo(0));
            _ = await engine
                .Received(1)
                .Run(Arg.Any<SettingsContainer>());
        }

        [Test]
        public async Task ShouldInitialiseCollaborationFactory()
        {
            ICollaborationEngine engine = Substitute.For<ICollaborationEngine>();
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();
            _ = fileSettings.GetSettings().Returns(FileSettings.Empty());

            GitHubSettingsReader settingReader = new(new MockedGitDiscoveryDriver(), _environmentVariablesProvider);
            List<ISettingsReader> settingsReaders = [settingReader];
            ICollaborationFactory collaborationFactory = Substitute.For<ICollaborationFactory>();
            _ = collaborationFactory.Settings.Returns(new CollaborationPlatformSettings());

            RepositoryCommand command = new(engine, logger, fileSettings, collaborationFactory, settingsReaders)
            {
                PersonalAccessToken = "abc",
                RepositoryUri = "http://github.com/abc/abc",
                ForkMode = ForkMode.PreferSingleRepository
            };

            _ = await command.OnExecute();

            _ = await collaborationFactory
                .Received(1)
                .Initialise(
                    Arg.Is(new Uri("https://api.github.com")),
                    Arg.Is("abc"),
                    Arg.Is<ForkMode?>(ForkMode.PreferSingleRepository),
                    Arg.Is((Platform?)null));
        }

        [Test]
        public async Task ShouldInitialiseForkModeFromFile()
        {
            ICollaborationEngine engine = Substitute.For<ICollaborationEngine>();
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();
            _ = fileSettings.GetSettings().Returns(
                new FileSettings
                {
                    ForkMode = ForkMode.PreferFork
                });

            GitHubSettingsReader settingReader = new(new MockedGitDiscoveryDriver(), _environmentVariablesProvider);
            List<ISettingsReader> settingsReaders = [settingReader];
            ICollaborationFactory collaborationFactory = Substitute.For<ICollaborationFactory>();
            _ = collaborationFactory.Settings.Returns(new CollaborationPlatformSettings());

            RepositoryCommand command = new(engine, logger, fileSettings, collaborationFactory, settingsReaders)
            {
                PersonalAccessToken = "abc",
                RepositoryUri = "http://github.com/abc/abc",
                ForkMode = null
            };

            _ = await command.OnExecute();

            _ = await collaborationFactory
                .Received(1)
                .Initialise(
                    Arg.Is(new Uri("https://api.github.com")),
                    Arg.Is("abc"),
                    Arg.Is<ForkMode?>(ForkMode.PreferFork),
                    Arg.Is((Platform?)null));
        }

        [Test]
        public async Task ShouldInitialisePlatformFromFile()
        {
            ICollaborationEngine engine = Substitute.For<ICollaborationEngine>();
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();
            _ = fileSettings.GetSettings().Returns(
                new FileSettings
                {
                    Platform = Platform.BitbucketLocal
                });

            GitHubSettingsReader settingReader = new(new MockedGitDiscoveryDriver(), _environmentVariablesProvider);
            List<ISettingsReader> settingsReaders = [settingReader];
            ICollaborationFactory collaborationFactory = Substitute.For<ICollaborationFactory>();
            _ = collaborationFactory.Settings.Returns(new CollaborationPlatformSettings());

            RepositoryCommand command = new(engine, logger, fileSettings, collaborationFactory, settingsReaders)
            {
                PersonalAccessToken = "abc",
                RepositoryUri = "http://github.com/abc/abc",
                ForkMode = null
            };

            _ = await command.OnExecute();

            _ = await collaborationFactory
                .Received(1)
                .Initialise(
                    Arg.Is(new Uri("https://api.github.com")),
                    Arg.Is("abc"),
                    Arg.Is((ForkMode?)null),
                    Arg.Is((Platform?)Platform.BitbucketLocal));
        }

        [TestCase(Platform.BitbucketLocal, "https://myRepo.ch/")]
        [TestCase(Platform.GitHub, "https://api.github.com")]
        public async Task ShouldInitialisePlatformFromParameter(Platform platform, string expectedApi)
        {
            ICollaborationEngine engine = Substitute.For<ICollaborationEngine>();
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();
            _ = fileSettings.GetSettings().Returns(new FileSettings());

            GitHubSettingsReader gitHubSettingReader = new(new MockedGitDiscoveryDriver(), _environmentVariablesProvider);
            BitBucketLocalSettingsReader bitbucketLocalSettingReader = new(_environmentVariablesProvider);
            List<ISettingsReader> settingsReaders = [gitHubSettingReader, bitbucketLocalSettingReader];
            ICollaborationFactory collaborationFactory = Substitute.For<ICollaborationFactory>();
            _ = collaborationFactory.Settings.Returns(new CollaborationPlatformSettings());
            _ = collaborationFactory.Initialise(default, default, default, default).ReturnsForAnyArgs(ValidationResult.Success);

            RepositoryCommand command = new(engine, logger, fileSettings, collaborationFactory, settingsReaders)
            {
                Platform = platform,
                RepositoryUri = "https://myRepo.ch/abc/abc" // Repo Uri does not contain any information about the platform.
            };

            _ = await command.OnExecute();

            _ = await collaborationFactory
                .Received(1)
                .Initialise(
                    Arg.Is(new Uri(expectedApi)), // Is populated by the settings reader. Thus, can be used to check if the correct one was selected.
                    Arg.Is((string)null),
                    Arg.Is((ForkMode?)null),
                    Arg.Is((Platform?)platform));
        }

        [Test]
        public async Task ShouldPopulateSourceControlServerSettings()
        {
            FileSettings fileSettings = FileSettings.Empty();

            (SettingsContainer settings, CollaborationPlatformSettings platformSettings) = await CaptureSettings(fileSettings);

            Assert.That(platformSettings, Is.Not.Null);
            Assert.That(platformSettings.Token, Is.Not.Null);
            Assert.That(platformSettings.Token, Is.EqualTo("testToken"));

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings.Scope, Is.EqualTo(ServerScope.Repository));
            Assert.That(settings.SourceControlServerSettings.OrganisationName, Is.Null);
        }

        [Test]
        public async Task UseCustomCheckoutDirectoryIfParameterIsProvidedForRemote()
        {
            Uri testUri = new("https://github.com");

            ICollaborationFactory collaborationFactorySubstitute = Substitute.For<ICollaborationFactory>();
            _ = collaborationFactorySubstitute.ForkFinder.FindPushFork(Arg.Any<string>(), Arg.Any<ForkData>()).Returns(Task.FromResult(new ForkData(testUri, "nukeeper", "nukeeper")));
            IFolderFactory folderFactorySubstitute = Substitute.For<IFolderFactory>();
            _ = folderFactorySubstitute.FolderFromPath(Arg.Any<string>())
                .Returns(ci => new Folder(Substitute.For<INuKeeperLogger>(), new System.IO.DirectoryInfo(ci.Arg<string>())));

            IRepositoryUpdater updater = Substitute.For<IRepositoryUpdater>();
            GitRepositoryEngine gitEngine = new(updater, collaborationFactorySubstitute, folderFactorySubstitute,
                Substitute.For<INuKeeperLogger>(), Substitute.For<IRepositoryFilter>(), Substitute.For<NuGet.Common.ILogger>());

            _ = await gitEngine.Run(new RepositorySettings
            {
                RepositoryUri = testUri,
                RepositoryOwner = "nukeeper",
                RepositoryName = "nukeeper"
            }, new GitUsernamePasswordCredentials()
            {
                Password = "..",
                Username = "nukeeper"
            }, new SettingsContainer()
            {
                SourceControlServerSettings = new SourceControlServerSettings()
                {
                    Scope = ServerScope.Repository
                },
                UserSettings = new UserSettings()
                {
                    Directory = "testdirectory"
                }
            }, null);

            _ = await updater.Received().Run(Arg.Any<IGitDriver>(),
                Arg.Any<RepositoryData>(),
                Arg.Is<SettingsContainer>(c => c.WorkingFolder.FullPath.EndsWith("testdirectory", StringComparison.Ordinal)));
        }

        [Test]
        public async Task UseCustomTargetBranchIfParameterIsProvided()
        {
            Uri testUri = new("https://github.com");

            ICollaborationFactory collaborationFactorySubstitute = Substitute.For<ICollaborationFactory>();
            _ = collaborationFactorySubstitute.ForkFinder.FindPushFork(Arg.Any<string>(), Arg.Any<ForkData>()).Returns(Task.FromResult(new ForkData(testUri, "nukeeper", "nukeeper")));

            IRepositoryUpdater updater = Substitute.For<IRepositoryUpdater>();
            GitRepositoryEngine gitEngine = new(updater, collaborationFactorySubstitute, Substitute.For<IFolderFactory>(),
                Substitute.For<INuKeeperLogger>(), Substitute.For<IRepositoryFilter>(), Substitute.For<NuGet.Common.ILogger>());

            _ = await gitEngine.Run(new RepositorySettings
            {
                RepositoryUri = testUri,
                RemoteInfo = new RemoteInfo()
                {
                    BranchName = "custombranch",
                },
                RepositoryOwner = "nukeeper",
                RepositoryName = "nukeeper"
            }, new GitUsernamePasswordCredentials()
            {
                Password = "..",
                Username = "nukeeper"
            }, new SettingsContainer()
            {
                SourceControlServerSettings = new SourceControlServerSettings()
                {
                    Scope = ServerScope.Repository
                }
            }, null);

            _ = await updater.Received().Run(Arg.Any<IGitDriver>(),
                Arg.Is<RepositoryData>(r => r.DefaultBranch == "custombranch"), Arg.Any<SettingsContainer>());
        }

        [Test]
        public async Task UseCustomTargetBranchIfParameterIsProvidedForLocal()
        {
            Uri testUri = new("https://github.com");

            ICollaborationFactory collaborationFactorySubstitute = Substitute.For<ICollaborationFactory>();
            _ = collaborationFactorySubstitute.ForkFinder.FindPushFork(Arg.Any<string>(), Arg.Any<ForkData>()).Returns(Task.FromResult(new ForkData(testUri, "nukeeper", "nukeeper")));

            IRepositoryUpdater updater = Substitute.For<IRepositoryUpdater>();
            GitRepositoryEngine gitEngine = new(updater, collaborationFactorySubstitute, Substitute.For<IFolderFactory>(),
                Substitute.For<INuKeeperLogger>(), Substitute.For<IRepositoryFilter>(), Substitute.For<NuGet.Common.ILogger>());

            _ = await gitEngine.Run(new RepositorySettings
            {
                RepositoryUri = testUri,
                RemoteInfo = new RemoteInfo()
                {
                    LocalRepositoryUri = testUri,
                    BranchName = "custombranch",
                    WorkingFolder = new Uri(Assembly.GetExecutingAssembly().Location),
                    RemoteName = "github"
                },
                RepositoryOwner = "nukeeper",
                RepositoryName = "nukeeper"
            }, new GitUsernamePasswordCredentials()
            {
                Password = "..",
                Username = "nukeeper"
            }, new SettingsContainer()
            {
                SourceControlServerSettings = new SourceControlServerSettings()
                {
                    Scope = ServerScope.Repository
                }
            }, null);

            _ = await updater.Received().Run(Arg.Any<IGitDriver>(),
                Arg.Is<RepositoryData>(r => r.DefaultBranch == "custombranch"), Arg.Any<SettingsContainer>());
        }

        [Test]
        public async Task WillNotReadMaxRepoFromFile()
        {
            FileSettings fileSettings = new()
            {
                MaxRepo = 42
            };

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.PackageFilters, Is.Not.Null);
            Assert.That(settings.UserSettings.MaxRepositoriesChanged, Is.EqualTo(1));
        }

        [Test]
        public async Task WillReadApiFromFile()
        {
            FileSettings fileSettings = new()
            {
                Api = "http://github.contoso.com/"
            };

            (SettingsContainer _, CollaborationPlatformSettings platformSettings) = await CaptureSettings(fileSettings);

            Assert.That(platformSettings, Is.Not.Null);
            Assert.That(platformSettings.BaseApiUrl, Is.Not.Null);
            Assert.That(platformSettings.BaseApiUrl, Is.EqualTo(new Uri("http://github.contoso.com/")));
        }

        [Test]
        public async Task WillReadBranchNamePrefixFromFile()
        {
            string testTemplate = "nukeeper/MyBranch";

            FileSettings fileSettings = new()
            {
                BranchNameTemplate = testTemplate
            };

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.BranchSettings, Is.Not.Null);
            Assert.That(settings.BranchSettings.BranchNameTemplate, Is.EqualTo(testTemplate));
        }

        [Test]
        public async Task WillReadConsolidateFromFile()
        {
            FileSettings fileSettings = new()
            {
                Consolidate = true
            };

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.UserSettings.ConsolidateUpdatesInSinglePullRequest, Is.True);
        }

        [Test]
        public async Task WillReadLabelFromFile()
        {
            FileSettings fileSettings = new()
            {
                Label = ["testLabel"]
            };

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings.Labels, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings.Labels, Has.Count.EqualTo(1));
            Assert.That(settings.SourceControlServerSettings.Labels, Does.Contain("testLabel"));
        }

        [Test]
        public async Task WillReadMaxPackageUpdatesFromFile()
        {
            FileSettings fileSettings = new()
            {
                MaxPackageUpdates = 42
            };

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.PackageFilters, Is.Not.Null);
            Assert.That(settings.PackageFilters.MaxPackageUpdates, Is.EqualTo(42));
        }

        [Test]
        public async Task WillReadMaxOpenPullRequestsFromFile()
        {
            FileSettings fileSettings = new()
            {
                MaxOpenPullRequests = 202
            };

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings);

            Assert.That(settings.UserSettings.MaxOpenPullRequests, Is.EqualTo(202));
        }

        [Test]
        public async Task MaxOpenPullRequestsIsOneIfConsolidatedIsTrue()
        {
            FileSettings fileSettings = new()
            {
                Consolidate = true,
                MaxPackageUpdates = 20
            };

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings);

            Assert.That(settings.UserSettings.MaxOpenPullRequests, Is.EqualTo(1));
        }

        [Test]
        public async Task MaxOpenPullRequestsIsMaxPackageUpdatesIfConsolidatedIsFalse()
        {
            FileSettings fileSettings = new()
            {
                Consolidate = false,
                MaxPackageUpdates = 20
            };

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings);

            Assert.That(settings.UserSettings.MaxOpenPullRequests, Is.EqualTo(20));
        }

        private static ICollaborationFactory GetCollaborationFactory(IEnvironmentVariablesProvider environmentVariablesProvider,
            IEnumerable<ISettingsReader> settingReaders = null)
        {
            return new CollaborationFactory(
                settingReaders ?? new ISettingsReader[] { new GitHubSettingsReader(new MockedGitDiscoveryDriver(), environmentVariablesProvider) },
                Substitute.For<INuKeeperLogger>(),
                null
            );
        }
    }
}
