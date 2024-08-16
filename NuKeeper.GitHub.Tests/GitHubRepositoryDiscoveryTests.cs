using NSubstitute;

using NuKeeper.Abstractions.CollaborationModels;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Logging;

using NUnit.Framework;

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NuKeeper.GitHub.Tests
{
    [TestFixture]
    public class GitHubRepositoryDiscoveryTests
    {
        [Test]
        public async Task SuccessInRepoMode()
        {
            SourceControlServerSettings settings = new()
            {
                Repository = new RepositorySettings(),
                Scope = ServerScope.Repository
            };

            IRepositoryDiscovery githubRepositoryDiscovery = MakeGithubRepositoryDiscovery();

            IEnumerable<RepositorySettings> reposResponse = await githubRepositoryDiscovery.GetRepositories(settings);

            List<RepositorySettings> repos = reposResponse.ToList();

            Assert.That(repos, Is.Not.Null);
            Assert.That(repos.Count, Is.EqualTo(1));
            Assert.That(repos[0], Is.EqualTo(settings.Repository));
        }

        [Test]
        public async Task RepoModeIgnoresIncludesAndExcludes()
        {
            SourceControlServerSettings settings = new()
            {
                Repository = new RepositorySettings(RepositoryBuilder.MakeRepository(name: "foo")),
                Scope = ServerScope.Repository,
                IncludeRepos = new Regex("^foo"),
                ExcludeRepos = new Regex("^foo")
            };

            IRepositoryDiscovery githubRepositoryDiscovery = MakeGithubRepositoryDiscovery();

            IEnumerable<RepositorySettings> reposResponse = await githubRepositoryDiscovery.GetRepositories(settings);

            List<RepositorySettings> repos = reposResponse.ToList();

            Assert.That(repos, Is.Not.Null);
            Assert.That(repos.Count, Is.EqualTo(1));

            RepositorySettings firstRepo = repos.First();
            Assert.That(firstRepo.RepositoryName, Is.EqualTo("foo"));
        }

        [Test]
        public async Task SuccessInOrgMode()
        {
            IRepositoryDiscovery githubRepositoryDiscovery = MakeGithubRepositoryDiscovery();

            IEnumerable<RepositorySettings> repos = await githubRepositoryDiscovery.GetRepositories(OrgModeSettings());

            Assert.That(repos, Is.Not.Null);
            Assert.That(repos, Is.Empty);
        }

        [Test]
        public async Task OrgModeValidReposAreIncluded()
        {
            List<Repository> inputRepos =
            [
                RepositoryBuilder.MakeRepository()
            ];

            IRepositoryDiscovery githubRepositoryDiscovery = MakeGithubRepositoryDiscovery(inputRepos.AsReadOnly());

            IEnumerable<RepositorySettings> repos = await githubRepositoryDiscovery.GetRepositories(OrgModeSettings());

            Assert.That(repos, Is.Not.Null);
            Assert.That(repos, Is.Not.Empty);
            Assert.That(repos.Count(), Is.EqualTo(1));

            RepositorySettings firstRepo = repos.First();
            Assert.That(firstRepo.RepositoryName, Is.EqualTo(inputRepos[0].Name));
            Assert.That(firstRepo.RepositoryUri.ToString(), Is.EqualTo(inputRepos[0].CloneUrl));
        }

        [Test]
        public async Task OrgModeInvalidReposAreExcluded()
        {
            List<Repository> inputRepos =
            [
                RepositoryBuilder.MakeRepository("http://a.com/repo1.git", false),
                RepositoryBuilder.MakeRepository("http://b.com/repob.git", true)
            ];

            IRepositoryDiscovery githubRepositoryDiscovery = MakeGithubRepositoryDiscovery(inputRepos.AsReadOnly());

            IEnumerable<RepositorySettings> repos = await githubRepositoryDiscovery.GetRepositories(OrgModeSettings());

            Assert.That(repos, Is.Not.Null);
            Assert.That(repos, Is.Not.Empty);
            Assert.That(repos.Count(), Is.EqualTo(1));

            RepositorySettings firstRepo = repos.First();
            Assert.That(firstRepo.RepositoryName, Is.EqualTo(inputRepos[1].Name));
            Assert.That(firstRepo.RepositoryUri.ToString(), Is.EqualTo(inputRepos[1].CloneUrl));
        }

        [Test]
        public async Task OrgModeWhenThereAreIncludes_OnlyConsiderMatches()
        {
            List<Repository> inputRepos =
            [
                RepositoryBuilder.MakeRepository(name: "foo"),
                RepositoryBuilder.MakeRepository(name: "bar")
            ];

            IRepositoryDiscovery githubRepositoryDiscovery = MakeGithubRepositoryDiscovery(inputRepos.AsReadOnly());

            SourceControlServerSettings settings = OrgModeSettings();
            settings.IncludeRepos = new Regex("^bar");
            IEnumerable<RepositorySettings> repos = await githubRepositoryDiscovery.GetRepositories(settings);

            Assert.That(repos, Is.Not.Null);
            Assert.That(repos, Is.Not.Empty);
            Assert.That(repos.Count(), Is.EqualTo(1));

            RepositorySettings firstRepo = repos.First();
            Assert.That(firstRepo.RepositoryName, Is.EqualTo("bar"));
        }

        [Test]
        public async Task OrgModeWhenThereAreExcludes_OnlyConsiderNonMatching()
        {
            List<Repository> inputRepos =
            [
                RepositoryBuilder.MakeRepository(name: "foo"),
                RepositoryBuilder.MakeRepository(name: "bar")
            ];

            IRepositoryDiscovery githubRepositoryDiscovery = MakeGithubRepositoryDiscovery(inputRepos.AsReadOnly());

            SourceControlServerSettings settings = OrgModeSettings();
            settings.ExcludeRepos = new Regex("^bar");
            IEnumerable<RepositorySettings> repos = await githubRepositoryDiscovery.GetRepositories(settings);

            Assert.That(repos, Is.Not.Null);
            Assert.That(repos, Is.Not.Empty);
            Assert.That(repos.Count(), Is.EqualTo(1));

            RepositorySettings firstRepo = repos.First();
            Assert.That(firstRepo.RepositoryName, Is.EqualTo("foo"));
        }

        [Test]
        public async Task OrgModeWhenThereAreIncludesAndExcludes_OnlyConsiderMatchesButRemoveNonMatching()
        {
            List<Repository> inputRepos =
            [
                RepositoryBuilder.MakeRepository(name: "foo"),
                RepositoryBuilder.MakeRepository(name: "bar")
            ];

            IRepositoryDiscovery githubRepositoryDiscovery = MakeGithubRepositoryDiscovery(inputRepos.AsReadOnly());

            SourceControlServerSettings settings = OrgModeSettings();
            settings.IncludeRepos = new Regex("^bar");
            settings.ExcludeRepos = new Regex("^bar");
            IEnumerable<RepositorySettings> repos = await githubRepositoryDiscovery.GetRepositories(settings);

            Assert.That(repos, Is.Not.Null);
            Assert.That(repos.Count(), Is.EqualTo(0));
        }

        private static IRepositoryDiscovery MakeGithubRepositoryDiscovery(IReadOnlyList<Repository> repositories = null)
        {
            ICollaborationPlatform collaborationPlatform = Substitute.For<ICollaborationPlatform>();
            _ = collaborationPlatform.GetRepositoriesForOrganisation(Arg.Any<string>())
                .Returns(Task.FromResult(repositories ?? new List<Repository>()));
            return new GitHubRepositoryDiscovery(Substitute.For<INuKeeperLogger>(), collaborationPlatform);
        }

        private static SourceControlServerSettings OrgModeSettings()
        {
            return new SourceControlServerSettings
            {
                OrganisationName = "testOrg",
                Scope = ServerScope.Organisation
            };
        }
    }
}
