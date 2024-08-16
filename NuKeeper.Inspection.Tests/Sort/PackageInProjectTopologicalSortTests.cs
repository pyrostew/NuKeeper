using NSubstitute;

using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Inspection.Sort;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuKeeper.Inspection.Tests.Sort
{
    [TestFixture]
    public class PackageInProjectTopologicalSortTests
    {
        [Test]
        public void CanSortEmptyList()
        {
            List<PackageInProject> items = [];

            PackageInProjectTopologicalSort sorter = new(Substitute.For<INuKeeperLogger>());

            List<PackageInProject> sorted = sorter.Sort(items)
                .ToList();

            Assert.That(sorted, Is.Not.Null);
            Assert.That(sorted, Is.Empty);
        }

        [Test]
        public void CanSortOneItem()
        {
            List<PackageInProject> items =
            [
                PackageFor("foo", "1.2.3", "bar{sep}fish.csproj"),
            ];

            PackageInProjectTopologicalSort sorter = new(Substitute.For<INuKeeperLogger>());

            List<PackageInProject> sorted = sorter.Sort(items)
                .ToList();

            AssertIsASortOf(sorted, items);
            Assert.That(sorted[0], Is.EqualTo(items[0]));
        }

        [Test]
        public void CanSortTwoUnrelatedItems()
        {
            List<PackageInProject> items =
            [
                PackageFor("foo", "1.2.3", "bar{sep}fish.csproj"),
                PackageFor("bar", "2.3.4", "project2{sep}p2.csproj")
            ];

            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();

            PackageInProjectTopologicalSort sorter = new(logger);

            List<PackageInProject> sorted = sorter.Sort(items)
                .ToList();

            AssertIsASortOf(sorted, items);
            logger.Received(1).Detailed("No dependencies between items, no need to sort on dependencies");
        }

        [Test]
        public void CanSortTwoRelatedItemsinCorrectOrder()
        {
            PackageInProject aProj = PackageFor("foo", "1.2.3", "someproject{sep}someproject.csproj");
            PackageInProject testProj = PackageFor("bar", "2.3.4", "someproject.tests{sep}someproject.tests.csproj", aProj);

            List<PackageInProject> items =
            [
                testProj,
                aProj
            ];

            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();

            PackageInProjectTopologicalSort sorter = new(logger);

            List<PackageInProject> sorted = sorter.Sort(items)
                .ToList();

            AssertIsASortOf(sorted, items);
            Assert.That(sorted[0], Is.EqualTo(items[0]));
            Assert.That(sorted[1], Is.EqualTo(items[1]));

            logger.DidNotReceive().Detailed("No dependencies between items, no need to sort on dependencies");
            logger.Received(1).Detailed("Sorted 2 projects by dependencies but no change made");
        }

        [Test]
        public void CanSortTwoRelatedItemsinReverseOrder()
        {
            PackageInProject aProj = PackageFor("foo", "1.2.3", "someproject{sep}someproject.csproj");
            PackageInProject testProj = PackageFor("bar", "2.3.4", "someproject.tests{sep}someproject.tests.csproj", aProj);

            List<PackageInProject> items =
            [
                aProj,
                testProj
            ];

            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();

            PackageInProjectTopologicalSort sorter = new(logger);

            List<PackageInProject> sorted = sorter.Sort(items)
                .ToList();

            AssertIsASortOf(sorted, items);
            Assert.That(sorted[0], Is.EqualTo(testProj));
            Assert.That(sorted[1], Is.EqualTo(aProj));

            logger.Received(1).Detailed(Arg.Is<string>(s =>
                s.StartsWith("Resorted 2 projects by dependencies,", StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        public void CanSortWithCycle()
        {
            PackageInProject aProj = PackageFor("foo", "1.2.3", "someproject{sep}someproject.csproj");
            PackageInProject testProj = PackageFor("bar", "2.3.4", "someproject.tests{sep}someproject.tests.csproj", aProj);
            // fake a circular ref - aproj is a new object but the same file path as above
            aProj = PackageFor("foo", "1.2.3", "someproject{sep}someproject.csproj", testProj);

            List<PackageInProject> items =
            [
                aProj,
                testProj
            ];

            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();

            PackageInProjectTopologicalSort sorter = new(logger);

            List<PackageInProject> sorted = sorter.Sort(items)
                .ToList();

            AssertIsASortOf(sorted, items);
            logger.Received(1).Minimal(Arg.Is<string>(
                s => s.StartsWith("Cannot sort by dependencies, cycle found at item", StringComparison.OrdinalIgnoreCase)));
        }

        private static void AssertIsASortOf(List<PackageInProject> sorted, List<PackageInProject> original)
        {
            Assert.That(sorted, Is.Not.Null);
            Assert.That(sorted, Is.Not.Empty);
            Assert.That(sorted.Count, Is.EqualTo(original.Count));
            Assert.That(original, Is.EquivalentTo(sorted));
        }

        private static PackageInProject PackageFor(string packageId, string packageVersion,
            string relativePath, PackageInProject refProject = null)
        {
            relativePath = relativePath.Replace("{sep}", $"{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
            string basePath = "c_temp" + Path.DirectorySeparatorChar + "test";

            List<string> refs = refProject != null ? [refProject.Path.FullName] : [];

            PackageVersionRange packageVersionRange = PackageVersionRange.Parse(packageId, packageVersion);

            return new PackageInProject(packageVersionRange,
                new PackagePath(basePath, relativePath, PackageReferenceType.ProjectFile),
                refs);
        }
    }
}
