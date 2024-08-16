using NuGet.Configuration;
using NuGet.Packaging.Core;

using NuKeeper.Abstractions.Formats;

using System;
using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Abstractions.NuGetApi
{
    public class PackageSearchMetadata
    {
        public PackageSearchMetadata(
            PackageIdentity identity,
            PackageSource source,
            DateTimeOffset? published,
            IEnumerable<PackageDependency> dependencies)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Published = published;
            Dependencies = dependencies?.ToList() ?? [];
        }

        public PackageIdentity Identity { get; }
        public PackageSource Source { get; }
        public DateTimeOffset? Published { get; }

        public IReadOnlyCollection<PackageDependency> Dependencies { get; }

        public override string ToString()
        {
            return Published.HasValue
                ? $"{Identity} from {Source}, published at {DateFormat.AsUtcIso8601(Published)}"
                : $"{Identity} from {Source}, no published date";
        }
    }
}
