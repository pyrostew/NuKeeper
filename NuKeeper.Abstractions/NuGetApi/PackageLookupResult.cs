using NuKeeper.Abstractions.Configuration;

using System;

namespace NuKeeper.Abstractions.NuGetApi
{
    public class PackageLookupResult
    {
        public PackageLookupResult(
            VersionChange allowedChange,
            PackageSearchMetadata major,
            PackageSearchMetadata minor,
            PackageSearchMetadata patch)
        {
            AllowedChange = allowedChange;
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public VersionChange AllowedChange { get; }

        public PackageSearchMetadata Major { get; }
        public PackageSearchMetadata Minor { get; }
        public PackageSearchMetadata Patch { get; }

        public PackageSearchMetadata Selected()
        {
            return AllowedChange switch
            {
                VersionChange.Major => Major,
                VersionChange.Minor => Minor,
                VersionChange.Patch => Patch,
                VersionChange.None => null,
                _ => throw new Exception($"Unknown version change {AllowedChange}"),
            };
        }
    }
}
