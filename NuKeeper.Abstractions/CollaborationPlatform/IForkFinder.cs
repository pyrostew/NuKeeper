using NuKeeper.Abstractions.CollaborationModels;

using System.Threading.Tasks;

namespace NuKeeper.Abstractions.CollaborationPlatform
{
    public interface IForkFinder
    {
        Task<ForkData> FindPushFork(string userName, ForkData fallbackFork);
    };
}
