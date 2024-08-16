using NuGet.Configuration;

using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.NuGetApi;
using NuKeeper.Abstractions.RepositoryInspection;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuKeeper.Inspection.Tests.Report
{
    public static class PackageUpdates
    {
        public static PackageUpdateSet UpdateSetFor(PackageVersionRange package, params PackageInProject[] packages)
        {
            DateTimeOffset publishedDate = new(2018, 2, 19, 11, 12, 7, TimeSpan.Zero);
            PackageSearchMetadata latest = new(package.SingleVersionIdentity(), new PackageSource("http://none"), publishedDate, null);

            PackageLookupResult updates = new(VersionChange.Major, latest, null, null);
            return new PackageUpdateSet(updates, packages);
        }

        public static PackageInProject MakePackageForV110(PackageVersionRange package)
        {
            PackagePath path = new(
                OsSpecifics.GenerateBaseDirectory(),
                Path.Combine("folder", "src", "project1", "packages.config"),
                PackageReferenceType.PackagesConfig);
            return new PackageInProject(package, path);
        }

        internal static List<PackageUpdateSet> PackageUpdateSets(int count)
        {
            List<PackageUpdateSet> result = [];
            foreach (int index in Enumerable.Range(1, count))
            {
                PackageVersionRange package = PackageVersionRange.Parse(
                    $"test.package{index}", $"1.2.{index}");

                PackageUpdateSet updateSet = UpdateSetFor(package, MakePackageForV110(package));
                result.Add(updateSet);
            }

            return result;
        }

        public static List<PackageUpdateSet> OnePackageUpdateSet()
        {
            PackageVersionRange package = PackageVersionRange.Parse("foo.bar", "1.2.3");

            return
            [
                UpdateSetFor(package, MakePackageForV110(package))
            ];
        }
    }
}
