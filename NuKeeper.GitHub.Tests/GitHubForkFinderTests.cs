using NSubstitute;

using NuKeeper.Abstractions.CollaborationModels;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Logging;

using NUnit.Framework;

using System;
using System.Threading.Tasks;

namespace NuKeeper.GitHub.Tests
{
    [TestFixture]
    public class GitHubForkFinderTests
    {
        [Test]
        public async Task ThrowsWhenNoPushableForkCanBeFound()
        {
            ForkData fallbackFork = DefaultFork();

            GitHubForkFinder forkFinder = new(Substitute.For<ICollaborationPlatform>(), Substitute.For<INuKeeperLogger>(), ForkMode.PreferFork);

            ForkData fork = await forkFinder.FindPushFork("testUser", fallbackFork);

            Assert.That(fork, Is.Null);
        }

        [Test]
        public async Task FallbackForkIsUsedWhenItIsFound()
        {
            ForkData fallbackFork = DefaultFork();
            Repository fallbackRepoData = RepositoryBuilder.MakeRepository();

            ICollaborationPlatform collaborationPlatform = Substitute.For<ICollaborationPlatform>();
            _ = collaborationPlatform.GetUserRepository(fallbackFork.Owner, fallbackFork.Name)
                .Returns(fallbackRepoData);

            GitHubForkFinder forkFinder = new(collaborationPlatform, Substitute.For<INuKeeperLogger>(), ForkMode.PreferFork);

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

            GitHubForkFinder forkFinder = new(collaborationPlatform, Substitute.For<INuKeeperLogger>(), ForkMode.PreferFork);

            ForkData fork = await forkFinder.FindPushFork("testUser", fallbackFork);

            Assert.That(fork, Is.Null);
        }

        [Test]
        public async Task WhenSuitableUserForkIsFoundItIsUsedOverFallback()
        {
            ForkData fallbackFork = DefaultFork();

            Repository userRepo = RepositoryBuilder.MakeRepository();

            ICollaborationPlatform collaborationPlatform = Substitute.For<ICollaborationPlatform>();
            _ = collaborationPlatform.GetUserRepository(Arg.Any<string>(), Arg.Any<string>())
                .Returns(userRepo);

            GitHubForkFinder forkFinder = new(collaborationPlatform, Substitute.For<INuKeeperLogger>(), ForkMode.PreferFork);

            ForkData fork = await forkFinder.FindPushFork("testUser", fallbackFork);

            Assert.That(fork, Is.Not.EqualTo(fallbackFork));
            AssertForkMatchesRepo(fork, userRepo);
        }

        [Test]
        public async Task WhenSuitableUserForkIsFound_ThatMatchesCloneHtmlUrl_ItIsUsedOverFallback()
        {
            ForkData fallbackFork = new(new Uri(RepositoryBuilder.ParentCloneUrl), "testOrg", "someRepo");

            Repository userRepo = RepositoryBuilder.MakeRepository();

            ICollaborationPlatform collaborationPlatform = Substitute.For<ICollaborationPlatform>();
            _ = collaborationPlatform.GetUserRepository(Arg.Any<string>(), Arg.Any<string>())
                .Returns(userRepo);

            GitHubForkFinder forkFinder = new(collaborationPlatform, Substitute.For<INuKeeperLogger>(), ForkMode.PreferFork);

            ForkData fork = await forkFinder.FindPushFork("testUser", fallbackFork);

            Assert.That(fork, Is.Not.EqualTo(fallbackFork));
            AssertForkMatchesRepo(fork, userRepo);
        }

        [Test]
        public async Task WhenSuitableUserForkIsFound_ThatMatchesCloneHtmlUrl_WithRepoUrlVariation()
        {
            ForkData fallbackFork = new(new Uri(RepositoryBuilder.ParentCloneBareUrl), "testOrg", "someRepo");

            Repository userRepo = RepositoryBuilder.MakeRepository();

            ICollaborationPlatform collaborationPlatform = Substitute.For<ICollaborationPlatform>();
            _ = collaborationPlatform.GetUserRepository(Arg.Any<string>(), Arg.Any<string>())
                .Returns(userRepo);

            GitHubForkFinder forkFinder = new(collaborationPlatform, Substitute.For<INuKeeperLogger>(), ForkMode.PreferFork);

            ForkData fork = await forkFinder.FindPushFork("testUser", fallbackFork);

            Assert.That(fork, Is.Not.EqualTo(fallbackFork));
            AssertForkMatchesRepo(fork, userRepo);
        }

        [Test]
        public async Task WhenSuitableUserForkIsFound_ThatMatchesCloneHtmlUrl_WithParentRepoUrlVariation()
        {
            ForkData fallbackFork = new(new Uri(RepositoryBuilder.ParentCloneUrl), "testOrg", "someRepo");

            Repository userRepo = RepositoryBuilder.MakeRepository(
                RepositoryBuilder.ForkCloneUrl,
                true, true, "userRepo",
                RepositoryBuilder.MakeParentRepo(RepositoryBuilder.ParentCloneBareUrl));

            ICollaborationPlatform collaborationPlatform = Substitute.For<ICollaborationPlatform>();
            _ = collaborationPlatform.GetUserRepository(Arg.Any<string>(), Arg.Any<string>())
                .Returns(userRepo);

            GitHubForkFinder forkFinder = new(collaborationPlatform, Substitute.For<INuKeeperLogger>(), ForkMode.PreferFork);

            ForkData fork = await forkFinder.FindPushFork("testUser", fallbackFork);

            Assert.That(fork, Is.Not.EqualTo(fallbackFork));
            AssertForkMatchesRepo(fork, userRepo);
        }

        [Test]
        public async Task WhenUnsuitableUserForkIsFoundItIsNotUsed()
        {
            ForkData fallbackFork = NoMatchFork();

            Repository userRepo = RepositoryBuilder.MakeRepository();

            ICollaborationPlatform collaborationPlatform = Substitute.For<ICollaborationPlatform>();
            _ = collaborationPlatform.GetUserRepository(Arg.Any<string>(), Arg.Any<string>())
                .Returns(userRepo);

            GitHubForkFinder forkFinder = new(collaborationPlatform, Substitute.For<INuKeeperLogger>(), ForkMode.PreferFork);

            ForkData fork = await forkFinder.FindPushFork("testUser", fallbackFork);

            Assert.That(fork, Is.EqualTo(fallbackFork));
        }

        [Test]
        public async Task WhenUserForkIsNotFoundItIsCreated()
        {
            ForkData fallbackFork = DefaultFork();

            Repository userRepo = RepositoryBuilder.MakeRepository();

            ICollaborationPlatform collaborationPlatform = Substitute.For<ICollaborationPlatform>();
            _ = collaborationPlatform.GetUserRepository(Arg.Any<string>(), Arg.Any<string>())
                .Returns((Repository)null);
            _ = collaborationPlatform.MakeUserFork(Arg.Any<string>(), Arg.Any<string>())
                .Returns(userRepo);

            GitHubForkFinder forkFinder = new(collaborationPlatform, Substitute.For<INuKeeperLogger>(), ForkMode.PreferFork);

            ForkData actualFork = await forkFinder.FindPushFork("testUser", fallbackFork);

            _ = await collaborationPlatform.Received(1).MakeUserFork(Arg.Any<string>(), Arg.Any<string>());

            Assert.That(actualFork, Is.Not.Null);
            Assert.That(actualFork, Is.Not.EqualTo(fallbackFork));
        }

        [Test]
        public async Task PreferSingleRepoModeWillNotPreferFork()
        {
            ForkData fallbackFork = DefaultFork();

            Repository userRepo = RepositoryBuilder.MakeRepository();

            ICollaborationPlatform collaborationPlatform = Substitute.For<ICollaborationPlatform>();
            _ = collaborationPlatform.GetUserRepository(Arg.Any<string>(), Arg.Any<string>())
                .Returns(userRepo);

            GitHubForkFinder forkFinder = new(collaborationPlatform, Substitute.For<INuKeeperLogger>(), ForkMode.PreferSingleRepository);

            ForkData fork = await forkFinder.FindPushFork("testUser", fallbackFork);

            Assert.That(fork, Is.EqualTo(fallbackFork));
        }

        [Test]
        public async Task PreferSingleRepoModeWillUseForkWhenUpstreamIsUnsuitable()
        {
            ForkData fallbackFork = DefaultFork();

            ICollaborationPlatform collaborationPlatform = Substitute.For<ICollaborationPlatform>();

            Repository defaultRepo = RepositoryBuilder.MakeRepository(true, false);
            _ = collaborationPlatform.GetUserRepository(fallbackFork.Owner, fallbackFork.Name)
                .Returns(defaultRepo);

            Repository userRepo = RepositoryBuilder.MakeRepository();

            _ = collaborationPlatform.GetUserRepository("testUser", fallbackFork.Name)
                .Returns(userRepo);

            GitHubForkFinder forkFinder = new(collaborationPlatform, Substitute.For<INuKeeperLogger>(), ForkMode.PreferSingleRepository);

            ForkData fork = await forkFinder.FindPushFork("testUser", fallbackFork);

            Assert.That(fork, Is.Not.EqualTo(fallbackFork));
            AssertForkMatchesRepo(fork, userRepo);
        }

        [Test]
        public async Task SingleRepoOnlyModeWillNotPreferFork()
        {
            ForkData fallbackFork = DefaultFork();

            Repository userRepo = RepositoryBuilder.MakeRepository();

            ICollaborationPlatform collaborationPlatform = Substitute.For<ICollaborationPlatform>();
            _ = collaborationPlatform.GetUserRepository(Arg.Any<string>(), Arg.Any<string>())
                .Returns(userRepo);

            GitHubForkFinder forkFinder = new(collaborationPlatform, Substitute.For<INuKeeperLogger>(), ForkMode.SingleRepositoryOnly);

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

            GitHubForkFinder forkFinder = new(collaborationPlatform, Substitute.For<INuKeeperLogger>(), ForkMode.SingleRepositoryOnly);

            ForkData fork = await forkFinder.FindPushFork("testUser", fallbackFork);

            Assert.That(fork, Is.Null);
        }

        private static ForkData DefaultFork()
        {
            return new ForkData(new Uri(RepositoryBuilder.ParentCloneUrl), "testOrg", "someRepo");
        }

        private static ForkData NoMatchFork()
        {
            return new ForkData(new Uri(RepositoryBuilder.NoMatchUrl), "testOrg", "someRepo");
        }

        private static void AssertForkMatchesRepo(ForkData fork, Repository repo)
        {
            Assert.That(fork, Is.Not.Null);
            Assert.That(fork.Name, Is.EqualTo(repo.Name));
            Assert.That(fork.Owner, Is.EqualTo(repo.Owner.Login));
            Assert.That(fork.Uri, Is.EqualTo(repo.CloneUrl));
        }
    }
}
