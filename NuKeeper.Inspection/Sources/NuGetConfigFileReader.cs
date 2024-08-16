using NuGet.Configuration;

using NuKeeper.Abstractions.Inspections.Files;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.NuGet;

using System;
using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Inspection.Sources
{
    public class NuGetConfigFileReader : INuGetConfigFileReader
    {
        private readonly INuKeeperLogger _logger;

        public NuGetConfigFileReader(INuKeeperLogger logger)
        {
            _logger = logger;
        }

        public NuGetSources ReadNugetSources(IFolder workingFolder)
        {
            if (workingFolder == null)
            {
                throw new ArgumentNullException(nameof(workingFolder));
            }

            ISettings settings = Settings.LoadDefaultSettings(workingFolder.FullPath);

            foreach (string file in settings.GetConfigFilePaths())
            {
                _logger.Detailed($"Reading file {file} for package sources");
            }

            List<PackageSource> enabledSources = SettingsUtility.GetEnabledSources(settings).ToList();

            return ReadFromFile(enabledSources);
        }

        private NuGetSources ReadFromFile(IReadOnlyCollection<PackageSource> sources)
        {
            foreach (PackageSource source in sources)
            {
                _logger.Detailed(
                    $"Read [{source.Name}] : {source.SourceUri} from file: {source.Source}");
            }

            return new NuGetSources(sources);
        }
    }
}
