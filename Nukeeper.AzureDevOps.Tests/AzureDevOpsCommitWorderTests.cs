using NuGet.Packaging.Core;
using NuGet.Versioning;

using NuKeeper.Abstractions;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Tests;

using NUnit.Framework;

using System;
using System.Collections.Generic;

namespace NuKeeper.AzureDevOps.Tests
{
    [TestFixture]
    public class AzureDevOpsCommitWorderTests
    {
        private const string CommitEmoji = "📦";

        // Azure DevOps allows a maximum of 4000 characters to be used in a pull request description:
        // https://visualstudio.uservoice.com/forums/330519-azure-devops-formerly-visual-studio-team-services/suggestions/20217283-raise-the-character-limit-for-pull-request-descrip
        private const int MaxCharacterCount = 4000;

        private ICommitWorder _sut;

        [SetUp]
        public void TestInitialize()
        {
            _sut = new AzureDevOpsCommitWorder();
        }

        [Test]
        public void MarkPullRequestTitle_UpdateIsCorrect()
        {
            List<PackageUpdateSet> updates = PackageUpdates.For(MakePackageForV110())
                .InList();

            string report = _sut.MakePullRequestTitle(updates);

            Assert.That(report, Is.Not.Null);
            Assert.That(report, Is.Not.Empty);
            Assert.That(report, Is.EqualTo($"{CommitEmoji} Automatic update of foo.bar to 1.2.3"));
        }

        [Test]
        public void MakeCommitMessage_OneUpdateIsCorrect()
        {
            PackageUpdateSet updates = PackageUpdates.For(MakePackageForV110());

            string report = _sut.MakeCommitMessage(updates);

            Assert.That(report, Is.Not.Null);
            Assert.That(report, Is.Not.Empty);
            Assert.That(report, Is.EqualTo($"{CommitEmoji} Automatic update of foo.bar to 1.2.3"));
        }

        [Test]
        public void MakeCommitMessage_TwoUpdatesIsCorrect()
        {
            PackageUpdateSet updates = PackageUpdates.For(MakePackageForV110(), MakePackageForV100());

            string report = _sut.MakeCommitMessage(updates);

            Assert.That(report, Is.Not.Null);
            Assert.That(report, Is.Not.Empty);
            Assert.That(report, Is.EqualTo($"{CommitEmoji} Automatic update of foo.bar to 1.2.3"));
        }

        [Test]
        public void MakeCommitMessage_TwoUpdatesSameVersionIsCorrect()
        {
            PackageUpdateSet updates = PackageUpdates.For(MakePackageForV110(), MakePackageForV110InProject3());

            string report = _sut.MakeCommitMessage(updates);

            Assert.That(report, Is.Not.Null);
            Assert.That(report, Is.Not.Empty);
            Assert.That(report, Is.EqualTo($"{CommitEmoji} Automatic update of foo.bar to 1.2.3"));
        }

        [Test]
        public void OneUpdate_MakeCommitDetails_IsNotEmpty()
        {
            List<PackageUpdateSet> updates = PackageUpdates.For(MakePackageForV110())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            Assert.That(report, Is.Not.Null);
            Assert.That(report, Is.Not.Empty);
        }

        [Test]
        public void OneUpdate_MakeCommitDetails_HasStandardTexts()
        {
            List<PackageUpdateSet> updates = PackageUpdates.For(MakePackageForV110())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            AssertContainsStandardText(report);
        }

        [Test]
        public void OneUpdate_MakeCommitDetails_HasVersionInfo()
        {
            List<PackageUpdateSet> updates = PackageUpdates.For(MakePackageForV110())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            Assert.That(report, Does.StartWith("NuKeeper has generated a minor update of `foo.bar` to `1.2.3` from `1.1.0`"));
        }

        [Test]
        public void OneUpdate_MakeCommitDetails_HasPublishedDate()
        {
            List<PackageUpdateSet> updates = PackageUpdates.For(MakePackageForV110())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            Assert.That(report, Does.Contain("`foo.bar 1.2.3` was published at `2018-02-19T11:12:07Z`"));
        }


        [Test]
        public void OneUpdate_MakeCommitDetails_HasProjectDetailsAsMarkdownTable()
        {
            List<PackageUpdateSet> updates = PackageUpdates.For(MakePackageForV110())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            Assert.That(report, Does.Contain("### 1 project update:"));
            Assert.That(report, Does.Contain("| Project   | Package   | From   | To   |"));
            Assert.That(report, Does.Contain("|:----------|:----------|-------:|-----:|"));
            Assert.That(report, Does.Contain($"| `folder\\src\\project1\\packages.config` | `foo.bar` | {NuGetVersionPackageLink("foo.bar", "1.1.0")} | {NuGetVersionPackageLink("foo.bar", "1.2.3")} |"));
        }

        [Test]
        public void TwoUpdates_MakeCommitDetails_NotEmpty()
        {
            List<PackageUpdateSet> updates = PackageUpdates.For(MakePackageForV110(), MakePackageForV100())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            Assert.That(report, Is.Not.Null);
            Assert.That(report, Is.Not.Empty);
        }

        [Test]
        public void TwoUpdates_MakeCommitDetails_HasStandardTexts()
        {
            List<PackageUpdateSet> updates = PackageUpdates.For(MakePackageForV110(), MakePackageForV100())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            AssertContainsStandardText(report);
            Assert.That(report, Does.Contain("1.0.0"));
        }

        [Test]
        public void TwoUpdates_MakeCommitDetails_HasVersionInfo()
        {
            List<PackageUpdateSet> updates = PackageUpdates.For(MakePackageForV110(), MakePackageForV100())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            Assert.That(report, Does.StartWith("NuKeeper has generated a minor update of `foo.bar` to `1.2.3`"));
            Assert.That(report, Does.Contain("2 versions of `foo.bar` were found in use: `1.1.0`, `1.0.0`"));
        }

        [Test]
        public void TwoUpdates_MakeCommitDetails_HasProjectList()
        {
            List<PackageUpdateSet> updates = PackageUpdates.For(MakePackageForV110(), MakePackageForV100())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            Assert.That(report, Does.Contain("2 project updates:"));
            Assert.That(report, Does.Contain("| Project   | Package   | From   | To   |"));
            Assert.That(report, Does.Contain("|:----------|:----------|-------:|-----:|"));
            Assert.That(report, Does.Contain($"| `folder\\src\\project1\\packages.config` | `foo.bar` | {NuGetVersionPackageLink("foo.bar", "1.1.0")} | {NuGetVersionPackageLink("foo.bar", "1.2.3")} |"));
            Assert.That(report, Does.Contain($"| `folder\\src\\project2\\packages.config` | `foo.bar` | {NuGetVersionPackageLink("foo.bar", "1.0.0")} | {NuGetVersionPackageLink("foo.bar", "1.2.3")} |"));
        }

        [Test]
        public void TwoUpdatesSameVersion_MakeCommitDetails_NotEmpty()
        {
            List<PackageUpdateSet> updates = PackageUpdates.For(MakePackageForV110(), MakePackageForV110InProject3())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            Assert.That(report, Is.Not.Null);
            Assert.That(report, Is.Not.Empty);
        }

        [Test]
        public void TwoUpdatesSameVersion_MakeCommitDetails_HasStandardTexts()
        {
            List<PackageUpdateSet> updates = PackageUpdates.For(MakePackageForV110(), MakePackageForV110InProject3())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            AssertContainsStandardText(report);
        }

        [Test]
        public void TwoUpdatesSameVersion_MakeCommitDetails_HasVersionInfo()
        {
            List<PackageUpdateSet> updates = PackageUpdates.For(MakePackageForV110(), MakePackageForV110InProject3())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            Assert.That(report, Does.StartWith("NuKeeper has generated a minor update of `foo.bar` to `1.2.3` from `1.1.0`"));
        }

        [Test]
        public void TwoUpdatesSameVersion_MakeCommitDetails_HasProjectList()
        {
            List<PackageUpdateSet> updates = PackageUpdates.For(MakePackageForV110(), MakePackageForV110InProject3())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            Assert.That(report, Does.Contain("2 project updates:"));
            Assert.That(report, Does.Contain("| Project   | Package   | From   | To   |"));
            Assert.That(report, Does.Contain("|:----------|:----------|-------:|-----:|"));
            Assert.That(report, Does.Contain($"| `folder\\src\\project1\\packages.config` | `foo.bar` | {NuGetVersionPackageLink("foo.bar", "1.1.0")} | {NuGetVersionPackageLink("foo.bar", "1.2.3")} |"));
            Assert.That(report, Does.Contain($"| `folder\\src\\project3\\packages.config` | `foo.bar` | {NuGetVersionPackageLink("foo.bar", "1.1.0")} | {NuGetVersionPackageLink("foo.bar", "1.2.3")} |"));
        }

        [Test]
        public void OneUpdate_MakeCommitDetails_HasVersionLimitData()
        {
            List<PackageUpdateSet> updates = PackageUpdates.LimitedToMinor(MakePackageForV110())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            Assert.That(report, Does.Contain($"There is also a higher version, `foo.bar 2.3.4`, but this was not applied as only `Minor` version changes are allowed."));
        }

        [Test]
        public void OneUpdateWithDate_MakeCommitDetails_HasVersionLimitDataWithDate()
        {
            DateTimeOffset publishedAt = new(2018, 2, 20, 11, 32, 45, TimeSpan.Zero);

            List<PackageUpdateSet> updates = PackageUpdates.LimitedToMinor(publishedAt, MakePackageForV110())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            Assert.That(report, Does.Contain($"There is also a higher version, `foo.bar 2.3.4` published at `2018-02-20T11:32:45Z`,"));
            Assert.That(report, Does.Contain(" ago, but this was not applied as only `Minor` version changes are allowed."));
        }

        [Test]
        public void MakeCommitDetails_DoesNotExceedPullRequestBodyLimit()
        {
            string packageNameExceedingPullRequestBodyLimit = new('a', MaxCharacterCount + 1);
            List<PackageUpdateSet> updateSet = PackageUpdates.MakeUpdateSet(packageNameExceedingPullRequestBodyLimit)
                .InList();

            string report = _sut.MakeCommitDetails(updateSet);

            Assert.That(report, Is.Not.Null);
            Assert.That(report, Is.Not.Empty);
            Assert.That(report.Length, Is.EqualTo(MaxCharacterCount));
            Assert.That(report, Does.EndWith("..."));
        }

        [Test]
        public void OneUpdateWithMajorVersionChange()
        {
            List<PackageUpdateSet> updates = PackageUpdates.ForNewVersion(new PackageIdentity("foo.bar", new NuGetVersion("2.1.1")), MakePackageForV110())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            Assert.That(report, Does.StartWith("NuKeeper has generated a major update of `foo.bar` to `2.1.1` from `1.1.0"));
        }

        [Test]
        public void OneUpdateWithMinorVersionChange()
        {
            List<PackageUpdateSet> updates = PackageUpdates.ForNewVersion(new PackageIdentity("foo.bar", new NuGetVersion("1.2.1")), MakePackageForV110())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            Assert.That(report, Does.StartWith("NuKeeper has generated a minor update of `foo.bar` to `1.2.1` from `1.1.0"));
        }

        [Test]
        public void OneUpdateWithPatchVersionChange()
        {
            List<PackageUpdateSet> updates = PackageUpdates.ForNewVersion(new PackageIdentity("foo.bar", new NuGetVersion("1.1.9")), MakePackageForV110())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            Assert.That(report, Does.StartWith("NuKeeper has generated a patch update of `foo.bar` to `1.1.9` from `1.1.0"));
        }

        [Test]
        public void OneUpdateWithInternalPackageSource()
        {
            List<PackageUpdateSet> updates = PackageUpdates.ForInternalSource(MakePackageForV110())
                .InList();

            string report = _sut.MakeCommitDetails(updates);

            Assert.That(report, Does.Not.Contain("on NuGet.org"));
            Assert.That(report, Does.Not.Contain("www.nuget.org"));
        }

        [Test]
        public void TwoUpdateSets()
        {
            PackageIdentity packageTwo = new("packageTwo", new NuGetVersion("3.4.5"));

            List<PackageUpdateSet> updates =
            [
                PackageUpdates.ForNewVersion(new PackageIdentity("foo.bar", new NuGetVersion("2.1.1")), MakePackageForV110()),
                PackageUpdates.ForNewVersion(packageTwo, MakePackageForV110("packageTwo"))
            ];

            string report = _sut.MakeCommitDetails(updates);

            Assert.That(report, Does.StartWith("2 packages were updated in 1 project:"));
            Assert.That(report, Does.Contain("| foo.bar | packageTwo |"));
            Assert.That(report, Does.Contain("NuKeeper has generated a major update of `foo.bar` to `2.1.1` from `1.1.0`"));
            Assert.That(report, Does.Contain("NuKeeper has generated a major update of `packageTwo` to `3.4.5` from `1.1.0`"));
        }

        private static void AssertContainsStandardText(string report)
        {
            Assert.That(report, Does.StartWith("NuKeeper has generated a minor update of `foo.bar` to `1.2.3`"));
            Assert.That(report, Does.Contain("This is an automated update. Merge only if it passes tests"));
            Assert.That(report, Does.EndWith("**NuKeeper**: https://github.com/NuKeeperDotNet/NuKeeper" + Environment.NewLine));
            Assert.That(report, Does.Contain("1.1.0"));

            Assert.That(report, Does.Not.Contain("Exception"));
            Assert.That(report, Does.Not.Contain("System.String"));
            Assert.That(report, Does.Not.Contain("Generic"));
            Assert.That(report, Does.Not.Contain("[ "));
            Assert.That(report, Does.Not.Contain(" ]"));
            Assert.That(report, Does.Not.Contain("There is also a higher version"));
        }

        private static PackageInProject MakePackageForV110(string packageName = "foo.bar")
        {
            PackagePath path = new("c:\\temp", "folder\\src\\project1\\packages.config",
                PackageReferenceType.PackagesConfig);
            return new PackageInProject(packageName, "1.1.0", path);
        }

        private static PackageInProject MakePackageForV100(string packageName = "foo.bar")
        {
            PackagePath path2 = new("c:\\temp", "folder\\src\\project2\\packages.config",
                PackageReferenceType.PackagesConfig);
            PackageInProject currentPackage2 = new(packageName, "1.0.0", path2);
            return currentPackage2;
        }

        private static PackageInProject MakePackageForV110InProject3()
        {
            PackagePath path = new("c:\\temp", "folder\\src\\project3\\packages.config", PackageReferenceType.PackagesConfig);

            return new PackageInProject("foo.bar", "1.1.0", path);
        }

        private static string NuGetVersionPackageLink(string packageId, string version)
        {
            string url = $"https://www.nuget.org/packages/{packageId}/{version}";
            return $"[{version}]({url})";
        }
    }
}
