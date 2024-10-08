using McMaster.Extensions.CommandLineUtils;

using NuKeeper.Abstractions.Configuration;
using NuKeeper.Inspection.Logging;

using System.IO;
using System.Threading.Tasks;

namespace NuKeeper.Commands
{
    internal abstract class LocalNuKeeperCommand : CommandBase
    {
        [Argument(0, Description = "The path to a .sln or project file, or to a directory containing a .NET solution/project. " +
               "If none is specified, the current directory will be used.")]
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        // ReSharper disable once MemberCanBePrivate.Global
        protected string Path { get; }

        protected LocalNuKeeperCommand(IConfigureLogger logger, IFileSettingsCache fileSettingsCache)
            : base(logger, fileSettingsCache)
        {
        }

        protected override async Task<ValidationResult> PopulateSettings(SettingsContainer settings)
        {
            ValidationResult baseResult = await base.PopulateSettings(settings);
            if (!baseResult.IsSuccess)
            {
                return baseResult;
            }

            if (!string.IsNullOrWhiteSpace(Path) && !Directory.Exists(Path))
            {
                return ValidationResult.Failure($"Path '{Path}' was not found");
            }

            settings.UserSettings.Directory = Path;
            return ValidationResult.Success;
        }
    }
}
