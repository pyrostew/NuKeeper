using NSubstitute;

using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.NuGetApi;
using NuKeeper.Inspection.NuGetApi;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NuKeeper.Inspection.Tests.NuGetApi
{
    [TestFixture]
    public class BulkPackageLookupTests
    {
        [Test]
        public async Task CanLookupEmptyList()
        {
            IApiPackageLookup apiLookup = Substitute.For<IApiPackageLookup>();
            BulkPackageLookup bulkLookup = BuildBulkPackageLookup(apiLookup);

            IDictionary<PackageIdentity, PackageLookupResult> results = await bulkLookup.FindVersionUpdates(
                Enumerable.Empty<PackageIdentity>(),
                NuGetSources.GlobalFeed,
                VersionChange.Major,
                UsePrerelease.FromPrerelease);

            Assert.That(results, Is.Not.Null);
            Assert.That(results, Is.Empty);

            _ = await apiLookup.DidNotReceive().FindVersionUpdate(
                Arg.Any<PackageIdentity>(), Arg.Any<NuGetSources>(), Arg.Any<VersionChange>(), Arg.Any<UsePrerelease>());
        }

        [Test]
        public async Task CanLookupOnePackage()
        {
            IApiPackageLookup apiLookup = Substitute.For<IApiPackageLookup>();

            ApiHasNewVersionForPackage(apiLookup, "foo");

            BulkPackageLookup bulkLookup = BuildBulkPackageLookup(apiLookup);

            List<PackageIdentity> queries =
            [
                new PackageIdentity("foo", new NuGetVersion(1, 2, 3))
            ];

            IDictionary<PackageIdentity, PackageLookupResult> results = await bulkLookup.FindVersionUpdates(queries,
                NuGetSources.GlobalFeed,
                VersionChange.Major,
                UsePrerelease.FromPrerelease);

            Assert.That(results, Is.Not.Null);
            Assert.That(results, Is.Not.Empty);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results.ElementAt(0).Key.Id, Is.EqualTo("foo"));
        }

        [Test]
        public async Task LookupOnePackageCallsApiOnce()
        {
            IApiPackageLookup apiLookup = Substitute.For<IApiPackageLookup>();

            ApiHasNewVersionForPackage(apiLookup, "foo");

            BulkPackageLookup bulkLookup = BuildBulkPackageLookup(apiLookup);

            List<PackageIdentity> queries =
            [
                new PackageIdentity("foo", new NuGetVersion(1, 2, 3))
            ];

            _ = await bulkLookup.FindVersionUpdates(queries,
                NuGetSources.GlobalFeed,
                VersionChange.Major,
                UsePrerelease.FromPrerelease);

            _ = await apiLookup.Received(1).FindVersionUpdate(
                Arg.Any<PackageIdentity>(), Arg.Any<NuGetSources>(), Arg.Any<VersionChange>(), Arg.Any<UsePrerelease>());
        }

        [Test]
        public async Task CanLookupTwoPackages()
        {
            IApiPackageLookup apiLookup = Substitute.For<IApiPackageLookup>();

            ApiHasNewVersionForPackage(apiLookup, "foo");
            ApiHasNewVersionForPackage(apiLookup, "bar");

            BulkPackageLookup bulkLookup = BuildBulkPackageLookup(apiLookup);

            List<PackageIdentity> queries =
            [
                new PackageIdentity("foo", new NuGetVersion(1, 2, 3)),
                new PackageIdentity("bar", new NuGetVersion(1, 2, 3))
            ];

            IDictionary<PackageIdentity, PackageLookupResult> results = await bulkLookup.FindVersionUpdates(queries,
                NuGetSources.GlobalFeed,
                VersionChange.Major,
                UsePrerelease.FromPrerelease);

            IEnumerable<PackageIdentity> packages = results.Select(kvp => kvp.Key);
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(packages, Has.Some.Matches<PackageIdentity>(pi => pi.Id == "foo"));
            Assert.That(packages, Has.Some.Matches<PackageIdentity>(pi => pi.Id == "bar"));
            Assert.That(packages, Has.None.Matches<PackageIdentity>(pi => pi.Id == "fish"));
        }

        [Test]
        public async Task LookupTwoPackagesCallsApiTwice()
        {
            IApiPackageLookup apiLookup = Substitute.For<IApiPackageLookup>();

            ApiHasNewVersionForPackage(apiLookup, "foo");
            ApiHasNewVersionForPackage(apiLookup, "bar");

            BulkPackageLookup bulkLookup = BuildBulkPackageLookup(apiLookup);

            List<PackageIdentity> queries =
            [
                new PackageIdentity("foo", new NuGetVersion(1, 2, 3)),
                new PackageIdentity("bar", new NuGetVersion(1, 2, 3))
            ];

            _ = await bulkLookup.FindVersionUpdates(queries,
                NuGetSources.GlobalFeed,
                VersionChange.Major,
                UsePrerelease.FromPrerelease);

            _ = await apiLookup.Received(2).FindVersionUpdate(
                Arg.Any<PackageIdentity>(), Arg.Any<NuGetSources>(), Arg.Any<VersionChange>(), Arg.Any<UsePrerelease>());
        }

        [Test]
        public async Task WhenThereAreMultipleVersionOfTheSamePackage()
        {
            IApiPackageLookup apiLookup = Substitute.For<IApiPackageLookup>();

            ApiHasNewVersionForPackage(apiLookup, "foo");

            BulkPackageLookup bulkLookup = BuildBulkPackageLookup(apiLookup);

            List<PackageIdentity> queries =
            [
                new PackageIdentity("foo", new NuGetVersion(1, 2, 3)),
                new PackageIdentity("foo", new NuGetVersion(1, 3, 4))
            ];

            IDictionary<PackageIdentity, PackageLookupResult> results = await bulkLookup.FindVersionUpdates(queries,
                NuGetSources.GlobalFeed,
                VersionChange.Major,
                UsePrerelease.FromPrerelease);

            _ = await apiLookup.Received(1).FindVersionUpdate(
                Arg.Any<PackageIdentity>(), Arg.Any<NuGetSources>(), Arg.Any<VersionChange>(), Arg.Any<UsePrerelease>());
            _ = await apiLookup.Received(1).FindVersionUpdate(Arg.Is<PackageIdentity>(
                pi => pi.Id == "foo" && pi.Version == new NuGetVersion(1, 3, 4)),
                Arg.Any<NuGetSources>(), Arg.Any<VersionChange>(), Arg.Any<UsePrerelease>());

            IEnumerable<PackageIdentity> packages = results.Select(kvp => kvp.Key);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(packages, Has.Some.Matches<PackageIdentity>(pi => pi.Id == "foo"));
            Assert.That(packages, Has.None.Matches<PackageIdentity>(pi => pi.Id == "bar"));
        }

        private static void ApiHasNewVersionForPackage(IApiPackageLookup lookup, string packageName)
        {
            PackageSearchMetadata responseMetaData = new(
                new PackageIdentity(packageName, new NuGetVersion(2, 3, 4)), new PackageSource("http://none"),
                DateTimeOffset.Now, null);

            _ = lookup.FindVersionUpdate(Arg.Is<PackageIdentity>(pm => pm.Id == packageName),
                    Arg.Any<NuGetSources>(), Arg.Any<VersionChange>(), Arg.Any<UsePrerelease>())
                .Returns(new PackageLookupResult(VersionChange.Major, responseMetaData, responseMetaData, responseMetaData));
        }

        private static BulkPackageLookup BuildBulkPackageLookup(IApiPackageLookup apiLookup)
        {
            return new BulkPackageLookup(apiLookup, new PackageLookupResultReporter(Substitute.For<INuKeeperLogger>()));
        }
    }
}
