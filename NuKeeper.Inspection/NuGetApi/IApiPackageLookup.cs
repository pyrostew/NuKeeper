using NuGet.Packaging.Core;

using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.NuGetApi;

using System.Threading.Tasks;

namespace NuKeeper.Inspection.NuGetApi
{
    public interface IApiPackageLookup
    {
        Task<PackageLookupResult> FindVersionUpdate(
            PackageIdentity package,
            NuGetSources sources,
            VersionChange allowedChange,
            UsePrerelease usePrerelease);
    }
}
