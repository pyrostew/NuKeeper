using NSubstitute;

using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.NuGetApi;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Update.Selection;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Update.Tests
{
    [TestFixture]
    public class UpdateSelectionTests
    {
        [Test]
        public void WhenThereAreNoInputs_NoTargetsOut()
        {
            List<PackageUpdateSet> updateSets = [];

            IUpdateSelection target = CreateUpdateSelection();

            IReadOnlyCollection<PackageUpdateSet> results = target.Filter(updateSets, OneTargetSelection());

            Assert.That(results, Is.Not.Null);
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void WhenThereIsOneInput_ItIsTheTarget()
        {
            List<PackageUpdateSet> updateSets = [UpdateFooFromOneVersion()];

            IUpdateSelection target = CreateUpdateSelection();

            IReadOnlyCollection<PackageUpdateSet> results = target.Filter(updateSets, OneTargetSelection());

            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results.First().SelectedId, Is.EqualTo("foo"));
        }

        [Test]
        public void WhenThereAreTwoInputs_FirstIsTheTarget()
        {
            List<PackageUpdateSet> updateSets =
            [
                UpdateFooFromOneVersion(),
                UpdateBarFromTwoVersions()
            ];

            IUpdateSelection target = CreateUpdateSelection();

            IReadOnlyCollection<PackageUpdateSet> results = target.Filter(updateSets, OneTargetSelection());

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results.First().SelectedId, Is.EqualTo("foo"));
        }

        [Test]
        public void WhenThePackageIsNotOldEnough()
        {
            List<PackageUpdateSet> updateSets =
            [
                UpdateFooFromOneVersion()
            ];

            IUpdateSelection target = CreateUpdateSelection();
            FilterSettings settings = MinAgeTargetSelection(TimeSpan.FromDays(7));

            IReadOnlyCollection<PackageUpdateSet> results = target.Filter(updateSets, settings);

            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public void WhenTheFirstPackageIsNotOldEnough()
        {
            List<PackageUpdateSet> updateSets =
            [
                UpdateFooFromOneVersion(TimeSpan.FromDays(6)),
                UpdateBarFromTwoVersions(TimeSpan.FromDays(8))
            ];

            IUpdateSelection target = CreateUpdateSelection();
            FilterSettings settings = MinAgeTargetSelection(TimeSpan.FromDays(7));

            IReadOnlyCollection<PackageUpdateSet> results = target.Filter(updateSets, settings);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results.First().SelectedId, Is.EqualTo("bar"));
        }

        [Test]
        public void WhenMinAgeIsLowBothPackagesAreIncluded()
        {
            List<PackageUpdateSet> updateSets =
            [
                UpdateFooFromOneVersion(TimeSpan.FromDays(6)),
                UpdateBarFromTwoVersions(TimeSpan.FromDays(8))
            ];

            IUpdateSelection target = CreateUpdateSelection();
            FilterSettings settings = MinAgeTargetSelection(TimeSpan.FromHours(12));

            IReadOnlyCollection<PackageUpdateSet> results = target.Filter(updateSets, settings);

            Assert.That(results.Count, Is.EqualTo(2));
        }

        [Test]
        public void WhenMinAgeIsHighNeitherPackagesAreIncluded()
        {
            List<PackageUpdateSet> updateSets =
            [
                UpdateFooFromOneVersion(TimeSpan.FromDays(6)),
                UpdateBarFromTwoVersions(TimeSpan.FromDays(8))
            ];

            IUpdateSelection target = CreateUpdateSelection();
            FilterSettings settings = MinAgeTargetSelection(TimeSpan.FromDays(10));

            IReadOnlyCollection<PackageUpdateSet> results = target.Filter(updateSets, settings);

            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public void WhenThePackageIsFromTheFuture()
        {
            List<PackageUpdateSet> updateSets =
            [
                UpdateFooFromOneVersion(TimeSpan.FromMinutes(-5))
            ];

            IUpdateSelection target = CreateUpdateSelection();
            FilterSettings settings = MinAgeTargetSelection(TimeSpan.FromDays(7));

            IReadOnlyCollection<PackageUpdateSet> results = target.Filter(updateSets, settings);

            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public void WhenMinAgeIsZeroAndThePackageIsFromTheFuture()
        {
            List<PackageUpdateSet> updateSets =
            [
                UpdateFooFromOneVersion(TimeSpan.FromMinutes(-5))
            ];

            IUpdateSelection target = CreateUpdateSelection();
            FilterSettings settings = MinAgeTargetSelection(TimeSpan.FromDays(0));

            IReadOnlyCollection<PackageUpdateSet> results = target.Filter(updateSets, settings);

            Assert.That(results.Count, Is.EqualTo(1));
        }

        private static PackageUpdateSet UpdateFoobarFromOneVersion()
        {
            PackageIdentity newPackage = LatestVersionOfPackageFoobar();

            List<PackageInProject> currentPackages =
            [
                new PackageInProject("foobar", "1.0.1", PathToProjectOne()),
                new PackageInProject("foobar", "1.0.1", PathToProjectTwo())
            ];

            PackageSearchMetadata latest = new(newPackage, new PackageSource("http://none"), DateTimeOffset.Now, null);

            PackageLookupResult updates = new(VersionChange.Major, latest, null, null);
            return new PackageUpdateSet(updates, currentPackages);
        }

        private static PackageUpdateSet UpdateFooFromOneVersion(TimeSpan? packageAge = null)
        {
            DateTimeOffset pubDate = DateTimeOffset.Now.Subtract(packageAge ?? TimeSpan.Zero);

            List<PackageInProject> currentPackages =
            [
                new PackageInProject("foo", "1.0.1", PathToProjectOne()),
                new PackageInProject("foo", "1.0.1", PathToProjectTwo())
            ];

            NuGetVersion matchVersion = new("4.0.0");
            PackageSearchMetadata match = new(new PackageIdentity("foo", matchVersion),
                new PackageSource("http://none"), pubDate, null);

            PackageLookupResult updates = new(VersionChange.Major, match, null, null);
            return new PackageUpdateSet(updates, currentPackages);
        }

        private static PackageUpdateSet UpdateBarFromTwoVersions(TimeSpan? packageAge = null)
        {
            DateTimeOffset pubDate = DateTimeOffset.Now.Subtract(packageAge ?? TimeSpan.Zero);

            List<PackageInProject> currentPackages =
            [
                new PackageInProject("bar", "1.0.1", PathToProjectOne()),
                new PackageInProject("bar", "1.2.1", PathToProjectTwo())
            ];

            PackageIdentity matchId = new("bar", new NuGetVersion("4.0.0"));
            PackageSearchMetadata match = new(matchId, new PackageSource("http://none"), pubDate, null);

            PackageLookupResult updates = new(VersionChange.Major, match, null, null);
            return new PackageUpdateSet(updates, currentPackages);
        }

        private static PackageIdentity LatestVersionOfPackageFoobar()
        {
            return new PackageIdentity("foobar", new NuGetVersion("1.2.3"));
        }

        private static PackagePath PathToProjectOne()
        {
            return new PackagePath("c_temp", "projectOne", PackageReferenceType.PackagesConfig);
        }

        private static PackagePath PathToProjectTwo()
        {
            return new PackagePath("c_temp", "projectTwo", PackageReferenceType.PackagesConfig);
        }

        private static IUpdateSelection CreateUpdateSelection()
        {
            return new UpdateSelection(Substitute.For<INuKeeperLogger>());
        }

        private static FilterSettings OneTargetSelection()
        {
            const int maxPullRequests = 1;

            return new FilterSettings
            {
                MaxPackageUpdates = maxPullRequests,
                MinimumAge = TimeSpan.Zero
            };
        }

        private static FilterSettings MinAgeTargetSelection(TimeSpan minAge)
        {
            const int maxPullRequests = 1000;

            return new FilterSettings
            {
                MaxPackageUpdates = maxPullRequests,
                MinimumAge = minAge
            };
        }
    }
}
