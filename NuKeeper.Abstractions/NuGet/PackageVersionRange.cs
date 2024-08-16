using NuGet.Packaging.Core;
using NuGet.Versioning;

using System;

namespace NuKeeper.Abstractions.NuGet
{
    public class PackageVersionRange
    {
        public PackageVersionRange(string id, VersionRange version)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Should not be null or empty", nameof(id));
            }

            Id = id;
            Version = version ?? throw new ArgumentNullException(nameof(version));
        }

        public string Id { get; }
        public VersionRange Version { get; }

        public static PackageVersionRange Parse(string id, string version)
        {
            bool success = VersionRange.TryParse(version, out VersionRange versionRange);
            return !success ? null : new PackageVersionRange(id, versionRange);
        }

        public PackageIdentity SingleVersionIdentity()
        {
            NuGetVersion version = VersionRanges.SingleVersion(Version);
            return version == null ? null : new PackageIdentity(Id, version);
        }
    }
}
