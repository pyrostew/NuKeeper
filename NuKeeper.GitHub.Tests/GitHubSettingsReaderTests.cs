using NSubstitute;

using NuKeeper.Abstractions;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Tests;

using NUnit.Framework;

using System;
using System.Threading.Tasks;

namespace NuKeeper.GitHub.Tests
{
    [TestFixture]
    public class GitHubSettingsReaderTests
    {
        private GitHubSettingsReader _gitHubSettingsReader;
        private IEnvironmentVariablesProvider _environmentVariablesProvider;

        [SetUp]
        public void Setup()
        {
            _environmentVariablesProvider = Substitute.For<IEnvironmentVariablesProvider>();
            _gitHubSettingsReader = new GitHubSettingsReader(new MockedGitDiscoveryDriver(), _environmentVariablesProvider);
        }

        [Test]
        public void ReturnsCorrectPlatform()
        {
            Platform platform = _gitHubSettingsReader.Platform;
            Assert.That(platform, Is.Not.Null);
            Assert.That(platform, Is.EqualTo(Platform.GitHub));
        }

        [Test]
        public void UpdateSettings_UpdatesSettings()
        {
            CollaborationPlatformSettings settings = new()
            {
                Token = "accessToken",
                BaseApiUrl = new Uri("https://github.custom.com/")
            };
            _gitHubSettingsReader.UpdateCollaborationPlatformSettings(settings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.BaseApiUrl, Is.EqualTo(new Uri("https://github.custom.com/")));
            Assert.That(settings.Token, Is.EqualTo("accessToken"));
            Assert.That(settings.ForkMode, Is.EqualTo(ForkMode.PreferFork));
        }

        [Test]
        public void AuthSettings_GetsCorrectSettingsFromEnvironment()
        {
            _ = _environmentVariablesProvider.GetEnvironmentVariable("NuKeeper_github_token").Returns("envToken");

            CollaborationPlatformSettings settings = new()
            {
                Token = "accessToken",
            };

            _gitHubSettingsReader.UpdateCollaborationPlatformSettings(settings);

            Assert.That(settings.Token, Is.EqualTo("envToken"));
        }

        [TestCase(null)]
        [TestCase("htps://missingt.com")]
        public async Task InvalidUrlReturnsNull(string value)
        {
            Uri testUri = value == null ? null : new Uri(value);
            bool canRead = await _gitHubSettingsReader.CanRead(testUri);

            Assert.That(canRead, Is.False);
        }

        [Test]
        public async Task RepositorySettings_GetsCorrectSettings()
        {
            RepositorySettings settings = await _gitHubSettingsReader.RepositorySettings(new Uri("https://github.com/owner/reponame.git"), true);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.RepositoryUri, Is.EqualTo(new Uri("https://github.com/owner/reponame.git")));
            Assert.That(settings.RepositoryName, Is.EqualTo("reponame"));
            Assert.That(settings.RepositoryOwner, Is.EqualTo("owner"));
            Assert.That(settings.SetAutoMerge, Is.EqualTo(false));
        }

        [Test]
        public async Task RepositorySettings_GetsCorrectSettingsWithTargetBranch()
        {
            RepositorySettings settings =
                await _gitHubSettingsReader.RepositorySettings(new Uri("https://github.com/owner/reponame.git"), true, "Feature1");

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.RepositoryUri, Is.EqualTo(new Uri("https://github.com/owner/reponame.git")));
            Assert.That(settings.RepositoryName, Is.EqualTo("reponame"));
            Assert.That(settings.RepositoryOwner, Is.EqualTo("owner"));
            Assert.That(settings.RemoteInfo, Is.Not.Null);
            Assert.That(settings.RemoteInfo.BranchName, Is.EqualTo("Feature1"));
            Assert.That(settings.SetAutoMerge, Is.False);
        }

        [TestCase(null)]
        [TestCase("https://github.com/owner/badpart/reponame.git")]
        public void RepositorySettings_InvalidUrlReturnsNull(string value)
        {
            Uri testUri = value == null ? null : new Uri(value);
            _ = Assert.ThrowsAsync<NuKeeperException>(() => _gitHubSettingsReader.RepositorySettings(testUri, true));
        }
    }
}
