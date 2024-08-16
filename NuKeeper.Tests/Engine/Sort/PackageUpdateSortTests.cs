using NSubstitute;

using NuGet.Packaging.Core;
using NuGet.Versioning;

using NuKeeper.Abstractions;
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
    public class PackageUpdateSortTests
    {
        private static readonly DateTimeOffset StandardPublishedDate = new(2018, 2, 19, 11, 12, 7, TimeSpan.Zero);

        [Test]
        public void CanSortWhenListIsEmpty()
        {
            List<PackageUpdateSet> items = [];

            List<PackageUpdateSet> output = Sort(items);

            Assert.That(output, Is.Not.Null);
        }

        [Test]
        public void CanSortOneItem()
        {
            List<PackageUpdateSet> items = OnePackageUpdateSet(1)
                .InList();

            List<PackageUpdateSet> output = Sort(items);

            Assert.That(output, Is.Not.Null);
            Assert.That(output.Count, Is.EqualTo(1));
            Assert.That(output[0], Is.EqualTo(items[0]));
        }

        [Test]
        public void CanSortTwoItems()
        {
            List<PackageUpdateSet> items =
            [
                OnePackageUpdateSet(1),
                OnePackageUpdateSet(2)
            ];

            List<PackageUpdateSet> output = Sort(items);

            Assert.That(output, Is.Not.Null);
            Assert.That(output.Count, Is.EqualTo(2));
        }

        [Test]
        public void CanSortThreeItems()
        {
            List<PackageUpdateSet> items =
            [
                OnePackageUpdateSet(1),
                OnePackageUpdateSet(2),
                OnePackageUpdateSet(3),
            ];

            List<PackageUpdateSet> output = Sort(items);

            Assert.That(output, Is.Not.Null);
            Assert.That(output.Count, Is.EqualTo(3));
        }

        [Test]
        public void TwoPackageVersionsIsSortedToTop()
        {
            PackageUpdateSet twoVersions = MakeTwoProjectVersions();
            List<PackageUpdateSet> items =
            [
                OnePackageUpdateSet(3),
                OnePackageUpdateSet(4),
                twoVersions
            ];

            List<PackageUpdateSet> output = Sort(items);

            Assert.That(output, Is.Not.Null);
            Assert.That(output[0], Is.EqualTo(twoVersions));
        }

        [Test]
        public void WillSortByProjectCount()
        {
            List<PackageUpdateSet> items =
            [
                OnePackageUpdateSet(1),
                OnePackageUpdateSet(2),
                OnePackageUpdateSet(3),
            ];

            List<PackageUpdateSet> output = Sort(items);

            Assert.That(output.Count, Is.EqualTo(3));
            Assert.That(output[0].CurrentPackages.Count, Is.EqualTo(3));
            Assert.That(output[1].CurrentPackages.Count, Is.EqualTo(2));
            Assert.That(output[2].CurrentPackages.Count, Is.EqualTo(1));
        }

        [Test]
        public void WillSortByProjectVersionsOverProjectCount()
        {
            PackageUpdateSet twoVersions = MakeTwoProjectVersions();
            List<PackageUpdateSet> items =
            [
                OnePackageUpdateSet(10),
                OnePackageUpdateSet(20),
                twoVersions,
            ];

            List<PackageUpdateSet> output = Sort(items);

            Assert.That(output.Count, Is.EqualTo(3));
            Assert.That(output[0], Is.EqualTo(twoVersions));
            Assert.That(output[1].CurrentPackages.Count, Is.EqualTo(20));
            Assert.That(output[2].CurrentPackages.Count, Is.EqualTo(10));
        }

        [Test]
        public void WillSortByBiggestVersionChange()
        {
            List<PackageUpdateSet> items =
            [
                PackageChange("1.2.4", "1.2.3"),
                PackageChange("2.0.0", "1.2.3"),
                PackageChange("1.3.0", "1.2.3")
            ];

            List<PackageUpdateSet> output = Sort(items);

            Assert.That(output.Count, Is.EqualTo(3));
            Assert.That(SelectedVersion(output[0]), Is.EqualTo("2.0.0"));
            Assert.That(SelectedVersion(output[1]), Is.EqualTo("1.3.0"));
            Assert.That(SelectedVersion(output[2]), Is.EqualTo("1.2.4"));
        }

        [Test]
        public void WillSortByGettingOutOfBetaFirst()
        {
            List<PackageUpdateSet> items =
            [
                PackageChange("2.0.0", "1.2.3"),
                PackageChange("1.2.4", "1.2.3-beta1"),
                PackageChange("1.3.0-pre-2", "1.2.3-beta1")
            ];

            List<PackageUpdateSet> output = Sort(items);

            Assert.That(output.Count, Is.EqualTo(3));
            Assert.That(SelectedVersion(output[0]), Is.EqualTo("1.2.4"));
            Assert.That(SelectedVersion(output[1]), Is.EqualTo("2.0.0"));
            Assert.That(SelectedVersion(output[2]), Is.EqualTo("1.3.0-pre-2"));
        }


        [Test]
        public void WillSortByOldestFirstOverPatchVersionIncrement()
        {
            List<PackageUpdateSet> items =
            [
                PackageChange("1.2.6", "1.2.3", StandardPublishedDate),
                PackageChange("1.2.5", "1.2.3", StandardPublishedDate.AddYears(-1)),
                PackageChange("1.2.4", "1.2.3", StandardPublishedDate.AddYears(-2))
            ];

            List<PackageUpdateSet> output = Sort(items);

            Assert.That(output.Count, Is.EqualTo(3));
            Assert.That(SelectedVersion(output[0]), Is.EqualTo("1.2.4"));
            Assert.That(SelectedVersion(output[1]), Is.EqualTo("1.2.5"));
            Assert.That(SelectedVersion(output[2]), Is.EqualTo("1.2.6"));
        }

        private static string SelectedVersion(PackageUpdateSet packageUpdateSet)
        {
            return packageUpdateSet.Selected.Identity.Version.ToString();
        }

        private static PackageInProject MakePackageInProjectFor(PackageIdentity package)
        {
            PackagePath path = new(
                Path.GetTempPath(),
                Path.Combine("folder", "src", "project1", "packages.config"),
                PackageReferenceType.PackagesConfig);
            return new PackageInProject(package.Id, package.Version.ToString(), path);
        }

        private static PackageUpdateSet OnePackageUpdateSet(int projectCount)
        {
            PackageIdentity newPackage = new("foo.bar", new NuGetVersion("1.4.5"));
            PackageIdentity package = new("foo.bar", new NuGetVersion("1.2.3"));

            List<PackageInProject> projects = [];
            foreach (int i in Enumerable.Range(1, projectCount))
            {
                projects.Add(MakePackageInProjectFor(package));
            }

            return PackageUpdates.UpdateSetFor(newPackage, projects.ToArray());
        }

        private static PackageUpdateSet MakeTwoProjectVersions()
        {
            PackageIdentity newPackage = new("foo.bar", new NuGetVersion("1.4.5"));

            PackageIdentity package123 = new("foo.bar", new NuGetVersion("1.2.3"));
            PackageIdentity package124 = new("foo.bar", new NuGetVersion("1.2.4"));
            List<PackageInProject> projects =
            [
                MakePackageInProjectFor(package123),
                MakePackageInProjectFor(package124),
            ];

            return PackageUpdates.UpdateSetFor(newPackage, projects.ToArray());
        }

        private static PackageUpdateSet PackageChange(string newVersion, string oldVersion, DateTimeOffset? publishedDate = null)
        {
            PackageIdentity newPackage = new("foo.bar", new NuGetVersion(newVersion));
            PackageIdentity oldPackage = new("foo.bar", new NuGetVersion(oldVersion));

            if (!publishedDate.HasValue)
            {
                publishedDate = StandardPublishedDate;
            }

            List<PackageInProject> projects =
            [
                MakePackageInProjectFor(oldPackage)
            ];

            return PackageUpdates.UpdateSetFor(newPackage, publishedDate.Value, projects.ToArray());
        }

        private static List<PackageUpdateSet> Sort(IReadOnlyCollection<PackageUpdateSet> input)
        {
            PackageUpdateSetSort sorter = new(Substitute.For<INuKeeperLogger>());
            return sorter.Sort(input)
                .ToList();
        }
    }
}
