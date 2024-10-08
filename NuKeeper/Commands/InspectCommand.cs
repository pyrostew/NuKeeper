using McMaster.Extensions.CommandLineUtils;

using NuKeeper.Abstractions.Configuration;
using NuKeeper.Inspection.Logging;
using NuKeeper.Local;

using System.Threading.Tasks;

namespace NuKeeper.Commands
{
    [Command("inspect", "i", Description = "Checks projects existing locally for possible updates.")]
    internal class InspectCommand : LocalNuKeeperCommand
    {
        private readonly ILocalEngine _engine;

        public InspectCommand(ILocalEngine engine, IConfigureLogger logger, IFileSettingsCache fileSettingsCache) :
            base(logger, fileSettingsCache)
        {
            _engine = engine;
        }

        protected override async Task<ValidationResult> PopulateSettings(SettingsContainer settings)
        {
            ValidationResult baseResult = await base.PopulateSettings(settings);
            return !baseResult.IsSuccess ? baseResult : ValidationResult.Success;
        }

        protected override async Task<int> Run(SettingsContainer settings)
        {
            await _engine.Run(settings, false);
            return 0;
        }
    }
}
