using NuKeeper.Abstractions.Configuration;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace NuKeeper.Abstractions.CollaborationPlatform
{
    public interface IRepositoryDiscovery
    {
        Task<IEnumerable<RepositorySettings>> GetRepositories(SourceControlServerSettings settings);
    }
}
