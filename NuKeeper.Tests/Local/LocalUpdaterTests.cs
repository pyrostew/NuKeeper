using NSubstitute;

using NuKeeper.Abstractions;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Inspections.Files;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Local;
using NuKeeper.Update;
using NuKeeper.Update.Process;
using NuKeeper.Update.Selection;

using NUnit.Framework;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace NuKeeper.Tests.Local
{
    [TestFixture]
    public class LocalUpdaterTests
    {
        [Test]
        public async Task EmptyListCase()
        {
            IUpdateSelection selection = Substitute.For<IUpdateSelection>();
            IUpdateRunner runner = Substitute.For<IUpdateRunner>();
            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();
            IFolder folder = Substitute.For<IFolder>();
            SolutionRestore restorer = new(Substitute.For<IFileRestoreCommand>());

            LocalUpdater updater = new(selection, runner, restorer, logger);

            await updater.ApplyUpdates(new List<PackageUpdateSet>(),
                folder,
                NuGetSources.GlobalFeed, Settings());

            await runner.Received(0)
                .Update(Arg.Any<PackageUpdateSet>(), Arg.Any<NuGetSources>());
        }

        [Test]
        public async Task SingleItemCase()
        {
            List<PackageUpdateSet> updates = PackageUpdates.MakeUpdateSet("foo")
                .InList();

            IUpdateSelection selection = Substitute.For<IUpdateSelection>();
            FilterIsPassThrough(selection);


            IUpdateRunner runner = Substitute.For<IUpdateRunner>();
            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();
            IFolder folder = Substitute.For<IFolder>();
            SolutionRestore restorer = new(Substitute.For<IFileRestoreCommand>());

            LocalUpdater updater = new(selection, runner, restorer, logger);

            await updater.ApplyUpdates(updates, folder, NuGetSources.GlobalFeed, Settings());

            await runner.Received(1)
                .Update(Arg.Any<PackageUpdateSet>(), Arg.Any<NuGetSources>());
        }

        [Test]
        public async Task TwoItemsCase()
        {

            List<PackageUpdateSet> updates =
            [
                PackageUpdates.MakeUpdateSet("foo"),
                PackageUpdates.MakeUpdateSet("bar")
            ];

            IUpdateSelection selection = Substitute.For<IUpdateSelection>();
            FilterIsPassThrough(selection);

            IUpdateRunner runner = Substitute.For<IUpdateRunner>();
            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();
            IFolder folder = Substitute.For<IFolder>();
            SolutionRestore restorer = new(Substitute.For<IFileRestoreCommand>());

            LocalUpdater updater = new(selection, runner, restorer, logger);

            await updater.ApplyUpdates(updates, folder, NuGetSources.GlobalFeed, Settings());

            await runner.Received(2)
                .Update(Arg.Any<PackageUpdateSet>(), Arg.Any<NuGetSources>());
        }


        private static void FilterIsPassThrough(IUpdateSelection selection)
        {
            _ = selection
                .Filter(
                    Arg.Any<IReadOnlyCollection<PackageUpdateSet>>(),
                    Arg.Any<FilterSettings>())
                .Returns(x => x.ArgAt<IReadOnlyCollection<PackageUpdateSet>>(0));
        }

        private static SettingsContainer Settings()
        {
            return new SettingsContainer
            {
                UserSettings = new UserSettings()
            };
        }
    }
}
