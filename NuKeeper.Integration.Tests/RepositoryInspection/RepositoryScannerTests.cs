using NuGet.Versioning;

using NuKeeper.Abstractions.Inspections.Files;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Inspection.Files;
using NuKeeper.Inspection.RepositoryInspection;

using NUnit.Framework;

using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NuKeeper.Integration.Tests.RepositoryInspection
{
    [TestFixture]
    public class RepositoryScannerTests : TestWithFailureLogging
    {
        private const string SinglePackageInFile =
            @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""foo"" version=""1.2.3"" targetFramework=""net45"" />
</packages>";
        private const string Vs2017ProjectFileTemplateWithPackages =
            @"<Project>
  <ItemGroup>
<PackageReference Include=""foo"" Version=""1.2.3""></PackageReference>
  </ItemGroup>
</Project>";

        private const string NuspecWithDependency =
            @"<package><metadata><dependencies>
<dependency id=""foo"" version=""3.3.3.5"" /></dependencies></metadata></package>";

        private const string DirectoryBuildProps =
            @"<Project>
  <ItemGroup>
    <PackageReference Include=""foo"" Version=""1.2.3""></PackageReference>
  </ItemGroup>
</Project>";

        private const string DirectoryBuildTargetsWithManyItemGroups =
            @"<Project>
  <ItemGroup>
    <PackageReference Include=""foo"" Version=""1.2.3""></PackageReference>
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""foo2"" Version=""3.2.1""></PackageReference>
  </ItemGroup>
</Project>";

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
        public void ValidEmptyDirectoryWorks()
        {
            IRepositoryScanner scanner = MakeScanner();

            System.Collections.Generic.IReadOnlyCollection<PackageInProject> results = scanner.FindAllNuGetPackages(_uniqueTemporaryFolder);

            Assert.That(results, Is.Not.Null);
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void FindsPackagesConfig()
        {
            IRepositoryScanner scanner = MakeScanner();

            WriteFile(_uniqueTemporaryFolder, "packages.config", SinglePackageInFile);

            System.Collections.Generic.IReadOnlyCollection<PackageInProject> results = scanner.FindAllNuGetPackages(_uniqueTemporaryFolder);

            Assert.That(results, Has.Count.EqualTo(1));
        }

        [Test]
        public void CorrectItemInPackagesConfig()
        {
            IRepositoryScanner scanner = MakeScanner();

            WriteFile(_uniqueTemporaryFolder, "packages.config", SinglePackageInFile);

            System.Collections.Generic.IReadOnlyCollection<PackageInProject> results = scanner.FindAllNuGetPackages(_uniqueTemporaryFolder);

            PackageInProject item = results.FirstOrDefault();

            Assert.That(item, Is.Not.Null);
            Assert.That(item.Id, Is.EqualTo("foo"));
            Assert.That(item.Version, Is.EqualTo(new NuGetVersion(1, 2, 3)));
            Assert.That(item.Path.PackageReferenceType, Is.EqualTo(PackageReferenceType.PackagesConfig));
        }

        [Test]
        public void FindsCsprojFile()
        {
            IRepositoryScanner scanner = MakeScanner();

            WriteFile(_uniqueTemporaryFolder, "sample.csproj", Vs2017ProjectFileTemplateWithPackages);

            System.Collections.Generic.IReadOnlyCollection<PackageInProject> results = scanner.FindAllNuGetPackages(_uniqueTemporaryFolder);

            Assert.That(results, Has.Count.EqualTo(1));
        }

        [Test]
        public void FindsVbprojFile()
        {
            IRepositoryScanner scanner = MakeScanner();

            WriteFile(_uniqueTemporaryFolder, "sample.vbproj", Vs2017ProjectFileTemplateWithPackages);

            System.Collections.Generic.IReadOnlyCollection<PackageInProject> results = scanner.FindAllNuGetPackages(_uniqueTemporaryFolder);

            Assert.That(results, Has.Count.EqualTo(1));
        }

        [Test]
        public void FindsFsprojFile()
        {
            IRepositoryScanner scanner = MakeScanner();

            WriteFile(_uniqueTemporaryFolder, "sample.fsproj", Vs2017ProjectFileTemplateWithPackages);

            System.Collections.Generic.IReadOnlyCollection<PackageInProject> results = scanner.FindAllNuGetPackages(_uniqueTemporaryFolder);

            Assert.That(results, Has.Count.EqualTo(1));
        }

        [Test]
        public void FindsNuspec()
        {
            IRepositoryScanner scanner = MakeScanner();

            WriteFile(_uniqueTemporaryFolder, "sample.nuspec", NuspecWithDependency);

            System.Collections.Generic.IReadOnlyCollection<PackageInProject> results = scanner.FindAllNuGetPackages(_uniqueTemporaryFolder);

            Assert.That(results, Has.Count.EqualTo(1));
        }

        [Test]
        public void CorrectItemInCsProjFile()
        {
            IRepositoryScanner scanner = MakeScanner();

            WriteFile(_uniqueTemporaryFolder, "sample.csproj", Vs2017ProjectFileTemplateWithPackages);

            System.Collections.Generic.IReadOnlyCollection<PackageInProject> results = scanner.FindAllNuGetPackages(_uniqueTemporaryFolder);

            PackageInProject item = results.FirstOrDefault();

            Assert.That(item, Is.Not.Null);
            Assert.That(item.Id, Is.EqualTo("foo"));
            Assert.That(item.Version, Is.EqualTo(new NuGetVersion(1, 2, 3)));
            Assert.That(item.Path.PackageReferenceType, Is.EqualTo(PackageReferenceType.ProjectFile));
        }

        [Test]
        public void CorrectItemInDirectoryBuildProps()
        {
            IRepositoryScanner scanner = MakeScanner();

            WriteFile(_uniqueTemporaryFolder, "Directory.Build.props", DirectoryBuildProps);

            System.Collections.Generic.IReadOnlyCollection<PackageInProject> results = scanner.FindAllNuGetPackages(_uniqueTemporaryFolder);

            PackageInProject item = results.FirstOrDefault();

            Assert.That(item, Is.Not.Null);
            Assert.That(item.Id, Is.EqualTo("foo"));
            Assert.That(item.Version, Is.EqualTo(new NuGetVersion(1, 2, 3)));
            Assert.That(item.Path.PackageReferenceType, Is.EqualTo(PackageReferenceType.DirectoryBuildTargets));
        }

        [Test]
        public void CorrectItemsInDirectoryBuildTargets()
        {
            IRepositoryScanner scanner = MakeScanner();

            WriteFile(_uniqueTemporaryFolder, "Directory.Build.targets", DirectoryBuildTargetsWithManyItemGroups);

            System.Collections.Generic.IReadOnlyCollection<PackageInProject> results = scanner.FindAllNuGetPackages(_uniqueTemporaryFolder);

            PackageInProject item = results.FirstOrDefault();
            PackageInProject item2 = results.Skip(1).FirstOrDefault();

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(item, Is.Not.Null);
            Assert.That(item.Id, Is.EqualTo("foo"));
            Assert.That(item.Version, Is.EqualTo(new NuGetVersion(1, 2, 3)));
            Assert.That(item.Path.PackageReferenceType, Is.EqualTo(PackageReferenceType.DirectoryBuildTargets));
            Assert.That(item2, Is.Not.Null);
            Assert.That(item2.Id, Is.EqualTo("foo2"));
            Assert.That(item2.Version, Is.EqualTo(new NuGetVersion(3, 2, 1)));
            Assert.That(item2.Path.PackageReferenceType, Is.EqualTo(PackageReferenceType.DirectoryBuildTargets));
        }

        [Test]
        public void CorrectItemsInPackagesProps()
        {
            IRepositoryScanner scanner = MakeScanner();

            WriteFile(_uniqueTemporaryFolder, "Packages.props", DirectoryBuildProps);

            System.Collections.Generic.IReadOnlyCollection<PackageInProject> results = scanner.FindAllNuGetPackages(_uniqueTemporaryFolder);

            PackageInProject item = results.FirstOrDefault();

            Assert.That(item, Is.Not.Null);
            Assert.That(item.Id, Is.EqualTo("foo"));
            Assert.That(item.Version, Is.EqualTo(new NuGetVersion(1, 2, 3)));
            Assert.That(item.Path.PackageReferenceType, Is.EqualTo(PackageReferenceType.DirectoryBuildTargets));
        }

        [Test]
        public void SelfTest()
        {
            IRepositoryScanner scanner = MakeScanner();
            Folder baseFolder = new(NukeeperLogger, GetOwnRootDir());

            System.Collections.Generic.IReadOnlyCollection<PackageInProject> results = scanner.FindAllNuGetPackages(baseFolder);

            Assert.That(results, Is.Not.Null, "in folder" + baseFolder.FullPath);
            Assert.That(results, Is.Not.Empty, "in folder" + baseFolder.FullPath);

        }

        private static DirectoryInfo GetOwnRootDir()
        {
            // If the test is running on (real example)
            // "C:\Code\NuKeeper\NuKeeper.Tests\bin\Debug\netcoreapp1.1\NuKeeper.dll"
            // then the app root directory to scan is "C:\Code\NuKeeper\"
            // So go up four dir levels to the root
            // Self is a convenient source of a valid project to scan
            string fullPath = new Uri(typeof(RepositoryScanner).GetTypeInfo().Assembly.Location).LocalPath;
            string runDir = Path.GetDirectoryName(fullPath);

            DirectoryInfo projectRootDir = Directory.GetParent(runDir).Parent.Parent.Parent;
            return projectRootDir;
        }

        private IFolder UniqueTemporaryFolder()
        {
            FolderFactory folderFactory = new(NukeeperLogger);
            return folderFactory.UniqueTemporaryFolder();
        }

        private IRepositoryScanner MakeScanner()
        {
            Abstractions.Logging.INuKeeperLogger logger = NukeeperLogger;
            return new RepositoryScanner(
                new ProjectFileReader(logger),
                new PackagesFileReader(logger),
                new NuspecFileReader(logger),
                new DirectoryBuildTargetsReader(logger),
                new DirectoryExclusions());
        }

        private static void WriteFile(IFolder path, string fileName, string contents)
        {
            using StreamWriter file = File.CreateText(Path.Combine(path.FullPath, fileName));
            file.WriteLine(contents);
        }
    }
}
