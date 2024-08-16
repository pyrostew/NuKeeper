using NSubstitute;

using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Update.Process;
using NuKeeper.Update.ProcessRunner;

using NUnit.Framework;

using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace NuKeeper.Tests.Engine
{
    [TestFixture]
    public class NugetRestoreTests
    {
        [Test]
        public async Task WhenNugetRestoreIsCalledThenArgsIncludePackageDirectory()
        {
            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();
            INuGetPath nuGetPath = Substitute.For<INuGetPath>();
            IMonoExecutor monoExecuter = Substitute.For<IMonoExecutor>();
            IExternalProcess externalProcess = Substitute.For<IExternalProcess>();
            FileInfo file = new("packages.config");
            _ = nuGetPath.Executable.Returns(@"c:\DoesNotExist\nuget.exe");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _ = externalProcess.Run(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(new ProcessOutput("", "", 0));
            }
            else
            {
                _ = monoExecuter.CanRun().Returns(true);
                _ = monoExecuter.Run(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(new ProcessOutput("", "", 0));
            }
            NuGetFileRestoreCommand cmd = new(logger, nuGetPath, monoExecuter, externalProcess);

            await cmd.Invoke(file, NuGetSources.GlobalFeed);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _ = await externalProcess.Received(1).Run(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
                _ = await externalProcess.ReceivedWithAnyArgs().Run(Arg.Any<string>(), Arg.Any<string>(), $"restore {file.Name} - Source ${NuGetSources.GlobalFeed} -NonInteractive -PackagesDirectory ..\\packages", Arg.Any<bool>());
            }
            else
            {
                logger.DidNotReceiveWithAnyArgs().Error(Arg.Any<string>(), Arg.Any<System.Exception>());
                _ = await monoExecuter.Received(1).Run(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
                _ = await monoExecuter.ReceivedWithAnyArgs().Run(Arg.Any<string>(), Arg.Any<string>(), $"restore {file.Name} - Source ${NuGetSources.GlobalFeed} -NonInteractive -PackagesDirectory ..\\packages", Arg.Any<bool>());
            }
        }
    }
}
