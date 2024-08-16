using NSubstitute;

using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;

using NUnit.Framework;

using System;
using System.Threading.Tasks;

namespace NuKeeper.Gitlab.Tests
{
    [TestFixture]
    public class GitlabSettingsReaderTests
    {
        private GitlabSettingsReader _gitlabSettingsReader;
        private IEnvironmentVariablesProvider _environmentVariablesProvider;

        [SetUp]
        public void Setup()
        {
            _environmentVariablesProvider = Substitute.For<IEnvironmentVariablesProvider>();

            _gitlabSettingsReader = new GitlabSettingsReader(_environmentVariablesProvider);
        }

        [Test]
        public void ReturnsCorrectPlatform()
        {
            Platform platform = _gitlabSettingsReader.Platform;

            Assert.That(Platform.GitLab == platform);
        }

        [Test]
        public void UpdatesAuthenticationTokenFromTheEnvironment()
        {
            _ = _environmentVariablesProvider.GetEnvironmentVariable("NuKeeper_gitlab_token").Returns("envToken");

            CollaborationPlatformSettings settings = new()
            {
                Token = "accessToken",
            };

            _gitlabSettingsReader.UpdateCollaborationPlatformSettings(settings);

            Assert.That(settings.Token == "envToken");
        }

        [Test]
        public async Task AssumesItCanReadGitLabUrls()
        {
            bool canRead = await _gitlabSettingsReader.CanRead(new Uri("https://gitlab.com/user/projectname.git"));

            Assert.That(canRead);
        }

        [Test]
        public async Task AssumesItCanReadGitLabOrganisationUrls()
        {
            bool canRead = await _gitlabSettingsReader.CanRead(new Uri("https://gitlab.com/org/user/projectname.git"));

            Assert.That(canRead);
        }

        [TestCase(null)]
        [TestCase("master")]
        public async Task GetsCorrectSettingsFromTheUrl(string targetBranch)
        {
            Uri repositoryUri = new("https://gitlab.com/user/projectname.git");
            RepositorySettings repositorySettings = await _gitlabSettingsReader.RepositorySettings(repositoryUri, true, targetBranch);

            Assert.That(repositorySettings, Is.Not.Null);
            Assert.That(new Uri("https://gitlab.com/api/v4/") == repositorySettings.ApiUri);
            Assert.That(repositoryUri == repositorySettings.RepositoryUri);
            Assert.That("user" == repositorySettings.RepositoryOwner);
            Assert.That("projectname" == repositorySettings.RepositoryName);
            Assert.That(targetBranch == repositorySettings.RemoteInfo?.BranchName);
            Assert.That(!repositorySettings.SetAutoMerge);
        }

        [TestCase(null)]
        [TestCase("master")]
        public async Task GetsCorrectSettingsFromTheOrganisationUrl(string targetBranch)
        {
            Uri repositoryUri = new("https://gitlab.com/org/user/projectname.git");
            RepositorySettings repositorySettings = await _gitlabSettingsReader.RepositorySettings(repositoryUri, true, targetBranch);

            Assert.That(repositorySettings, Is.Not.Null);
            Assert.That(new Uri("https://gitlab.com/api/v4/") == repositorySettings.ApiUri);
            Assert.That(repositoryUri == repositorySettings.RepositoryUri);
            Assert.That("org/user" == repositorySettings.RepositoryOwner);
            Assert.That("projectname" == repositorySettings.RepositoryName);
            Assert.That(targetBranch == repositorySettings.RemoteInfo?.BranchName);
            Assert.That(!repositorySettings.SetAutoMerge);
        }
    }
}
