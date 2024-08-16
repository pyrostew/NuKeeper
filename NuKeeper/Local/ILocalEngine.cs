using NuKeeper.Abstractions.Configuration;

using System.Threading.Tasks;

namespace NuKeeper.Local
{
    public interface ILocalEngine
    {
        Task Run(SettingsContainer settings, bool write);
    }
}
