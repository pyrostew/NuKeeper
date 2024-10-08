using NuKeeper.Abstractions.CollaborationModels;

using System;

namespace NuKeeper.Abstractions.Configuration
{
    public class RepositorySettings
    {
        public RepositorySettings()
        {
        }

        public RepositorySettings(Repository repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            RepositoryUri = repository.CloneUrl;
            RepositoryOwner = repository.Owner.Login;
            RepositoryName = repository.Name;
        }

        public Uri RepositoryUri { get; set; }

        public string RepositoryOwner { get; set; }

        public string RepositoryName { get; set; }

        public Uri ApiUri { get; set; }

        public bool IsLocalRepo => RemoteInfo?.LocalRepositoryUri != null;

        public RemoteInfo RemoteInfo { get; set; }

        public bool SetAutoMerge { get; set; }
    }
}
