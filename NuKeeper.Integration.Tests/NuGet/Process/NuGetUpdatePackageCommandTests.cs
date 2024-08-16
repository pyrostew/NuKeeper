using NuGet.Configuration;
using NuGet.Versioning;

using NuKeeper.Abstractions.Inspections.Files;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Inspection.Files;
using NuKeeper.Update.Process;
using NuKeeper.Update.ProcessRunner;

using NUnit.Framework;

using System;
using System.IO;
using System.Threading.Tasks;

namespace NuKeeper.Integration.Tests.NuGet.Process
{
    [TestFixture]
    public class NuGetUpdatePackageCommandTests : TestWithFailureLogging
    {
        private readonly string _testDotNetClassicProject =
            @"<Project ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <TargetFrameworkVersion>v4.7</TargetFrameworkVersion>
  </PropertyGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";

        private readonly string _testPackagesConfig =
            @"<packages><package id=""Microsoft.AspNet.WebApi.Client"" version=""{packageVersion}"" targetFramework=""net47"" /></packages>";

        private readonly string _nugetConfig =
            @"<configuration><config><add key=""repositoryPath"" value="".\packages"" /></config></configuration>";

        private IFolder _uniqueTemporaryFolder;

        [SetUp]
        public void Setup()
        {
            _uniqueTemporaryFolder = UniqueTemporaryFolder();
        }

        [TearDown]
        public void TearDown()
        {
            _uniqueTemporaryFolder.TryDelete();
        }

        [Test]
        public async Task ShouldUpdateDotnetClassicProject()
        {
            const string oldPackageVersion = "5.2.3";
            const string newPackageVersion = "5.2.4";
            const string expectedPackageString =
                "<package id=\"Microsoft.AspNet.WebApi.Client\" version=\"{packageVersion}\" targetFramework=\"net47\" />";
            const string testFolder = nameof(ShouldUpdateDotnetClassicProject);

            string testProject = $"{testFolder}.csproj";

            string workDirectory = Path.Combine(_uniqueTemporaryFolder.FullPath, testFolder);
            _ = Directory.CreateDirectory(workDirectory);
            string packagesFolder = Path.Combine(workDirectory, "packages");
            _ = Directory.CreateDirectory(packagesFolder);

            string projectContents = _testDotNetClassicProject.Replace("{packageVersion}", oldPackageVersion,
                StringComparison.OrdinalIgnoreCase);
            string projectPath = Path.Combine(workDirectory, testProject);
            await File.WriteAllTextAsync(projectPath, projectContents);

            string packagesConfigContents = _testPackagesConfig.Replace("{packageVersion}", oldPackageVersion,
                StringComparison.OrdinalIgnoreCase);
            string packagesConfigPath = Path.Combine(workDirectory, "packages.config");
            await File.WriteAllTextAsync(packagesConfigPath, packagesConfigContents);

            await File.WriteAllTextAsync(Path.Combine(workDirectory, "nuget.config"), _nugetConfig);

            Abstractions.Logging.INuKeeperLogger logger = NukeeperLogger;
            ExternalProcess externalProcess = new(logger);

            MonoExecutor monoExecutor = new(logger, externalProcess);

            NuGetPath nuGetPath = new(logger);
            NuGetVersion nuGetVersion = new(newPackageVersion);
            PackageSource packageSource = new(NuGetConstants.V3FeedUrl);

            NuGetFileRestoreCommand restoreCommand = new(logger, nuGetPath, monoExecutor, externalProcess);
            NuGetUpdatePackageCommand updateCommand = new(logger, nuGetPath, monoExecutor, externalProcess);

            PackageInProject packageToUpdate = new("Microsoft.AspNet.WebApi.Client", oldPackageVersion,
                new PackagePath(workDirectory, testProject, PackageReferenceType.PackagesConfig));

            await restoreCommand.Invoke(packageToUpdate, nuGetVersion, packageSource, NuGetSources.GlobalFeed);

            await updateCommand.Invoke(packageToUpdate, nuGetVersion, packageSource, NuGetSources.GlobalFeed);

            string contents = await File.ReadAllTextAsync(packagesConfigPath);
            Assert.That(contents,
                Does.Contain(expectedPackageString.Replace("{packageVersion}", newPackageVersion,
                    StringComparison.OrdinalIgnoreCase)));
            Assert.That(contents,
                Does.Not.Contain(expectedPackageString.Replace("{packageVersion}", oldPackageVersion,
                    StringComparison.OrdinalIgnoreCase)));
        }

        private IFolder UniqueTemporaryFolder()
        {
            FolderFactory factory = new(NukeeperLogger);
            return factory.UniqueTemporaryFolder();
        }
    }
}
