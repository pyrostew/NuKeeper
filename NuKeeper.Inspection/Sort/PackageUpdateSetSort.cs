using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.RepositoryInspection;

using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Inspection.Sort
{
    public class PackageUpdateSetSort : IPackageUpdateSetSort
    {
        private readonly INuKeeperLogger _logger;

        public PackageUpdateSetSort(INuKeeperLogger logger)
        {
            _logger = logger;
        }

        public IEnumerable<PackageUpdateSet> Sort(IReadOnlyCollection<PackageUpdateSet> input)
        {
            PrioritySort prioritySorter = new();
            PackageUpdateSetTopologicalSort topoSorter = new(_logger);

            IEnumerable<PackageUpdateSet> priorityOrder = prioritySorter.Sort(input);
            return topoSorter.Sort(priorityOrder.ToList());
        }
    }
}
