using NSubstitute;

using NuGet.Packaging.Core;
using NuGet.Versioning;

using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Inspection.Sort;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuKeeper.Tests.Engine.Sort
{
    [TestFixture]
    public class PackageUpdateSortDependencyTests
    {
        private static readonly DateTimeOffset StandardPublishedDate = new(2018, 2, 19, 11, 12, 7, TimeSpan.Zero);

        [Test]
        public void WillSortByProjectCountWhenThereAreNoDeps()
        {
            PackageUpdateSet upstream = OnePackageUpdateSet("upstream", 1, null);
            PackageUpdateSet downstream = OnePackageUpdateSet("downstream", 2, null);

            List<PackageUpdateSet> items =
            [
                downstream,
                upstream
            ];

            List<PackageUpdateSet> output = Sort(items);

            Assert.That(output.Count, Is.EqualTo(2));
            Assert.That(output[0].SelectedId, Is.EqualTo("downstream"));
            Assert.That(output[1].SelectedId, Is.EqualTo("upstream"));
        }

        [Test]
        public void WillSortByDependencyWhenItExists()
        {
            PackageUpdateSet upstream = OnePackageUpdateSet("upstream", 1, null);
            List<PackageDependency> depOnUpstream =
            [
                new PackageDependency("upstream", VersionRange.All)
            ];

            PackageUpdateSet downstream = OnePackageUpdateSet("downstream", 2, depOnUpstream);

            List<PackageUpdateSet> items =
            [
                downstream,
                upstream
            ];

            List<PackageUpdateSet> output = Sort(items);

            Assert.That(output.Count, Is.EqualTo(2));
            Assert.That(output[0].SelectedId, Is.EqualTo("upstream"));
            Assert.That(output[1].SelectedId, Is.EqualTo("downstream"));
        }

        [Test]
        public void WillSortSecondAndThirdByDependencyWhenItExists()
        {
            PackageUpdateSet upstream = OnePackageUpdateSet("upstream", 1, null);
            List<PackageDependency> depOnUpstream =
            [
                new PackageDependency("upstream", VersionRange.All)
            ];

            PackageUpdateSet downstream = OnePackageUpdateSet("downstream", 2, depOnUpstream);

            List<PackageUpdateSet> items =
            [
                OnePackageUpdateSet("nodeps", 3, null),
                downstream,
                upstream
            ];

            List<PackageUpdateSet> output = Sort(items);

            Assert.That(output.Count, Is.EqualTo(3));
            Assert.That(output[0].SelectedId, Is.EqualTo("nodeps"));
            Assert.That(output[1].SelectedId, Is.EqualTo("upstream"));
            Assert.That(output[2].SelectedId, Is.EqualTo("downstream"));
        }

        [Test]
        public void SortWithThreeLevels()
        {
            PackageUpdateSet level1 = OnePackageUpdateSet("l1", 1, null);
            List<PackageDependency> depOnLevel1 =
            [
                new PackageDependency("l1", VersionRange.All)
            ];

            PackageUpdateSet level2 = OnePackageUpdateSet("l2", 2, depOnLevel1);
            List<PackageDependency> depOnLevel2 =
            [
                new PackageDependency("l2", VersionRange.All)
            ];

            PackageUpdateSet level3 = OnePackageUpdateSet("l3", 2, depOnLevel2);

            List<PackageUpdateSet> items =
            [
                level3,
                level2,
                level1
            ];

            List<PackageUpdateSet> output = Sort(items);

            Assert.That(output.Count, Is.EqualTo(3));
            Assert.That(output[0].SelectedId, Is.EqualTo("l1"));
            Assert.That(output[1].SelectedId, Is.EqualTo("l2"));
            Assert.That(output[2].SelectedId, Is.EqualTo("l3"));
        }

        [Test]
        public void SortWhenTwoPackagesDependOnSameUpstream()
        {
            PackageUpdateSet level1 = OnePackageUpdateSet("l1", 1, null);
            List<PackageDependency> depOnLevel1 =
            [
                new PackageDependency("l1", VersionRange.All)
            ];

            PackageUpdateSet level2A = OnePackageUpdateSet("l2a", 2, depOnLevel1);
            PackageUpdateSet level2B = OnePackageUpdateSet("l2b", 2, depOnLevel1);

            List<PackageUpdateSet> items =
            [
                level2A,
                level2B,
                level1
            ];

            List<PackageUpdateSet> output = Sort(items);

            Assert.That(output.Count, Is.EqualTo(3));
            Assert.That(output[0].SelectedId, Is.EqualTo("l1"));

            // prior ordering should be preserved here
            Assert.That(output[1].SelectedId, Is.EqualTo("l2a"));
            Assert.That(output[2].SelectedId, Is.EqualTo("l2b"));
        }

        [Test]
        public void SortWhenDependenciesAreCircular()
        {
            List<PackageDependency> depOnA =
            [
                new PackageDependency("PackageA", VersionRange.All)
            ];

            List<PackageDependency> depOnB =
            [
                new PackageDependency("PackageB", VersionRange.All)
            ];

            // circular dependencies should not happen, but probably will
            // do not break
            PackageUpdateSet packageA = OnePackageUpdateSet("PackageA", 1, depOnB);
            PackageUpdateSet packageB = OnePackageUpdateSet("PackageB", 1, depOnA);


            List<PackageUpdateSet> items =
            [
                packageA,
                packageB
            ];

            List<PackageUpdateSet> output = Sort(items);

            Assert.That(output.Count, Is.EqualTo(2));
            Assert.That(output[0].SelectedId, Is.EqualTo("PackageA"));
            Assert.That(output[1].SelectedId, Is.EqualTo("PackageB"));
        }

        private static PackageUpdateSet OnePackageUpdateSet(string packageName, int projectCount,
            List<PackageDependency> deps)
        {
            PackageIdentity newPackage = new(packageName, new NuGetVersion("1.4.5"));
            PackageIdentity package = new(packageName, new NuGetVersion("1.2.3"));

            List<PackageInProject> projects = [];
            foreach (int i in Enumerable.Range(1, projectCount))
            {
                projects.Add(MakePackageInProjectFor(package));
            }

            return PackageUpdates.For(newPackage, StandardPublishedDate, projects, deps);
        }

        private static PackageInProject MakePackageInProjectFor(PackageIdentity package)
        {
            PackagePath path = new(
                Path.GetTempPath(),
                Path.Combine("folder", "src", "project1", "packages.config"),
                PackageReferenceType.PackagesConfig);
            return new PackageInProject(package.Id, package.Version.ToString(), path);
        }

        private static List<PackageUpdateSet> Sort(IReadOnlyCollection<PackageUpdateSet> input)
        {
            PackageUpdateSetSort sorter = new(Substitute.For<INuKeeperLogger>());
            return sorter.Sort(input)
                .ToList();
        }
    }
}
