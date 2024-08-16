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
    public class GlobalCommandTests
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

            GlobalCommand command = new(engine, logger, fileSettings, collaborationFactory);

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

            GlobalCommand command = new(engine, logger, fileSettings, collaborationFactory)
            {
                PersonalAccessToken = "testToken",
                Include = "testRepos",
                ApiEndpoint = "https://github.contoso.com"
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

            GlobalCommand command = new(engine, logger, fileSettings, collaborationFactory)
            {
                PersonalAccessToken = "testToken",
                Include = "testRepos",
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
            Assert.That(platformSettings.BaseApiUrl, Is.Not.Null);
            Assert.That(platformSettings.BaseApiUrl.ToString(), Is.EqualTo("http://github.contoso.com/"));


            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings, Is.Not.Null);
            Assert.That(settings.SourceControlServerSettings.Repository, Is.Null);
            Assert.That(settings.SourceControlServerSettings.OrganisationName, Is.Null);
        }

        [Test]
        public async Task EmptyFileResultsInRequiredParams()
        {
            FileSettings fileSettings = FileSettings.Empty();

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.UserSettings, Is.Not.Null);
            Assert.That(settings.UserSettings.MaxRepositoriesChanged, Is.EqualTo(10));

            Assert.That(settings.PackageFilters, Is.Not.Null);
            Assert.That(settings.PackageFilters.Includes, Is.Not.Null);
            Assert.That(settings.PackageFilters.Includes.ToString(), Is.EqualTo("testRepos"));

            Assert.That(settings.BranchSettings, Is.Not.Null);
        }

        [Test]
        public async Task EmptyFileResultsInDefaultSettings()
        {
            FileSettings fileSettings = FileSettings.Empty();

            (SettingsContainer settings, CollaborationPlatformSettings _) = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.PackageFilters, Is.Not.Null);
            Assert.That(settings.UserSettings, Is.Not.Null);
            Assert.That(settings.BranchSettings, Is.Not.Null);

            Assert.That(settings.PackageFilters.MinimumAge, Is.EqualTo(TimeSpan.FromDays(7)));
            Assert.That(settings.PackageFilters.Excludes, Is.Null);
            Assert.That(settings.PackageFilters.MaxPackageUpdates, Is.EqualTo(3));

            Assert.That(settings.UserSettings.AllowedChange, Is.EqualTo(VersionChange.Major));
            Assert.That(settings.UserSettings.NuGetSources, Is.Null);
            Assert.That(settings.UserSettings.OutputDestination, Is.EqualTo(OutputDestination.Console));
            Assert.That(settings.UserSettings.OutputFormat, Is.EqualTo(OutputFormat.Text));

            Assert.That(settings.BranchSettings.BranchNameTemplate, Is.Null);
            Assert.That(settings.BranchSettings.DeleteBranchAfterMerge, Is.EqualTo(true));

            Assert.That(settings.SourceControlServerSettings.Scope, Is.EqualTo(ServerScope.Global));
            Assert.That(settings.SourceControlServerSettings.IncludeRepos, Is.Null);
            Assert.That(settings.SourceControlServerSettings.ExcludeRepos, Is.Null);
        }

        [Test]
        public async Task WillReadApiFromFile()
        {
            FileSettings fileSettings = new()
            {
                Api = "http://github.fish.com/"
            };

            (SettingsContainer _, CollaborationPlatformSettings platformSettings) = await CaptureSettings(fileSettings);

            Assert.That(platformSettings, Is.Not.Null);
            Assert.That(platformSettings.BaseApiUrl, Is.Not.Null);
            Assert.That(platformSettings.BaseApiUrl, Is.EqualTo(new Uri("http://github.fish.com/")));
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

        public static async Task<(SettingsContainer settingsContainer, CollaborationPlatformSettings platformSettings)> CaptureSettings(FileSettings settingsIn)
        {
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();
            _ = fileSettings.GetSettings().Returns(settingsIn);

            CollaborationFactory collaborationFactory = GetCollaborationFactory((d, e) => new GitHubSettingsReader(d, e));

            SettingsContainer settingsOut = null;
            ICollaborationEngine engine = Substitute.For<ICollaborationEngine>();
            _ = await engine.Run(Arg.Do<SettingsContainer>(x => settingsOut = x));

            GlobalCommand command = new(engine, logger, fileSettings, collaborationFactory)
            {
                PersonalAccessToken = "testToken",
                ApiEndpoint = settingsIn.Api ?? "http://github.contoso.com/",
                Include = settingsIn.Include ?? "testRepos"
            };

            _ = await command.OnExecute();

            return (settingsOut, collaborationFactory.Settings);
        }
    }
}
