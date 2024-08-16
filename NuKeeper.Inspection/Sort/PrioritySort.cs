using NuGet.Versioning;

using NuKeeper.Abstractions.RepositoryInspection;

using System;
using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Inspection.Sort
{
    public class PrioritySort : IPackageUpdateSetSort
    {
        private const long Shift = 1000;

        public IEnumerable<PackageUpdateSet> Sort(
            IReadOnlyCollection<PackageUpdateSet> packages)
        {
            return packages.OrderByDescending(Priority);
        }

        private static long Priority(PackageUpdateSet update)
        {
            long countCurrentVersions = update.CountCurrentVersions();
            long countUsages = update.CurrentPackages.Count;
            long versionChangeScore = ScoreVersionChange(update);
            long ageScore = ScoreAge(update);

            long score = countCurrentVersions;
            score *= Shift;
            score += countUsages;
            score *= Shift;
            score = score + versionChangeScore + ageScore;
            return score;
        }

        private static long ScoreAge(PackageUpdateSet update)
        {
            DateTimeOffset? publishedDate = update.Selected.Published;
            if (!publishedDate.HasValue)
            {
                return 0;
            }

            DateTime published = publishedDate.Value.ToUniversalTime().DateTime;
            TimeSpan interval = DateTime.UtcNow.Subtract(published);
            return interval.Days;
        }

        private static long ScoreVersionChange(PackageUpdateSet update)
        {
            NuGetVersion newVersion = update.Selected.Identity.Version;
            NuGetVersion versionInUse = update.CurrentPackages
                .Select(p => p.Version)
                .Max();

            return ScoreVersionChange(newVersion, versionInUse);
        }

        private static long ScoreVersionChange(NuGetVersion newVersion, NuGetVersion oldVersion)
        {
            long preReleaseScore = 0;
            if (oldVersion.IsPrerelease && !newVersion.IsPrerelease)
            {
                preReleaseScore = Shift * 12;
            }

            int majors = newVersion.Major - oldVersion.Major;
            if (majors > 0)
            {
                return (majors * 100) + preReleaseScore;
            }

            int minors = newVersion.Minor - oldVersion.Minor;
            if (minors > 0)
            {
                return (minors * 10) + preReleaseScore;
            }

            int patches = newVersion.Patch - oldVersion.Patch;
            return patches > 0 ? patches + preReleaseScore : preReleaseScore;
        }
    }
}
