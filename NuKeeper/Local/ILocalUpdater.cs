using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Inspections.Files;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace NuKeeper.Local
{
    public interface ILocalUpdater
    {
        Task ApplyUpdates(
            IReadOnlyCollection<PackageUpdateSet> updates,
            IFolder workingFolder,
            NuGetSources sources,
            SettingsContainer settings);
    }
}
