using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.RepositoryInspection;

using System.Collections.Generic;

namespace NuKeeper.Update.Selection
{
    public interface IUpdateSelection
    {
        IReadOnlyCollection<PackageUpdateSet> Filter(
            IReadOnlyCollection<PackageUpdateSet> potentialUpdates,
            FilterSettings settings);
    }
}
