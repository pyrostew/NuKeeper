using NuKeeper.Abstractions.Formats;
using NuKeeper.Abstractions.RepositoryInspection;

using System;
using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Engine
{
    public static class UpdatesLogger
    {
        public static string OldVersionsToBeUpdated(IReadOnlyCollection<PackageUpdateSet> updates)
        {
            return updates == null
                ? throw new ArgumentNullException(nameof(updates))
                : updates.Count == 1
                ? $"Updating {DescribeOldVersions(updates.First())}"
                : $"Updating {updates.Count} packages" + Environment.NewLine +
                updates.Select(DescribeOldVersions).JoinWithSeparator(Environment.NewLine);
        }

        private static string DescribeOldVersions(PackageUpdateSet updateSet)
        {
            IEnumerable<string> oldVersions = updateSet.CurrentPackages
                .Select(u => u.Version.ToString())
                .Distinct();

            return $"'{updateSet.SelectedId}' from {oldVersions.JoinWithCommas()} to {updateSet.SelectedVersion} in {updateSet.CurrentPackages.Count} projects";
        }
    }
}
