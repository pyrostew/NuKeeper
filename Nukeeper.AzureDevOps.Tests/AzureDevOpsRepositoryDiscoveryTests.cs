using NSubstitute;

using NuKeeper.Abstractions.CollaborationModels;
using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Formats;
using NuKeeper.Abstractions.Logging;
using NuKeeper.AzureDevOps;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nukeeper.AzureDevOps.Tests
{
    public class AzureDevOpsRepositoryDiscoveryTests
    {
        [Test]
        public async Task SuccessInRepoMode()
        {
            SourceControlServerSettings settings = new()
            {
                Repository = new RepositorySettings { RepositoryUri = new Uri("https://repo/") },
                Scope = ServerScope.Repository
            };

            IRepositoryDiscovery githubRepositoryDiscovery = MakeAzureDevOpsRepositoryDiscovery();

            IEnumerable<RepositorySettings> reposResponse = await githubRepositoryDiscovery.GetRepositories(settings);

            List<RepositorySettings> repos = reposResponse.ToList();

            Assert.That(repos, Is.Not.Null);
            Assert.That(repos.Count, Is.EqualTo(1));
            Assert.That(repos[0], Is.EqualTo(settings.Repository));
        }

        [Test]
        public async Task SuccessInRepoModeReplacesToken()
        {
            SourceControlServerSettings settings = new()
            {
                Repository = new RepositorySettings { RepositoryUri = new Uri("https://user:--PasswordToReplace--@repo/") },
                Scope = ServerScope.Repository
            };

            IRepositoryDiscovery githubRepositoryDiscovery = MakeAzureDevOpsRepositoryDiscovery();

            IEnumerable<RepositorySettings> reposResponse = await githubRepositoryDiscovery.GetRepositories(settings);

            List<RepositorySettings> repos = reposResponse.ToList();

            Assert.That(repos, Is.Not.Null);
            Assert.That(repos.Count, Is.EqualTo(1));
            Assert.That(repos[0], Is.EqualTo(settings.Repository));
            Assert.That(repos[0].RepositoryUri.ToString(), Is.EqualTo("https://user:token@repo/"));
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

            IRepositoryDiscovery githubRepositoryDiscovery = MakeAzureDevOpsRepositoryDiscovery();

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
            IRepositoryDiscovery githubRepositoryDiscovery = MakeAzureDevOpsRepositoryDiscovery();

            IEnumerable<RepositorySettings> repos = await githubRepositoryDiscovery.GetRepositories(OrgModeSettings());

            Assert.That(repos, Is.Not.Null);
            Assert.That(repos, Is.Empty);
        }

        [Test]
        public async Task OrgModeInvalidReposAreExcluded()
        {
            List<Repository> inputRepos =
            [
                RepositoryBuilder.MakeRepository("http://a.com/repo1.git".ToUri(), false),
                RepositoryBuilder.MakeRepository("http://b.com/repob.git".ToUri(), true)
            ];

            IRepositoryDiscovery githubRepositoryDiscovery = MakeAzureDevOpsRepositoryDiscovery(inputRepos.AsReadOnly());

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

            IRepositoryDiscovery githubRepositoryDiscovery = MakeAzureDevOpsRepositoryDiscovery(inputRepos.AsReadOnly());

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

            IRepositoryDiscovery githubRepositoryDiscovery = MakeAzureDevOpsRepositoryDiscovery(inputRepos.AsReadOnly());

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

            IRepositoryDiscovery githubRepositoryDiscovery = MakeAzureDevOpsRepositoryDiscovery(inputRepos.AsReadOnly());

            SourceControlServerSettings settings = OrgModeSettings();
            settings.IncludeRepos = new Regex("^bar");
            settings.ExcludeRepos = new Regex("^bar");
            IEnumerable<RepositorySettings> repos = await githubRepositoryDiscovery.GetRepositories(settings);

            Assert.That(repos, Is.Not.Null);
            Assert.That(repos.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task SuccessInGlobalMode()
        {
            IRepositoryDiscovery githubRepositoryDiscovery = MakeAzureDevOpsRepositoryDiscovery();

            IEnumerable<RepositorySettings> repos = await githubRepositoryDiscovery.GetRepositories(new SourceControlServerSettings { Scope = ServerScope.Global });

            Assert.That(repos, Is.Not.Null);
            Assert.That(repos, Is.Empty);
        }

        private static IRepositoryDiscovery MakeAzureDevOpsRepositoryDiscovery(IReadOnlyList<Repository> repositories = null)
        {
            ICollaborationPlatform collaborationPlatform = Substitute.For<ICollaborationPlatform>();
            _ = collaborationPlatform.GetRepositoriesForOrganisation(Arg.Any<string>())
                .Returns(repositories ?? new List<Repository>());
            return new AzureDevOpsRepositoryDiscovery(Substitute.For<INuKeeperLogger>(), collaborationPlatform, "token");
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
