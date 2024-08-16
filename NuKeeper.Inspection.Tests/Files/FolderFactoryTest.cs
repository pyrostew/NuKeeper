using NSubstitute;

using NuKeeper.Abstractions.Logging;
using NuKeeper.Inspection.Files;

using NUnit.Framework;

using System;
using System.IO;
using System.Linq;

namespace NuKeeper.Inspection.Tests.Files
{
    [TestFixture]
    public class FolderFactoryTest
    {
        [Test]
        public void OnlySelectTempFoldersOlderThanOneHour()
        {
            FolderFactory factory = new(Substitute.For<INuKeeperLogger>());

            // set up edge cases
            Abstractions.Inspections.Files.IFolder folder1 = factory.UniqueTemporaryFolder();
            Directory.SetLastWriteTime(folder1.FullPath, DateTime.Now.AddHours(-1).AddMinutes(-1));
            Abstractions.Inspections.Files.IFolder folder2 = factory.UniqueTemporaryFolder();
            Directory.SetLastWriteTime(folder2.FullPath, DateTime.Now.AddHours(-1).AddMinutes(1));

            DirectoryInfo baseDirInfo = new(FolderFactory.NuKeeperTempFilesPath());

            DirectoryInfo[] toDelete = FolderFactory.GetTempDirsToCleanup(baseDirInfo).ToArray();

            Assert.That(1 == toDelete.Length, "Only 1 folder should be marked for deletion");
            Assert.That(folder1.FullPath == toDelete[0].FullName, "wrong folder marked for deletion");

            folder1.TryDelete();
            folder2.TryDelete();
        }

        [Test]
        public void OnlySelectTempFoldersWithPrefix()
        {
            FolderFactory factory = new(Substitute.For<INuKeeperLogger>());

            // set up edge cases
            Abstractions.Inspections.Files.IFolder folder1 = factory.UniqueTemporaryFolder();
            Directory.SetLastWriteTime(folder1.FullPath, DateTime.Now.AddHours(-2));
            string notToToDeletePath = Path.Combine(FolderFactory.NuKeeperTempFilesPath(), "tools");
            _ = Directory.CreateDirectory(notToToDeletePath);
            Directory.SetLastWriteTime(notToToDeletePath, DateTime.Now.AddHours(-2));

            DirectoryInfo baseDirInfo = new(FolderFactory.NuKeeperTempFilesPath());

            DirectoryInfo[] toDelete = FolderFactory.GetTempDirsToCleanup(baseDirInfo).ToArray();

            Assert.That(1 == toDelete.Length, "Only 1 folder should be marked for deletion");
            Assert.That(folder1.FullPath == toDelete[0].FullName, "wrong folder marked for deletion");

            folder1.TryDelete();
            if (Directory.Exists(notToToDeletePath))
            {
                Directory.Delete(notToToDeletePath);
            }
        }
    }
}
