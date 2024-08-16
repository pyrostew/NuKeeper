using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Git;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace NuKeeper.Engine.Packages
{
    public interface IPackageUpdater
    {
        Task<(int UpdatesMade, bool ThresholdReached)> MakeUpdatePullRequests(
            IGitDriver git,
            RepositoryData repository,
            IReadOnlyCollection<PackageUpdateSet> updates,
            NuGetSources sources,
            SettingsContainer settings
        );
    }
}
