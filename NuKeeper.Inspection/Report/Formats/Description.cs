using NuKeeper.Abstractions.Formats;
using NuKeeper.Abstractions.RepositoryInspection;

using System;
using System.Linq;

namespace NuKeeper.Inspection.Report.Formats
{
    public static class Description
    {
        public static string ForUpdateSet(PackageUpdateSet update)
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            int occurrences = update.CurrentPackages.Count;
            System.Collections.Generic.List<NuGet.Versioning.NuGetVersion> versionsInUse = update.CurrentPackages
                .Select(p => p.Version)
                .ToList();

            NuGet.Versioning.NuGetVersion lowest = versionsInUse.Min();
            NuGet.Versioning.NuGetVersion highest = versionsInUse.Max();

            string versionInUse = lowest == highest ? highest.ToString() : $"{lowest} - {highest}";
            string ago = "?";
            if (update.Selected.Published.HasValue)
            {
                DateTime pubDate = update.Selected.Published.Value.UtcDateTime;
                ago = TimeSpanFormat.Ago(pubDate, DateTime.UtcNow);
            }

            string optS = occurrences > 1 ? "s" : string.Empty;

            return $"{update.SelectedId} to {update.SelectedVersion} from {versionInUse} in {occurrences} place{optS} since {ago}.";
        }

    }
}
