using NuKeeper.Update.Process;

using NUnit.Framework;

namespace NuKeeper.Integration.Tests.NuGet.Process
{
    [TestFixture]
    public class NuGetPathTests : TestWithFailureLogging
    {
        [Test]
        public void HasNugetPath()
        {
            string nugetPath = new NuGetPath(NukeeperLogger).Executable;

            Assert.That(nugetPath, Is.Not.Empty);
            Assert.That(nugetPath, Does.Exist);
        }
    }
}
