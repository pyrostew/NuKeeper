using NuKeeper.Abstractions.Inspections.Files;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NuKeeper.Update.Process
{
    public class SolutionRestore : ISolutionRestore
    {
        private readonly IFileRestoreCommand _fileRestoreCommand;

        public SolutionRestore(IFileRestoreCommand fileRestoreCommand)
        {
            _fileRestoreCommand = fileRestoreCommand;
        }

        public async Task CheckRestore(IEnumerable<PackageUpdateSet> targetUpdates, IFolder workingFolder, NuGetSources sources)
        {
            if (workingFolder == null)
            {
                throw new ArgumentNullException(nameof(workingFolder));
            }

            if (AnyProjectRequiresNuGetRestore(targetUpdates))
            {
                await Restore(workingFolder, sources);
            }
        }

        private async Task Restore(IFolder workingFolder, NuGetSources sources)
        {
            IReadOnlyCollection<System.IO.FileInfo> solutionFiles = workingFolder.Find("*.sln");

            foreach (System.IO.FileInfo sln in solutionFiles)
            {
                await _fileRestoreCommand.Invoke(sln, sources);
            }
        }

        private static bool AnyProjectRequiresNuGetRestore(IEnumerable<PackageUpdateSet> targetUpdates)
        {
            return targetUpdates.SelectMany(u => u.CurrentPackages)
                .Any(p => p.Path.PackageReferenceType != PackageReferenceType.ProjectFile);
        }
    }
}

