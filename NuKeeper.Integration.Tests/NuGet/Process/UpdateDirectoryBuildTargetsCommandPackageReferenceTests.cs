using NuGet.Versioning;

using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Update.Process;

using NUnit.Framework;

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NuKeeper.Integration.Tests.NuGet.Process
{
    [TestFixture]
    public class UpdateDirectoryBuildTargetsCommandPackageReferenceTests : TestWithFailureLogging
    {
        private readonly string _testFileWithUpdate =
@"<Project><ItemGroup><PackageReference Update=""foo"" Version=""{packageVersion}"" /></ItemGroup></Project>";

        private readonly string _testFileWithInclude =
@"<Project><ItemGroup><PackageReference Include=""foo"" Version=""{packageVersion}"" /></ItemGroup></Project>";

        [Test]
        public async Task ShouldUpdateValidFileWithUpdateAttribute()
        {
            await ExecuteValidUpdateTest(_testFileWithUpdate, "<PackageReference Update=\"foo\" Version=\"{packageVersion}\" />");
        }

        [Test]
        public async Task ShouldUpdateValidFileWithIncludeAttribute()
        {
            await ExecuteValidUpdateTest(_testFileWithInclude, "<PackageReference Include=\"foo\" Version=\"{packageVersion}\" />");
        }

        [Test]
        public async Task ShouldUpdateValidFileWithIncludeAndVerboseVersion()
        {
            await ExecuteValidUpdateTest(
                @"<Project><ItemGroup><PackageReference Include=""foo""><Version>{packageVersion}</Version></PackageReference></ItemGroup></Project>",
                @"<Version>{packageVersion}</Version>");
        }

        private async Task ExecuteValidUpdateTest(string testProjectContents, string expectedPackageString, [CallerMemberName] string memberName = "")
        {
            const string oldPackageVersion = "5.2.31";
            const string newPackageVersion = "5.3.4";

            string testFolder = memberName;
            string testFile = "Directory.Build.props";
            string workDirectory = Path.Combine(TestContext.CurrentContext.WorkDirectory, testFolder);
            _ = Directory.CreateDirectory(workDirectory);
            string projectContents = testProjectContents.Replace("{packageVersion}", oldPackageVersion, StringComparison.OrdinalIgnoreCase);
            string projectPath = Path.Combine(workDirectory, testFile);
            await File.WriteAllTextAsync(projectPath, projectContents);

            UpdateDirectoryBuildTargetsCommand command = new(NukeeperLogger);

            PackageInProject package = new("foo", oldPackageVersion,
                new PackagePath(workDirectory, testFile, PackageReferenceType.DirectoryBuildTargets));

            await command.Invoke(package, new NuGetVersion(newPackageVersion), null, NuGetSources.GlobalFeed);

            string contents = await File.ReadAllTextAsync(projectPath);
            Assert.That(contents, Does.Contain(expectedPackageString.Replace("{packageVersion}", newPackageVersion, StringComparison.OrdinalIgnoreCase)));
            Assert.That(contents, Does.Not.Contain(expectedPackageString.Replace("{packageVersion}", oldPackageVersion, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
