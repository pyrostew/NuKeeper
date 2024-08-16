using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Git;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.RepositoryInspection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NuKeeper.Engine.Packages
{
    public class ExistingCommitFilter : IExistingCommitFilter
    {
        private readonly ICollaborationFactory _collaborationFactory;
        private readonly INuKeeperLogger _logger;

        public ExistingCommitFilter(ICollaborationFactory collaborationFactory, INuKeeperLogger logger)
        {
            _collaborationFactory = collaborationFactory;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<PackageUpdateSet>> Filter(IGitDriver git, IReadOnlyCollection<PackageUpdateSet> updates, string baseBranch, string headBranch)
        {
            if (git == null)
            {
                throw new ArgumentNullException(nameof(git));
            }

            if (updates == null)
            {
                throw new ArgumentNullException(nameof(updates));
            }

            try
            {
                List<PackageUpdateSet> filtered = [];
                // commit messages are compared without whitespace because the system tends to add ws.
                IReadOnlyCollection<string> commitMessages = await git.GetNewCommitMessages(baseBranch, headBranch);
                IEnumerable<string> compactCommitMessages = commitMessages.Select(m => new string(m.Where(c => !char.IsWhiteSpace(c)).ToArray()));

                foreach (PackageUpdateSet update in updates)
                {
                    string updateCommitMessage = _collaborationFactory.CommitWorder.MakeCommitMessage(update);
                    string compactUpdateCommitMessage = new(updateCommitMessage.Where(c => !char.IsWhiteSpace(c)).ToArray());

                    if (!compactCommitMessages.Contains(compactUpdateCommitMessage))
                    {
                        filtered.Add(update);
                    }

                }
                return filtered;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed on existing Commit check for {baseBranch} <= {headBranch}", ex);

                return updates;
            }
        }
    }
}
