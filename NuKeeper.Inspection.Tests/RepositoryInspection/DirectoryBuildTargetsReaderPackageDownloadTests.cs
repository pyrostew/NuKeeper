﻿using NSubstitute;

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
    public class DirectoryBuildTargetsReaderPackageDownloadTests
    {
        private const string PackagesFileWithSinglePackage =
            @"<Project><ItemGroup><PackageDownload Include=""foo"" Version=""[1.2.3.4]"" /></ItemGroup></Project>";

        private const string PackagesFileWithTwoPackages = @"<Project><ItemGroup>
<PackageDownload Include=""foo"" Version=""[1.2.3.4]"" />
<PackageDownload Update=""bar"" Version=""[2.3.4.5]"" /></ItemGroup></Project>";

        [Test]
        public void EmptyPackagesListShouldBeParsed()
        {
            const string emptyContents =
                @"<Project/>";

            DirectoryBuildTargetsReader reader = MakeReader();
            System.Collections.Generic.IReadOnlyCollection<PackageInProject> packages = reader.Read(StreamFromString(emptyContents), TempPath());

            Assert.That(packages, Is.Not.Null);
            Assert.That(packages, Is.Empty);
        }

        [Test]
        public void SinglePackageShouldBeRead()
        {
            DirectoryBuildTargetsReader reader = MakeReader();
            System.Collections.Generic.IReadOnlyCollection<PackageInProject> packages = reader.Read(StreamFromString(PackagesFileWithSinglePackage), TempPath());

            Assert.That(packages, Is.Not.Null);
            Assert.That(packages, Is.Not.Empty);
        }

        [Test]
        public void SinglePackageShouldBePopulated()
        {
            DirectoryBuildTargetsReader reader = MakeReader();
            System.Collections.Generic.IReadOnlyCollection<PackageInProject> packages = reader.Read(StreamFromString(PackagesFileWithSinglePackage), TempPath());

            PackageInProject package = packages.FirstOrDefault();
            PackageAssert.IsPopulated(package);
        }

        [Test]
        public void SinglePackageFromVerboseFormatShouldBePopulated()
        {
            const string verboseFormatVersion =
                @"<Project><ItemGroup><PackageDownload Include=""foo""><PrivateAssets>all</PrivateAssets><Version>1.2.3.4</Version></PackageDownload></ItemGroup></Project>";

            DirectoryBuildTargetsReader reader = MakeReader();
            System.Collections.Generic.IReadOnlyCollection<PackageInProject> packages = reader.Read(StreamFromString(verboseFormatVersion), TempPath());

            PackageInProject package = packages.FirstOrDefault();
            PackageAssert.IsPopulated(package);
        }

        [Test]
        public void SinglePackageShouldBeCorrect()
        {
            DirectoryBuildTargetsReader reader = MakeReader();
            System.Collections.Generic.IReadOnlyCollection<PackageInProject> packages = reader.Read(StreamFromString(PackagesFileWithSinglePackage), TempPath());

            PackageInProject package = packages.FirstOrDefault();

            Assert.That(package, Is.Not.Null);
            Assert.That(package.Id, Is.EqualTo("foo"));
            Assert.That(package.Version, Is.EqualTo(new NuGetVersion("1.2.3.4")));
            Assert.That(package.Path.PackageReferenceType, Is.EqualTo(PackageReferenceType.DirectoryBuildTargets));
        }

        [Test]
        public void TwoPackagesShouldBePopulated()
        {
            DirectoryBuildTargetsReader reader = MakeReader();
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
            DirectoryBuildTargetsReader reader = MakeReader();
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

            DirectoryBuildTargetsReader reader = MakeReader();
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

            DirectoryBuildTargetsReader reader = MakeReader();
            System.Collections.Generic.List<PackageInProject> packages = reader.Read(StreamFromString(badVersion), TempPath())
                .ToList();

            Assert.That(packages.Count, Is.EqualTo(1));
            PackageAssert.IsPopulated(packages[0]);
        }

        private static PackagePath TempPath()
        {
            return new PackagePath(
                OsSpecifics.GenerateBaseDirectory(),
                Path.Combine("src", "Directory.Build.Props"),
                PackageReferenceType.DirectoryBuildTargets);
        }

        private static DirectoryBuildTargetsReader MakeReader()
        {
            return new DirectoryBuildTargetsReader(Substitute.For<INuKeeperLogger>());
        }

        private static Stream StreamFromString(string contents)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(contents));
        }
    }
}