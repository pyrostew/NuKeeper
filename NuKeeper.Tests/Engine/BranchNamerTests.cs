using NuKeeper.Abstractions;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Engine;

using NUnit.Framework;

using System.Collections.Generic;

namespace NuKeeper.Tests.Engine
{
    [TestFixture]
    public class BranchNamerTests
    {
        private const string BranchNameTemplate = "nukeeper/{default}";

        [Test]
        public void TestWithSinglePackage()
        {
            List<PackageUpdateSet> packages = PackageUpdates.MakeUpdateSet("SomePackage")
                .InList();

            string branchName = BranchNamer.MakeName(packages);

            Assert.That(branchName, Is.EqualTo("nukeeper-update-SomePackage-to-1.2.3"));
        }

        [Test]
        public void TestWithSinglePackageAndBranchNamePrefix()
        {
            List<PackageUpdateSet> packages = PackageUpdates.MakeUpdateSet("SomePackage")
                .InList();

            string branchName = BranchNamer.MakeName(packages, BranchNameTemplate);

            Assert.That(branchName, Is.EqualTo("nukeeper/nukeeper-update-SomePackage-to-1.2.3"));
        }

        [Test]
        public void TestWithTwoPackages()
        {
            List<PackageUpdateSet> packages =
            [
                PackageUpdates.MakeUpdateSet("SomePackage"),
                PackageUpdates.MakeUpdateSet("OtherPackage")
            ];

            string branchName = BranchNamer.MakeName(packages);

            Assert.That(branchName, Is.EqualTo("nukeeper-update-2-packages-AA9F9828431C8BFB7A18D3D8F0CF229D"));
        }

        [Test]
        public void TestWithTwoPackagesAndBranchNamePrefix()
        {
            List<PackageUpdateSet> packages =
            [
                PackageUpdates.MakeUpdateSet("SomePackage"),
                PackageUpdates.MakeUpdateSet("OtherPackage")
            ];

            string branchName = BranchNamer.MakeName(packages, BranchNameTemplate);

            Assert.That(branchName, Is.EqualTo("nukeeper/nukeeper-update-2-packages-AA9F9828431C8BFB7A18D3D8F0CF229D"));
        }

        [Test]
        public void TestWithThreePackages()
        {
            List<PackageUpdateSet> packages =
            [
                PackageUpdates.MakeUpdateSet("SomePackage"),
                PackageUpdates.MakeUpdateSet("OtherPackage"),
                PackageUpdates.MakeUpdateSet("SomethingElse"),
            ];

            string branchName = BranchNamer.MakeName(packages);

            Assert.That(branchName, Is.EqualTo("nukeeper-update-3-packages-BBBB3BF2315D6111CFCBF6A4A7A29DD8"));
        }

        [Test]
        public void TestWithThreePackagesAndBranchNamePrefix()
        {
            List<PackageUpdateSet> packages =
            [
                PackageUpdates.MakeUpdateSet("SomePackage"),
                PackageUpdates.MakeUpdateSet("OtherPackage"),
                PackageUpdates.MakeUpdateSet("SomethingElse"),
            ];

            string branchName = BranchNamer.MakeName(packages, BranchNameTemplate);

            Assert.That(branchName, Is.EqualTo("nukeeper/nukeeper-update-3-packages-BBBB3BF2315D6111CFCBF6A4A7A29DD8"));
        }

        [Test]
        public void EquivalentInputs_HaveSameHash()
        {
            List<PackageUpdateSet> packages1 =
            [
                PackageUpdates.MakeUpdateSet("SomePackage", "2.3.4"),
                PackageUpdates.MakeUpdateSet("OtherPackage", "2.3.4")
            ];

            List<PackageUpdateSet> packages2 =
            [
                PackageUpdates.MakeUpdateSet("SomePackage", "2.3.4"),
                PackageUpdates.MakeUpdateSet("OtherPackage", "2.3.4")
            ];

            string branchName1 = BranchNamer.MakeName(packages1);
            string branchName2 = BranchNamer.MakeName(packages2);

            Assert.That(branchName1, Is.EqualTo(branchName2));
        }

        [Test]
        public void EquivalentInputsWithBranchNamePrefix_HaveSameHash()
        {
            List<PackageUpdateSet> packages1 =
            [
                PackageUpdates.MakeUpdateSet("SomePackage", "2.3.4"),
                PackageUpdates.MakeUpdateSet("OtherPackage", "2.3.4")
            ];

            List<PackageUpdateSet> packages2 =
            [
                PackageUpdates.MakeUpdateSet("SomePackage", "2.3.4"),
                PackageUpdates.MakeUpdateSet("OtherPackage", "2.3.4")
            ];

            string branchName1 = BranchNamer.MakeName(packages1, BranchNameTemplate);
            string branchName2 = BranchNamer.MakeName(packages2, BranchNameTemplate);

            Assert.That(branchName1, Is.EqualTo(branchName2));
        }

        [Test]
        public void VersionChange_ChangesHash()
        {
            List<PackageUpdateSet> packages1 =
            [
                PackageUpdates.MakeUpdateSet("SomePackage", "2.3.4"),
                PackageUpdates.MakeUpdateSet("OtherPackage", "2.3.4")
            ];

            List<PackageUpdateSet> packages2 =
            [
                PackageUpdates.MakeUpdateSet("SomePackage", "2.3.4"),
                PackageUpdates.MakeUpdateSet("OtherPackage", "2.3.5")
            ];

            string branchName1 = BranchNamer.MakeName(packages1);
            string branchName2 = BranchNamer.MakeName(packages2);

            Assert.That(branchName1, Is.Not.EqualTo(branchName2));
        }

        [Test]
        public void VersionChangeWithBranchNamePrefix_ChangesHash()
        {
            List<PackageUpdateSet> packages1 =
            [
                PackageUpdates.MakeUpdateSet("SomePackage", "2.3.4"),
                PackageUpdates.MakeUpdateSet("OtherPackage", "2.3.4")
            ];

            List<PackageUpdateSet> packages2 =
            [
                PackageUpdates.MakeUpdateSet("SomePackage", "2.3.4"),
                PackageUpdates.MakeUpdateSet("OtherPackage", "2.3.5")
            ];

            string branchName1 = BranchNamer.MakeName(packages1, BranchNameTemplate);
            string branchName2 = BranchNamer.MakeName(packages2, BranchNameTemplate);

            Assert.That(branchName1, Is.Not.EqualTo(branchName2));
        }

        [Test]
        public void NameChange_ChangesHash()
        {
            List<PackageUpdateSet> packages1 =
            [
                PackageUpdates.MakeUpdateSet("SomePackage", "2.3.4"),
                PackageUpdates.MakeUpdateSet("OtherPackage", "2.3.4")
            ];

            List<PackageUpdateSet> packages2 =
            [
                PackageUpdates.MakeUpdateSet("ZomePackage", "2.3.4"),
                PackageUpdates.MakeUpdateSet("OtherPackage", "2.3.4")
            ];

            string branchName1 = BranchNamer.MakeName(packages1);
            string branchName2 = BranchNamer.MakeName(packages2);

            Assert.That(branchName1, Is.Not.EqualTo(branchName2));
        }

        [Test]
        public void NameChangeWithBranchNamePrefix_ChangesHash()
        {
            List<PackageUpdateSet> packages1 =
            [
                PackageUpdates.MakeUpdateSet("SomePackage", "2.3.4"),
                PackageUpdates.MakeUpdateSet("OtherPackage", "2.3.4")
            ];

            List<PackageUpdateSet> packages2 =
            [
                PackageUpdates.MakeUpdateSet("ZomePackage", "2.3.4"),
                PackageUpdates.MakeUpdateSet("OtherPackage", "2.3.4")
            ];

            string branchName1 = BranchNamer.MakeName(packages1, BranchNameTemplate);
            string branchName2 = BranchNamer.MakeName(packages2, BranchNameTemplate);

            Assert.That(branchName1, Is.Not.EqualTo(branchName2));
        }
    }
}
