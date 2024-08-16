using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using NuKeeper.Abstractions;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.NuGetApi;
using NuKeeper.Abstractions.RepositoryInspection;

using System;
using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Tests
{
    public static class PackageUpdates
    {
        private static readonly DateTimeOffset StandardPublishedDate = new(2018, 2, 19, 11, 12, 7, TimeSpan.Zero);

        public static PackageUpdateSet UpdateSet()
        {
            return MakeUpdateSet("foo", "1.2.3", PackageReferenceType.PackagesConfig);
        }

        public static PackageUpdateSet MakeUpdateSet(string packageName,
            string version = "1.2.3",
            PackageReferenceType packageRefType = PackageReferenceType.ProjectFile)
        {
            PackageVersionRange packageId = PackageVersionRange.Parse(packageName, version);

            PackageSearchMetadata latest = new(
                packageId.SingleVersionIdentity(), OfficialPackageSource(),
                null,
                Enumerable.Empty<PackageDependency>());

            PackageLookupResult packages = new(VersionChange.Major, latest, null, null);

            List<PackageInProject> pip = new PackageInProject(packageId, MakePackagePath(packageRefType), null)
                .InList();

            return new PackageUpdateSet(packages, pip);
        }

        public static PackageUpdateSet For(
            PackageIdentity package,
            DateTimeOffset published,
            IEnumerable<PackageInProject> packages,
            IEnumerable<PackageDependency> dependencies)
        {
            PackageSearchMetadata latest = new(package, OfficialPackageSource(), published, dependencies);
            PackageLookupResult updates = new(VersionChange.Major, latest, null, null);
            return new PackageUpdateSet(updates, packages);
        }

        public static PackageUpdateSet ForPackageRefType(PackageReferenceType refType)
        {
            return MakeUpdateSet("foo", "1.2.3", refType);
        }

        public static PackageUpdateSet For(params PackageInProject[] packages)
        {
            PackageIdentity newPackage = new("foo.bar", new NuGetVersion("1.2.3"));
            return ForNewVersion(newPackage, packages);
        }

        public static PackageUpdateSet ForNewVersion(PackageIdentity newPackage, params PackageInProject[] packages)
        {
            DateTimeOffset publishedDate = new(2018, 2, 19, 11, 12, 7, TimeSpan.Zero);
            PackageSearchMetadata latest = new(newPackage, OfficialPackageSource(), publishedDate, null);

            PackageLookupResult updates = new(VersionChange.Major, latest, null, null);
            return new PackageUpdateSet(updates, packages);
        }

        public static PackageUpdateSet ForInternalSource(params PackageInProject[] packages)
        {
            PackageIdentity newPackage = new("foo.bar", new NuGetVersion("1.2.3"));
            DateTimeOffset publishedDate = new(2018, 2, 19, 11, 12, 7, TimeSpan.Zero);
            PackageSearchMetadata latest = new(newPackage,
                InternalPackageSource(), publishedDate, null);

            PackageLookupResult updates = new(VersionChange.Major, latest, null, null);
            return new PackageUpdateSet(updates, packages);
        }

        public static PackageUpdateSet UpdateSetFor(PackageIdentity package, params PackageInProject[] packages)
        {
            return UpdateSetFor(package, StandardPublishedDate, packages);
        }

        public static PackageUpdateSet UpdateSetFor(PackageIdentity package, DateTimeOffset published, params PackageInProject[] packages)
        {
            PackageSearchMetadata latest = new(package, OfficialPackageSource(), published, null);

            PackageLookupResult updates = new(VersionChange.Major, latest, null, null);
            return new PackageUpdateSet(updates, packages);
        }

        public static PackageUpdateSet LimitedToMinor(params PackageInProject[] packages)
        {
            return LimitedToMinor(null, packages);
        }

        public static PackageUpdateSet LimitedToMinor(DateTimeOffset? publishedAt,
            params PackageInProject[] packages)
        {
            PackageIdentity latestId = new("foo.bar", new NuGetVersion("2.3.4"));
            PackageSearchMetadata latest = new(latestId, OfficialPackageSource(), publishedAt, null);

            PackageSearchMetadata match = new(
                new PackageIdentity("foo.bar", new NuGetVersion("1.2.3")), OfficialPackageSource(), null, null);

            PackageLookupResult updates = new(VersionChange.Minor, latest, match, null);
            return new PackageUpdateSet(updates, packages);
        }

        // todo move these to PackageUpdates.
        public static PackageUpdateSet UpdateFooFromOneVersion(TimeSpan? packageAge = null)
        {
            DateTimeOffset pubDate = DateTimeOffset.Now.Subtract(packageAge ?? TimeSpan.Zero);

            List<PackageInProject> currentPackages =
            [
                new PackageInProject("foo", "1.0.1", PathToProjectOne()),
                new PackageInProject("foo", "1.0.1", PathToProjectTwo())
            ];

            NuGetVersion matchVersion = new("4.0.0");
            PackageSearchMetadata match = new(new PackageIdentity("foo", matchVersion),
                OfficialPackageSource(), pubDate, null);

            PackageLookupResult updates = new(VersionChange.Major, match, null, null);
            return new PackageUpdateSet(updates, currentPackages);
        }

        public static PackageUpdateSet UpdateBarFromTwoVersions(TimeSpan? packageAge = null)
        {
            DateTimeOffset pubDate = DateTimeOffset.Now.Subtract(packageAge ?? TimeSpan.Zero);

            List<PackageInProject> currentPackages =
            [
                new PackageInProject("bar", "1.0.1", PathToProjectOne()),
                new PackageInProject("bar", "1.2.1", PathToProjectTwo())
            ];

            PackageIdentity matchId = new("bar", new NuGetVersion("4.0.0"));
            PackageSearchMetadata match = new(matchId, OfficialPackageSource(), pubDate, null);

            PackageLookupResult updates = new(VersionChange.Major, match, null, null);
            return new PackageUpdateSet(updates, currentPackages);
        }

        private static PackagePath MakePackagePath(PackageReferenceType packageRefType)
        {
            return new PackagePath("c:\\foo", "bar\\aproj.csproj", packageRefType);
        }

        private static PackagePath PathToProjectOne()
        {
            return new PackagePath("c_temp", "projectOne", PackageReferenceType.PackagesConfig);
        }

        private static PackagePath PathToProjectTwo()
        {
            return new PackagePath("c_temp", "projectTwo", PackageReferenceType.PackagesConfig);
        }

        public static PackageSource OfficialPackageSource()
        {
            return new PackageSource(NuGetConstants.V3FeedUrl);
        }

        public static PackageSource InternalPackageSource()
        {
            return new PackageSource("http://internalfeed.myco.com/api");
        }
    }
}
