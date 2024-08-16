using NSubstitute;

using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Git;

using NUnit.Framework;

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace NuKeeper.Gitea.Tests
{
    [TestFixture]
    public class GiteaSettingsReaderTests
    {
        private GiteaSettingsReader _giteaSettingsReader;
        private IEnvironmentVariablesProvider _environmentVariablesProvider;
        private IGitDiscoveryDriver _gitDiscovery;

        [SetUp]
        public void Setup()
        {
            _environmentVariablesProvider = Substitute.For<IEnvironmentVariablesProvider>();
            _gitDiscovery = Substitute.For<IGitDiscoveryDriver>();
            IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
            _ = httpClientFactory.CreateClient().Returns(new HttpClient());
            _giteaSettingsReader = new GiteaSettingsReader(_gitDiscovery, _environmentVariablesProvider, httpClientFactory);
        }

        [Test]
        public void ReturnsCorrectPlatform()
        {
            Platform platform = _giteaSettingsReader.Platform;

            Assert.That(Platform.Gitea, Is.EqualTo(platform));
        }

        /// <summary>
        /// Test for #739
        /// </summary>
        [Test]
        public void CanRead_NoException_OnBadUri()
        {
            Assert.DoesNotThrowAsync(() => _giteaSettingsReader.CanRead(new Uri("https://try.gitea.io/")));
        }

        [Test]
        public void UpdatesAuthenticationTokenFromTheEnvironment()
        {
            _ = _environmentVariablesProvider.GetEnvironmentVariable("NuKeeper_gitea_token").Returns("envToken");

            CollaborationPlatformSettings settings = new()
            {
                Token = "accessToken",
            };

            _giteaSettingsReader.UpdateCollaborationPlatformSettings(settings);

            Assert.That("envToken", Is.EqualTo(settings.Token));
        }

        [Test]
        public async Task AssumesItCanReadGiteaUrls()
        {
            bool canRead = await _giteaSettingsReader.CanRead(new Uri("https://try.gitea.io/SharpSteff/NuKeeper-TestFork"));
            Assert.That(canRead);
        }

        [Test]
        public void AssumesItCanNotReadGitHubUrls()
        {
            Task<bool> canRead = _giteaSettingsReader.CanRead(new Uri("https://github.com/SharpSteff/NuKeeper-TestFork"));

            Assert.That(canRead, Is.Not.True);
        }

        [TestCase(null)]
        [TestCase("master")]
        public async Task GetsCorrectSettingsFromTheUrl(string targetBranch)
        {
            Uri repositoryUri = new("https://try.gitea.io/SharpSteff/NuKeeper-TestFork");
            RepositorySettings repositorySettings = await _giteaSettingsReader.RepositorySettings(repositoryUri, true, targetBranch);

            Assert.That(repositorySettings, Is.Not.Null);
            Assert.That(new Uri("https://try.gitea.io/api/v1/"), Is.EqualTo(repositorySettings.ApiUri));
            Assert.That(repositoryUri, Is.EqualTo(repositorySettings.RepositoryUri));
            Assert.That("SharpSteff", Is.EqualTo(repositorySettings.RepositoryOwner));
            Assert.That("NuKeeper-TestFork", Is.EqualTo(repositorySettings.RepositoryName));
            Assert.That(targetBranch, Is.EqualTo(repositorySettings.RemoteInfo?.BranchName));
            Assert.That(repositorySettings.SetAutoMerge, Is.False);
        }
    }
}
