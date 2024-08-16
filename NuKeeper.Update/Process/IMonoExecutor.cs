using NuKeeper.Update.ProcessRunner;

using System.Threading.Tasks;

namespace NuKeeper.Update.Process
{
    public interface IMonoExecutor : IExternalProcess
    {
        Task<bool> CanRun();
    }
}
