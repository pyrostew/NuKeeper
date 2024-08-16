using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.NuGetApi;
using NuKeeper.Abstractions.RepositoryInspection;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Inspection.Tests.RepositoryInspection
{
    [TestFixture]
    public class PackageUpdateSetTests
    {
        private readonly PackageSource _source = new("http://someSource");

        [Test]
        public void NullPackageLookupData_IsNotAllowed()
        {
            List<PackageInProject> packages =
            [
                new PackageInProject("foo", "1.0.0", PathToProjectOne())
            ];

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new PackageUpdateSet(null, packages));

            Assert.That(exception.ParamName, Is.EqualTo("packages"));
        }


        [Test]
        public void NullPackageMatch_IsNotAllowed()
        {
            List<PackageInProject> packages =
            [
                new PackageInProject("foo", "1.0.0", PathToProjectOne())
            ];

            PackageLookupResult lookupResult = new(VersionChange.Major, null, null, null);

            _ = Assert.Throws<ArgumentException>(() => new PackageUpdateSet(lookupResult, packages));
        }

        [Test]
        public void NullPackages_IsNotAllowed()
        {
            PackageLookupResult lookupResult = new(VersionChange.Major, LatestFooMetadata(), null, null);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new PackageUpdateSet(lookupResult, null));

            Assert.That(exception.ParamName, Is.EqualTo("currentPackages"));
        }

        [Test]
        public void EmptyPackages_IsNotAllowed()
        {
            PackageLookupResult lookupResult = new(VersionChange.Major, LatestFooMetadata(), null, null);

            ArgumentException exception = Assert.Throws<ArgumentException>(
                () => new PackageUpdateSet(lookupResult, Enumerable.Empty<PackageInProject>()));
            Assert.That(exception.ParamName, Is.EqualTo("currentPackages"));
        }

        [Test]
        public void OneUpdate_IsValid()
        {
            PackageIdentity fooVersionFour = new("foo", VersionFour());
            PackageSearchMetadata highest = new(fooVersionFour, _source, DateTimeOffset.Now, null);

            List<PackageInProject> currentPackages =
            [
                new PackageInProject("foo", "1.0.0", PathToProjectOne())
            ];

            PackageLookupResult lookupResult = new(VersionChange.Major, highest, null, null);
            PackageUpdateSet updates = new(lookupResult, currentPackages);

            Assert.That(updates, Is.Not.Null);

            Assert.That(updates.Packages.Major?.Identity.Version, Is.EqualTo(VersionFour()));
            Assert.That(updates.AllowedChange, Is.EqualTo(VersionChange.Major));

            Assert.That(updates.Selected, Is.Not.Null);
            Assert.That(updates.Selected.Identity, Is.EqualTo(fooVersionFour));
            Assert.That(updates.SelectedId, Is.EqualTo("foo"));
            Assert.That(updates.SelectedVersion, Is.EqualTo(highest.Identity.Version));
            Assert.That(updates.Selected.Source, Is.EqualTo(_source));
        }

        [Test]
        public void OneUpdate_HasCorrectCurrentPackages()
        {
            List<PackageInProject> currentPackages =
            [
                new PackageInProject("foo", "1.0.0", PathToProjectOne())
            ];

            PackageLookupResult lookupResult = new(VersionChange.Major, LatestFooMetadata(), null, null);
            PackageUpdateSet updates = new(lookupResult, currentPackages);

            Assert.That(updates.CurrentPackages, Is.Not.Null);
            Assert.That(updates.CurrentPackages.Count, Is.EqualTo(1));
            Assert.That(updates.CurrentPackages.First().Id, Is.EqualTo("foo"));
        }

        [Test]
        public void TwoUpdates_AreValid()
        {
            PackageIdentity newPackage = LatestVersionOfPackageFoo();

            List<PackageInProject> currentPackages =
            [
                new PackageInProject("foo", "1.0.0", PathToProjectOne()),
                new PackageInProject("foo", "1.0.1", PathToProjectTwo())
            ];

            PackageLookupResult lookupResult = new(VersionChange.Major, LatestFooMetadata(), null, null);
            PackageUpdateSet updates = new(lookupResult, currentPackages);

            Assert.That(updates, Is.Not.Null);
            Assert.That(updates.Selected, Is.Not.Null);
            Assert.That(updates.Selected.Identity, Is.EqualTo(LatestVersionOfPackageFoo()));

            Assert.That(updates.SelectedId, Is.EqualTo("foo"));
            Assert.That(updates.SelectedVersion, Is.EqualTo(newPackage.Version));
        }

        [Test]
        public void TwoUpdates_HaveCorrectCurrentPackages()
        {
            List<PackageInProject> currentPackages =
            [
                new PackageInProject("foo", "1.0.0", PathToProjectOne()),
                new PackageInProject("foo", "1.0.1", PathToProjectTwo())
            ];

            PackageLookupResult lookupResult = new(VersionChange.Major, LatestFooMetadata(), null, null);
            PackageUpdateSet updates = new(lookupResult, currentPackages);

            Assert.That(updates.CurrentPackages, Is.Not.Null);
            List<PackageInProject> currents = updates.CurrentPackages.ToList();

            Assert.That(currents.Count, Is.EqualTo(2));
            Assert.That(currents[0].Id, Is.EqualTo("foo"));
            Assert.That(currents[0].Version, Is.EqualTo(new NuGetVersion("1.0.0")));

            Assert.That(currents[1].Id, Is.EqualTo("foo"));
            Assert.That(currents[1].Version, Is.EqualTo(new NuGetVersion("1.0.1")));
        }

        [Test]
        public void CannotHaveUpdateForDifferentPackageToNewVersion()
        {
            List<PackageInProject> currentPackageBar =
            [
                new PackageInProject("bar", "1.0.0", PathToProjectOne())
            ];

            PackageLookupResult lookupResult = new(VersionChange.Major, LatestFooMetadata(), null, null);

            _ = Assert.Throws<ArgumentException>(() => new PackageUpdateSet(lookupResult, currentPackageBar));
        }

        [Test]
        public void WhenPackageDoesNotMatch_ExceptionMessageContainsMismatchedPackages()
        {
            List<PackageInProject> currentPackages =
            [
                new PackageInProject("bar", "1.0.0", PathToProjectOne()),
                new PackageInProject("bar", "1.0.0", PathToProjectTwo()),
                new PackageInProject("fish", "1.0.0", PathToProjectOne())
            ];

            PackageLookupResult lookupResult = new(VersionChange.Major, LatestFooMetadata(), null, null);
            ArgumentException ex = Assert.Throws<ArgumentException>(() => new PackageUpdateSet(lookupResult, currentPackages));

            Assert.That(ex.Message, Is.EqualTo("Updates must all be for package 'foo', got 'bar, fish'"));
        }

        [Test]
        public void CannotHaveUpdateForDifferentPackagesInCurrentList()
        {
            List<PackageInProject> currentPackagesFooAndBar =
            [
                new PackageInProject("foo", "1.0.0", PathToProjectOne()),
                new PackageInProject("bar", "1.0.0", PathToProjectOne())
            ];

            PackageLookupResult lookupResult = new(VersionChange.Major, LatestFooMetadata(), null, null);
            _ = Assert.Throws<ArgumentException>(() => new PackageUpdateSet(lookupResult, currentPackagesFooAndBar));
        }

        [Test]
        public void CountCurrentVersions_WhenThereIsOneUpdate()
        {
            List<PackageInProject> currentPackages =
            [
                new PackageInProject("foo", "1.0.1", PathToProjectOne())
            ];

            PackageLookupResult lookupResult = new(VersionChange.Major, LatestFooMetadata(), null, null);
            PackageUpdateSet updates = new(lookupResult, currentPackages);

            Assert.That(updates.CountCurrentVersions(), Is.EqualTo(1));
        }

        [Test]
        public void CountCurrentVersions_WhenThereAreTwoIdenticalUpdates()
        {
            List<PackageInProject> currentPackages =
            [
                new PackageInProject("foo", "1.0.1", PathToProjectOne()),
                new PackageInProject("foo", "1.0.1", PathToProjectTwo())
            ];

            PackageLookupResult lookupResult = new(VersionChange.Major, LatestFooMetadata(), null, null);
            PackageUpdateSet updates = new(lookupResult, currentPackages);

            Assert.That(updates.CountCurrentVersions(), Is.EqualTo(1));
        }

        [Test]
        public void CountCurrentVersions_WhenThereAreTwoDifferentUpdates()
        {
            List<PackageInProject> currentPackages =
            [
                new PackageInProject("foo", "1.0.0", PathToProjectOne()),
                new PackageInProject("foo", "1.0.1", PathToProjectTwo())
            ];

            PackageLookupResult lookupResult = new(VersionChange.Major, LatestFooMetadata(), null, null);
            PackageUpdateSet updates = new(lookupResult, currentPackages);

            Assert.That(updates.CountCurrentVersions(), Is.EqualTo(2));
        }

        private static PackageIdentity LatestVersionOfPackageFoo()
        {
            return new PackageIdentity("foo", new NuGetVersion("1.2.3"));
        }

        private PackageSearchMetadata LatestFooMetadata()
        {
            return new PackageSearchMetadata(
                LatestVersionOfPackageFoo(),
                _source, DateTimeOffset.Now, null);
        }

        private static NuGetVersion VersionFour()
        {
            return new NuGetVersion("4.0.0");
        }

        private static PackagePath PathToProjectOne()
        {
            return new PackagePath("c_temp", "projectOne", PackageReferenceType.PackagesConfig);
        }

        private static PackagePath PathToProjectTwo()
        {
            return new PackagePath("c_temp", "projectTwo", PackageReferenceType.PackagesConfig);
        }
    }
}
