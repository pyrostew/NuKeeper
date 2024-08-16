using NSubstitute;

using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Inspections.Files;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.NuGetApi;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Inspection.NuGetApi;
using NuKeeper.Inspection.RepositoryInspection;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NuKeeper.Inspection.Tests
{
    [TestFixture]
    public class UpdateFinderTests
    {
        [Test]
        public async Task FindWithoutResults()
        {
            IRepositoryScanner scanner = Substitute.For<IRepositoryScanner>();
            IPackageUpdatesLookup updater = Substitute.For<IPackageUpdatesLookup>();
            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();
            IFolder folder = Substitute.For<IFolder>();

            ReturnsUpdateSetForEachPackage(updater);

            UpdateFinder finder = new(scanner, updater, logger);

            IReadOnlyCollection<PackageUpdateSet> results = await finder.FindPackageUpdateSets(
                folder, NuGetSources.GlobalFeed, VersionChange.Major, UsePrerelease.FromPrerelease);

            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(0));

            logger
                .DidNotReceive()
                .Error(Arg.Any<string>(), Arg.Any<Exception>());
        }

        [Test]
        public async Task FindWithOneResult()
        {
            IRepositoryScanner scanner = Substitute.For<IRepositoryScanner>();
            IPackageUpdatesLookup updater = Substitute.For<IPackageUpdatesLookup>();
            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();
            IFolder folder = Substitute.For<IFolder>();

            PackageInProject pip = BuildPackageInProject("somePackage");

            _ = scanner.FindAllNuGetPackages(Arg.Any<IFolder>())
                .Returns(new List<PackageInProject> { pip });

            ReturnsUpdateSetForEachPackage(updater);

            UpdateFinder finder = new(scanner, updater, logger);

            IReadOnlyCollection<PackageUpdateSet> results = await finder.FindPackageUpdateSets(
                folder, NuGetSources.GlobalFeed, VersionChange.Major, UsePrerelease.FromPrerelease);

            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results.First().SelectedId, Is.EqualTo("somePackage"));

            logger
                .DidNotReceive()
                .Error(Arg.Any<string>(), Arg.Any<Exception>());
        }

        [Test]
        public async Task FindOneFilteredIncludeResult()
        {
            IRepositoryScanner scanner = Substitute.For<IRepositoryScanner>();
            IPackageUpdatesLookup updater = Substitute.For<IPackageUpdatesLookup>();
            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();
            IFolder folder = Substitute.For<IFolder>();

            PackageInProject pip = BuildPackageInProject("somePackage");
            PackageInProject anotherPackage = BuildPackageInProject("anotherPackage");

            _ = scanner.FindAllNuGetPackages(Arg.Any<IFolder>())
                .Returns(new List<PackageInProject> { pip, anotherPackage, anotherPackage, anotherPackage });

            ReturnsUpdateSetForEachPackage(updater);

            UpdateFinder finder = new(scanner, updater, logger);

            IReadOnlyCollection<PackageUpdateSet> results = await finder.FindPackageUpdateSets(
                folder, NuGetSources.GlobalFeed, VersionChange.Major, UsePrerelease.FromPrerelease, new Regex("^somePackage"), null);

            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results.First().SelectedId, Is.EqualTo("somePackage"));

            logger
                .DidNotReceive()
                .Error(Arg.Any<string>(), Arg.Any<Exception>());
        }

        [Test]
        public async Task FindTwoFilteredExcludeIncludeResults()
        {
            IRepositoryScanner scanner = Substitute.For<IRepositoryScanner>();
            IPackageUpdatesLookup updater = Substitute.For<IPackageUpdatesLookup>();
            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();
            IFolder folder = Substitute.For<IFolder>();

            PackageInProject pip = BuildPackageInProject("somePackage");
            PackageInProject anotherPackage = BuildPackageInProject("anotherPackage");
            PackageInProject andAnotherPackage = BuildPackageInProject("andAnotherPackage");

            _ = scanner.FindAllNuGetPackages(Arg.Any<IFolder>())
                .Returns(new List<PackageInProject> { pip, anotherPackage, andAnotherPackage });

            ReturnsUpdateSetForEachPackage(updater);

            UpdateFinder finder = new(scanner, updater, logger);

            IReadOnlyCollection<PackageUpdateSet> results = await finder.FindPackageUpdateSets(
                folder, NuGetSources.GlobalFeed, VersionChange.Major, UsePrerelease.FromPrerelease, new Regex("^andAnotherPackage"), new Regex("^anotherPackage"));

            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results.First().SelectedId, Is.EqualTo("andAnotherPackage"));

            logger
                .DidNotReceive()
                .Error(Arg.Any<string>(), Arg.Any<Exception>());
        }

        [Test]
        public async Task FindOneFilteredExcludeResult()
        {
            IRepositoryScanner scanner = Substitute.For<IRepositoryScanner>();
            IPackageUpdatesLookup updater = Substitute.For<IPackageUpdatesLookup>();
            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();
            IFolder folder = Substitute.For<IFolder>();

            PackageInProject pip = BuildPackageInProject("somePackage");
            PackageInProject anotherPackage = BuildPackageInProject("anotherPackage");

            _ = scanner.FindAllNuGetPackages(Arg.Any<IFolder>())
                .Returns(new List<PackageInProject> { pip, anotherPackage, anotherPackage, anotherPackage });

            ReturnsUpdateSetForEachPackage(updater);

            UpdateFinder finder = new(scanner, updater, logger);

            IReadOnlyCollection<PackageUpdateSet> results = await finder.FindPackageUpdateSets(
                folder, NuGetSources.GlobalFeed, VersionChange.Major, UsePrerelease.FromPrerelease, null, new Regex("^anotherPackage"));

            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results.First().SelectedId, Is.EqualTo("somePackage"));

            logger
                .DidNotReceive()
                .Error(Arg.Any<string>(), Arg.Any<Exception>());
        }

        [Test]
        public async Task FindSkipsMetapackageResult()
        {
            IRepositoryScanner scanner = Substitute.For<IRepositoryScanner>();
            IPackageUpdatesLookup updater = Substitute.For<IPackageUpdatesLookup>();
            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();
            IFolder folder = Substitute.For<IFolder>();

            PackageInProject pip = BuildPackageInProject("somePackage");
            PackageInProject aspnetCore = BuildPackageInProject("Microsoft.AspNetCore.App");

            _ = scanner.FindAllNuGetPackages(Arg.Any<IFolder>())
                .Returns(new List<PackageInProject> { pip, aspnetCore });

            ReturnsUpdateSetForEachPackage(updater);

            UpdateFinder finder = new(scanner, updater, logger);

            IReadOnlyCollection<PackageUpdateSet> results = await finder.FindPackageUpdateSets(
                folder, NuGetSources.GlobalFeed, VersionChange.Major, UsePrerelease.FromPrerelease);

            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results.First().SelectedId, Is.EqualTo("somePackage"));

            logger
                .Received(1)
                .Error(Arg.Is<string>(
                    s => s.StartsWith("Metapackage 'Microsoft.AspNetCore.App'", StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        public async Task FindSkipsBothMetapackageResult()
        {
            IRepositoryScanner scanner = Substitute.For<IRepositoryScanner>();
            IPackageUpdatesLookup updater = Substitute.For<IPackageUpdatesLookup>();
            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();
            IFolder folder = Substitute.For<IFolder>();

            PackageInProject aspnetCoreAll = BuildPackageInProject("Microsoft.AspNetCore.All");
            PackageInProject pip = BuildPackageInProject("somePackage");
            PackageInProject aspnetCoreApp = BuildPackageInProject("Microsoft.AspNetCore.App");

            _ = scanner.FindAllNuGetPackages(Arg.Any<IFolder>())
                .Returns(new List<PackageInProject> { aspnetCoreAll, pip, aspnetCoreApp });

            ReturnsUpdateSetForEachPackage(updater);

            UpdateFinder finder = new(scanner, updater, logger);

            IReadOnlyCollection<PackageUpdateSet> results = await finder.FindPackageUpdateSets(
                folder, NuGetSources.GlobalFeed, VersionChange.Major, UsePrerelease.FromPrerelease);

            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results.First().SelectedId, Is.EqualTo("somePackage"));

            logger
                .Received(1)
                .Error(Arg.Is<string>(s => s.StartsWith("Metapackage 'Microsoft.AspNetCore.App'", StringComparison.OrdinalIgnoreCase)));
            logger
                .Received(1)
                .Error(Arg.Is<string>(s => s.StartsWith("Metapackage 'Microsoft.AspNetCore.All'", StringComparison.OrdinalIgnoreCase)));
        }

        private void ReturnsUpdateSetForEachPackage(IPackageUpdatesLookup updater)
        {
            _ = updater.FindUpdatesForPackages(
                    Arg.Any<IReadOnlyCollection<PackageInProject>>(),
                    Arg.Any<NuGetSources>(),
                    Arg.Any<VersionChange>(),
                    Arg.Any<UsePrerelease>())
                .Returns(a => a.ArgAt<IReadOnlyCollection<PackageInProject>>(0)
                    .Select(BuildPackageUpdateSet)
                    .ToList());
        }

        private static PackageInProject BuildPackageInProject(string packageName)
        {
            PackagePath path = new("c:\\temp", "folder\\src\\project1\\packages.config",
                PackageReferenceType.PackagesConfig);
            return new PackageInProject(packageName, "1.1.0", path);
        }

        private PackageUpdateSet BuildPackageUpdateSet(PackageInProject pip)
        {
            PackageIdentity package = new(pip.Id, new NuGetVersion("1.4.5"));
            PackageSearchMetadata latest = new(package, new PackageSource("http://none"), null, null);

            PackageLookupResult updates = new(VersionChange.Major, latest, null, null);

            return new PackageUpdateSet(updates, new List<PackageInProject> { pip });
        }
    }
}
