using NSubstitute;

using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Logging;
using NuKeeper.AzureDevOps;
using NuKeeper.Collaboration;
using NuKeeper.Engine;
using NuKeeper.GitHub;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace NuKeeper.Tests.Engine
{
    [TestFixture]
    public class CollaborationFactoryTests
    {
        private static CollaborationFactory GetCollaborationFactory()
        {
            Uri azureUri = new("https://dev.azure.com");
            Uri gitHubUri = new("https://api.github.com");

            ISettingsReader settingReader1 = Substitute.For<ISettingsReader>();
            _ = settingReader1.CanRead(azureUri).Returns(true);
            _ = settingReader1.Platform.Returns(Platform.AzureDevOps);

            ISettingsReader settingReader2 = Substitute.For<ISettingsReader>();
            _ = settingReader2.CanRead(gitHubUri).Returns(true);
            _ = settingReader2.Platform.Returns(Platform.GitHub);

            List<ISettingsReader> readers = [settingReader1, settingReader2];
            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();
            IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
            _ = httpClientFactory.CreateClient().Returns(new HttpClient());

            return new CollaborationFactory(readers, logger, httpClientFactory);
        }

        [Test]
        public void UnitialisedFactoryHasNulls()
        {
            CollaborationFactory f = GetCollaborationFactory();

            Assert.That(f, Is.Not.Null);
            Assert.That(f.CollaborationPlatform, Is.Null);
            Assert.That(f.ForkFinder, Is.Null);
            Assert.That(f.RepositoryDiscovery, Is.Null);
        }

        [Test]
        public async Task UnknownApiReturnsUnableToFindPlatform()
        {
            CollaborationFactory collaborationFactory = GetCollaborationFactory();

            ValidationResult result = await collaborationFactory.Initialise(
                    new Uri("https://unknown.com/"), null,
                    ForkMode.SingleRepositoryOnly, null);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage,
                Is.EqualTo("Unable to find collaboration platform for uri https://unknown.com/"));
        }

        [Test]
        public async Task UnknownApiCanHaveManualPlatform()
        {
            CollaborationFactory collaborationFactory = GetCollaborationFactory();

            ValidationResult result = await collaborationFactory.Initialise(
                    new Uri("https://unknown.com/"), "token",
                    ForkMode.SingleRepositoryOnly,
                    Platform.GitHub);

            Assert.That(result.IsSuccess);
            AssertGithub(collaborationFactory);
        }

        [Test]
        public async Task ManualPlatformWillOverrideUri()
        {
            CollaborationFactory collaborationFactory = GetCollaborationFactory();

            ValidationResult result = await collaborationFactory.Initialise(
                new Uri("https://api.github.myco.com"), "token",
                ForkMode.SingleRepositoryOnly,
                Platform.AzureDevOps);

            Assert.That(result.IsSuccess);
            AssertAzureDevOps(collaborationFactory);
        }

        [Test]
        public async Task AzureDevOpsUrlReturnsAzureDevOps()
        {
            CollaborationFactory collaborationFactory = GetCollaborationFactory();

            ValidationResult result = await collaborationFactory.Initialise(new Uri("https://dev.azure.com"), "token",
                ForkMode.SingleRepositoryOnly, null);
            Assert.That(result.IsSuccess);

            AssertAzureDevOps(collaborationFactory);
            AssertAreSameObject(collaborationFactory);
        }

        [Test]
        public async Task GithubUrlReturnsGitHub()
        {
            CollaborationFactory collaborationFactory = GetCollaborationFactory();

            ValidationResult result = await collaborationFactory.Initialise(new Uri("https://api.github.com"), "token",
                ForkMode.PreferFork, null);
            Assert.That(result.IsSuccess);

            AssertGithub(collaborationFactory);
            AssertAreSameObject(collaborationFactory);
        }

        private static void AssertAreSameObject(ICollaborationFactory collaborationFactory)
        {
            ICollaborationPlatform collaborationPlatform = collaborationFactory.CollaborationPlatform;
            Assert.That(collaborationPlatform, Is.SameAs(collaborationFactory.CollaborationPlatform));

            IRepositoryDiscovery repositoryDiscovery = collaborationFactory.RepositoryDiscovery;
            Assert.That(repositoryDiscovery, Is.SameAs(collaborationFactory.RepositoryDiscovery));

            IForkFinder forkFinder = collaborationFactory.ForkFinder;
            Assert.That(forkFinder, Is.SameAs(collaborationFactory.ForkFinder));

            CollaborationPlatformSettings settings = collaborationFactory.Settings;
            Assert.That(settings, Is.SameAs(collaborationFactory.Settings));
        }

        private static void AssertGithub(ICollaborationFactory collaborationFactory)
        {
            Assert.That(collaborationFactory.ForkFinder, Is.InstanceOf<GitHubForkFinder>());
            Assert.That(collaborationFactory.RepositoryDiscovery, Is.InstanceOf<GitHubRepositoryDiscovery>());
            Assert.That(collaborationFactory.CollaborationPlatform, Is.InstanceOf<OctokitClient>());
            Assert.That(collaborationFactory.Settings, Is.InstanceOf<CollaborationPlatformSettings>());
            Assert.That(collaborationFactory.CommitWorder, Is.InstanceOf<DefaultCommitWorder>());
        }

        private static void AssertAzureDevOps(ICollaborationFactory collaborationFactory)
        {
            Assert.That(collaborationFactory.ForkFinder, Is.InstanceOf<AzureDevOpsForkFinder>());
            Assert.That(collaborationFactory.RepositoryDiscovery, Is.InstanceOf<AzureDevOpsRepositoryDiscovery>());
            Assert.That(collaborationFactory.CollaborationPlatform, Is.InstanceOf<AzureDevOpsPlatform>());
            Assert.That(collaborationFactory.Settings, Is.InstanceOf<CollaborationPlatformSettings>());
            Assert.That(collaborationFactory.CommitWorder, Is.InstanceOf<AzureDevOpsCommitWorder>());
        }
    }
}
