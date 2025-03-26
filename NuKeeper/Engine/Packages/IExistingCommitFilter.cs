using System.Collections.Generic;
using System.Threading.Tasks;

using NuKeeper.Abstractions.Git;
using NuKeeper.Abstractions.RepositoryInspection;

namespace NuKeeper.Engine.Packages
{
    public interface IExistingCommitFilter
    {
        Task<IReadOnlyCollection<PackageUpdateSet>> Filter(
            IGitDriver git,
            IReadOnlyCollection<PackageUpdateSet> updates,
            string baseBranch,
            string headBranch);
    }
}
