using NuKeeper.Abstractions.Configuration;

using System;
using System.Threading.Tasks;

namespace NuKeeper.Abstractions.CollaborationPlatform
{
    public interface ISettingsReader
    {
        Platform Platform { get; }

        Task<bool> CanRead(Uri repositoryUri);

        Task<RepositorySettings> RepositorySettings(Uri repositoryUri, bool setAutoMerge, string targetBranch = null);

        void UpdateCollaborationPlatformSettings(CollaborationPlatformSettings settings);
    }
}
