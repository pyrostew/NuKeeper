using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NuKeeper.Inspection.NuGetApi
{
    public class PackageUpdatesLookup : IPackageUpdatesLookup
    {
        private readonly IBulkPackageLookup _bulkPackageLookup;

        public PackageUpdatesLookup(IBulkPackageLookup bulkPackageLookup)
        {
            _bulkPackageLookup = bulkPackageLookup;
        }

        public async Task<IReadOnlyCollection<PackageUpdateSet>> FindUpdatesForPackages(
            IReadOnlyCollection<PackageInProject> packages,
            NuGetSources sources,
            VersionChange allowedChange,
            UsePrerelease usePrerelease)
        {
            IEnumerable<NuGet.Packaging.Core.PackageIdentity> packageIds = packages
                .Select(p => p.Identity)
                .Distinct();

            IDictionary<NuGet.Packaging.Core.PackageIdentity, Abstractions.NuGetApi.PackageLookupResult> latestVersions = await _bulkPackageLookup.FindVersionUpdates(
                packageIds, sources, allowedChange, usePrerelease);

            List<PackageUpdateSet> results = [];

            foreach (NuGet.Packaging.Core.PackageIdentity packageId in latestVersions.Keys)
            {
                Abstractions.NuGetApi.PackageLookupResult latestPackage = latestVersions[packageId];
                NuGet.Versioning.NuGetVersion matchVersion = latestPackage.Selected().Identity.Version;

                List<PackageInProject> updatesForThisPackage = packages
                    .Where(p => p.Identity.Equals(packageId) && p.Version < matchVersion)
                    .ToList();

                if (updatesForThisPackage.Count > 0)
                {
                    PackageUpdateSet updateSet = new(latestPackage, updatesForThisPackage);
                    results.Add(updateSet);
                }
            }

            return results;
        }
    }
}
