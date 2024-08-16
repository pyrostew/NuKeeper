using NuKeeper.Abstractions;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Engine;

using NUnit.Framework;

using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Tests.Engine
{
    [TestFixture]
    public class UpdateConsolidatorTests
    {
        [Test]
        public void WhenOneItemIsConsolidated()
        {
            List<PackageUpdateSet> items = PackageUpdates.MakeUpdateSet("foo")
                .InList();

            IReadOnlyCollection<IReadOnlyCollection<PackageUpdateSet>> output = UpdateConsolidator.Consolidate(items, true);

            List<IReadOnlyCollection<PackageUpdateSet>> listOfLists = output.ToList();

            // one list, containing all the items
            Assert.That(listOfLists.Count, Is.EqualTo(1));
            Assert.That(listOfLists[0].Count, Is.EqualTo(1));
        }

        [Test]
        public void WhenOneItemIsNotConsolidated()
        {
            List<PackageUpdateSet> items = PackageUpdates.MakeUpdateSet("foo")
                .InList();

            IReadOnlyCollection<IReadOnlyCollection<PackageUpdateSet>> output = UpdateConsolidator.Consolidate(items, false);

            List<IReadOnlyCollection<PackageUpdateSet>> listOfLists = output.ToList();

            // one list, containing all the items
            Assert.That(listOfLists.Count, Is.EqualTo(1));
            Assert.That(listOfLists[0].Count, Is.EqualTo(1));
        }

        [Test]
        public void WhenItemsAreConsolidated()
        {
            List<PackageUpdateSet> items =
            [
                PackageUpdates.MakeUpdateSet("foo"),
                PackageUpdates.MakeUpdateSet("bar")
            ];

            IReadOnlyCollection<IReadOnlyCollection<PackageUpdateSet>> output = UpdateConsolidator.Consolidate(items, true);

            List<IReadOnlyCollection<PackageUpdateSet>> listOfLists = output.ToList();

            // one list, containing all the items
            Assert.That(listOfLists.Count, Is.EqualTo(1));
            Assert.That(listOfLists[0].Count, Is.EqualTo(2));
        }

        [Test]
        public void WhenItemsAreNotConsolidated()
        {
            List<PackageUpdateSet> items =
            [
                PackageUpdates.MakeUpdateSet("foo"),
                PackageUpdates.MakeUpdateSet("bar")
            ];

            IReadOnlyCollection<IReadOnlyCollection<PackageUpdateSet>> output = UpdateConsolidator.Consolidate(items, false);

            List<IReadOnlyCollection<PackageUpdateSet>> listOfLists = output.ToList();

            // two lists, each containing 1 item
            Assert.That(listOfLists.Count, Is.EqualTo(2));
            Assert.That(listOfLists.SelectMany(x => x).Count(), Is.EqualTo(2));
            Assert.That(listOfLists[0].Count, Is.EqualTo(1));
            Assert.That(listOfLists[1].Count, Is.EqualTo(1));
        }
    }
}
