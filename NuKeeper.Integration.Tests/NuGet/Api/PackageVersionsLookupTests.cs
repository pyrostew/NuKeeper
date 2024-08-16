using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.NuGetApi;
using NuKeeper.Inspection.NuGetApi;

using NUnit.Framework;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NuKeeper.Integration.Tests.Nuget.Api
{
    [TestFixture]
    public class PackageVersionsLookupTests : TestWithFailureLogging
    {
        [Test]
        public async Task WellKnownPackageName_ShouldReturnResultsList()
        {
            IPackageVersionsLookup lookup = BuildPackageLookup();

            IReadOnlyCollection<PackageSearchMetadata> packages = await lookup.Lookup("Newtonsoft.Json", false, NuGetSources.GlobalFeed);

            Assert.That(packages, Is.Not.Null);

            List<PackageSearchMetadata> packageList = packages.ToList();
            Assert.That(packageList, Is.Not.Empty);
            Assert.That(packageList.Count, Is.GreaterThan(1));
        }

        [Test]
        public async Task WellKnownPackageName_ShouldReturnPopulatedResults()
        {
            IPackageVersionsLookup lookup = BuildPackageLookup();

            IReadOnlyCollection<PackageSearchMetadata> packages = await lookup.Lookup("Newtonsoft.Json", false, NuGetSources.GlobalFeed);

            Assert.That(packages, Is.Not.Null);

            List<PackageSearchMetadata> packageList = packages.ToList();
            PackageSearchMetadata latest = packageList
                .OrderByDescending(p => p.Identity.Version)
                .FirstOrDefault();

            Assert.That(latest, Is.Not.Null);
            Assert.That(latest.Identity, Is.Not.Null);
            Assert.That(latest.Identity.Version, Is.Not.Null);

            Assert.That(latest.Identity.Id, Is.EqualTo("Newtonsoft.Json"));
            Assert.That(latest.Identity.Version.Major, Is.GreaterThan(1));
            Assert.That(latest.Published.HasValue, Is.True);
            Assert.That(latest.Identity.Version.IsPrerelease, Is.False);
        }

        [Test]
        public async Task CanGetPreReleases()
        {
            IPackageVersionsLookup lookup = BuildPackageLookup();

            IReadOnlyCollection<PackageSearchMetadata> packages = await lookup.Lookup("Moq", true, NuGetSources.GlobalFeed);

            Assert.That(packages, Is.Not.Null);

            List<PackageSearchMetadata> betas = packages
                .Where(p => p.Identity.Version.IsPrerelease)
                .OrderByDescending(p => p.Identity.Version)
                .ToList();

            Assert.That(betas, Is.Not.Null);
            Assert.That(betas, Is.Not.Empty);

            PackageSearchMetadata beta = betas.FirstOrDefault();

            Assert.That(beta, Is.Not.Null);
            Assert.That(beta.Identity, Is.Not.Null);
            Assert.That(beta.Identity.Version, Is.Not.Null);

            Assert.That(beta.Identity.Id, Is.EqualTo("Moq"));
            Assert.That(beta.Identity.Version.IsPrerelease, Is.True);
        }

        [Test]
        public async Task PackageShouldHaveDependencies()
        {
            IPackageVersionsLookup lookup = BuildPackageLookup();

            IReadOnlyCollection<PackageSearchMetadata> packages = await lookup.Lookup("Moq", false, NuGetSources.GlobalFeed);

            Assert.That(packages, Is.Not.Null);

            List<PackageSearchMetadata> packageList = packages.ToList();
            PackageSearchMetadata latest = packageList
                .OrderByDescending(p => p.Identity.Version)
                .FirstOrDefault();

            Assert.That(latest, Is.Not.Null);
            Assert.That(latest.Dependencies, Is.Not.Null);
            Assert.That(latest.Dependencies, Is.Not.Empty);
        }

        [Test]
        public async Task CanBeCalledTwice()
        {
            IPackageVersionsLookup lookup = BuildPackageLookup();
            IReadOnlyCollection<PackageSearchMetadata> packages1 = await lookup.Lookup("Newtonsoft.Json", false, NuGetSources.GlobalFeed);
            Assert.That(packages1, Is.Not.Null);

            IReadOnlyCollection<PackageSearchMetadata> packages2 = await lookup.Lookup("Moq", false, NuGetSources.GlobalFeed);
            Assert.That(packages2, Is.Not.Null);
        }

        [Test]
        public async Task CanBeCalledInParallel()
        {
            IPackageVersionsLookup lookup = BuildPackageLookup();

            List<Task<IReadOnlyCollection<PackageSearchMetadata>>> tasks = [];

            for (int i = 0; i < 10; i++)
            {
                Task<IReadOnlyCollection<PackageSearchMetadata>> task = lookup.Lookup("Newtonsoft.Json", false, NuGetSources.GlobalFeed);
                tasks.Add(task);
            }

            _ = await Task.WhenAll(tasks);

            foreach (Task<IReadOnlyCollection<PackageSearchMetadata>> task in tasks)
            {
                Assert.That(task.IsCompletedSuccessfully);
            }
        }

        private IPackageVersionsLookup BuildPackageLookup()
        {
            return new PackageVersionsLookup(NugetLogger, NukeeperLogger);
        }
    }
}
