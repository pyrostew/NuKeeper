using NuKeeper.Abstractions.Logging;
using NuKeeper.Inspection.Files;
using NuKeeper.Inspection.Logging;

using NUnit.Framework;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NuKeeper.Git.Tests
{
    public class GitCmdDiscoveryDriverTest
    {
        private INuKeeperLogger _logger;
        private string _pathTogit;
        private DirectoryInfo _repo;

        private const string _origin = "https://github.com/NuKeeperDotNet/NuKeeper.git";

        [OneTimeSetUp]
        public void Setup()
        {
            _logger = new ConfigurableLogger();
            _pathTogit = TestDirectoryHelper.DiscoverPathToGit();
            if (_pathTogit == null)
            {
                Assert.Ignore("no git implementation found!");
            }

            // create a local repo to test against
            DirectoryInfo folder = TestDirectoryHelper.UniqueTemporaryFolder();
            GitCmdDriver gitDriver = new(_pathTogit, _logger, new Folder(_logger, folder), new Abstractions.Git.GitUsernamePasswordCredentials());
            Assert.DoesNotThrowAsync(() => gitDriver.Clone(new Uri(_origin)));
            _repo = folder;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TestDirectoryHelper.DeleteDirectory(_repo);
        }

        [Test]
        public async Task ShouldDiscoverLocalRepo()
        {
            GitCmdDiscoveryDriver gitDiscoveryDriver = new(_pathTogit, _logger);
            Uri repo = await gitDiscoveryDriver.DiscoverRepo(new Uri(_repo.FullName));
            Assert.That(_origin == repo.AbsoluteUri);
        }

        [Test]
        public async Task ShouldGetRemotes()
        {
            GitCmdDiscoveryDriver gitDiscoveryDriver = new(_pathTogit, _logger);
            LibGit2SharpDiscoveryDriver classicGitDiscoveryDriver = new(_logger);
            System.Collections.Generic.IEnumerable<Abstractions.Git.GitRemote> remotes = await gitDiscoveryDriver.GetRemotes(new Uri(_repo.FullName));
            System.Collections.Generic.IEnumerable<Abstractions.Git.GitRemote> classicRemotes = await classicGitDiscoveryDriver.GetRemotes(new Uri(_repo.FullName));

            Abstractions.Git.GitRemote[] remotesArray = remotes?.ToArray();
            Abstractions.Git.GitRemote[] classicRemotesArray = classicRemotes?.ToArray();

            Assert.That(remotesArray, Is.Not.Null, "GitCmdDiscoveryDriver returned null for GetRemotes");
            Assert.That(classicRemotesArray, Is.Not.Null, "LibGit2SharpDiscoveryDriver returned null for GetRemotes");

            Assert.That(remotesArray?.Length == classicRemotesArray?.Length, "Lib2Sharp and GitCmd should have the same number of results");

            for (int count = 0; count < classicRemotesArray.Length; count++)
            {
                Abstractions.Git.GitRemote classicRemote = classicRemotesArray[count];
                Abstractions.Git.GitRemote remote = remotesArray.Where(r => r.Name.Equals(classicRemote.Name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                Assert.That(classicRemote, Is.Not.Null, $"GitCmd does not find remote {remote.Name}");
                Assert.That(classicRemote.Url == remote.Url, $"GitCmd does return the same url: {remote.Url}");
            }
        }
    }
}
