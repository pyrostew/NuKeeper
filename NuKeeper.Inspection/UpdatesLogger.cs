using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Inspection.Logging;

using System;
using System.Collections.Generic;
using System.Text;

namespace NuKeeper.Inspection
{
    public static class UpdatesLogger
    {
        public static LogData Log(IReadOnlyCollection<PackageUpdateSet> updates)
        {
            if (updates == null)
            {
                throw new ArgumentNullException(nameof(updates));
            }

            string headline = $"Found {updates.Count} possible updates";
            StringBuilder details = new();

            foreach (PackageUpdateSet updateSet in updates)
            {
                foreach (PackageInProject current in updateSet.CurrentPackages)
                {
                    _ = details.AppendLine($"{updateSet.SelectedId} from {current.Version} to {updateSet.SelectedVersion} in {current.Path.RelativePath}");
                }
            }
            return new LogData
            {
                Terse = headline,
                Info = details.ToString()
            };
        }
    }
}
