using NuGet.Packaging.Core;
using NuGet.Versioning;

using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Inspection.NuGetApi;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NuKeeper.Integration.Tests.NuGet.Api
{
    [TestFixture]
    public class BulkPackageLookupTests : TestWithFailureLogging
    {
        [Test]
        public async Task CanFindUpdateForOneWellKnownPackage()
        {
            List<PackageIdentity> packages = [Current("Moq")];

            BulkPackageLookup lookup = BuildBulkPackageLookup();

            IDictionary<PackageIdentity, Abstractions.NuGetApi.PackageLookupResult> results = await lookup.FindVersionUpdates(
                packages, NuGetSources.GlobalFeed, VersionChange.Major,
                UsePrerelease.FromPrerelease);

            IEnumerable<PackageIdentity> updatedPackages = results.Select(p => p.Key);
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(updatedPackages, Has.Some.Matches<PackageIdentity>(p => p.Id == "Moq"));
        }

        [Test]
        public async Task CanFindUpdateForTwoWellKnownPackages()
        {
            List<PackageIdentity> packages =
            [
                Current("Moq"),
                Current("Newtonsoft.Json")
            ];

            BulkPackageLookup lookup = BuildBulkPackageLookup();

            IDictionary<PackageIdentity, Abstractions.NuGetApi.PackageLookupResult> results = await lookup.FindVersionUpdates(
                packages, NuGetSources.GlobalFeed, VersionChange.Major,
                UsePrerelease.FromPrerelease);

            IEnumerable<PackageIdentity> updatedPackages = results.Select(p => p.Key);
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(updatedPackages, Has.Some.Matches<PackageIdentity>(p => p.Id == "Moq"));
            Assert.That(updatedPackages, Has.Some.Matches<PackageIdentity>(p => p.Id == "Newtonsoft.Json"));
        }

        [Test]
        public async Task FindsSingleUpdateForPackageDifferingOnlyByCase()
        {
            List<PackageIdentity> packages =
            [
                Current("nunit"),
                Current("NUnit")
            ];

            BulkPackageLookup lookup = BuildBulkPackageLookup();

            IDictionary<PackageIdentity, Abstractions.NuGetApi.PackageLookupResult> results = await lookup.FindVersionUpdates(
                packages, NuGetSources.GlobalFeed, VersionChange.Major,
                UsePrerelease.FromPrerelease);

            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task InvalidPackageIsIgnored()
        {
            List<PackageIdentity> packages =
            [
                Current(Guid.NewGuid().ToString())
            ];

            BulkPackageLookup lookup = BuildBulkPackageLookup();

            IDictionary<PackageIdentity, Abstractions.NuGetApi.PackageLookupResult> results = await lookup.FindVersionUpdates(
                packages, NuGetSources.GlobalFeed, VersionChange.Major,
                UsePrerelease.FromPrerelease);

            Assert.That(results, Is.Not.Null);
            Assert.That(results, Is.Empty);
        }

        [Test]
        public async Task TestEmptyList()
        {
            BulkPackageLookup lookup = BuildBulkPackageLookup();

            IDictionary<PackageIdentity, Abstractions.NuGetApi.PackageLookupResult> results = await lookup.FindVersionUpdates(
                Enumerable.Empty<PackageIdentity>(), NuGetSources.GlobalFeed, VersionChange.Major,
                UsePrerelease.FromPrerelease);

            Assert.That(results, Is.Not.Null);
            Assert.That(results, Is.Empty);
        }

        [Test]
        public async Task ValidPackagesWorkDespiteInvalidPackages()
        {
            List<PackageIdentity> packages =
            [
                Current("Moq"),
                Current(Guid.NewGuid().ToString()),
                Current("Newtonsoft.Json"),
                Current(Guid.NewGuid().ToString())
            ];

            BulkPackageLookup lookup = BuildBulkPackageLookup();

            IDictionary<PackageIdentity, Abstractions.NuGetApi.PackageLookupResult> results = await lookup.FindVersionUpdates(
                packages, NuGetSources.GlobalFeed, VersionChange.Major,
                UsePrerelease.FromPrerelease);

            IEnumerable<PackageIdentity> updatedPackages = results.Select(p => p.Key);
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(updatedPackages, Has.Some.Matches<PackageIdentity>(p => p.Id == "Moq"));
            Assert.That(updatedPackages, Has.Some.Matches<PackageIdentity>(p => p.Id == "Newtonsoft.Json"));
        }

        private BulkPackageLookup BuildBulkPackageLookup()
        {
            ApiPackageLookup lookup = new(new PackageVersionsLookup(NugetLogger, NukeeperLogger));
            return new BulkPackageLookup(lookup, new PackageLookupResultReporter(NukeeperLogger));
        }

        private static PackageIdentity Current(string packageId)
        {
            return new PackageIdentity(packageId, new NuGetVersion(1, 2, 3));
        }
    }
}
