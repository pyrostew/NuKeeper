using NuKeeper.Abstractions.Configuration;

using System.Threading.Tasks;

namespace NuKeeper.Collaboration
{
    public interface ICollaborationEngine
    {
        Task<int> Run(SettingsContainer settings);
    }
}
