using NuKeeper.Abstractions.CollaborationModels;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Git;

using System.Threading.Tasks;

namespace NuKeeper.Engine
{
    public interface IGitRepositoryEngine
    {
        Task<int> Run(RepositorySettings repository,
            GitUsernamePasswordCredentials credentials,
            SettingsContainer settings, User user);
    }
}
