using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Inspection.Sort;
using NuKeeper.Update.Process;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NuKeeper.Update
{
    public class UpdateRunner : IUpdateRunner
    {
        private readonly INuKeeperLogger _logger;
        private readonly IFileRestoreCommand _fileRestoreCommand;
        private readonly INuGetUpdatePackageCommand _nuGetUpdatePackageCommand;
        private readonly IDotNetUpdatePackageCommand _dotNetUpdatePackageCommand;
        private readonly IUpdateProjectImportsCommand _updateProjectImportsCommand;
        private readonly IUpdateNuspecCommand _updateNuspecCommand;
        private readonly IUpdateDirectoryBuildTargetsCommand _updateDirectoryBuildTargetsCommand;

        public UpdateRunner(
            INuKeeperLogger logger,
            IFileRestoreCommand fileRestoreCommand,
            INuGetUpdatePackageCommand nuGetUpdatePackageCommand,
            IDotNetUpdatePackageCommand dotNetUpdatePackageCommand,
            IUpdateProjectImportsCommand updateProjectImportsCommand,
            IUpdateNuspecCommand updateNuspecCommand,
            IUpdateDirectoryBuildTargetsCommand updateDirectoryBuildTargetsCommand)
        {
            _logger = logger;
            _fileRestoreCommand = fileRestoreCommand;
            _nuGetUpdatePackageCommand = nuGetUpdatePackageCommand;
            _dotNetUpdatePackageCommand = dotNetUpdatePackageCommand;
            _updateProjectImportsCommand = updateProjectImportsCommand;
            _updateNuspecCommand = updateNuspecCommand;
            _updateDirectoryBuildTargetsCommand = updateDirectoryBuildTargetsCommand;
        }

        public async Task Update(PackageUpdateSet updateSet, NuGetSources sources)
        {
            if (updateSet == null)
            {
                throw new ArgumentNullException(nameof(updateSet));
            }

            IReadOnlyCollection<PackageInProject> sortedUpdates = Sort(updateSet.CurrentPackages);

            _logger.Detailed($"Updating '{updateSet.SelectedId}' to {updateSet.SelectedVersion} in {sortedUpdates.Count} projects");

            foreach (PackageInProject current in sortedUpdates)
            {
                IReadOnlyCollection<IPackageCommand> updateCommands = GetUpdateCommands(current.Path.PackageReferenceType);
                foreach (IPackageCommand updateCommand in updateCommands)
                {
                    await updateCommand.Invoke(current,
                        updateSet.SelectedVersion, updateSet.Selected.Source,
                        sources);
                }
            }
        }

        private IReadOnlyCollection<PackageInProject> Sort(IReadOnlyCollection<PackageInProject> packages)
        {
            PackageInProjectTopologicalSort sorter = new(_logger);
            return sorter.Sort(packages)
                .ToList();
        }

        private IReadOnlyCollection<IPackageCommand> GetUpdateCommands(
            PackageReferenceType packageReferenceType)
        {
            return packageReferenceType switch
            {
                PackageReferenceType.PackagesConfig => new IPackageCommand[]
                                    {
                        _fileRestoreCommand,
                        _nuGetUpdatePackageCommand
                                    },
                PackageReferenceType.ProjectFileOldStyle => new IPackageCommand[]
                    {
                        _updateProjectImportsCommand,
                        _fileRestoreCommand,
                        _dotNetUpdatePackageCommand
                    },
                PackageReferenceType.ProjectFile => new[] { _dotNetUpdatePackageCommand },
                PackageReferenceType.Nuspec => new[] { _updateNuspecCommand },
                PackageReferenceType.DirectoryBuildTargets => new[] { _updateDirectoryBuildTargetsCommand },
                _ => throw new ArgumentOutOfRangeException(nameof(packageReferenceType)),
            };
        }
    }
}
