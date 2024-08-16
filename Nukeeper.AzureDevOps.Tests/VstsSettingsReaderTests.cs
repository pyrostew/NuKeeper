using NSubstitute;

using NuKeeper.Abstractions;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.AzureDevOps;
using NuKeeper.Tests;

using NUnit.Framework;

using System;
using System.Threading.Tasks;

namespace Nukeeper.AzureDevOps.Tests
{
    [TestFixture]
    public class VstsSettingsReaderTests
    {
        private ISettingsReader _azureSettingsReader;
        private IEnvironmentVariablesProvider _environmentVariablesProvider;

        [SetUp]
        public void Setup()
        {
            _environmentVariablesProvider = Substitute.For<IEnvironmentVariablesProvider>();
            _azureSettingsReader = new VstsSettingsReader(new MockedGitDiscoveryDriver(), _environmentVariablesProvider);
        }

        [Test]
        public async Task ReturnsTrueIfCanRead()
        {
            bool canRead = await _azureSettingsReader.CanRead(new Uri("https://org.visualstudio.com"));
            Assert.That(canRead);
        }

        [Test]
        public void ReturnsCorrectPlatform()
        {
            Platform platform = _azureSettingsReader.Platform;
            Assert.That(platform == Platform.AzureDevOps);
        }

        [Test]
        public void UpdateSettings_UpdatesSettings()
        {
            CollaborationPlatformSettings settings = new()
            {
                Token = "accessToken",
                BaseApiUrl = new Uri("https://dev.azure.com/")
            };
            _azureSettingsReader.UpdateCollaborationPlatformSettings(settings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.BaseApiUrl.ToString() == "https://dev.azure.com/");
            Assert.That(settings.Token == "accessToken");
            Assert.That(settings.ForkMode == ForkMode.SingleRepositoryOnly);
        }

        [Test]
        public void AuthSettings_GetsCorrectSettingsFromEnvironment()
        {
            _ = _environmentVariablesProvider.GetEnvironmentVariable("NuKeeper_azure_devops_token").Returns("envToken");

            CollaborationPlatformSettings settings = new()
            {
                Token = "accessToken",
            };

            _azureSettingsReader.UpdateCollaborationPlatformSettings(settings);

            Assert.That(settings.Token == "envToken");
        }

        [TestCase(null)]
        [TestCase("htps://dev.azure.com")]
        public async Task InvalidUrlReturnsNull(string value)
        {
            Uri uriToTest = value == null ? null : new Uri(value);
            bool canRead = await _azureSettingsReader.CanRead(uriToTest);

            Assert.That(!canRead);
        }

        [Test]
        public async Task RepositorySettings_GetsCorrectSettings()
        {
            RepositorySettings settings = await _azureSettingsReader.RepositorySettings(new Uri("https://org.visualstudio.com/project/_git/reponame"), true);

            Assert.That(settings, Is.Not.Null);
            Assert.That("https://org.visualstudio.com/" == settings.ApiUri.ToString());
            Assert.That("https://org.visualstudio.com/project/_git/reponame/" == settings.RepositoryUri.ToString());
            Assert.That(settings.RepositoryName == "reponame");
            Assert.That(settings.RepositoryOwner == "project");
            Assert.That(settings.SetAutoMerge);
        }

        [Test]
        public async Task RepositorySettings_ReturnsNull()
        {
            RepositorySettings settings = await _azureSettingsReader.RepositorySettings(null, true);
            Assert.That(settings is null);
        }

        [Test]
        public void RepositorySettings_InvalidFormat()
        {
            _ = Assert.ThrowsAsync<NuKeeperException>(() =>
                _azureSettingsReader.RepositorySettings(
                    new Uri("https://org.visualstudio.com/project/_git/reponame/thisShouldNotBeHere/"), true));
        }
    }
}
