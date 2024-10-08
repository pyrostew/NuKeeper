using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Update.Process;

using NUnit.Framework;

using System;
using System.IO;
using System.Threading.Tasks;

namespace NuKeeper.Integration.Tests.NuGet.Process
{
    [TestFixture]
    public class UpdateProjectImportsCommandTests
    {
        private readonly string _testWebApiProject =
            @"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <Import Project=""$(MSBuildBinPath)\Microsoft.CSharp.targets"" />
  <Import Project=""$(VSToolsPath)\WebApplications\Microsoft.WebApplication.targets"" Condition=""'$(VSToolsPath)' != ''"" />
  <Import Project=""$(VSToolsPath)\DummyImportWithoutCondition\Microsoft.WebApplication.targets"" />
</Project>";

        private readonly string _projectWithReference =
            @"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003""><ItemGroup><ProjectReference Include=""{importPath}"" /></ItemGroup></Project>";

        private readonly string _unpatchedImport =
            @"<Import Project=""$(VSToolsPath)\WebApplications\Microsoft.WebApplication.targets"" Condition=""'$(VSToolsPath)' != ''"" />";

        private readonly string _patchedImport =
            @"<Import Project=""$(VSToolsPath)\WebApplications\Microsoft.WebApplication.targets"" Condition=""Exists('$(VSToolsPath)\WebApplications\Microsoft.WebApplication.targets')"" />";

        [Test]
        public async Task ShouldUpdateConditionOnTaskImport()
        {
            string workDirectory = Path.Combine(TestContext.CurrentContext.WorkDirectory,
                nameof(ShouldUpdateConditionOnTaskImport));

            _ = Directory.CreateDirectory(workDirectory);
            string projectName = nameof(ShouldUpdateConditionOnTaskImport) + ".csproj";
            string projectPath = Path.Combine(workDirectory, projectName);
            await File.WriteAllTextAsync(projectPath, _testWebApiProject);

            UpdateProjectImportsCommand subject = new();

            PackageInProject package = new("acme", "1.0.0",
                new PackagePath(workDirectory, projectName, PackageReferenceType.ProjectFileOldStyle));

            await subject.Invoke(package, null, null, NuGetSources.GlobalFeed);

            string updatedContents = await File.ReadAllTextAsync(projectPath);

            Assert.That(updatedContents, Does.Not.Contain(_unpatchedImport));
            Assert.That(updatedContents, Does.Contain(_patchedImport));
        }

        [Test]
        public async Task ShouldFollowResolvableImports()
        {
            string workDirectory = Path.Combine(TestContext.CurrentContext.WorkDirectory,
                nameof(ShouldFollowResolvableImports));

            _ = Directory.CreateDirectory(workDirectory);

            string projectName = nameof(ShouldFollowResolvableImports) + ".csproj";
            string projectPath = Path.Combine(workDirectory, projectName);
            await File.WriteAllTextAsync(projectPath, _testWebApiProject);

            string intermediateProject = Path.Combine(workDirectory, "Intermediate.csproj");
            string intermediateContents = _projectWithReference.Replace("{importPath}", projectPath, StringComparison.OrdinalIgnoreCase);
            await File.WriteAllTextAsync(intermediateProject, intermediateContents);

            string rootProject = Path.Combine(workDirectory, "RootProject.csproj");
            string rootContents = _projectWithReference.Replace("{importPath}",
                Path.Combine("..", nameof(ShouldFollowResolvableImports), "Intermediate.csproj"),
                StringComparison.OrdinalIgnoreCase);
            await File.WriteAllTextAsync(rootProject, rootContents);

            UpdateProjectImportsCommand subject = new();

            PackageInProject package = new("acme", "1.0.0",
                new PackagePath(workDirectory, "RootProject.csproj", PackageReferenceType.ProjectFileOldStyle));

            await subject.Invoke(package, null, null, NuGetSources.GlobalFeed);

            string updatedContents = await File.ReadAllTextAsync(projectPath);

            Assert.That(updatedContents, Does.Not.Contain(_unpatchedImport));
            Assert.That(updatedContents, Does.Contain(_patchedImport));
        }
    }
}
