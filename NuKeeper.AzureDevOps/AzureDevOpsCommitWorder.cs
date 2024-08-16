using NuGet.Packaging.Core;
using NuGet.Versioning;

using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Formats;
using NuKeeper.Abstractions.RepositoryInspection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuKeeper.AzureDevOps
{
    public class AzureDevOpsCommitWorder : ICommitWorder
    {
        private const string CommitEmoji = "📦";

        // Azure DevOps allows a maximum of 4000 characters to be used in a pull request description:
        // https://visualstudio.uservoice.com/forums/330519-azure-devops-formerly-visual-studio-team-services/suggestions/20217283-raise-the-character-limit-for-pull-request-descrip
        private const int MaxCharacterCount = 4000;

        public string MakePullRequestTitle(IReadOnlyCollection<PackageUpdateSet> updates)
        {
            return updates == null
                ? throw new ArgumentNullException(nameof(updates))
                : updates.Count == 1 ? PackageTitle(updates.First()) : $"{CommitEmoji} Automatic update of {updates.Count} packages";
        }

        private static string PackageTitle(PackageUpdateSet updates)
        {
            return $"{CommitEmoji} Automatic update of {updates.SelectedId} to {updates.SelectedVersion}";
        }

        public string MakeCommitMessage(PackageUpdateSet updates)
        {
            return updates == null ? throw new ArgumentNullException(nameof(updates)) : $"{PackageTitle(updates)}";
        }

        public string MakeCommitDetails(IReadOnlyCollection<PackageUpdateSet> updates)
        {
            if (updates == null)
            {
                throw new ArgumentNullException(nameof(updates));
            }

            StringBuilder builder = new();

            if (updates.Count > 1)
            {
                MultiPackage(updates, builder);
            }

            foreach (PackageUpdateSet update in updates)
            {
                _ = builder.AppendLine(MakeCommitVersionDetails(update));
            }

            AddCommitFooter(builder);

            if (builder.Length > MaxCharacterCount)
            {
                // Strip end of commit details since Azure DevOps can't handle a bigger pull request description.
                return $"{builder.ToString(0, MaxCharacterCount - 3)}...";
            }

            return builder.ToString();
        }

        private static void MultiPackage(IReadOnlyCollection<PackageUpdateSet> updates, StringBuilder builder)
        {
            IEnumerable<string> packageNames = updates
                .Select(p => p.SelectedId);

            List<string> projects = updates.SelectMany(
                    u => u.CurrentPackages)
                .Select(p => p.Path.FullName)
                .Distinct()
                .ToList();

            string projectOptS = (projects.Count > 1) ? "s" : string.Empty;

            _ = builder.AppendLine($"{updates.Count} packages were updated in {projects.Count} project{projectOptS}:");
            string updatedPackageNames = "|";
            foreach (string packageName in packageNames)
            {
                updatedPackageNames += $" {packageName} |";
            }

            _ = builder.AppendLine(updatedPackageNames);
            _ = builder.AppendLine("");
            _ = builder.AppendLine("## Details of updated packages");
            _ = builder.AppendLine("");
        }

        private static string MakeCommitVersionDetails(PackageUpdateSet updates)
        {
            List<NuGetVersion> versionsInUse = updates.CurrentPackages
                .Select(u => u.Version)
                .Distinct()
                .ToList();

            List<string> oldVersions = versionsInUse
                .Select(v => CodeQuote(v.ToString()))
                .ToList();

            NuGetVersion minOldVersion = versionsInUse.Min();

            string newVersion = CodeQuote(updates.SelectedVersion.ToString());
            string packageId = CodeQuote(updates.SelectedId);

            string changeLevel = ChangeLevel(minOldVersion, updates.SelectedVersion);

            StringBuilder builder = new();

            if (oldVersions.Count == 1)
            {
                _ = builder.AppendLine($"NuKeeper has generated a {changeLevel} update of {packageId} to {newVersion} from {oldVersions.JoinWithCommas()}");
            }
            else
            {
                _ = builder.AppendLine($"NuKeeper has generated a {changeLevel} update of {packageId} to {newVersion}");
                _ = builder.AppendLine($"{oldVersions.Count} versions of {packageId} were found in use: {oldVersions.JoinWithCommas()}");
            }

            if (updates.Selected.Published.HasValue)
            {
                string packageWithVersion = CodeQuote(updates.SelectedId + " " + updates.SelectedVersion);
                string pubDateString = CodeQuote(DateFormat.AsUtcIso8601(updates.Selected.Published));
                DateTime pubDate = updates.Selected.Published.Value.UtcDateTime;
                string ago = TimeSpanFormat.Ago(pubDate, DateTime.UtcNow);

                _ = builder.AppendLine($"{packageWithVersion} was published at {pubDateString}, {ago}");
            }

            NuGetVersion highestVersion = updates.Packages.Major?.Identity.Version;
            if (highestVersion != null && (highestVersion > updates.SelectedVersion))
            {
                LogHighestVersion(updates, highestVersion, builder);
            }

            _ = builder.AppendLine();

            string updateOptS = (updates.CurrentPackages.Count > 1) ? "s" : string.Empty;
            _ = builder.AppendLine($"### {updates.CurrentPackages.Count} project update{updateOptS}:");

            _ = builder.AppendLine("| Project   | Package   | From   | To   |");
            _ = builder.AppendLine("|:----------|:----------|-------:|-----:|");

            foreach (PackageInProject current in updates.CurrentPackages)
            {
                string line;
                if (SourceIsPublicNuget(updates.Selected.Source.SourceUri))
                {
                    line = $"| {CodeQuote(current.Path.RelativePath)} | {CodeQuote(updates.SelectedId)} | {NuGetVersionPackageLink(current.Identity)} | {NuGetVersionPackageLink(updates.Selected.Identity)} |";
                    _ = builder.AppendLine(line);

                    continue;
                }

                line = $"| {CodeQuote(current.Path.RelativePath)} | {CodeQuote(updates.SelectedId)} | {current.Version} | {updates.SelectedVersion} |";
                _ = builder.AppendLine(line);
            }

            return builder.ToString();
        }

        private static void AddCommitFooter(StringBuilder builder)
        {
            _ = builder.AppendLine("This is an automated update. Merge only if it passes tests");
            _ = builder.AppendLine("**NuKeeper**: https://github.com/NuKeeperDotNet/NuKeeper");
        }

        private static string ChangeLevel(NuGetVersion oldVersion, NuGetVersion newVersion)
        {
            return newVersion.Major > oldVersion.Major
                ? "major"
                : newVersion.Minor > oldVersion.Minor
                ? "minor"
                : newVersion.Patch > oldVersion.Patch
                ? "patch"
                : !newVersion.IsPrerelease && oldVersion.IsPrerelease ? "out of beta" : string.Empty;
        }

        private static void LogHighestVersion(PackageUpdateSet updates, NuGetVersion highestVersion, StringBuilder builder)
        {
            string allowedChange = CodeQuote(updates.AllowedChange.ToString());
            string highest = CodeQuote(updates.SelectedId + " " + highestVersion);
            string highestPublishedAt = HighestPublishedAt(updates.Packages.Major.Published);

            _ = builder.AppendLine(
                $"There is also a higher version, {highest}{highestPublishedAt}, " +
                $"but this was not applied as only {allowedChange} version changes are allowed.");
        }

        private static string HighestPublishedAt(DateTimeOffset? highestPublishedAt)
        {
            if (!highestPublishedAt.HasValue)
            {
                return string.Empty;
            }

            DateTimeOffset highestPubDate = highestPublishedAt.Value;
            string formattedPubDate = CodeQuote(DateFormat.AsUtcIso8601(highestPubDate));
            string highestAgo = TimeSpanFormat.Ago(highestPubDate.UtcDateTime, DateTime.UtcNow);

            return $" published at {formattedPubDate}, {highestAgo}";
        }

        private static string CodeQuote(string value)
        {
            return "`" + value + "`";
        }

        private static bool SourceIsPublicNuget(Uri sourceUrl)
        {
            return
                sourceUrl != null &&
                sourceUrl.ToString().StartsWith("https://api.nuget.org/", StringComparison.OrdinalIgnoreCase);
        }

        private static string NuGetVersionPackageLink(PackageIdentity package)
        {
            string url = $"https://www.nuget.org/packages/{package.Id}/{package.Version}";
            return $"[{package.Version}]({url})";
        }
    }
}
