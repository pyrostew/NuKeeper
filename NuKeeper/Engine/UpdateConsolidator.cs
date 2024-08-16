using NuKeeper.Abstractions.RepositoryInspection;

using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Engine
{
    public static class UpdateConsolidator
    {
        public static IReadOnlyCollection<IReadOnlyCollection<PackageUpdateSet>> Consolidate(
            IReadOnlyCollection<PackageUpdateSet> updates, bool consolidate)
        {
            return consolidate
                ? new List<IReadOnlyCollection<PackageUpdateSet>> { updates }
                : updates.Select(u => new List<PackageUpdateSet> { u }).ToList();
        }
    }
}
