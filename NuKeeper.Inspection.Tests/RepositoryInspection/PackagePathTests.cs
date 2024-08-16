using NuKeeper.Abstractions.RepositoryInspection;

using NUnit.Framework;

using System.IO;

namespace NuKeeper.Inspection.Tests.RepositoryInspection
{
    [TestFixture]
    public class PackagePathTests
    {
        private string _baseDirectory;

        [SetUp]
        public void SetUp()
        {
            _baseDirectory = OsSpecifics.GenerateBaseDirectory();
        }

        [Test]
        public void ConstructorShouldProduceExpectedSimplePropsForProjectFile()
        {
            char sep = Path.DirectorySeparatorChar;

            PackagePath path = new(
                _baseDirectory,
                $"{sep}checkout1{sep}src{sep}myproj.csproj",
                PackageReferenceType.ProjectFile);

            Assert.That(path.BaseDirectory, Is.EqualTo(_baseDirectory));
            Assert.That(path.RelativePath, Is.EqualTo($"checkout1{sep}src{sep}myproj.csproj"));
            Assert.That(path.PackageReferenceType, Is.EqualTo(PackageReferenceType.ProjectFile));
        }

        [Test]
        public void ConstructorShouldProduceExpectedSimplePropsForPackagesConfigFile()
        {
            char sep = Path.DirectorySeparatorChar;
            PackagePath path = new(
                _baseDirectory,
                $"{sep}checkout1{sep}src{sep}packages.config",
                PackageReferenceType.PackagesConfig);

            Assert.That(path.BaseDirectory, Is.EqualTo(_baseDirectory));
            Assert.That(path.RelativePath, Is.EqualTo($"checkout1{sep}src{sep}packages.config"));
            Assert.That(path.PackageReferenceType, Is.EqualTo(PackageReferenceType.PackagesConfig));
        }

        [Test]
        public void ConstructorShouldProduceExpectedParsedFileName()
        {
            char sep = Path.DirectorySeparatorChar;
            PackagePath path = new(
                _baseDirectory,
                $"checkout1{sep}src{sep}myproj.csproj",
                PackageReferenceType.ProjectFile);


            Assert.That(path.Info.Name, Is.EqualTo("myproj.csproj"));
        }

        [Test]
        public void ConstructorShouldProduceExpectedParsedFullPath()
        {
            char sep = Path.DirectorySeparatorChar;
            PackagePath path = new(
                _baseDirectory,
                $"checkout1{sep}src{sep}myproj.csproj",
                PackageReferenceType.ProjectFile);


            Assert.That(path.Info.DirectoryName, Is.EqualTo($"{_baseDirectory}{sep}checkout1{sep}src"));
            Assert.That(path.FullName, Is.EqualTo($"{_baseDirectory}{sep}checkout1{sep}src{sep}myproj.csproj"));
        }

        [Test]
        public void ConstructorShouldProduceExpectedInfoForProjectFile()
        {
            char sep = Path.DirectorySeparatorChar;

            PackagePath path = new(
                _baseDirectory,
                $"{sep}checkout1{sep}src{sep}myproj.csproj",
                PackageReferenceType.ProjectFile);

            Assert.That(path.Info, Is.Not.Null);
            Assert.That(path.Info.Name, Is.EqualTo("myproj.csproj"));
        }

        [Test]
        public void ConstructorShouldProduceExpectedPathForProjectFile()
        {
            char sep = Path.DirectorySeparatorChar;

            PackagePath path = new(
                _baseDirectory,
                $"{sep}checkout1{sep}src{sep}myproj.csproj",
                PackageReferenceType.ProjectFile);

            Assert.That(path.Info, Is.Not.Null);
            Assert.That(path.Info.DirectoryName, Is.EqualTo($"{_baseDirectory}{sep}checkout1{sep}src"));
            Assert.That(path.Info.FullName, Is.EqualTo($"{_baseDirectory}{sep}checkout1{sep}src{sep}myproj.csproj"));
        }

        [Test]
        public void ConstructorShouldProduceExpectedParsedPropsWithExtraSlash()
        {
            char sep = Path.DirectorySeparatorChar;
            PackagePath path = new(
                _baseDirectory,
                $"{sep}checkout1{sep}src{sep}myproj.csproj",
                PackageReferenceType.ProjectFile);


            Assert.That(path.RelativePath, Is.EqualTo($"checkout1{sep}src{sep}myproj.csproj"));
            Assert.That(path.Info.Name, Is.EqualTo("myproj.csproj"));
        }

        [Test]
        public void ConstructorShouldProduceExpectedParsedFullPathWithExtraSlash()
        {
            char sep = Path.DirectorySeparatorChar;
            PackagePath path = new(
                _baseDirectory,
                $"{sep}checkout1{sep}src{sep}myproj.csproj",
                PackageReferenceType.ProjectFile);


            Assert.That(path.BaseDirectory, Is.EqualTo(_baseDirectory));
            Assert.That(path.Info.DirectoryName, Is.EqualTo($"{_baseDirectory}{sep}checkout1{sep}src"));
            Assert.That(path.FullName, Is.EqualTo($"{_baseDirectory}{sep}checkout1{sep}src{sep}myproj.csproj"));
        }
    }
}
