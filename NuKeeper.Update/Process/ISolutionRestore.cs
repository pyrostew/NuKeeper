using NuKeeper.Abstractions.Inspections.Files;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace NuKeeper.Update.Process
{
    public interface ISolutionRestore
    {
        Task CheckRestore(IEnumerable<PackageUpdateSet> targetUpdates, IFolder workingFolder, NuGetSources sources);
    }
}
