using NSubstitute;

using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Inspections.Files;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Inspection;
using NuKeeper.Inspection.Report;
using NuKeeper.Inspection.Sort;
using NuKeeper.Inspection.Sources;
using NuKeeper.Local;

using NUnit.Framework;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace NuKeeper.Tests.Local
{
    [TestFixture]
    public class LocalEngineTests
    {
        [Test]
        public async Task CanRunInspect()
        {
            IUpdateFinder finder = Substitute.For<IUpdateFinder>();
            ILocalUpdater updater = Substitute.For<ILocalUpdater>();
            LocalEngine engine = MakeLocalEngine(finder, updater);

            SettingsContainer settings = new()
            {
                UserSettings = new UserSettings()
            };

            await engine.Run(settings, false);

            _ = await finder.Received()
                .FindPackageUpdateSets(Arg.Any<IFolder>(),
                    Arg.Any<NuGetSources>(),
                    Arg.Any<VersionChange>(),
                    Arg.Any<UsePrerelease>());

            await updater.Received(0)
                .ApplyUpdates(
                    Arg.Any<IReadOnlyCollection<PackageUpdateSet>>(),
                    Arg.Any<IFolder>(),
                    Arg.Any<NuGetSources>(),
                    Arg.Any<SettingsContainer>());
        }

        [Test]
        public async Task CanRunUpdate()
        {
            IUpdateFinder finder = Substitute.For<IUpdateFinder>();
            ILocalUpdater updater = Substitute.For<ILocalUpdater>();
            LocalEngine engine = MakeLocalEngine(finder, updater);

            SettingsContainer settings = new()
            {
                UserSettings = new UserSettings()
            };

            await engine.Run(settings, true);

            _ = await finder.Received()
                .FindPackageUpdateSets(Arg.Any<IFolder>(),
                    Arg.Any<NuGetSources>(),
                    Arg.Any<VersionChange>(),
                    Arg.Any<UsePrerelease>());

            await updater
                .Received(1)
                .ApplyUpdates(
                    Arg.Any<IReadOnlyCollection<PackageUpdateSet>>(),
                    Arg.Any<IFolder>(),
                    Arg.Any<NuGetSources>(),
                    Arg.Any<SettingsContainer>());
        }

        private static LocalEngine MakeLocalEngine(IUpdateFinder finder, ILocalUpdater updater)
        {
            INuGetSourcesReader reader = Substitute.For<INuGetSourcesReader>();
            _ = finder.FindPackageUpdateSets(
                    Arg.Any<IFolder>(), Arg.Any<NuGetSources>(),
                    Arg.Any<VersionChange>(),
                    Arg.Any<UsePrerelease>())
                .Returns(new List<PackageUpdateSet>());

            IPackageUpdateSetSort sorter = Substitute.For<IPackageUpdateSetSort>();
            _ = sorter.Sort(Arg.Any<IReadOnlyCollection<PackageUpdateSet>>())
                .Returns(x => x.ArgAt<IReadOnlyCollection<PackageUpdateSet>>(0));

            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();

            NuGet.Common.ILogger nugetLogger = Substitute.For<NuGet.Common.ILogger>();

            IReporter reporter = Substitute.For<IReporter>();

            LocalEngine engine = new(reader, finder, sorter, updater,
                reporter, logger, nugetLogger);
            Assert.That(engine, Is.Not.Null);
            return engine;
        }
    }
}
