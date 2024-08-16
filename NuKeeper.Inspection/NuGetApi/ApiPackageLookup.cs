using NuGet.Packaging.Core;

using NuKeeper.Abstractions;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.NuGetApi;

using System;
using System.Threading.Tasks;

namespace NuKeeper.Inspection.NuGetApi
{
    public class ApiPackageLookup : IApiPackageLookup
    {
        private readonly IPackageVersionsLookup _packageVersionsLookup;

        public ApiPackageLookup(IPackageVersionsLookup packageVersionsLookup)
        {
            _packageVersionsLookup = packageVersionsLookup;
        }

        public async Task<PackageLookupResult> FindVersionUpdate(
            PackageIdentity package,
            NuGetSources sources,
            VersionChange allowedChange,
            UsePrerelease usePrerelease)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            bool includePrerelease = ShouldAllowPrerelease(package, usePrerelease);

            System.Collections.Generic.IReadOnlyCollection<PackageSearchMetadata> foundVersions = await _packageVersionsLookup.Lookup(package.Id, includePrerelease, sources);
            return VersionChanges.MakeVersions(package.Version, foundVersions, allowedChange);
        }

        private static bool ShouldAllowPrerelease(PackageIdentity package, UsePrerelease usePrerelease)
        {
            return usePrerelease switch
            {
                UsePrerelease.Always => true,
                UsePrerelease.Never => false,
                UsePrerelease.FromPrerelease => package.Version.IsPrerelease,
                _ => throw new NuKeeperException($"Invalid UsePrerelease value: {usePrerelease}"),
            };
        }
    }
}
