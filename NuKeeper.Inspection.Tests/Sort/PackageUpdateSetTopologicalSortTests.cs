using NSubstitute;

using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.NuGetApi;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Inspection.Sort;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Inspection.Tests.Sort
{
    [TestFixture]
    public class PackageUpdateSetTopologicalSortTests
    {
        [Test]
        public void CanSortEmptyList()
        {
            List<PackageUpdateSet> items = [];

            PackageUpdateSetTopologicalSort sorter = new(Substitute.For<INuKeeperLogger>());

            List<PackageUpdateSet> sorted = sorter.Sort(items)
                .ToList();

            Assert.That(sorted, Is.Not.Null);
            Assert.That(sorted, Is.Empty);
        }

        [Test]
        public void CanSortOneItemInList()
        {
            List<PackageUpdateSet> items =
            [
                MakeUpdateSet("foo", "1.2.3")
            ];

            PackageUpdateSetTopologicalSort sorter = new(Substitute.For<INuKeeperLogger>());

            List<PackageUpdateSet> sorted = sorter.Sort(items)
                .ToList();

            AssertIsASortOf(sorted, items);
            Assert.That(sorted[0], Is.EqualTo(items[0]));
        }

        [Test]
        public void CanSortTwoUnrelatedItems()
        {
            List<PackageUpdateSet> items =
            [
                MakeUpdateSet("fish", "1.2.3"),
                MakeUpdateSet("bar", "2.3.4")
            ];

            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();

            PackageUpdateSetTopologicalSort sorter = new(logger);

            List<PackageUpdateSet> sorted = sorter.Sort(items)
                .ToList();

            AssertIsASortOf(sorted, items);
            Assert.That(sorted[0], Is.EqualTo(items[0]));
            Assert.That(sorted[1], Is.EqualTo(items[1]));

            logger.Received(1).Detailed("No dependencies between items, no need to sort on dependencies");
            logger.Received(1).Detailed("Sorted 2 packages by dependencies but no change made");
        }

        [Test]
        public void CanSortTwoRelatedItemsInCorrectOrder()
        {
            PackageUpdateSet fishPackage = MakeUpdateSet("fish", "1.2.3");

            List<PackageUpdateSet> items =
            [
                fishPackage,
                MakeUpdateSet("bar", "2.3.4", DependencyOn(fishPackage))
            ];


            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();

            PackageUpdateSetTopologicalSort sorter = new(logger);

            List<PackageUpdateSet> sorted = sorter.Sort(items)
                .ToList();

            AssertIsASortOf(sorted, items);
            Assert.That(sorted[0], Is.EqualTo(items[0]));
            Assert.That(sorted[1], Is.EqualTo(items[1]));

            logger.DidNotReceive().Detailed("No dependencies between items, no need to sort on dependencies");
            logger.Received(1).Detailed("Sorted 2 packages by dependencies but no change made");
        }

        [Test]
        public void CanSortTwoRelatedItemsinReverseOrder()
        {
            PackageUpdateSet fishPackage = MakeUpdateSet("fish", "1.2.3");

            List<PackageUpdateSet> items =
            [
                MakeUpdateSet("bar", "2.3.4", DependencyOn(fishPackage)),
                fishPackage
            ];


            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();

            PackageUpdateSetTopologicalSort sorter = new(logger);

            List<PackageUpdateSet> sorted = sorter.Sort(items)
                .ToList();

            AssertIsASortOf(sorted, items);

            Assert.That(sorted[0], Is.EqualTo(items[1]));
            Assert.That(sorted[1], Is.EqualTo(items[0]));

            logger.DidNotReceive().Detailed("No dependencies between items, no need to sort on dependencies");
            logger.Received(1).Detailed("Resorted 2 packages by dependencies, first change is fish moved to position 0 from 1.");
        }

        [Test]
        public void CanSortThreeRelatePackages()
        {
            PackageUpdateSet apexPackage = MakeUpdateSet("apex", "1.2.3");

            List<PackageUpdateSet> items =
            [
                MakeUpdateSet("foo", "1.2.3", DependencyOn(apexPackage)),
                apexPackage,
                MakeUpdateSet("bar", "2.3.4", DependencyOn(apexPackage)),
            ];


            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();

            PackageUpdateSetTopologicalSort sorter = new(logger);

            List<PackageUpdateSet> sorted = sorter.Sort(items)
                .ToList();


            AssertIsASortOf(sorted, items);
            Assert.That(sorted[0], Is.EqualTo(apexPackage));
        }

        [Test]
        public void CanSortWithCycle()
        {
            PackageUpdateSet pakageOne = MakeUpdateSet("one", "1.2.3");
            PackageUpdateSet packageAlpha = MakeUpdateSet("alpha", "2.3.4", DependencyOn(pakageOne));
            pakageOne = MakeUpdateSet("one", "1.2.3", DependencyOn(packageAlpha));

            List<PackageUpdateSet> items =
            [
                pakageOne,
                packageAlpha
            ];

            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();

            PackageUpdateSetTopologicalSort sorter = new(logger);

            List<PackageUpdateSet> sorted = sorter.Sort(items)
                .ToList();

            AssertIsASortOf(sorted, items);
            logger.Received(1).Minimal(Arg.Is<string>(
                s => s.StartsWith("Cannot sort by dependencies, cycle found at item", StringComparison.OrdinalIgnoreCase)));
        }


        private static void AssertIsASortOf(List<PackageUpdateSet> sorted, List<PackageUpdateSet> original)
        {
            Assert.That(sorted, Is.Not.Null);
            Assert.That(sorted, Is.Not.Empty);
            Assert.That(sorted.Count, Is.EqualTo(original.Count));
            Assert.That(original, Is.EquivalentTo(sorted));
        }

        private static PackageDependency DependencyOn(PackageUpdateSet package)
        {
            return new PackageDependency(package.SelectedId, new VersionRange(package.SelectedVersion));
        }

        private static PackageUpdateSet MakeUpdateSet(string packageId, string packageVersion, PackageDependency upstream = null)
        {
            List<PackageInProject> currentPackages =
            [
                new PackageInProject(packageId, packageVersion, PathToProjectOne()),
                new PackageInProject(packageId, packageVersion, PathToProjectTwo())
            ];

            PackageSearchMetadata majorUpdate = Metadata(packageId, packageVersion, upstream);

            PackageLookupResult lookupResult = new(VersionChange.Major,
                majorUpdate, null, null);
            PackageUpdateSet updates = new(lookupResult, currentPackages);

            return updates;
        }

        private static PackagePath PathToProjectOne()
        {
            return new PackagePath("c_temp", "projectOne", PackageReferenceType.PackagesConfig);
        }

        private static PackagePath PathToProjectTwo()
        {
            return new PackagePath("c_temp", "projectTwo", PackageReferenceType.PackagesConfig);
        }

        private static PackageSearchMetadata Metadata(string packageId, string version, PackageDependency upstream)
        {
            List<PackageDependency> upstreams = upstream != null ? [upstream] : [];

            return new PackageSearchMetadata(
                new PackageIdentity(packageId, new NuGetVersion(version)),
                new PackageSource(NuGetConstants.V3FeedUrl),
                DateTimeOffset.Now, upstreams);
        }

    }
}
