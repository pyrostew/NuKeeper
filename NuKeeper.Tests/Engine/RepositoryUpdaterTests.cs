using NSubstitute;

using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using NuKeeper.Abstractions;
using NuKeeper.Abstractions.CollaborationModels;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Git;
using NuKeeper.Abstractions.Inspections.Files;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.NuGetApi;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Engine;
using NuKeeper.Engine.Packages;
using NuKeeper.Inspection;
using NuKeeper.Inspection.Report;
using NuKeeper.Inspection.Sort;
using NuKeeper.Inspection.Sources;
using NuKeeper.Update;
using NuKeeper.Update.Process;
using NuKeeper.Update.Selection;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NuKeeper.Tests.Engine
{
    [TestFixture]
    public class RepositoryUpdaterTests
    {
        private INuGetSourcesReader _sourcesReader;
        private INuKeeperLogger _nukeeperLogger;
        private IUpdateFinder _updateFinder;
        private IPackageUpdater _packageUpdater;
        private List<PackageUpdateSet> _packagesToReturn;

        [SetUp]
        public void Initialize()
        {
            _packagesToReturn = [];

            _sourcesReader = Substitute.For<INuGetSourcesReader>();
            _nukeeperLogger = Substitute.For<INuKeeperLogger>();
            _updateFinder = Substitute.For<IUpdateFinder>();
            _packageUpdater = Substitute.For<IPackageUpdater>();
            _ = _updateFinder
                .FindPackageUpdateSets(
                    Arg.Any<IFolder>(),
                    Arg.Any<NuGetSources>(),
                    Arg.Any<VersionChange>(),
                    Arg.Any<UsePrerelease>(),
                    Arg.Any<Regex>()
                )
                .Returns(_packagesToReturn);
        }

        [Test]
        public async Task WhenThereAreNoUpdates_CountIsZero()
        {
            IPackageUpdateSelection updateSelection = Substitute.For<IPackageUpdateSelection>();
            UpdateSelectionAll(updateSelection);

            (IRepositoryUpdater repoUpdater, IPackageUpdater packageUpdater) = MakeRepositoryUpdater(
                updateSelection,
                []);

            IGitDriver git = Substitute.For<IGitDriver>();
            RepositoryData repo = MakeRepositoryData();

            int count = await repoUpdater.Run(git, repo, MakeSettings());

            Assert.That(count, Is.EqualTo(0));
            await AssertDidNotReceiveMakeUpdate(packageUpdater);
        }

        [Test]
        public async Task WhenThereIsAnUpdate_CountIsOne()
        {
            IPackageUpdateSelection updateSelection = Substitute.For<IPackageUpdateSelection>();
            UpdateSelectionAll(updateSelection);

            List<PackageUpdateSet> updates = PackageUpdates.UpdateSet()
                .InList();

            (IRepositoryUpdater repoUpdater, IPackageUpdater packageUpdater) = MakeRepositoryUpdater(
                updateSelection, updates);

            IGitDriver git = Substitute.For<IGitDriver>();
            RepositoryData repo = MakeRepositoryData();

            int count = await repoUpdater.Run(git, repo, MakeSettings());

            Assert.That(count, Is.EqualTo(1));
            await AssertReceivedMakeUpdate(packageUpdater, 1);
        }

        [TestCase(0, 0, true, true, 0, 0)]
        [TestCase(1, 0, true, true, 1, 0)]
        [TestCase(2, 0, true, true, 2, 0)]
        [TestCase(3, 0, true, true, 3, 0)]
        [TestCase(1, 1, true, true, 0, 0)]
        [TestCase(2, 1, true, true, 1, 0)]
        [TestCase(3, 1, true, true, 2, 0)]
        [TestCase(1, 0, false, true, 1, 0)]
        [TestCase(1, 1, false, true, 0, 0)]
        [TestCase(1, 0, true, false, 1, 1)]
        [TestCase(2, 0, true, false, 2, 1)]
        [TestCase(3, 0, true, false, 3, 1)]
        [TestCase(1, 1, true, false, 0, 0)]
        [TestCase(2, 1, true, false, 1, 1)]
        [TestCase(3, 1, true, false, 2, 1)]
        [TestCase(1, 0, false, false, 1, 1)]
        [TestCase(1, 1, false, false, 0, 0)]

        public async Task WhenThereAreUpdates_CountIsAsExpected(
            int numberOfUpdates,
            int existingCommitsPerBranch,
            bool consolidateUpdates,
            bool pullRequestExists,
            int expectedUpdates,
            int expectedPrs
        )
        {
            IPackageUpdateSelection updateSelection = Substitute.For<IPackageUpdateSelection>();
            ICollaborationFactory collaborationFactory = Substitute.For<ICollaborationFactory>();
            IGitDriver gitDriver = Substitute.For<IGitDriver>();
            IExistingCommitFilter existingCommitFilder = Substitute.For<IExistingCommitFilter>();
            UpdateSelectionAll(updateSelection);

            _ = gitDriver.GetCurrentHead().Returns("def");
            _ = gitDriver.CheckoutNewBranch(Arg.Any<string>()).Returns(true);

            _ = collaborationFactory
                .CollaborationPlatform
                .PullRequestExists(Arg.Any<ForkData>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(pullRequestExists);

            PackageUpdater packageUpdater = new(collaborationFactory,
                existingCommitFilder,
                Substitute.For<IUpdateRunner>(),
                Substitute.For<INuKeeperLogger>());

            List<PackageUpdateSet> updates = Enumerable.Range(1, numberOfUpdates)
                .Select(_ => PackageUpdates.UpdateSet())
                .ToList();

            System.Collections.ObjectModel.ReadOnlyCollection<PackageUpdateSet> filteredUpdates = updates.Skip(existingCommitsPerBranch).ToList().AsReadOnly();

            _ = existingCommitFilder.Filter(Arg.Any<IGitDriver>(), Arg.Any<IReadOnlyCollection<PackageUpdateSet>>(), Arg.Any<string>(), Arg.Any<string>()).Returns(filteredUpdates);

            SettingsContainer settings = MakeSettings(consolidateUpdates);

            (IRepositoryUpdater repoUpdater, _) = MakeRepositoryUpdater(
                updateSelection, updates, packageUpdater);

            RepositoryData repo = MakeRepositoryData();

            int count = await repoUpdater.Run(gitDriver, repo, settings);

            Assert.That(count, Is.EqualTo(expectedUpdates), "Returned count of updates");

            await collaborationFactory.CollaborationPlatform.Received(expectedPrs)
                .OpenPullRequest(
                    Arg.Any<ForkData>(),
                    Arg.Any<PullRequestRequest>(),
                    Arg.Any<IEnumerable<string>>());

            await gitDriver.Received(expectedUpdates).Commit(Arg.Any<string>());
        }

        [Test]
        public async Task WhenUpdatesAreFilteredOut_CountIsZero()
        {
            IPackageUpdateSelection updateSelection = Substitute.For<IPackageUpdateSelection>();
            UpdateSelectionNone(updateSelection);

            List<PackageUpdateSet> twoUpdates =
            [
                PackageUpdates.UpdateSet(),
                PackageUpdates.UpdateSet()
            ];

            (IRepositoryUpdater repoUpdater, IPackageUpdater packageUpdater) = MakeRepositoryUpdater(
                updateSelection,
                twoUpdates);

            IGitDriver git = Substitute.For<IGitDriver>();
            RepositoryData repo = MakeRepositoryData();

            int count = await repoUpdater.Run(git, repo, MakeSettings());

            Assert.That(count, Is.EqualTo(0));
            await AssertDidNotReceiveMakeUpdate(packageUpdater);
        }

        [Test]
        public async Task Run_TwoUpdatesOneExistingPrAndMaxOpenPrIsGreaterThanOne_CreatesPrForSecondUpdate()
        {
            _packagesToReturn.Add(MakePackageUpdateSet("foo.bar", "1.0.0"));
            _packagesToReturn.Add(MakePackageUpdateSet("notfoo.bar", "2.0.0"));
            _ = _packageUpdater
                .MakeUpdatePullRequests(
                    Arg.Any<IGitDriver>(),
                    Arg.Any<RepositoryData>(),
                    Arg.Any<IReadOnlyCollection<PackageUpdateSet>>(),
                    Arg.Any<NuGetSources>(),
                    Arg.Any<SettingsContainer>()
                )
                .Returns((0, false), (1, false));
            RepositoryData repoData = MakeRepositoryData();
            SettingsContainer settings = MakeSettings();
            settings.UserSettings.MaxOpenPullRequests = 2;
            settings.PackageFilters.MaxPackageUpdates = 1;
            RepositoryUpdater sut = MakeRepositoryUpdater();

            int result = await sut.Run(Substitute.For<IGitDriver>(), repoData, settings);

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public async Task Run_MultipleUpdatesMaxPackageUpdatesIsOne_StillOnlyCreatesOnePr()
        {
            _packagesToReturn.Add(MakePackageUpdateSet("foo.bar", "1.0.0"));
            _packagesToReturn.Add(MakePackageUpdateSet("notfoo.bar", "2.0.0"));
            _packagesToReturn.Add(MakePackageUpdateSet("baz.bar", "3.0.0"));
            _ = _packageUpdater
                .MakeUpdatePullRequests(
                    Arg.Any<IGitDriver>(),
                    Arg.Any<RepositoryData>(),
                    Arg.Any<IReadOnlyCollection<PackageUpdateSet>>(),
                    Arg.Any<NuGetSources>(),
                    Arg.Any<SettingsContainer>()
                )
                .Returns(ci => (((IReadOnlyCollection<PackageUpdateSet>)ci[2]).Count, false));
            RepositoryData repoData = MakeRepositoryData();
            SettingsContainer settings = MakeSettings();
            settings.UserSettings.MaxOpenPullRequests = 10;
            settings.PackageFilters.MaxPackageUpdates = 1;
            RepositoryUpdater sut = MakeRepositoryUpdater();

            int result = await sut.Run(Substitute.For<IGitDriver>(), repoData, settings);

            Assert.That(result, Is.EqualTo(1));
        }

        private static PackageUpdateSet MakePackageUpdateSet(string packageName, string version)
        {
            return new PackageUpdateSet(
                new PackageLookupResult(
                    VersionChange.Major,
                    MakePackageSearchMetadata(packageName, version),
                    null,
                    null
                ),
                new List<PackageInProject>
                {
                    MakePackageInProject(packageName, version)
                }
            );
        }

        private static PackageInProject MakePackageInProject(string packageName, string version)
        {
            return new PackageInProject(
                new PackageVersionRange(
                    packageName,
                    VersionRange.Parse(version)
                ),
                new PackagePath(
                    "projectA",
                    "MyFolder",
                    PackageReferenceType.PackagesConfig
                )
            );
        }

        private static PackageSearchMetadata MakePackageSearchMetadata(string packageName, string version)
        {
            return new PackageSearchMetadata(
                new PackageIdentity(
                    packageName,
                    NuGetVersion.Parse(version)
                ),
                new PackageSource("https://api.nuget.com/v3/"),
                new DateTimeOffset(2019, 1, 12, 0, 0, 0, TimeSpan.Zero),
                null
            );
        }

        private static async Task AssertReceivedMakeUpdate(
            IPackageUpdater packageUpdater,
            int count)
        {
            _ = await packageUpdater.Received(count)
                .MakeUpdatePullRequests(
                    Arg.Any<IGitDriver>(),
                Arg.Any<RepositoryData>(),
                Arg.Any<IReadOnlyCollection<PackageUpdateSet>>(),
                Arg.Any<NuGetSources>(),
                Arg.Any<SettingsContainer>());
        }

        private static async Task AssertDidNotReceiveMakeUpdate(
            IPackageUpdater packageUpdater)
        {
            _ = await packageUpdater.DidNotReceiveWithAnyArgs()
                .MakeUpdatePullRequests(
                    Arg.Any<IGitDriver>(),
                Arg.Any<RepositoryData>(),
                Arg.Any<IReadOnlyCollection<PackageUpdateSet>>(),
                Arg.Any<NuGetSources>(),
                Arg.Any<SettingsContainer>());
        }

        private static void UpdateSelectionAll(IPackageUpdateSelection updateSelection)
        {
            _ = updateSelection.SelectTargets(
                    Arg.Any<ForkData>(),
                    Arg.Any<IReadOnlyCollection<PackageUpdateSet>>(),
                    Arg.Any<FilterSettings>())
                .Returns(c => c.ArgAt<IReadOnlyCollection<PackageUpdateSet>>(1));
        }

        private static void UpdateSelectionNone(IPackageUpdateSelection updateSelection)
        {
            _ = updateSelection.SelectTargets(
                    Arg.Any<ForkData>(),
                    Arg.Any<IReadOnlyCollection<PackageUpdateSet>>(),
                    Arg.Any<FilterSettings>())
                .Returns(new List<PackageUpdateSet>());
        }

        private static SettingsContainer MakeSettings(
            bool consolidateUpdates = false
        )
        {
            return new SettingsContainer
            {
                SourceControlServerSettings = new SourceControlServerSettings()
                {
                    Repository = new RepositorySettings()
                },
                UserSettings = new UserSettings
                {
                    MaxOpenPullRequests = int.MaxValue,
                    ConsolidateUpdatesInSinglePullRequest = consolidateUpdates
                },
                BranchSettings = new BranchSettings(),
                PackageFilters = new FilterSettings
                {
                    MaxPackageUpdates = 3,
                    MinimumAge = new TimeSpan(7, 0, 0, 0),
                }
            };
        }

        private static (
            IRepositoryUpdater repositoryUpdater,
            IPackageUpdater packageUpdater
        ) MakeRepositoryUpdater(
            IPackageUpdateSelection updateSelection,
            List<PackageUpdateSet> updates,
            IPackageUpdater packageUpdater = null)
        {
            INuGetSourcesReader sources = Substitute.For<INuGetSourcesReader>();
            IUpdateFinder updateFinder = Substitute.For<IUpdateFinder>();
            IFileRestoreCommand fileRestore = Substitute.For<IFileRestoreCommand>();
            IReporter reporter = Substitute.For<IReporter>();

            _ = updateFinder.FindPackageUpdateSets(
                    Arg.Any<IFolder>(),
                    Arg.Any<NuGetSources>(),
                    Arg.Any<VersionChange>(),
                    Arg.Any<UsePrerelease>())
                .Returns(updates);

            if (packageUpdater == null)
            {
                packageUpdater = Substitute.For<IPackageUpdater>();
                _ = packageUpdater.MakeUpdatePullRequests(
                        Arg.Any<IGitDriver>(),
                        Arg.Any<RepositoryData>(),
                        Arg.Any<IReadOnlyCollection<PackageUpdateSet>>(),
                        Arg.Any<NuGetSources>(),
                        Arg.Any<SettingsContainer>())
                    .Returns((1, false));
            }

            RepositoryUpdater repoUpdater = new(
                sources, updateFinder, updateSelection, packageUpdater,
                Substitute.For<INuKeeperLogger>(), new SolutionRestore(fileRestore),
                reporter);

            return (repoUpdater, packageUpdater);
        }

        private RepositoryUpdater MakeRepositoryUpdater()
        {
            PackageUpdateSelection packageUpdateSelector = new(
                new PackageUpdateSetSort(_nukeeperLogger),
                new UpdateSelection(_nukeeperLogger),
                _nukeeperLogger
            );

            return new RepositoryUpdater(
                _sourcesReader,
                _updateFinder,
                packageUpdateSelector,
                _packageUpdater,
                _nukeeperLogger,
                Substitute.For<ISolutionRestore>(),
                Substitute.For<IReporter>()
            );
        }

        private static RepositoryData MakeRepositoryData()
        {
            return new RepositoryData(
                new ForkData(new Uri("http://foo.com"), "me", "test"),
                new ForkData(new Uri("http://foo.com"), "me", "test"));
        }
    }
}
