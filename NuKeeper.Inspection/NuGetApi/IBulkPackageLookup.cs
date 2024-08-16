using NuGet.Packaging.Core;

using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.NuGetApi;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace NuKeeper.Inspection.NuGetApi
{
    public interface IBulkPackageLookup
    {
        Task<IDictionary<PackageIdentity, PackageLookupResult>> FindVersionUpdates(
            IEnumerable<PackageIdentity> packages,
            NuGetSources sources,
            VersionChange allowedChange,
            UsePrerelease usePrerelease);
    }
}
