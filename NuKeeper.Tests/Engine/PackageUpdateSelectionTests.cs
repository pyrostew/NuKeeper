using NSubstitute;

using NuKeeper.Abstractions;
using NuKeeper.Abstractions.CollaborationModels;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Engine.Packages;
using NuKeeper.Inspection.Sort;
using NuKeeper.Update.Selection;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Tests.Engine
{
    [TestFixture]
    public class PackageUpdateSelectionTests
    {
        [Test]
        public void WhenThereAreNoInputs_NoTargetsOut()
        {
            IPackageUpdateSelection target = MakeSelection();

            IReadOnlyCollection<PackageUpdateSet> results = target.SelectTargets(PushFork(),
                new List<PackageUpdateSet>(), NoFilter());

            Assert.That(results, Is.Not.Null);
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void WhenThereIsOneInput_ItIsTheTarget()
        {
            List<PackageUpdateSet> updateSets = PackageUpdates.UpdateFooFromOneVersion()
                .InList();

            IPackageUpdateSelection target = MakeSelection();

            IReadOnlyCollection<PackageUpdateSet> results = target.SelectTargets(PushFork(), updateSets, NoFilter());

            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results.First().SelectedId, Is.EqualTo("foo"));
        }

        [Test]
        public void WhenThereAreTwoInputs_MoreVersionsFirst_FirstIsTheTarget()
        {
            // sort should not change this ordering
            List<PackageUpdateSet> updateSets =
            [
                PackageUpdates.UpdateBarFromTwoVersions(),
                PackageUpdates.UpdateFooFromOneVersion()
            ];

            IPackageUpdateSelection target = MakeSelection();

            IReadOnlyCollection<PackageUpdateSet> results = target.SelectTargets(PushFork(), updateSets, NoFilter());

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.First().SelectedId, Is.EqualTo("bar"));
        }

        [Test]
        public void WhenThereAreTwoInputs_MoreVersionsSecond_SecondIsTheTarget()
        {
            // sort should change this ordering
            List<PackageUpdateSet> updateSets =
            [
                PackageUpdates.UpdateFooFromOneVersion(),
                PackageUpdates.UpdateBarFromTwoVersions()
            ];

            IPackageUpdateSelection target = MakeSelection();

            IReadOnlyCollection<PackageUpdateSet> results = target.SelectTargets(PushFork(), updateSets, NoFilter());

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.First().SelectedId, Is.EqualTo("bar"));
        }

        private static IPackageUpdateSelection MakeSelection()
        {
            INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();
            UpdateSelection updateSelection = new(logger);
            return new PackageUpdateSelection(MakeSort(), updateSelection, logger);
        }

        private static FilterSettings NoFilter()
        {
            return new FilterSettings
            {
                MaxPackageUpdates = int.MaxValue,
                MinimumAge = TimeSpan.Zero
            };
        }

        private static ForkData PushFork()
        {
            return new ForkData(new Uri("http://github.com/foo/bar"), "me", "test");
        }

        private static IPackageUpdateSetSort MakeSort()
        {
            return new PackageUpdateSetSort(Substitute.For<INuKeeperLogger>());
        }
    }
}
