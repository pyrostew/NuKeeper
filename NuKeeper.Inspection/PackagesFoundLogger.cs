using NuKeeper.Abstractions.Formats;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Inspection.Logging;

using System;
using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Inspection
{
    public static class PackagesFoundLogger
    {
        public static LogData Log(IReadOnlyCollection<PackageInProject> packages)
        {
            if (packages == null)
            {
                throw new ArgumentNullException(nameof(packages));
            }

            int projectPathCount = packages
                .Select(p => p.Path)
                .Distinct()
                .Count();

            List<string> packageIds = packages
                .OrderBy(p => p.Id)
                .Select(p => p.Id)
                .Distinct()
                .ToList();

            string headline = $"Found {packages.Count} packages in use, {packageIds.Count} distinct, in {projectPathCount} projects.";

            return new LogData
            {
                Terse = headline,
                Info = packageIds.JoinWithCommas()
            };
        }
    }
}
