using NuKeeper.Abstractions.Git;
using NuKeeper.Abstractions.Inspections.Files;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Update.ProcessRunner;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NuKeeper.Git
{
    public class GitCmdDriver : IGitDriver
    {
        private readonly GitUsernamePasswordCredentials _gitCredentials;
        private readonly string _pathGit;
        private readonly INuKeeperLogger _logger;

        public GitCmdDriver(string pathToGit, INuKeeperLogger logger,
            IFolder workingFolder, GitUsernamePasswordCredentials credentials)
        {
            if (string.IsNullOrWhiteSpace(pathToGit))
            {
                throw new ArgumentNullException(nameof(pathToGit));
            }

            if (Path.GetFileNameWithoutExtension(pathToGit) != "git")
            {
                throw new InvalidOperationException($"Invalid path '{pathToGit}'. Path must point to 'git' cmd");
            }

            _pathGit = pathToGit;
            _logger = logger;
            WorkingFolder = workingFolder ?? throw new ArgumentNullException(nameof(workingFolder));
            _gitCredentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        }

        public IFolder WorkingFolder { get; }

        public async Task AddRemote(string name, Uri endpoint)
        {
            _ = await StartGitProcess($"remote add {name} {CreateCredentialsUri(endpoint, _gitCredentials)}", true);
        }

        public async Task Checkout(string branchName)
        {
            _ = await StartGitProcess($"checkout {branchName}", false);
        }

        public async Task CheckoutRemoteToLocal(string branchName)
        {
            _ = await StartGitProcess($"checkout -b {branchName} origin/{branchName}", false);
        }

        public async Task<bool> CheckoutNewBranch(string branchName)
        {
            try
            {
                _ = await StartGitProcess($"checkout -b {branchName}", true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task Clone(Uri pullEndpoint)
        {
            await Clone(pullEndpoint, null);
        }

        public async Task Clone(Uri pullEndpoint, string branchName)
        {
            _logger.Normal($"Git clone {pullEndpoint}, branch {branchName ?? "default"}, to {WorkingFolder.FullPath}");
            string branchparam = branchName == null ? "" : $" -b {branchName}";
            _ = await StartGitProcess($"clone{branchparam} {CreateCredentialsUri(pullEndpoint, _gitCredentials)} .", true); // Clone into current folder
            _logger.Detailed("Git clone complete");
        }

        public async Task Commit(string message)
        {
            _logger.Detailed($"Git commit with message '{message}'");
            _ = await StartGitProcess($"commit -a -m \"{message}\"", true);
        }

        public async Task<string> GetCurrentHead()
        {
            string getBranchHead = await StartGitProcess($"symbolic-ref -q --short HEAD", true);
            return string.IsNullOrEmpty(getBranchHead) ?
                await StartGitProcess($"rev-parse HEAD", true) :
                getBranchHead;
        }

        public async Task Push(string remoteName, string branchName)
        {
            _logger.Detailed($"Git push to {remoteName}/{branchName}");
            _ = await StartGitProcess($"push {remoteName} {branchName}", true);
        }

        private async Task<string> StartGitProcess(string arguments, bool ensureSuccess)
        {
            ExternalProcess process = new(_logger);
            ProcessOutput output = await process.Run(WorkingFolder.FullPath, _pathGit, arguments, ensureSuccess);
            return output.Output.TrimEnd(Environment.NewLine.ToCharArray());
        }

        private Uri CreateCredentialsUri(Uri pullEndpoint, GitUsernamePasswordCredentials gitCredentials)
        {
            return _gitCredentials?.Username == null
                ? pullEndpoint
                : new UriBuilder(pullEndpoint) { UserName = Uri.EscapeDataString(gitCredentials.Username), Password = gitCredentials.Password }.Uri;
        }

        public async Task<IReadOnlyCollection<string>> GetNewCommitMessages(string baseBranchName, string headBranchName)
        {
            string commitlog = await StartGitProcess($"log --oneline --no-decorate --right-only {baseBranchName}...{headBranchName}", true);
            IEnumerable<string> commitMsgWithId = commitlog
                .Split(Environment.NewLine.ToCharArray())
                .Select(m => m.Trim())
                .Where(m => !string.IsNullOrWhiteSpace(m));
            List<string> commitMessages = commitMsgWithId
                .Select(m => string.Join(" ", m.Split(' ').Skip(1)))
                .Select(m => m.Trim())
                .Where(m => !string.IsNullOrWhiteSpace(m)).ToList();

            return commitMessages.AsReadOnly();
        }
    }
}
