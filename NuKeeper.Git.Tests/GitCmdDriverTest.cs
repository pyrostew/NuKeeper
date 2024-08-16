using NuKeeper.Abstractions.Logging;
using NuKeeper.Inspection.Files;
using NuKeeper.Inspection.Logging;

using NUnit.Framework;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NuKeeper.Git.Tests
{
    public class GitCmdDriverTest
    {
        private INuKeeperLogger _logger;
        private string _pathToGit;

        [OneTimeSetUp]
        public void Setup()
        {
            _logger = new ConfigurableLogger();
            _pathToGit = TestDirectoryHelper.DiscoverPathToGit();
        }

        [TestCase("https://github.com/NuKeeperDotNet/NuKeeper.git")]
        [TestCase("https://github.com/NuKeeperDotNet/NuKeeperWebsite.git")]
        public async Task CloneRepoAndCheckout(string path)
        {
            if (_pathToGit == null)
            {
                Assert.Ignore("no git implementation found!");
            }

            DirectoryInfo folder = TestDirectoryHelper.UniqueTemporaryFolder();
            try
            {
                GitCmdDriver gitDriver = new(_pathToGit, _logger, new Folder(_logger, folder), new Abstractions.Git.GitUsernamePasswordCredentials());
                Assert.DoesNotThrowAsync(() => gitDriver.Clone(new Uri(path)));
                Assert.That(Directory.Exists(Path.Combine(folder.FullName, ".git")), "Local git repo should exist in {0}", folder.FullName);

                // Checkout master branch
                Assert.DoesNotThrowAsync(() => gitDriver.Checkout("master"));
                string head = await gitDriver.GetCurrentHead();
                Assert.That(head == "master");

                // Checkout new branch
                Assert.DoesNotThrowAsync(() => gitDriver.CheckoutNewBranch("newBranch"));
                head = await gitDriver.GetCurrentHead();
                Assert.That("newBranch" == head);
            }
            finally
            {
                TestDirectoryHelper.DeleteDirectory(folder);
            }
        }

        [Test]
        public async Task GetNewCommitMessages()
        {
            // in this test we assume the Nukeeper repo has at least 2 branches and one of them is master.
            // if not, the test will return OK (because we cannot run it)
            string repoUri = "https://github.com/NuKeeperDotNet/NuKeeper.git";

            DirectoryInfo folder = TestDirectoryHelper.UniqueTemporaryFolder();
            try
            {
                Abstractions.Git.GitUsernamePasswordCredentials creds = new();
                GitCmdDriver cmdGitDriver = new(_pathToGit, _logger, new Folder(_logger, folder), creds);
                LibGit2SharpDriver origGitDriver = new(_logger, new Folder(_logger, folder), creds, null);

                // get the repo
                await origGitDriver.Clone(new Uri(repoUri));
                // get the remote branches, use git directly to avoid having to dress up a platform
                string gitOutput = await StartGitProcess("branch -r", folder.FullName);
                string[] branchNames = gitOutput.Split('\n')
                    .Select(b => b.Trim()).ToArray();

                string master = branchNames.Where(b => b.EndsWith("/master", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (master != null && branchNames.Length > 1)
                {
                    string headBranch = branchNames.First(b => !b.Equals(master, StringComparison.InvariantCultureIgnoreCase));
                    string localHeadBranch = Regex.Replace(headBranch, "^origin/", "");

                    // We have chosen the head branche here, lets check it out.
                    await cmdGitDriver.CheckoutRemoteToLocal(localHeadBranch);

                    // finally start the test
                    System.Collections.Generic.IReadOnlyCollection<string> origMessages = await origGitDriver.GetNewCommitMessages("master", localHeadBranch);
                    System.Collections.Generic.IReadOnlyCollection<string> cmdMessages = await cmdGitDriver.GetNewCommitMessages("master", localHeadBranch);

                    string[] origMessagesArray = origMessages.ToArray();
                    string[] cmdMessagesArray = cmdMessages.ToArray();

                    Assert.That(origMessagesArray, Is.EquivalentTo(cmdMessagesArray), "GitCmdDriver does not return the right amount of messages");

                    foreach (string message in origMessages)
                    {
                        Assert.That(cmdMessages.Contains(message), $"GitCmdDriver does not return commit message {message}");
                    }
                }
            }
            finally
            {
                TestDirectoryHelper.DeleteDirectory(folder);
            }
        }

        // stripped down version
        private async Task<string> StartGitProcess(string arguments, string workingFolder)
        {
            ProcessStartInfo processInfo = new(_pathToGit, arguments)
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = workingFolder
            };

            Process process = Process.Start(processInfo);
            string textOut = await process.StandardOutput.ReadToEndAsync();
            string textErr = await process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            Assert.That(0 == process.ExitCode, $"Git exited with code {process.ExitCode}: {textErr}");

            return textOut.TrimEnd(Environment.NewLine.ToCharArray());
        }
    }
}
