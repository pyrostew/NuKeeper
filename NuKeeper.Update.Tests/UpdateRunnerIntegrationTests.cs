using NuKeeper.Abstractions.Logging;
using NuKeeper.Integration.Tests.LogHelpers;
using NuKeeper.Update.Process;
using NuKeeper.Update.ProcessRunner;

using NUnit.Framework;

namespace NuKeeper.Update.Tests
{
   [TestFixture]
   public class UpdateRunnerIntegrationTests
   {
      public void UpdateSimpleNETProject()
      {
         INuKeeperLogger logger = new NuKeeperTestLogger();
         INuGetPath nuGetPath = new NuGetPath(logger);
         IExternalProcess externalProcess = new ExternalProcess(logger);
         IMonoExecutor monoExecutor = new MonoExecutor(logger, externalProcess);
         IFileRestoreCommand fileRestoreCommand = new NuGetFileRestoreCommand(logger, nuGetPath, monoExecutor, externalProcess);
         INuGetUpdatePackageCommand nuGetUpdatePackageCommand = new NuGetUpdatePackageCommand(logger, nuGetPath, monoExecutor, externalProcess);
         IDotNetUpdatePackageCommand dotNetUpdatePackageCommand = new DotNetUpdatePackageCommand(externalProcess);
         IUpdateProjectImportsCommand updateProjectImportsCommand = new UpdateProjectImportsCommand();
         IUpdateNuspecCommand updateNuspecCommand = new UpdateNuspecCommand(logger);
         IUpdateDirectoryBuildTargetsCommand updateDirectoryBuildTargetsCommand = new UpdateDirectoryBuildTargetsCommand(logger);

         UpdateRunner runner = new(logger, );


      }
   }
}
