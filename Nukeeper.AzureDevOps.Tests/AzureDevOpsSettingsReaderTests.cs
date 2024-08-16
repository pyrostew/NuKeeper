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
    public class AzureDevOpsSettingsReaderTests
    {
        private ISettingsReader _azureSettingsReader;
        private IEnvironmentVariablesProvider _environmentVariablesProvider;

        [SetUp]
        public void Setup()
        {
            _environmentVariablesProvider = Substitute.For<IEnvironmentVariablesProvider>();
            _azureSettingsReader = new AzureDevOpsSettingsReader(new MockedGitDiscoveryDriver(), _environmentVariablesProvider);
        }

        [Test]
        public async Task ReturnsTrueIfCanRead()
        {
            bool canRead = await _azureSettingsReader.CanRead(new Uri("https://dev.azure.com/org"));
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
        [TestCase("htps://org.visualstudio.com")]
        public async Task InvalidUrlReturnsNull(string value)
        {
            Uri uriToTest = value == null ? null : new Uri(value);
            bool canRead = await _azureSettingsReader.CanRead(uriToTest);

            Assert.That(!canRead);
        }

        [Test]
        public async Task RepositorySettings_GetsCorrectSettingsOrganisation()
        {
            RepositorySettings settings = await _azureSettingsReader.RepositorySettings(new Uri("https://dev.azure.com/org/project/_git/reponame"), true, "develop");

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.ApiUri.ToString() == "https://dev.azure.com/org/");
            Assert.That(settings.RepositoryUri.ToString() == "https://dev.azure.com/org/project/_git/reponame");
            Assert.That(settings.RepositoryName == "reponame");
            Assert.That(settings.RepositoryOwner == "project");
            Assert.That(settings.SetAutoMerge);
            Assert.That(settings.RemoteInfo.BranchName == "develop");
        }

        [Test]
        public async Task RepositorySettings_GetsCorrectSettingsPrivate()
        {
            RepositorySettings settings = await _azureSettingsReader.RepositorySettings(new Uri("https://dev.azure.com/owner/_git/reponame"), true, "main");

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.ApiUri.ToString() == "https://dev.azure.com/");
            Assert.That(settings.RepositoryUri.ToString() == "https://dev.azure.com/owner/_git/reponame");
            Assert.That(settings.RepositoryName == "reponame");
            Assert.That(settings.RepositoryOwner == "owner");
            Assert.That(settings.SetAutoMerge);
            Assert.That(settings.RemoteInfo.BranchName == "main");
        }

        [Test]
        public async Task RepositorySettings_ReturnsNull()
        {
            RepositorySettings settings = await _azureSettingsReader.RepositorySettings(null, true);
            Assert.That(settings is null);
        }

        [Test]
        public void RepositorySettings_PathTooLong()
        {
            _ = Assert.ThrowsAsync<NuKeeperException>(() => _azureSettingsReader.RepositorySettings(new Uri("https://dev.azure.com/org/project/_git/reponame/thisShouldNotBeHere/"), true));
        }

        [Test]
        public async Task RepositorySettings_HandlesSpacesInRepo()
        {
            RepositorySettings settings = await _azureSettingsReader.RepositorySettings(new Uri("https://dev.azure.com/org/project%20name/_git/repo%20name"), true);

            Assert.That(settings, Is.Not.Null);
            Assert.That("https://dev.azure.com/org/" == settings.ApiUri.ToString());
            Assert.That("https://dev.azure.com/org/project%20name/_git/repo%20name" == settings.RepositoryUri.AbsoluteUri);
            Assert.That("repo name" == settings.RepositoryName);
            Assert.That("project name" == settings.RepositoryOwner);
        }
    }
}
