using NSubstitute;

using NuKeeper.Abstractions.Logging;
using NuKeeper.Update.Process;
using NuKeeper.Update.ProcessRunner;

using NUnit.Framework;

using System;
using System.Threading.Tasks;

namespace NuKeeper.Update.Tests.Process
{
    [TestFixture]
    public class MonoExecutorTests
    {
        [TestCase(0, true)]
        [TestCase(1, false)]
        public async Task WhenCallingCanRun_ShouldCheckExternalProcessResult(int exitCode, bool expectedCanExecute)
        {
            INuKeeperLogger nuKeeperLogger = Substitute.For<INuKeeperLogger>();
            IExternalProcess externalProcess = Substitute.For<IExternalProcess>();

            _ = externalProcess.Run("", "mono", "--version", false).
                Returns(new ProcessOutput("", "", exitCode));

            MonoExecutor monoExecutor = new(nuKeeperLogger, externalProcess);

            bool canRun = await monoExecutor.CanRun();

            Assert.That(expectedCanExecute, Is.EqualTo(canRun));
        }

        [Test]
        public async Task WhenCallingCanRun_ShouldOnlyCallExternalProcessOnce()
        {
            INuKeeperLogger nuKeeperLogger = Substitute.For<INuKeeperLogger>();
            IExternalProcess externalProcess = Substitute.For<IExternalProcess>();

            _ = externalProcess.Run("", "mono", "--version", false).
                Returns(new ProcessOutput("", "", 0));

            MonoExecutor monoExecutor = new(nuKeeperLogger, externalProcess);

            _ = await monoExecutor.CanRun();
            _ = await monoExecutor.CanRun();
            _ = await monoExecutor.CanRun();

            _ = await externalProcess.Received(1).Run(
                "",
                "mono",
                "--version",
                false);
        }

        [Test]
        public void WhenCallingRun_ShouldThrowIfMonoWasNotFound()
        {
            INuKeeperLogger nuKeeperLogger = Substitute.For<INuKeeperLogger>();
            IExternalProcess externalProcess = Substitute.For<IExternalProcess>();

            _ = externalProcess.Run("", "mono", "--version", false).
                Returns(new ProcessOutput("", "", 1));

            MonoExecutor monoExecutor = new(nuKeeperLogger, externalProcess);

            _ = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await monoExecutor.Run("wd", "command", "args", true));
        }

        [Test]
        public async Task WhenCallingRun_ShouldPassArgumentToUnderlyingExternalProcess()
        {
            INuKeeperLogger nuKeeperLogger = Substitute.For<INuKeeperLogger>();
            IExternalProcess externalProcess = Substitute.For<IExternalProcess>();

            _ = externalProcess.Run("", "mono", "--version", false).
                Returns(new ProcessOutput("", "", 0));

            MonoExecutor monoExecutor = new(nuKeeperLogger, externalProcess);
            _ = await monoExecutor.Run("wd", "command", "args", true);

            _ = await externalProcess.Received(1).Run("wd", "mono", "command args", true);
        }
    }
}
