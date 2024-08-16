using NuKeeper.Abstractions.NuGet;

using System.IO;
using System.Threading.Tasks;

namespace NuKeeper.Update.Process
{
    public interface IFileRestoreCommand : IPackageCommand
    {
        Task Invoke(FileInfo file, NuGetSources sources);
    }
}
