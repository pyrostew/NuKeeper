using McMaster.Extensions.CommandLineUtils;

using NuKeeper.Abstractions;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Formats;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.Output;
using NuKeeper.Engine;
using NuKeeper.Inspection.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NuKeeper.Commands
{
    [HelpOption]
    internal abstract class CommandBase
    {
        private readonly IConfigureLogger _configureLogger;
        protected readonly IFileSettingsCache FileSettingsCache;

        [Option(CommandOptionType.SingleValue, ShortName = "c", LongName = "change",
            Description = "Allowed version change: Patch, Minor, Major. Defaults to Major.")]
        public VersionChange? AllowedChange { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "", LongName = "useprerelease",
            Description = "Allowed prerelease: Always, Never, FromPrerelease. Defaults to FromPrerelease.")]
        public UsePrerelease? UsePrerelease { get; set; }

        [Option(CommandOptionType.MultipleValue, ShortName = "s", LongName = "source",
            Description =
                "Specifies a NuGet package source to use during the operation. This setting overrides all of the sources specified in the NuGet.config files. Multiple sources can be provided by specifying this option multiple times.")]
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        // ReSharper disable once MemberCanBePrivate.Global
        protected string[] Source { get; }

        protected NuGetSources NuGetSources => Source == null ? null : new NuGetSources(Source);

        [Option(CommandOptionType.SingleValue, ShortName = "a", LongName = "age",
            Description = "Exclude updates that do not meet a minimum age, in order to not consume packages immediately after they are released. Examples: 0 = zero, 12h = 12 hours, 3d = 3 days, 2w = two weeks. The default is 7 days.")]
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        // ReSharper disable once MemberCanBePrivate.Global
        protected string MinimumPackageAge { get; }

        [Option(CommandOptionType.SingleValue, ShortName = "i", LongName = "include",
            Description = "Only consider packages matching this regex pattern.")]
        public string Include { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "e", LongName = "exclude",
            Description = "Do not consider packages matching this regex pattern.")]
        public string Exclude { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "v", LongName = "verbosity",
            Description = "Sets the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed].")]
        public LogLevel? Verbosity { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "", LongName = "logdestination",
            Description = "Destination for logging.")]
        public LogDestination? LogDestination { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "", LongName = "logfile",
            Description = "Log to the named file.")]
        public string LogFile { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "", LongName = "outputformat",
            Description = "Format for output.")]
        public OutputFormat? OutputFormat { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "", LongName = "outputdestination",
            Description = "Destination for output.")]
        public OutputDestination? OutputDestination { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "", LongName = "outputfile",
            Description = "File name for output.")]
        public string OutputFileName { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "", LongName = "branchnametemplate",
            Description = "Template used for creating the branch name.")]
        public string BranchNameTemplate { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "git", LongName = "gitclipath",
            Description = "Path to git to use instead of lib2gitsharp implementation")]
        public string GitCliPath { get; set; }

        protected CommandBase(IConfigureLogger logger, IFileSettingsCache fileSettingsCache)
        {
            _configureLogger = logger;
            FileSettingsCache = fileSettingsCache;
        }

        public async Task<int> OnExecute()
        {
            InitialiseLogging();

            SettingsContainer settings = MakeSettings();

            ValidationResult validationResult = await PopulateSettings(settings);
            if (!validationResult.IsSuccess)
            {
                INuKeeperLogger logger = _configureLogger as INuKeeperLogger;
                logger?.Error(validationResult.ErrorMessage);
                return -1;
            }

            return await Run(settings);
        }

        private void InitialiseLogging()
        {
            FileSettings settingsFromFile = FileSettingsCache.GetSettings();

            LogDestination defaultLogDestination = string.IsNullOrWhiteSpace(LogFile)
                ? Abstractions.Logging.LogDestination.Console
                : Abstractions.Logging.LogDestination.File;

            LogDestination logDest = Concat.FirstValue(LogDestination, settingsFromFile.LogDestination,
                defaultLogDestination);

            LogLevel logLevel = Concat.FirstValue(Verbosity, settingsFromFile.Verbosity, LogLevel.Normal);
            string logFile = Concat.FirstValue(LogFile, settingsFromFile.LogFile, "nukeeper.log");

            _configureLogger.Initialise(logLevel, logDest, logFile);
        }

        private SettingsContainer MakeSettings()
        {
            FileSettings fileSettings = FileSettingsCache.GetSettings();
            VersionChange allowedChange = Concat.FirstValue(AllowedChange, fileSettings.Change, VersionChange.Major);
            UsePrerelease usePrerelease = Concat.FirstValue(UsePrerelease, fileSettings.UsePrerelease, Abstractions.Configuration.UsePrerelease.FromPrerelease);
            string branchNameTemplate = Concat.FirstValue(BranchNameTemplate, fileSettings.BranchNameTemplate);
            string gitpath = Concat.FirstValue(GitCliPath, fileSettings.GitCliPath);

            SettingsContainer settings = new()
            {
                SourceControlServerSettings = new SourceControlServerSettings(),
                PackageFilters = new FilterSettings(),
                UserSettings = new UserSettings
                {
                    AllowedChange = allowedChange,
                    UsePrerelease = usePrerelease,
                    NuGetSources = NuGetSources,
                    GitPath = gitpath
                },
                BranchSettings = new BranchSettings
                {
                    BranchNameTemplate = branchNameTemplate
                }
            };

            return settings;
        }

        protected virtual async Task<ValidationResult> PopulateSettings(SettingsContainer settings)
        {
            TimeSpan? minPackageAge = ReadMinPackageAge();
            if (!minPackageAge.HasValue)
            {
                return await Task.FromResult(ValidationResult.Failure($"Min package age '{MinimumPackageAge}' could not be parsed"));
            }

            settings.PackageFilters.MinimumAge = minPackageAge.Value;

            ValidationResult regexIncludeValid = PopulatePackageIncludes(settings);
            if (!regexIncludeValid.IsSuccess)
            {
                return regexIncludeValid;
            }

            ValidationResult regexExcludeValid = PopulatePackageExcludes(settings);
            if (!regexExcludeValid.IsSuccess)
            {
                return regexExcludeValid;
            }

            FileSettings settingsFromFile = FileSettingsCache.GetSettings();

            OutputDestination defaultOutputDestination = string.IsNullOrWhiteSpace(OutputFileName)
                ? Abstractions.Output.OutputDestination.Console
                : Abstractions.Output.OutputDestination.File;

            settings.UserSettings.OutputDestination =
                Concat.FirstValue(OutputDestination, settingsFromFile.OutputDestination,
                    defaultOutputDestination);

            settings.UserSettings.OutputFormat =
                Concat.FirstValue(OutputFormat, settingsFromFile.OutputFormat,
                    Abstractions.Output.OutputFormat.Text);

            settings.UserSettings.OutputFileName =
                Concat.FirstValue(OutputFileName, settingsFromFile.OutputFileName,
                    "nukeeper.out");

            ValidationResult branchNameTemplateValid = PopulateBranchNameTemplate(settings);
            return !branchNameTemplateValid.IsSuccess ? branchNameTemplateValid : await Task.FromResult(ValidationResult.Success);
        }

        private TimeSpan? ReadMinPackageAge()
        {
            const string defaultMinPackageAge = "7d";
            FileSettings settingsFromFile = FileSettingsCache.GetSettings();
            string valueWithFallback = Concat.FirstValue(MinimumPackageAge, settingsFromFile.Age, defaultMinPackageAge);

            return DurationParser.Parse(valueWithFallback);
        }

        private ValidationResult PopulatePackageIncludes(
            SettingsContainer settings)
        {
            FileSettings settingsFromFile = FileSettingsCache.GetSettings();
            string value = Concat.FirstValue(Include, settingsFromFile.Include);

            if (string.IsNullOrWhiteSpace(value))
            {
                settings.PackageFilters.Includes = null;
                return ValidationResult.Success;
            }

            try
            {
                settings.PackageFilters.Includes = new Regex(value);
            }
            catch (ArgumentException ex)
            {
                {
                    return ValidationResult.Failure(
                        $"Unable to parse regex '{value}' for Include: {ex.Message}");
                }
            }

            return ValidationResult.Success;
        }

        private ValidationResult PopulatePackageExcludes(
            SettingsContainer settings)
        {
            FileSettings settingsFromFile = FileSettingsCache.GetSettings();
            string value = Concat.FirstValue(Exclude, settingsFromFile.Exclude);

            if (string.IsNullOrWhiteSpace(value))
            {
                settings.PackageFilters.Excludes = null;
                return ValidationResult.Success;
            }

            try
            {
                settings.PackageFilters.Excludes = new Regex(value);
            }
            catch (ArgumentException ex)
            {
                {
                    return ValidationResult.Failure(
                        $"Unable to parse regex '{value}' for Exclude: {ex.Message}");
                }
            }

            return ValidationResult.Success;
        }

        private ValidationResult PopulateBranchNameTemplate(
            SettingsContainer settings)
        {
            FileSettings settingsFromFile = FileSettingsCache.GetSettings();
            string value = Concat.FirstValue(BranchNameTemplate, settingsFromFile.BranchNameTemplate);

            if (string.IsNullOrWhiteSpace(value))
            {
                settings.BranchSettings.BranchNameTemplate = null;
                return ValidationResult.Success;
            }

            // Validating git branch names: https://stackoverflow.com/a/12093994/1661209
            // We validate the user defined branch name prefix in combination with a actual branch name that NuKeeper could create.
            // We want to validate the combination since the prefix doesn't need to fully comply with the rules (E.G. 'nukeeper/' is not allowed soley as a branch name).

            StringBuilder tokenErrors = new();
            IEnumerable<string> tokenSet = Regex.Matches(value, @"{(\w+)}").Select(match => match.Groups[1].Value);
            foreach (string token in tokenSet)
            {
                if (!BranchNamer.IsValidTemplateToken(token))
                {
                    _ = tokenErrors.Append($",{token}");
                }
            }

            // Check for valid placeholders
            if (tokenErrors.Length > 0)
            {
                return ValidationResult.Failure(
                    $"Provided branch template has unknown tokens: '{tokenErrors.ToString().Trim(',')}'.");
            }

            // Test if the generated branchname would be ok.
            // We assume tokens will be generated in valid values, so we use dummy values here
            Dictionary<string, string> tokenValues = [];
            foreach (string token in BranchNamer.TemplateTokens)
            {
                tokenValues.Add(token, "dummy");
            }

            string validationValue = BranchNamer.MakeName(tokenValues, value);
            if (!Regex.IsMatch(validationValue, @"^(?!@$|build-|/|.*([/.]\.|//|@\{|\\))[^\000-\037\177 ~^:?*[]+/[^\000-\037\177 ~^:?*[]+(?<!\.lock|[/.])$"))
            {
                return ValidationResult.Failure(
                    $"Provided branch template '{value}' does not comply with branch naming rules.");
            }

            settings.BranchSettings.BranchNameTemplate = value;
            return ValidationResult.Success;
        }

        protected abstract Task<int> Run(SettingsContainer settings);
    }
}
