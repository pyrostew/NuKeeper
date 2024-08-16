using NuKeeper.Abstractions.NuGet;

using NUnit.Framework;

namespace NuKeeper.Abstractions.Tests.NuGet
{
    [TestFixture]
    public class NuGetSourcesTests
    {
        [Test]
        public void ShouldGenerateCommandLineArguments()
        {
            NuGetSources subject = new("one", "two");

            string result = subject.CommandLine("-s");

            Assert.That("-s one -s two" == result);
        }

        [Test]
        public void ShouldEscapeLocalPaths()
        {
            NuGetSources subject = new("file://one", "C:/Program Files (x86)/Microsoft SDKs/NuGetPackages/", "http://two");

            string result = subject.CommandLine("-s");

            Assert.That("-s file://one -s \"C:/Program Files (x86)/Microsoft SDKs/NuGetPackages/\" -s http://two" == result);
        }
    }
}
