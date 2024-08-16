using NuGet.Versioning;

using NuKeeper.Abstractions;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.NuGetApi;

using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Inspection.NuGetApi
{
    public static class VersionChanges
    {
        public static PackageLookupResult MakeVersions(
            NuGetVersion current,
            IEnumerable<PackageSearchMetadata> candidateVersions,
            VersionChange allowedChange)
        {
            List<PackageSearchMetadata> orderedCandidates = candidateVersions
                .OrderByDescending(p => p.Identity.Version)
                .ToList();

            PackageSearchMetadata major = FirstMatch(orderedCandidates, current, VersionChange.Major);
            PackageSearchMetadata minor = FirstMatch(orderedCandidates, current, VersionChange.Minor);
            PackageSearchMetadata patch = FirstMatch(orderedCandidates, current, VersionChange.Patch);
            return new PackageLookupResult(allowedChange, major, minor, patch);
        }

        private static PackageSearchMetadata FirstMatch(
            IList<PackageSearchMetadata> candidates,
            NuGetVersion current,
            VersionChange allowedChange)
        {
            return candidates.FirstOrDefault(p => Filter(current, p.Identity.Version, allowedChange));
        }

        private static bool Filter(NuGetVersion v1, NuGetVersion v2, VersionChange allowedChange)
        {
            return allowedChange switch
            {
                VersionChange.Major => true,
                VersionChange.Minor => v1.Major == v2.Major,
                VersionChange.Patch => (v1.Major == v2.Major) && (v1.Minor == v2.Minor),
                VersionChange.None => v1 == v2,
                _ => throw new NuKeeperException($"Unknown version change {allowedChange}"),
            };
        }
    }
}
