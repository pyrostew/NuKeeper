using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.RepositoryInspection;

using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Inspection.Sort
{
    public class PackageInProjectTopologicalSort
    {
        private readonly INuKeeperLogger _logger;

        public PackageInProjectTopologicalSort(INuKeeperLogger logger)
        {
            _logger = logger;
        }

        public IEnumerable<PackageInProject> Sort(IReadOnlyCollection<PackageInProject> input)
        {
            TopologicalSort<PackageInProject> topo = new(_logger, Match);

            List<SortItemData<PackageInProject>> inputMap = input.Select(p =>
                    new SortItemData<PackageInProject>(p, ProjectDeps(p, input)))
                .ToList();

            List<PackageInProject> sorted = topo.Sort(inputMap)
                .ToList();

            sorted.Reverse();

            ReportSort(input.ToList(), sorted);

            return sorted;
        }

        private bool Match(PackageInProject a, PackageInProject b)
        {
            return a.Path.FullName == b.Path.FullName;
        }

        private static IReadOnlyCollection<PackageInProject> ProjectDeps(PackageInProject selected,
            IReadOnlyCollection<PackageInProject> all)
        {
            IReadOnlyCollection<string> deps = selected.ProjectReferences;
            return all.Where(i => deps.Any(d => d == i.Path.FullName)).ToList();
        }

        private void ReportSort(IList<PackageInProject> input, IList<PackageInProject> output)
        {
            bool hasChange = false;

            for (int i = 0; i < output.Count; i++)
            {
                if (input[i] != output[i])
                {
                    hasChange = true;
                    PackageInProject firstChange = output[i];
                    int originalIndex = input.IndexOf(firstChange);
                    _logger.Detailed($"Resorted {output.Count} projects by dependencies, first change is {firstChange.Path.RelativePath} moved to position {i} from {originalIndex}.");
                    break;
                }
            }

            if (!hasChange)
            {
                _logger.Detailed($"Sorted {output.Count} projects by dependencies but no change made");
            }
        }
    }
}
