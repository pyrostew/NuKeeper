using McMaster.Extensions.CommandLineUtils;

using NuGet.Configuration;
using NuGet.Versioning;

using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Update.ProcessRunner;

using System;
using System.Threading.Tasks;

namespace NuKeeper.Update.Process
{
    public class DotNetUpdatePackageCommand : IDotNetUpdatePackageCommand
    {
        private readonly IExternalProcess _externalProcess;

        public DotNetUpdatePackageCommand(IExternalProcess externalProcess)
        {
            _externalProcess = externalProcess;
        }

        public async Task Invoke(PackageInProject currentPackage,
            NuGetVersion newVersion, PackageSource packageSource, NuGetSources allSources)
        {
            if (currentPackage == null)
            {
                throw new ArgumentNullException(nameof(currentPackage));
            }

            if (packageSource == null)
            {
                throw new ArgumentNullException(nameof(packageSource));
            }

            if (allSources == null)
            {
                throw new ArgumentNullException(nameof(allSources));
            }

            string projectPath = currentPackage.Path.Info.DirectoryName;
            string projectFileNameCommandLine = ArgumentEscaper.EscapeAndConcatenate(new string[] { currentPackage.Path.Info.Name });
            string sourceUrl = UriEscapedForArgument(packageSource.SourceUri);
            string sources = allSources.CommandLine("-s");

            string restoreCommand = $"restore {projectFileNameCommandLine} {sources}";
            _ = await _externalProcess.Run(projectPath, "dotnet", restoreCommand, true);

            if (currentPackage.Path.PackageReferenceType == PackageReferenceType.ProjectFileOldStyle)
            {
                string removeCommand = $"remove {projectFileNameCommandLine} package {currentPackage.Id}";
                _ = await _externalProcess.Run(projectPath, "dotnet", removeCommand, true);
            }

            string addCommand = $"add {projectFileNameCommandLine} package {currentPackage.Id} -v {newVersion} -s {sourceUrl}";
            _ = await _externalProcess.Run(projectPath, "dotnet", addCommand, true);
        }

        private static string UriEscapedForArgument(Uri uri)
        {
            return uri == null ? string.Empty : ArgumentEscaper.EscapeAndConcatenate(new string[] { uri.ToString() });
        }
    }
}
