using NSubstitute;

using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Engine;
using NuKeeper.GitHub;

using NUnit.Framework;

using System;
using System.Threading.Tasks;

namespace NuKeeper.Integration.Tests.Engine
{
    [TestFixture]
    public class RepositoryFilterTests : TestWithFailureLogging
    {
        [Test]
        public async Task ShouldFilterOutNonDotnetRepository()
        {
            IRepositoryFilter subject = MakeRepositoryFilter();

            bool result =
                await subject.ContainsDotNetProjects(new RepositorySettings
                {
                    RepositoryName = "jquery",
                    RepositoryOwner = "jquery"
                });
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ShouldNotFilterOutADotnetRepository()
        {
            IRepositoryFilter subject = MakeRepositoryFilter();

            bool result =
                await subject.ContainsDotNetProjects(new RepositorySettings { RepositoryName = "sdk", RepositoryOwner = "dotnet" });
            Assert.That(result, Is.True);
        }

        private RepositoryFilter MakeRepositoryFilter()
        {
            ICollaborationFactory collaborationFactory = Substitute.For<ICollaborationFactory>();
            OctokitClient gitHubClient = new(NukeeperLogger);
            gitHubClient.Initialise(new AuthSettings(new Uri("https://api.github.com"), GetPAT()));
            collaborationFactory.CollaborationPlatform.Returns(gitHubClient);

            return new RepositoryFilter(collaborationFactory, NukeeperLogger);
        }

        private string GetPAT() => Environment.GetEnvironmentVariable("GitHubPAT");
    }
}
