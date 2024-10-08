using NSubstitute;

using NuGet.Versioning;

using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Inspection.RepositoryInspection;

using NUnit.Framework;

using System;
using System.IO;
using System.Linq;
using System.Text;

namespace NuKeeper.Inspection.Tests.RepositoryInspection
{
    [TestFixture]
    public class NuspecFileReaderTests
    {
        private const string PackagesFileWithSinglePackage =
            @"<package><metadata><dependencies>
<dependency id=""foo"" version=""1.2.3.4"" /></dependencies></metadata></package>";

        private const string PackagesFileWithTwoPackages = @"<package><metadata><dependencies>
<dependency id=""foo"" version=""1.2.3.4"" />
<dependency id=""bar"" version=""2.3.4.5"" /></dependencies></metadata></package>";

        [Test]
        public void EmptyPackagesListShouldBeParsed()
        {
            const string emptyContents =
                @"<package/>";

            NuspecFileReader reader = MakeReader();
            System.Collections.Generic.IReadOnlyCollection<PackageInProject> packages = reader.Read(StreamFromString(emptyContents), TempPath());

            Assert.That(packages, Is.Not.Null);
            Assert.That(packages, Is.Empty);
        }

        [Test]
        public void SinglePackageShouldBeRead()
        {
            NuspecFileReader reader = MakeReader();
            System.Collections.Generic.IReadOnlyCollection<PackageInProject> packages = reader.Read(StreamFromString(PackagesFileWithSinglePackage), TempPath());

            Assert.That(packages, Is.Not.Null);
            Assert.That(packages, Is.Not.Empty);
        }

        [Test]
        public void SinglePackageShouldBePopulated()
        {
            NuspecFileReader reader = MakeReader();
            System.Collections.Generic.IReadOnlyCollection<PackageInProject> packages = reader.Read(StreamFromString(PackagesFileWithSinglePackage), TempPath());

            PackageInProject package = packages.FirstOrDefault();
            PackageAssert.IsPopulated(package);
        }

        [Test]
        public void SinglePackageShouldBeCorrect()
        {
            NuspecFileReader reader = MakeReader();
            System.Collections.Generic.IReadOnlyCollection<PackageInProject> packages = reader.Read(StreamFromString(PackagesFileWithSinglePackage), TempPath());

            PackageInProject package = packages.FirstOrDefault();

            Assert.That(package, Is.Not.Null);
            Assert.That(package.Id, Is.EqualTo("foo"));
            Assert.That(package.Version, Is.EqualTo(new NuGetVersion("1.2.3.4")));
            Assert.That(package.Path.PackageReferenceType, Is.EqualTo(PackageReferenceType.Nuspec));
        }

        [Test]
        public void TwoPackagesShouldBePopulated()
        {
            NuspecFileReader reader = MakeReader();
            System.Collections.Generic.List<PackageInProject> packages = reader.Read(StreamFromString(PackagesFileWithTwoPackages), TempPath())
                .ToList();

            Assert.That(packages, Is.Not.Null);
            Assert.That(packages.Count, Is.EqualTo(2));

            PackageAssert.IsPopulated(packages[0]);
            PackageAssert.IsPopulated(packages[1]);
        }

        [Test]
        public void TwoPackagesShouldBeRead()
        {
            NuspecFileReader reader = MakeReader();
            System.Collections.Generic.List<PackageInProject> packages = reader.Read(StreamFromString(PackagesFileWithTwoPackages), TempPath())
                .ToList();

            Assert.That(packages.Count, Is.EqualTo(2));

            Assert.That(packages[0].Id, Is.EqualTo("foo"));
            Assert.That(packages[0].Version, Is.EqualTo(new NuGetVersion("1.2.3.4")));

            Assert.That(packages[1].Id, Is.EqualTo("bar"));
            Assert.That(packages[1].Version, Is.EqualTo(new NuGetVersion("2.3.4.5")));
        }

        [Test]
        public void ResultIsReiterable()
        {
            PackagePath path = TempPath();

            NuspecFileReader reader = MakeReader();
            System.Collections.Generic.IReadOnlyCollection<PackageInProject> packages = reader.Read(StreamFromString(PackagesFileWithTwoPackages), path);

            foreach (PackageInProject package in packages)
            {
                PackageAssert.IsPopulated(package);
            }

            Assert.That(packages.Select(p => p.Path), Is.All.EqualTo(path));
        }

        [Test]
        public void WhenOnePackageCannotBeRead_TheOthersAreStillRead()
        {
            string badVersion = PackagesFileWithTwoPackages.Replace("1.2.3.4", "notaversion", StringComparison.OrdinalIgnoreCase);

            NuspecFileReader reader = MakeReader();
            System.Collections.Generic.List<PackageInProject> packages = reader.Read(StreamFromString(badVersion), TempPath())
                .ToList();

            Assert.That(packages.Count, Is.EqualTo(1));
            PackageAssert.IsPopulated(packages[0]);
        }

        private static PackagePath TempPath()
        {
            return new PackagePath(
                OsSpecifics.GenerateBaseDirectory(),
                Path.Combine("src", "sample.nuspec"),
                PackageReferenceType.Nuspec);
        }

        private static NuspecFileReader MakeReader()
        {
            return new NuspecFileReader(Substitute.For<INuKeeperLogger>());
        }

        private static Stream StreamFromString(string contents)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(contents));
        }
    }
}
