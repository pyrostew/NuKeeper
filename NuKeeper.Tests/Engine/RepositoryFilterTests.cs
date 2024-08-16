using NSubstitute;
using NSubstitute.ExceptionExtensions;

using NuKeeper.Abstractions.CollaborationModels;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Engine;

using NUnit.Framework;

using System;
using System.Threading.Tasks;

namespace NuKeeper.Tests.Engine
{
    [TestFixture]
    public class RepositoryFilterTests
    {
        [Test]
        public async Task ShouldFilterWhenNoMatchFound()
        {
            ICollaborationFactory collaborationFactory = Substitute.For<ICollaborationFactory>();
            _ = collaborationFactory.CollaborationPlatform.Search(null).ReturnsForAnyArgs(Task.FromResult(new SearchCodeResult(0)));

            IRepositoryFilter subject = new RepositoryFilter(collaborationFactory, Substitute.For<INuKeeperLogger>());

            bool result = await subject.ContainsDotNetProjects(MakeSampleRepository());

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ShouldNotFilterWhenMatchFound()
        {
            ICollaborationFactory collaborationFactory = Substitute.For<ICollaborationFactory>();
            _ = collaborationFactory.CollaborationPlatform.Search(null).ReturnsForAnyArgs(Task.FromResult(new SearchCodeResult(1)));

            IRepositoryFilter subject = new RepositoryFilter(collaborationFactory, Substitute.For<INuKeeperLogger>());

            bool result = await subject.ContainsDotNetProjects(MakeSampleRepository());

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ShouldNotFilterWhenSearchFails()
        {
            ICollaborationFactory collaborationFactory = Substitute.For<ICollaborationFactory>();
            _ = collaborationFactory.CollaborationPlatform.Search(null).ThrowsForAnyArgs(new Exception());

            IRepositoryFilter subject = new RepositoryFilter(collaborationFactory, Substitute.For<INuKeeperLogger>());

            bool result = await subject.ContainsDotNetProjects(MakeSampleRepository());

            Assert.That(result, Is.True);
        }

        private static RepositorySettings MakeSampleRepository()
        {
            return new RepositorySettings
            {
                RepositoryName = "sample-repo",
                RepositoryOwner = "sample-owner"
            };
        }
    }
}
