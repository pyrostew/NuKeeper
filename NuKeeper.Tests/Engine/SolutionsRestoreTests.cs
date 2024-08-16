using NSubstitute;

using NuKeeper.Abstractions;
using NuKeeper.Abstractions.Inspections.Files;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Update.Process;

using NUnit.Framework;

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NuKeeper.Tests.Engine
{
    [TestFixture]
    public class SolutionsRestoreTests
    {
        [Test]
        public async Task WhenThereAreNoSolutionsTheCommandIsNotCalled()
        {
            IFileRestoreCommand cmd = Substitute.For<IFileRestoreCommand>();
            IFolder folder = Substitute.For<IFolder>();

            List<PackageUpdateSet> packages = [];

            SolutionRestore solutionRestore = new(cmd);

            await solutionRestore.CheckRestore(packages, folder, NuGetSources.GlobalFeed);

            await cmd.DidNotReceiveWithAnyArgs()
                .Invoke(Arg.Any<FileInfo>(), Arg.Any<NuGetSources>());
        }

        [Test]
        public async Task WhenThereAreNoMatchingPackagesTheCommandIsNotCalled()
        {
            List<PackageUpdateSet> packages = PackageUpdates.ForPackageRefType(PackageReferenceType.ProjectFile)
                .InList();

            FileInfo sln = new("foo.sln");

            IFileRestoreCommand cmd = Substitute.For<IFileRestoreCommand>();
            IFolder folder = Substitute.For<IFolder>();
            _ = folder.Find(Arg.Any<string>()).Returns(new[] { sln });

            SolutionRestore solutionRestore = new(cmd);

            await solutionRestore.CheckRestore(packages, folder, NuGetSources.GlobalFeed);

            await cmd.DidNotReceiveWithAnyArgs()
                .Invoke(Arg.Any<FileInfo>(), Arg.Any<NuGetSources>());
        }

        [Test]
        public async Task WhenThereIsOneSolutionsTheCommandIsCalled()
        {
            List<PackageUpdateSet> packages = PackageUpdates.ForPackageRefType(PackageReferenceType.PackagesConfig)
                .InList();

            FileInfo sln = new("foo.sln");

            IFileRestoreCommand cmd = Substitute.For<IFileRestoreCommand>();
            IFolder folder = Substitute.For<IFolder>();
            _ = folder.Find(Arg.Any<string>()).Returns(new[] { sln });

            SolutionRestore solutionRestore = new(cmd);

            await solutionRestore.CheckRestore(packages, folder, NuGetSources.GlobalFeed);

            await cmd.Received(1).Invoke(Arg.Any<FileInfo>(), Arg.Any<NuGetSources>());
            await cmd.Received().Invoke(sln, Arg.Any<NuGetSources>());
        }

        [Test]
        public async Task WhenThereAreTwoSolutionsTheCommandIsCalledForEachOfThem()
        {
            List<PackageUpdateSet> packages = PackageUpdates.ForPackageRefType(PackageReferenceType.PackagesConfig)
                .InList();

            FileInfo sln1 = new("foo.sln");
            FileInfo sln2 = new("bar.sln");

            IFileRestoreCommand cmd = Substitute.For<IFileRestoreCommand>();
            IFolder folder = Substitute.For<IFolder>();
            _ = folder.Find(Arg.Any<string>()).Returns(new[] { sln1, sln2 });

            SolutionRestore solutionRestore = new(cmd);

            await solutionRestore.CheckRestore(packages, folder, NuGetSources.GlobalFeed);

            await cmd.Received(2).Invoke(Arg.Any<FileInfo>(), Arg.Any<NuGetSources>());
            await cmd.Received().Invoke(sln1, Arg.Any<NuGetSources>());
            await cmd.Received().Invoke(sln2, Arg.Any<NuGetSources>());
        }
    }
}
