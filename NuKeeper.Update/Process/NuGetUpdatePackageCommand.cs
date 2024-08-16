using NuGet.Configuration;
using NuGet.Versioning;

using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Update.ProcessRunner;

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace NuKeeper.Update.Process
{
    public class NuGetUpdatePackageCommand : INuGetUpdatePackageCommand
    {
        private readonly IExternalProcess _externalProcess;
        private readonly INuKeeperLogger _logger;
        private readonly INuGetPath _nuGetPath;
        private readonly IMonoExecutor _monoExecutor;

        public NuGetUpdatePackageCommand(
            INuKeeperLogger logger,
            INuGetPath nuGetPath,
            IMonoExecutor monoExecutor,
            IExternalProcess externalProcess)
        {
            _logger = logger;
            _nuGetPath = nuGetPath;
            _monoExecutor = monoExecutor;
            _externalProcess = externalProcess;
        }

        public async Task Invoke(PackageInProject currentPackage,
            NuGetVersion newVersion, PackageSource packageSource, NuGetSources allSources)
        {
            if (currentPackage == null)
            {
                throw new ArgumentNullException(nameof(currentPackage));
            }

            if (allSources == null)
            {
                throw new ArgumentNullException(nameof(allSources));
            }

            string projectPath = currentPackage.Path.Info.DirectoryName;

            string nuget = _nuGetPath.Executable;
            if (string.IsNullOrWhiteSpace(nuget))
            {
                _logger.Normal("Cannot find NuGet.exe for package update");
                return;
            }

            string sources = allSources.CommandLine("-Source");
            string updateCommand = $"update packages.config -Id {currentPackage.Id} -Version {newVersion} {sources} -NonInteractive";

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (await _monoExecutor.CanRun())
                {
                    _ = await _monoExecutor.Run(projectPath, nuget, updateCommand, true);
                }
                else
                {
                    _logger.Error("Cannot run NuGet.exe. It requires either Windows OS Platform or Mono installation");
                }
            }
            else
            {
                _ = await _externalProcess.Run(projectPath, nuget, updateCommand, true);
            }
        }
    }
}
