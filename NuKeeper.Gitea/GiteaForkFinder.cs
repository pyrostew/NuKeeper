using NuKeeper.Abstractions.CollaborationModels;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Logging;
using NuKeeper.GitHub;

using System;
using System.Threading.Tasks;

namespace NuKeeper.Gitea
{
    public class GiteaForkFinder : IForkFinder
    {
        private readonly ICollaborationPlatform _collaborationPlatform;
        private readonly INuKeeperLogger _logger;
        private readonly ForkMode _forkMode;

        public GiteaForkFinder(ICollaborationPlatform collaborationPlatform, INuKeeperLogger logger, ForkMode forkMode)
        {
            _collaborationPlatform = collaborationPlatform;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _forkMode = forkMode;

            _logger.Detailed($"FindPushFork. Fork Mode is {_forkMode}");
        }

        public async Task<ForkData> FindPushFork(string userName, ForkData fallbackFork)
        {
            return fallbackFork == null
                ? throw new ArgumentNullException(nameof(fallbackFork))
                : _forkMode switch
                {
                    ForkMode.PreferFork => await FindUserForkOrUpstream(userName, fallbackFork),
                    ForkMode.PreferSingleRepository => await FindUpstreamRepoOrUserFork(userName, fallbackFork),
                    ForkMode.SingleRepositoryOnly => await FindUpstreamRepoOnly(fallbackFork),
                    _ => throw new ArgumentOutOfRangeException($"Unknown fork mode: {_forkMode}"),
                };
        }

        private async Task<ForkData> FindUserForkOrUpstream(string userName, ForkData pullFork)
        {
            ForkData userFork = await TryFindUserFork(userName, pullFork);
            if (userFork != null)
            {
                return userFork;
            }

            // as a fallback, we want to pull and push from the same origin repo.
            bool canUseOriginRepo = await IsPushableRepo(pullFork);
            if (canUseOriginRepo)
            {
                _logger.Normal($"No fork for user {userName}. Using upstream fork for user {pullFork.Owner} at {pullFork.Uri}");
                return pullFork;
            }

            NoPushableForkFound(pullFork.Name);
            return null;
        }

        private async Task<ForkData> FindUpstreamRepoOrUserFork(string userName, ForkData pullFork)
        {
            // prefer to pull and push from the same origin repo.
            bool canUseOriginRepo = await IsPushableRepo(pullFork);
            if (canUseOriginRepo)
            {
                _logger.Normal($"Using upstream fork as push, for user {pullFork.Owner} at {pullFork.Uri}");
                return pullFork;
            }

            // fall back to trying a fork
            ForkData userFork = await TryFindUserFork(userName, pullFork);
            if (userFork != null)
            {
                return userFork;
            }

            NoPushableForkFound(pullFork.Name);
            return null;
        }

        private async Task<ForkData> FindUpstreamRepoOnly(ForkData pullFork)
        {
            // Only want to pull and push from the same origin repo.
            bool canUseOriginRepo = await IsPushableRepo(pullFork);
            if (canUseOriginRepo)
            {
                _logger.Normal($"Using upstream fork as push, for project {pullFork.Owner} at {pullFork.Uri}");
                return pullFork;
            }

            NoPushableForkFound(pullFork.Name);
            return null;
        }

        private void NoPushableForkFound(string name)
        {
            _logger.Error($"No pushable fork found for {name} in mode {_forkMode}");
        }

        private async Task<bool> IsPushableRepo(ForkData originFork)
        {
            Repository originRepo = await _collaborationPlatform.GetUserRepository(originFork.Owner, originFork.Name);
            return originRepo?.UserPermissions.Push == true;
        }

        private async Task<ForkData> TryFindUserFork(string userName, ForkData originFork)
        {
            Repository userFork = await _collaborationPlatform.GetUserRepository(userName, originFork.Name);
            if (userFork != null)
            {
                bool isMatchingFork = RepoIsForkOf(userFork, originFork.Uri);
                bool forkIsPushable = userFork.UserPermissions.Push;
                if (isMatchingFork && forkIsPushable)
                {
                    // the user has a pushable fork
                    return RepositoryToForkData(userFork);
                }

                // the user has a repo of that name, but it can't be used. 
                // Don't try to create it
                _logger.Normal($"User '{userName}' fork of '{originFork.Name}' exists but is unsuitable. Matching: {isMatchingFork}. Pushable: {forkIsPushable}");
                return null;
            }

            // no user fork exists, try and create it as a fork of the main repo
            Repository newFork = await _collaborationPlatform.MakeUserFork(originFork.Owner, originFork.Name);
            return newFork != null ? RepositoryToForkData(newFork) : null;
        }

        private static bool RepoIsForkOf(Repository userRepo, Uri originRepo)
        {
            if (!userRepo.Fork)
            {
                return false;
            }

            if (userRepo.Parent?.CloneUrl == null)
            {
                return false;
            }

            if (originRepo == null)
            {
                return false;
            }

            Uri userParentUrl = GithubUriHelpers.Normalise(userRepo.Parent.CloneUrl);
            Uri originUrl = GithubUriHelpers.Normalise(originRepo);

            return userParentUrl.Equals(originUrl);
        }

        private static ForkData RepositoryToForkData(Repository repo)
        {
            return new ForkData(repo.CloneUrl, repo.Owner.Login, repo.Name);
        }
    }
}
