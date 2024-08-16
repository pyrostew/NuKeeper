using NuKeeper.Abstractions.RepositoryInspection;

using System.Collections.Generic;

namespace NuKeeper.Inspection.Sort
{
    public interface IPackageUpdateSetSort
    {
        IEnumerable<PackageUpdateSet> Sort(IReadOnlyCollection<PackageUpdateSet> input);
    }
}
