using NSubstitute;

using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.NuGetApi;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Inspection.NuGetApi;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NuKeeper.Inspection.Tests.NuGetApi
{
    [TestFixture]
    public class PackageUpdatesLookupTests
    {
        private IApiPackageLookup _apiPackageLookup;

        [SetUp]
        public void Initialize()
        {
            _apiPackageLookup = Substitute.For<IApiPackageLookup>();

            _ = _apiPackageLookup
                .FindVersionUpdate(
                    Arg.Any<PackageIdentity>(),
                    Arg.Any<NuGetSources>(),
                    VersionChange.Minor,
                    Arg.Any<UsePrerelease>()
                )
                .Returns(ci =>
                    GetPackageLookupResult(
                        (PackageIdentity)ci[0],
                        (VersionChange)ci[2]
                    )
                );
        }

        [Test]
        public async Task FindUpdatesForPackages_OnePackageDifferentVersionsInDifferentProjects_RespectsAllowedChangeForEachVersionIndependently()
        {
            List<PackageInProject> packagesInProjects =
            [
                MakePackageInProject("foo.bar", "6.0.1", "root", "myprojectA"),
                MakePackageInProject("foo.bar", "10.2.1", "root", "myprojectB")
            ];
            PackageUpdatesLookup sut = MakePackageUpdatesLookup();

            IReadOnlyCollection<PackageUpdateSet> result = await sut.FindUpdatesForPackages(
                packagesInProjects,
                new NuGetSources("https://api.nuget.com/"),
                VersionChange.Minor,
                UsePrerelease.Never
            );

            List<string> versionUpdates = result.Select(p => p.SelectedVersion.ToNormalizedString()).ToList();
            Assert.That(versionUpdates, Has.Some.Matches<string>(version => version.StartsWith("6.", StringComparison.OrdinalIgnoreCase)));
            Assert.That(versionUpdates, Has.Some.Matches<string>(version => version.StartsWith("10.", StringComparison.OrdinalIgnoreCase)));
        }

        private static PackageLookupResult GetPackageLookupResult(
            PackageIdentity package,
            VersionChange versionChange
        )
        {
            string packageName = package.Id;
            int major = package.Version.Major;
            int minor = package.Version.Minor;
            int patch = package.Version.Patch;

            return new PackageLookupResult(
                versionChange,
                MakePackageSearchMetadata(packageName, $"{major + 1}.0.0"),
                MakePackageSearchMetadata(packageName, $"{major}.{minor + 1}.0"),
                MakePackageSearchMetadata(packageName, $"{major}.{minor}.{patch + 1}")
            );
        }

        private static PackageSearchMetadata MakePackageSearchMetadata(
            string packageName,
            string version,
            string source = "https://api.nuget.com/"
        )
        {
            return new PackageSearchMetadata(
                new PackageIdentity(
                    packageName,
                    new NuGetVersion(version)
                ),
                new PackageSource(source),
                null,
                null
            );
        }

        private static PackageInProject MakePackageInProject(
            string packageName,
            string version,
            string rootDir,
            string relativeDir
        )
        {
            return new PackageInProject(
                packageName,
                version,
                new PackagePath(
                    rootDir,
                    relativeDir,
                    PackageReferenceType.PackagesConfig
                )
            );
        }

        private PackageUpdatesLookup MakePackageUpdatesLookup()
        {
            return new PackageUpdatesLookup(
                new BulkPackageLookup(
                    _apiPackageLookup,
                    Substitute.For<IPackageLookupResultReporter>()
                )
            );
        }
    }
}
