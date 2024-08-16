using NuKeeper.Abstractions.Inspections.Files;
using NuKeeper.Abstractions.Logging;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace NuKeeper.Inspection.Files
{
    public class FolderFactory : IFolderFactory
    {
        private const string FolderPrefix = "repo-";
        private readonly INuKeeperLogger _logger;

        public FolderFactory(INuKeeperLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Select folders to cleanup at startup
        /// </summary>
        /// <param name="nukeeperTemp">NuKeepers temp folder</param>
        /// <returns></returns>
        public static IEnumerable<DirectoryInfo> GetTempDirsToCleanup(DirectoryInfo nukeeperTemp)
        {
            if (nukeeperTemp == null)
            {
                throw new ArgumentNullException(nameof(nukeeperTemp));
            }

            IEnumerable<DirectoryInfo> dirs = nukeeperTemp.Exists ? nukeeperTemp.EnumerateDirectories() : Enumerable.Empty<DirectoryInfo>();
            DateTime filterDatetime = DateTime.Now.AddHours(-1);
            return dirs.Where(d =>
                d.Name.StartsWith(FolderPrefix, StringComparison.InvariantCultureIgnoreCase) &&
                d.LastWriteTime < filterDatetime);
        }

        public static string NuKeeperTempFilesPath()
        {
            return Path.Combine(Path.GetTempPath(), "NuKeeper");
        }

        /// <summary>
        /// Cleanup folders that are not automatically have been cleaned.
        /// </summary>
        public void DeleteExistingTempDirs()
        {
            DirectoryInfo dirInfo = new(NuKeeperTempFilesPath());
            foreach (DirectoryInfo dir in GetTempDirsToCleanup(dirInfo))
            {
                Folder folder = new(_logger, dir);
                folder.TryDelete();
            }
        }

        public IFolder FolderFromPath(string folderPath)
        {
            return new Folder(_logger, new DirectoryInfo(folderPath));
        }

        public IFolder UniqueTemporaryFolder()
        {
            DirectoryInfo tempDir = new(GetUniqueTemporaryPath());
            tempDir.Create();
            return new Folder(_logger, tempDir);
        }

        private static string GetUniqueTemporaryPath()
        {
            string uniqueName = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            return Path.Combine(NuKeeperTempFilesPath(), $"{FolderPrefix}{uniqueName}");
        }
    }
}
