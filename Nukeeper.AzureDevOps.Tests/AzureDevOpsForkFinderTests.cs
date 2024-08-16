using NSubstitute;

using NuKeeper.Abstractions.CollaborationModels;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Logging;
using NuKeeper.AzureDevOps;

using NUnit.Framework;

using System;
using System.Threading.Tasks;

namespace Nukeeper.AzureDevOps.Tests
{
    public class AzureDevOpsForkFinderTests
    {

        [Test]
        public async Task ThrowsWhenNoPushableForkCanBeFound()
        {
            ForkData fallbackFork = DefaultFork();

            AzureDevOpsForkFinder forkFinder = new(Substitute.For<ICollaborationPlatform>(), Substitute.For<INuKeeperLogger>(), ForkMode.SingleRepositoryOnly);

            ForkData fork = await forkFinder.FindPushFork("testUser", fallbackFork);

            Assert.That(fork, Is.Null);
        }

        [Test]
        public void ThrowsWhenPreferFork()
        {
            ForkData fallbackFork = DefaultFork();

            ArgumentOutOfRangeException argument = Assert.Throws<ArgumentOutOfRangeException>(() => new AzureDevOpsForkFinder(Substitute.For<ICollaborationPlatform>(), Substitute.For<INuKeeperLogger>(), ForkMode.PreferFork));
        }


        [Test]
        public void ThrowsWhenPreferSingleRepository()
        {
            ForkData fallbackFork = DefaultFork();

            ArgumentOutOfRangeException argument = Assert.Throws<ArgumentOutOfRangeException>(() => new AzureDevOpsForkFinder(Substitute.For<ICollaborationPlatform>(), Substitute.For<INuKeeperLogger>(), ForkMode.PreferSingleRepository));
        }

        [Test]
        public async Task FallbackForkIsUsedWhenItIsFound()
        {
            ForkData fallbackFork = DefaultFork();
            Repository fallbackRepoData = RepositoryBuilder.MakeRepository();

            ICollaborationPlatform collaborationPlatform = Substitute.For<ICollaborationPlatform>();
            _ = collaborationPlatform.GetUserRepository(fallbackFork.Owner, fallbackFork.Name)
                .Returns(fallbackRepoData);

            AzureDevOpsForkFinder forkFinder = new(collaborationPlatform, Substitute.For<INuKeeperLogger>(), ForkMode.SingleRepositoryOnly);

            ForkData fork = await forkFinder.FindPushFork("testUser", fallbackFork);

            Assert.That(fork, Is.Not.Null);
            Assert.That(fork, Is.EqualTo(fallbackFork));
        }

        [Test]
        public async Task FallbackForkIsNotUsedWhenItIsNotPushable()
        {
            ForkData fallbackFork = DefaultFork();
            Repository fallbackRepoData = RepositoryBuilder.MakeRepository(true, false);

            ICollaborationPlatform collaborationPlatform = Substitute.For<ICollaborationPlatform>();
            _ = collaborationPlatform.GetUserRepository(fallbackFork.Owner, fallbackFork.Name)
                .Returns(fallbackRepoData);

            AzureDevOpsForkFinder forkFinder = new(Substitute.For<ICollaborationPlatform>(), Substitute.For<INuKeeperLogger>(), ForkMode.SingleRepositoryOnly);

            ForkData fork = await forkFinder.FindPushFork("testUser", fallbackFork);

            Assert.That(fork, Is.Null);
        }

        [Test]
        public async Task SingleRepoOnlyModeWillNotPreferFork()
        {
            ForkData fallbackFork = DefaultFork();

            Repository userRepo = RepositoryBuilder.MakeRepository();

            ICollaborationPlatform collaborationPlatform = Substitute.For<ICollaborationPlatform>();
            _ = collaborationPlatform.GetUserRepository(Arg.Any<string>(), Arg.Any<string>())
                .Returns(userRepo);

            AzureDevOpsForkFinder forkFinder = new(collaborationPlatform, Substitute.For<INuKeeperLogger>(), ForkMode.SingleRepositoryOnly);

            ForkData fork = await forkFinder.FindPushFork("testUser", fallbackFork);

            Assert.That(fork, Is.EqualTo(fallbackFork));
        }


        [Test]
        public async Task SingleRepoOnlyModeWillNotUseForkWhenUpstreamIsUnsuitable()
        {
            ForkData fallbackFork = DefaultFork();

            ICollaborationPlatform collaborationPlatform = Substitute.For<ICollaborationPlatform>();

            Repository defaultRepo = RepositoryBuilder.MakeRepository(true, false);
            _ = collaborationPlatform.GetUserRepository(fallbackFork.Owner, fallbackFork.Name)
                .Returns(defaultRepo);

            Repository userRepo = RepositoryBuilder.MakeRepository();

            _ = collaborationPlatform.GetUserRepository("testUser", fallbackFork.Name)
                .Returns(userRepo);

            AzureDevOpsForkFinder forkFinder = new(collaborationPlatform, Substitute.For<INuKeeperLogger>(), ForkMode.SingleRepositoryOnly);

            ForkData fork = await forkFinder.FindPushFork("testUser", fallbackFork);

            Assert.That(fork, Is.Null);
        }

        private static ForkData DefaultFork()
        {
            return new ForkData(RepositoryBuilder.ParentCloneUrl, "testOrg", "someRepo");
        }
    }
}
