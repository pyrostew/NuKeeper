using NuKeeper.Abstractions.Configuration;

using System.Threading.Tasks;

namespace NuKeeper.Engine
{
    public interface IRepositoryFilter
    {
        Task<bool> ContainsDotNetProjects(RepositorySettings repository);
    }
}
