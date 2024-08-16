using NSubstitute;

using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Git;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.Output;
using NuKeeper.AzureDevOps;
using NuKeeper.Collaboration;
using NuKeeper.Commands;
using NuKeeper.GitHub;
using NuKeeper.Inspection.Logging;

using NUnit.Framework;

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace NuKeeper.Tests.Commands
{
    [TestFixture]
    public class OrganisationCommandTests
    {
        private static CollaborationFactory GetCollaborationFactory(Func<IGitDiscoveryDriver, IEnvironmentVariablesProvider, ISettingsReader> createSettingsReader)
        {
            IEnvironmentVariablesProvider environmentVariablesProvider = Substitute.For<IEnvironmentVariablesProvider>();

            IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
            _ = httpClientFactory.CreateClient().Returns(new HttpClient());

            return new CollaborationFactory(
                new ISettingsReader[] { createSettingsReader(new MockedGitDiscoveryDriver(), environmentVariablesProvider) },
                Substitute.For<INuKeeperLogger>(),
                httpClientFactory
            );
        }

        [Test]
        public async Task ShouldCallEngineAndNotSucceedWithoutParams()
        {
            ICollaborationEngine engine = Substitute.For<ICollaborationEngine>();
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();
            _ = fileSettings.GetSettings().Returns(FileSettings.Empty());
            CollaborationFactory collaborationFactory = GetCollaborationFactory((d, e) => new GitHubSettingsReader(d, e));

            OrganisationCommand command = new(engine, logger, fileSettings, collaborationFactory);

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

            CollaborationFactory collaborationFactory = GetCollaborationFactory((d, e) => new GitHubSettingsReader(d, e));

            OrganisationCommand command = new(engine, logger, fileSettings, collaborationFactory)
            {
                PersonalAccessToken = "abc",
                OrganisationName = "testOrg"
            };

            int status = await command.OnExecute();

            Assert.That(status, Is.EqualTo(0));
            _ = await engine
                .Received(1)
                .Run(Arg.Any<SettingsContainer>());
        }

        [Test]
        public async Task ShouldCallEngineAndSucceedWithRequiredAzureDevOpsParams()
        {
            ICollaborationEngine engine = Substitute.For<ICollaborationEngine>();
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();
            _ = fileSettings.GetSettings().Returns(FileSettings.Empty());

            CollaborationFactory collaborationFactory = GetCollaborationFactory((d, e) => new AzureDevOpsSettingsReader(d, e));

            OrganisationCommand command = new(engine, logger, fileSettings, collaborationFactory)
            {
                PersonalAccessToken = "abc",
                OrganisationName = "testOrg",
                ApiEndpoint = "https://dev.azure.com/org"
            };

            int status = await command.OnExecute();

            Assert.That(status, Is.EqualTo(0));
            _ = await engine
                .Received(1)
                .Run(Arg.Any<SettingsContainer>());
        }

        [Test]
        public async Task ShouldPopulateSettings()
        {
            FileSettings fileSettings = FileSettings.Empty();

            (SettingsContainer settings, CollaborationPlatformSettings platformSettings) = await CaptureSettings(fileSettings);

            Assert.That(platformSettings, Is.Not.Null);
            Assert.That(platformSettings.Token, Is.Not.Null);
            Assert.That(platformSettings.Token, Is.EqualTo("testToken"));

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings.Scope, Is.EqualTo(ServerScope.Organisation));
            Assert.That(settings.SourceControlServerSettings.Repository, Is.Null);
            Assert.That(settings.SourceControlServerSettings.OrganisationName, Is.EqualTo("testOrg"));
        }

        [Test]
        public async Task EmptyFileResultsInDefaultSettings()
        {
            FileSettings fileSettings = FileSettings.Empty();

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.PackageFilters, Is.Not.Null);
            Assert.That(settings.UserSettings, Is.Not.Null);
            Assert.That(settings.BranchSettings, Is.Not.Null);

            Assert.That(settings.PackageFilters.MinimumAge, Is.EqualTo(TimeSpan.FromDays(7)));
            Assert.That(settings.PackageFilters.Excludes, Is.Null);
            Assert.That(settings.PackageFilters.Includes, Is.Null);
            Assert.That(settings.PackageFilters.MaxPackageUpdates, Is.EqualTo(3));

            Assert.That(settings.UserSettings.AllowedChange, Is.EqualTo(VersionChange.Major));
            Assert.That(settings.UserSettings.NuGetSources, Is.Null);
            Assert.That(settings.UserSettings.OutputDestination, Is.EqualTo(OutputDestination.Console));
            Assert.That(settings.UserSettings.OutputFormat, Is.EqualTo(OutputFormat.Text));

            Assert.That(settings.BranchSettings.BranchNameTemplate, Is.Null);

            Assert.That(settings.UserSettings.MaxRepositoriesChanged, Is.EqualTo(10));

            Assert.That(settings.BranchSettings.DeleteBranchAfterMerge, Is.EqualTo(true));

            Assert.That(settings.SourceControlServerSettings.IncludeRepos, Is.Null);
            Assert.That(settings.SourceControlServerSettings.ExcludeRepos, Is.Null);
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
        public async Task WillReadRepoFiltersFromFile()
        {
            FileSettings fileSettings = new()
            {
                IncludeRepos = "foo",
                ExcludeRepos = "bar"
            };

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings.IncludeRepos, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings.ExcludeRepos, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings.IncludeRepos.ToString(), Is.EqualTo("foo"));
            Assert.That(settings.SourceControlServerSettings.ExcludeRepos.ToString(), Is.EqualTo("bar"));
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
        public async Task WillReadMaxRepoFromFile()
        {
            FileSettings fileSettings = new()
            {
                MaxRepo = 42
            };

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.PackageFilters, Is.Not.Null);
            Assert.That(settings.UserSettings.MaxRepositoriesChanged, Is.EqualTo(42));
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
        public async Task CommandLineWillOverrideIncludeRepo()
        {
            FileSettings fileSettings = new()
            {
                IncludeRepos = "foo",
                ExcludeRepos = "bar"
            };

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings, true);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings.IncludeRepos, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings.ExcludeRepos, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings.IncludeRepos.ToString(), Is.EqualTo("IncludeFromCommand"));
            Assert.That(settings.SourceControlServerSettings.ExcludeRepos.ToString(), Is.EqualTo("bar"));
        }

        [Test]
        public async Task CommandLineWillOverrideExcludeRepo()
        {
            FileSettings fileSettings = new()
            {
                IncludeRepos = "foo",
                ExcludeRepos = "bar"
            };

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings, false, true);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings.IncludeRepos, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings.ExcludeRepos, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings.IncludeRepos.ToString(), Is.EqualTo("foo"));
            Assert.That(settings.SourceControlServerSettings.ExcludeRepos.ToString(), Is.EqualTo("ExcludeFromCommand"));
        }

        [Test]
        public async Task CommandLineWillOverrideForkMode()
        {
            (SettingsContainer _, CollaborationPlatformSettings platformSettings) = await CaptureSettings(FileSettings.Empty(), false, false, null, ForkMode.PreferSingleRepository);

            Assert.That(platformSettings, Is.Not.Null);
            Assert.That(platformSettings.ForkMode, Is.Not.Null);
            Assert.That(platformSettings.ForkMode, Is.EqualTo(ForkMode.PreferSingleRepository));
        }

        [Test]
        public async Task CommandLineWillOverrideMaxRepo()
        {
            FileSettings fileSettings = new()
            {
                MaxRepo = 12,
            };

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings, false, true, 22);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.UserSettings.MaxRepositoriesChanged, Is.EqualTo(22));
        }

        public static async Task<(SettingsContainer fileSettings, CollaborationPlatformSettings platformSettings)> CaptureSettings(
            FileSettings settingsIn,
            bool addCommandRepoInclude = false,
            bool addCommandRepoExclude = false,
            int? maxRepo = null,
            ForkMode? forkMode = null)
        {
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();

            SettingsContainer settingsOut = null;
            ICollaborationEngine engine = Substitute.For<ICollaborationEngine>();
            _ = await engine.Run(Arg.Do<SettingsContainer>(x => settingsOut = x));

            _ = fileSettings.GetSettings().Returns(settingsIn);

            CollaborationFactory collaborationFactory = GetCollaborationFactory((d, e) => new GitHubSettingsReader(d, e));

            OrganisationCommand command = new(engine, logger, fileSettings, collaborationFactory)
            {
                PersonalAccessToken = "testToken",
                OrganisationName = "testOrg"
            };

            if (addCommandRepoInclude)
            {
                command.IncludeRepos = "IncludeFromCommand";
            }

            if (addCommandRepoExclude)
            {
                command.ExcludeRepos = "ExcludeFromCommand";
            }

            if (forkMode != null)
            {
                command.ForkMode = forkMode;
            }

            command.MaxRepositoriesChanged = maxRepo;

            _ = await command.OnExecute();

            return (settingsOut, collaborationFactory.Settings);
        }
    }
}
