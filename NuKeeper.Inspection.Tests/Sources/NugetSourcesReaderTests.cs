using NSubstitute;

using NuGet.Configuration;

using NuKeeper.Abstractions.Inspections.Files;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Inspection.Files;
using NuKeeper.Inspection.Sources;

using NUnit.Framework;

using System.IO;
using System.Linq;

namespace NuKeeper.Inspection.Tests.Sources
{
    public class NugetSourcesReaderTests
    {
        private IFolder _uniqueTemporaryFolder;

        [SetUp]
        public void Setup()
        {
            _uniqueTemporaryFolder = TemporaryFolder();
        }

        [TearDown]
        public void TearDown()
        {
            _uniqueTemporaryFolder.TryDelete();
        }

        [Test]
        public void OverrideSourcesAreUsedWhenSupplied()
        {
            NuGetSources overrrideSources = new("overrideA");
            INuGetSourcesReader reader = MakeNuGetSourcesReader();

            NuGetSources result = reader.Read(_uniqueTemporaryFolder, overrrideSources);

            Assert.That(result, Is.EqualTo(overrrideSources));
        }

        [Test]
        public void GlobalFeedIsUsedAsLastResort()
        {
            INuGetSourcesReader reader = MakeNuGetSourcesReader();

            NuGetSources result = reader.Read(_uniqueTemporaryFolder, null);

            Assert.That(result.Items.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(result.Items, Does.Contain(new PackageSource("https://api.nuget.org/v3/index.json", "nuget.org")));
        }

        private const string ConfigFileContents =
            @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""From A file"" value=""https://fromFile1.com"" />
  </packageSources>
</configuration>";

        [Test]
        public void ConfigFileIsUsed()
        {
            INuGetSourcesReader reader = MakeNuGetSourcesReader();

            IFolder folder = _uniqueTemporaryFolder;
            string path = Path.Join(folder.FullPath, "nuget.config");
            File.WriteAllText(path, ConfigFileContents);

            NuGetSources result = reader.Read(folder, null);

            Assert.That(result.Items.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(result.Items.First(), Is.EqualTo(new PackageSource("https://fromFile1.com", "From A file")));
        }


        [Test]
        public void SettingsOverridesConfigFile()
        {
            INuGetSourcesReader reader = MakeNuGetSourcesReader();

            IFolder folder = _uniqueTemporaryFolder;
            string path = Path.Join(folder.FullPath, "nuget.config");
            File.WriteAllText(path, ConfigFileContents);

            NuGetSources result = reader.Read(folder, new NuGetSources("https://fromConfigA.com"));

            Assert.That(result.Items.Count, Is.EqualTo(1));
            Assert.That(result.Items.First(), Is.EqualTo(new PackageSource("https://fromConfigA.com")));
        }

        private static IFolder TemporaryFolder()
        {
            FolderFactory ff = new(Substitute.For<INuKeeperLogger>());
            return ff.UniqueTemporaryFolder();
        }

        private static INuGetSourcesReader MakeNuGetSourcesReader()
        {
            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();
            return new NuGetSourcesReader(
                new NuGetConfigFileReader
                    (logger), logger);
        }
    }
}
