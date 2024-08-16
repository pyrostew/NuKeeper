using NuGet.Versioning;

using NuKeeper.Abstractions.Formats;
using NuKeeper.Abstractions.NuGetApi;
using NuKeeper.Abstractions.RepositoryInspection;

using System;
using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Inspection.Report.Formats
{
    public class CsvReportFormat : IReportFormat
    {
        private readonly IReportWriter _writer;

        public CsvReportFormat(IReportWriter writer)
        {
            _writer = writer;
        }

        public void Write(string name, IReadOnlyCollection<PackageUpdateSet> updates)
        {
            if (updates == null)
            {
                throw new ArgumentNullException(nameof(updates));
            }

            WriteHeading();

            foreach (PackageUpdateSet update in updates)
            {
                WriteLine(update);
            }
        }

        private void WriteHeading()
        {
            _writer.WriteLine(
                "Package id,Package source," +
                "Usage count,Versions in use,Lowest version in use,Highest Version in use," +
                "Major version update,Major published date," +
                "Minor version update,Minor published date," +
                "Patch version update,Patch published date"
                );
        }

        private void WriteLine(PackageUpdateSet update)
        {
            int occurrences = update.CurrentPackages.Count;
            List<NuGetVersion> versionsInUse = update.CurrentPackages
                .Select(p => p.Version)
                .ToList();

            NuGetVersion lowest = versionsInUse.Min();
            NuGetVersion highest = versionsInUse.Max();

            NuGet.Configuration.PackageSource packageSource = update.Selected.Source;

            string majorData = PackageVersionAndDate(lowest, update.Packages.Major);
            string minorData = PackageVersionAndDate(lowest, update.Packages.Minor);
            string patchData = PackageVersionAndDate(lowest, update.Packages.Patch);

            _writer.WriteLine(
                $"{update.SelectedId},{packageSource}," +
                $"{occurrences},{update.CountCurrentVersions()},{lowest},{highest}," +
                $"{majorData},{minorData},{patchData}");
        }

        private static string PackageVersionAndDate(NuGetVersion baseline, PackageSearchMetadata packageVersion)
        {
            const string none = ",";

            if (packageVersion == null)
            {
                return none;
            }

            if (packageVersion.Identity.Version <= baseline)
            {
                return none;
            }

            NuGetVersion version = packageVersion.Identity.Version;
            string date = DateFormat.AsUtcIso8601(packageVersion.Published);
            return $"{version},{date}";
        }
    }
}
