using NuGet.Versioning;

using NuKeeper.Abstractions.NuGetApi;
using NuKeeper.Abstractions.RepositoryInspection;

using System;
using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Inspection.Report.Formats
{
    public class MetricsReportFormat : IReportFormat
    {
        private readonly IReportWriter _writer;

        public MetricsReportFormat(IReportWriter writer)
        {
            _writer = writer;
        }

        public void Write(string name, IReadOnlyCollection<PackageUpdateSet> updates)
        {
            if (updates == null)
            {
                throw new ArgumentNullException(nameof(updates));
            }

            _writer.WriteLine($"Packages with updates: {updates.Count}");
            WriteMajorMinorPatchCount(updates);
            WriteProjectCount(updates);
            WriteLibYears(updates);
        }

        private void WriteMajorMinorPatchCount(IReadOnlyCollection<PackageUpdateSet> updates)
        {
            int majors = 0;
            int minors = 0;
            int patches = 0;
            foreach (PackageUpdateSet update in updates)
            {
                NuGetVersion baselineVersion = MinCurrentVersion(update);

                PackageSearchMetadata majorUpdate = FilteredPackageVersion(baselineVersion, update.Packages.Major);
                PackageSearchMetadata minorUpdate = FilteredPackageVersion(baselineVersion, update.Packages.Minor);
                PackageSearchMetadata patchUpdate = FilteredPackageVersion(baselineVersion, update.Packages.Patch);

                if (majorUpdate != null && majorUpdate.Identity.Version.Major > baselineVersion.Major)
                {
                    majors++;
                }
                if (minorUpdate != null && minorUpdate.Identity.Version.Minor > baselineVersion.Minor)
                {
                    minors++;
                }

                if (patchUpdate != null)
                {
                    patches++;
                }
            }

            _writer.WriteLine($"Packages with Major version updates: {majors}");
            _writer.WriteLine($"Packages with Minor version updates: {minors}");
            _writer.WriteLine($"Packages with Patch version updates: {patches}");
        }

        private static NuGetVersion MinCurrentVersion(PackageUpdateSet updates)
        {
            return updates.CurrentPackages
                .Select(p => p.Version)
                .Min();
        }

        private void WriteProjectCount(IReadOnlyCollection<PackageUpdateSet> updates)
        {
            List<PackageInProject> currentPackagesInProjects = updates
                .SelectMany(p => p.CurrentPackages)
                .ToList();

            int projectCount = currentPackagesInProjects
                .Select(c => c.Path.FullName)
                .Distinct()
                .Count();

            _writer.WriteLine($"Projects with updates: {projectCount}");
            _writer.WriteLine($"Updates in projects: {currentPackagesInProjects.Count}");

        }

        private void WriteLibYears(IReadOnlyCollection<PackageUpdateSet> updates)
        {
            TimeSpan totalAge = Age.Sum(updates);
            string displayValue = Age.AsLibYears(totalAge);
            _writer.WriteLine($"LibYears: {displayValue}");
        }

        private static PackageSearchMetadata FilteredPackageVersion(NuGetVersion baseline, PackageSearchMetadata packageVersion)
        {
            return packageVersion == null ? null : packageVersion.Identity.Version <= baseline ? null : packageVersion;
        }

    }
}
