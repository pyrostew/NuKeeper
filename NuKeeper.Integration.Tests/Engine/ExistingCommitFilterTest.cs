using NSubstitute;

using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Git;
using NuKeeper.Abstractions.NuGetApi;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Engine.Packages;

using NUnit.Framework;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NuKeeper.Integration.Tests.Engine
{
    [TestFixture]
    public class ExistingCommitFilterTest : TestWithFailureLogging
    {
        [Test]
        public async Task DoFilter()
        {
            string[] nugetsToUpdate = new[]
            {
                "First.Nuget",
                "Second.Nuget"
            };

            string[] nugetsAlreadyCommitted = new[]
            {
                "Second.Nuget",
            };

            IGitDriver git = MakeGitDriver(nugetsAlreadyCommitted);

            List<PackageUpdateSet> updates = nugetsToUpdate.Select(MakeUpdateSet).ToList();

            IExistingCommitFilter subject = MakeExistingCommitFilter();

            IReadOnlyCollection<PackageUpdateSet> result = await subject.Filter(git, updates.AsReadOnly(), "base", "head");

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.FirstOrDefault()?.SelectedId, Is.EqualTo("First.Nuget"));
        }

        [Test]
        public async Task DoNotFilter()
        {
            string[] nugetsToUpdate = new[]
            {
                "First.Nuget",
                "Second.Nuget"
            };

            string[] nugetsAlreadyCommitted = new[]
            {
                "Third.Nuget",
            };

            IGitDriver git = MakeGitDriver(nugetsAlreadyCommitted);

            List<PackageUpdateSet> updates = nugetsToUpdate.Select(MakeUpdateSet).ToList();

            IExistingCommitFilter subject = MakeExistingCommitFilter();

            IReadOnlyCollection<PackageUpdateSet> result = await subject.Filter(git, updates.AsReadOnly(), "base", "head");

            Assert.That(result.Count, Is.EqualTo(2));
        }

        private IExistingCommitFilter MakeExistingCommitFilter()
        {
            ICollaborationFactory collaborationFactory = Substitute.For<ICollaborationFactory>();

            ICollaborationPlatform gitClient = Substitute.For<ICollaborationPlatform>();
            _ = collaborationFactory.CollaborationPlatform.Returns(gitClient);

            ICommitWorder commitWorder = Substitute.For<ICommitWorder>();
            _ = commitWorder.MakeCommitMessage(Arg.Any<PackageUpdateSet>()).Returns(p => $"Automatic update of {((PackageUpdateSet)p[0]).SelectedId} to {((PackageUpdateSet)p[0]).SelectedVersion}");
            _ = collaborationFactory.CommitWorder.Returns(commitWorder);

            return new ExistingCommitFilter(collaborationFactory, NukeeperLogger);
        }

        private static Task<IReadOnlyCollection<string>> FixedReturnVal(string[] ids)
        {
            return Task.Run(() =>
            {
                return (IReadOnlyCollection<string>)ids.Select(id => CreateCommitMessage(id, new NuGetVersion("3.0.0"))).ToList().AsReadOnly();
            });
        }

        private static IGitDriver MakeGitDriver(string[] ids)
        {
            string[] l = ids.Select(id => CreateCommitMessage(id, new NuGetVersion("3.0.0"))).ToArray();

            IGitDriver git = Substitute.For<IGitDriver>();
            _ = git.GetNewCommitMessages(Arg.Any<string>(), Arg.Any<string>())
                .Returns(FixedReturnVal(ids));

            return git;
        }

        private static string CreateCommitMessage(string id, NuGetVersion version)
        {
            return $"Automatic update of {id} to {version}";
        }

        private static PackageUpdateSet MakeUpdateSet(string id)
        {
            List<PackageInProject> currentPackages =
            [
                new PackageInProject(id, "1.0.0", new PackagePath("base", "rel", PackageReferenceType.ProjectFile)),
                new PackageInProject(id, "2.0.0", new PackagePath("base", "rel", PackageReferenceType.ProjectFile)),
            ];

            PackageSearchMetadata majorUpdate = new(
                new PackageIdentity(
                    id,
                    new NuGetVersion("3.0.0")),
                new PackageSource("https://api.nuget.org/v3/index.json"), null, null);

            PackageLookupResult lookupResult = new(VersionChange.Major, majorUpdate, null, null);

            return new PackageUpdateSet(lookupResult, currentPackages);
        }
    }
}
