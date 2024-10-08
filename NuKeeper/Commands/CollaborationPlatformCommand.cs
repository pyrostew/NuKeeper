using McMaster.Extensions.CommandLineUtils;

using NuKeeper.Abstractions;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Collaboration;
using NuKeeper.Inspection.Logging;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NuKeeper.Commands
{
    internal abstract class CollaborationPlatformCommand : CommandBase
    {
        private readonly ICollaborationEngine _engine;
        public readonly ICollaborationFactory CollaborationFactory;

        [Argument(1, Name = "Token",
            Description = "Personal access token to authorise access to server.")]
        public string PersonalAccessToken { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "f", LongName = "fork",
            Description =
                "Prefer to make branches on a fork of the writer repository, or on that repository itself. Allowed values are PreferFork, PreferSingleRepository, SingleRepositoryOnly.")]
        public ForkMode? ForkMode { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "m", LongName = "maxpackageupdates",
            Description = "The maximum number of package updates to apply on one repository. Defaults to 3.")]
        public int? MaxPackageUpdates { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "", LongName = "maxopenpullrequests",
            Description = "The maximum number of open pull requests for one repository. Defaults to 1 if `--consolidate` is specified, otherwise defaults to `--maxpackageupdates`.")]
        public int? MaxOpenPullRequests { get; set; }

        [Option(CommandOptionType.NoValue, ShortName = "n", LongName = "consolidate",
            Description = "Consolidate updates into a single pull request. Defaults to false.")]
        public bool? Consolidate { get; set; }

        [Option(CommandOptionType.MultipleValue, ShortName = "l", LongName = "label",
            Description =
                "Label to apply to GitHub pull requests. Defaults to 'nukeeper'. Multiple labels can be provided by specifying this option multiple times.")]
        public List<string> Label { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "g", LongName = "api",
            Description =
                "Api Base Url. If you are using an internal server and not a public one, you must set it to the api url of your server.")]
        public string ApiEndpoint { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "", LongName = "platform",
            Description = "Sets the collaboration platform type. By default this is inferred from the Url.")]
        public Platform? Platform { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "d", LongName = "deletebranchaftermerge",
            Description = "Deletes branch created by NuKeeper after merge. Defaults to true.")]
        public bool? DeleteBranchAfterMerge { get; set; }

        private readonly HashSet<Platform> _platformsSupportingDeleteBranchAfterMerge = [];

        protected CollaborationPlatformCommand(ICollaborationEngine engine, IConfigureLogger logger,
            IFileSettingsCache fileSettingsCache, ICollaborationFactory collaborationFactory) :
            base(logger, fileSettingsCache)
        {
            _engine = engine;
            CollaborationFactory = collaborationFactory;
            _ = _platformsSupportingDeleteBranchAfterMerge.Add(Abstractions.CollaborationPlatform.Platform.AzureDevOps);
            _ = _platformsSupportingDeleteBranchAfterMerge.Add(Abstractions.CollaborationPlatform.Platform.Bitbucket);
            _ = _platformsSupportingDeleteBranchAfterMerge.Add(Abstractions.CollaborationPlatform.Platform.GitLab);
            _ = _platformsSupportingDeleteBranchAfterMerge.Add(Abstractions.CollaborationPlatform.Platform.Gitea);
        }

        protected override async Task<ValidationResult> PopulateSettings(SettingsContainer settings)
        {
            ValidationResult baseResult = await base.PopulateSettings(settings);
            if (!baseResult.IsSuccess)
            {
                return baseResult;
            }

            FileSettings fileSettings = FileSettingsCache.GetSettings();

            string endpoint = Concat.FirstValue(ApiEndpoint, fileSettings.Api, settings.SourceControlServerSettings.Repository?.ApiUri.ToString());
            ForkMode? forkMode = ForkMode ?? fileSettings.ForkMode;
            Platform? platform = Platform ?? fileSettings.Platform;

            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri baseUri))
            {
                return ValidationResult.Failure($"Bad Api Base '{endpoint}'");
            }

            try
            {
                ValidationResult collaborationResult = await CollaborationFactory.Initialise(
                    baseUri, PersonalAccessToken,
                    forkMode, platform);

                if (!collaborationResult.IsSuccess)
                {
                    return collaborationResult;
                }
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure(ex.Message);
            }

            if (CollaborationFactory.Settings.Token == null)
            {
                return ValidationResult.Failure("The required access token was not found");
            }

            bool consolidate =
                Concat.FirstValue(Consolidate, fileSettings.Consolidate, false);

            settings.UserSettings.ConsolidateUpdatesInSinglePullRequest = consolidate;

            const int defaultMaxPackageUpdates = 3;
            int maxPackageUpdates =
                Concat.FirstValue(MaxPackageUpdates, fileSettings.MaxPackageUpdates, defaultMaxPackageUpdates);

            settings.PackageFilters.MaxPackageUpdates = maxPackageUpdates;

            const int defaultMaxOpenPullRequests = 1;
            settings.UserSettings.MaxOpenPullRequests = Concat.FirstValue(
                MaxOpenPullRequests,
                fileSettings.MaxOpenPullRequests,
                consolidate ?
                    defaultMaxOpenPullRequests
                    : maxPackageUpdates
            );

            List<string> defaultLabels = ["nukeeper"];

            settings.SourceControlServerSettings.Labels =
                Concat.FirstPopulatedList(Label, fileSettings.Label, defaultLabels);

            ValidationResult deleteBranchAfterMergeValid = PopulateDeleteBranchAfterMerge(settings);
            return !deleteBranchAfterMergeValid.IsSuccess ? deleteBranchAfterMergeValid : ValidationResult.Success;
        }

        protected override async Task<int> Run(SettingsContainer settings)
        {
            _ = await _engine.Run(settings);
            return 0;
        }

        private ValidationResult PopulateDeleteBranchAfterMerge(
            SettingsContainer settings)
        {
            FileSettings fileSettings = FileSettingsCache.GetSettings();

            bool defaultValue = !Platform.HasValue || _platformsSupportingDeleteBranchAfterMerge.Contains(Platform.Value);

            // The default value is true, if it is supported for the corresponding platform.

            settings.BranchSettings.DeleteBranchAfterMerge = Concat.FirstValue(DeleteBranchAfterMerge, fileSettings.DeleteBranchAfterMerge, defaultValue);

            // Ensure that the resulting DeleteBranchAfterMerge value is supported.
            return settings.BranchSettings.DeleteBranchAfterMerge &&
                Platform.HasValue &&
                !_platformsSupportingDeleteBranchAfterMerge.Contains(Platform.Value)
                ? ValidationResult.Failure("Deletion of source branch after merge is currently only available for Azure DevOps, Gitlab and Bitbucket.")
                : ValidationResult.Success;
        }
    }
}
