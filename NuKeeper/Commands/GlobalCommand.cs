using McMaster.Extensions.CommandLineUtils;

using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Collaboration;
using NuKeeper.Inspection.Logging;

using System;
using System.Threading.Tasks;

namespace NuKeeper.Commands
{
    [Command("global", Description = "Performs version checks and generates pull requests for all repositories the provided token can access.")]
    internal class GlobalCommand : MultipleRepositoryCommand
    {
        public GlobalCommand(ICollaborationEngine engine, IConfigureLogger logger, IFileSettingsCache fileSettingsCache, ICollaborationFactory collaborationFactory)
            : base(engine, logger, fileSettingsCache, collaborationFactory)
        {
        }

        protected override async Task<ValidationResult> PopulateSettings(SettingsContainer settings)
        {
            ValidationResult baseResult = await base.PopulateSettings(settings);
            if (!baseResult.IsSuccess)
            {
                return baseResult;
            }

            settings.SourceControlServerSettings.Scope = ServerScope.Global;

            if (settings.PackageFilters.Includes == null)
            {
                return ValidationResult.Failure("Global mode must have an include regex");
            }

            string apiHost = CollaborationFactory.Settings.BaseApiUrl.Host;
            return apiHost.EndsWith("github.com", StringComparison.OrdinalIgnoreCase)
                ? ValidationResult.Failure("Global mode must not use public github")
                : ValidationResult.Success;
        }
    }
}
