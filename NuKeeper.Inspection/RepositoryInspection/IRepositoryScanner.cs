using NuKeeper.Abstractions.Inspections.Files;
using NuKeeper.Abstractions.RepositoryInspection;

using System.Collections.Generic;

namespace NuKeeper.Inspection.RepositoryInspection
{
    public interface IRepositoryScanner
    {
        IReadOnlyCollection<PackageInProject> FindAllNuGetPackages(IFolder workingFolder);
    }
}
