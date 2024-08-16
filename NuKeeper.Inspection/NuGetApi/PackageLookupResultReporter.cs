using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.NuGetApi;

namespace NuKeeper.Inspection.NuGetApi
{
    public class PackageLookupResultReporter : IPackageLookupResultReporter
    {
        private readonly INuKeeperLogger _logger;

        public PackageLookupResultReporter(INuKeeperLogger logger)
        {
            _logger = logger;
        }

        public void Report(PackageLookupResult lookupResult)
        {
            NuGet.Versioning.NuGetVersion highestVersion = lookupResult?.Major?.Identity?.Version;
            if (highestVersion == null)
            {
                return;
            }

            string allowing = lookupResult.AllowedChange == VersionChange.Major
                ? string.Empty
                : $" Allowing {lookupResult.AllowedChange} version updates.";

            NuGet.Versioning.NuGetVersion highestMatchVersion = lookupResult.Selected()?.Identity?.Version;

            string packageId = lookupResult.Major.Identity.Id;

            if (highestMatchVersion == null)
            {
                _logger.Normal($"Package {packageId} version {highestVersion} is available but is not allowed.{allowing}");
                return;
            }

            if (highestVersion > highestMatchVersion)
            {
                _logger.Normal($"Selected update of package {packageId} to version {highestMatchVersion}, but version {highestVersion} is also available.{allowing}");
            }
            else
            {
                _logger.Detailed($"Selected update of package {packageId} to highest version, {highestMatchVersion}.{allowing}");
            }
        }
    }
}
