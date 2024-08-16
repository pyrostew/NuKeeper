using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Git;

using System.Threading.Tasks;

namespace NuKeeper.Engine
{
    public interface IRepositoryUpdater
    {
        Task<int> Run(IGitDriver git, RepositoryData repository, SettingsContainer settings);
    }
}
