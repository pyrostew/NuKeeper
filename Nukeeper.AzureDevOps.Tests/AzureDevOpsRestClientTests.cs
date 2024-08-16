using Newtonsoft.Json;

using NSubstitute;

using NuKeeper.Abstractions;
using NuKeeper.Abstractions.Logging;
using NuKeeper.AzureDevOps;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Nukeeper.AzureDevOps.Tests
{
    public class AzureDevOpsRestClientTests
    {
        [Test]
        public void InitializesCorrectly()
        {
            HttpClient httpClient = new();
            IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
            _ = httpClientFactory.CreateClient().Returns(httpClient);
            _ = new AzureDevOpsRestClient(httpClientFactory, Substitute.For<INuKeeperLogger>(), "PAT", null);

            string encodedToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{string.Empty}:PAT"));
            Assert.That(httpClient.DefaultRequestHeaders.Accept.Contains(new MediaTypeWithQualityHeaderValue("application/json")));
            Assert.That(httpClient.DefaultRequestHeaders.Authorization.Equals(new AuthenticationHeaderValue("Basic", encodedToken)));
        }

        [Test]
        public void ThrowsWithBadJson()
        {
            FakeHttpMessageHandler fakeHttpMessageHandler = new(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject("<body>Login Page</body>"), Encoding.UTF8, "application/json")
            });
            HttpClient fakeHttpClient = new(fakeHttpMessageHandler) { BaseAddress = new Uri("https://fakebaseAddress.com/") };
            IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
            _ = httpClientFactory.CreateClient().Returns(fakeHttpClient);
            AzureDevOpsRestClient restClient = new(httpClientFactory, Substitute.For<INuKeeperLogger>(), "PAT", fakeHttpClient.BaseAddress);
            NuKeeperException exception = Assert.ThrowsAsync<NuKeeperException>(async () => await restClient.GetGitRepositories("Project"));
        }

        [Test]
        public void ThrowsWithUnauthorized()
        {
            FakeHttpMessageHandler fakeHttpMessageHandler = new(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("", Encoding.UTF8, "application/json")
            });
            HttpClient fakeHttpClient = new(fakeHttpMessageHandler) { BaseAddress = new Uri("https://fakebaseAddress.com/") };
            IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
            _ = httpClientFactory.CreateClient().Returns(fakeHttpClient);
            AzureDevOpsRestClient restClient = new(httpClientFactory, Substitute.For<INuKeeperLogger>(), "PAT", fakeHttpClient.BaseAddress);
            NuKeeperException exception = Assert.ThrowsAsync<NuKeeperException>(async () => await restClient.GetGitRepositories("Project"));
            Assert.That(exception.Message.Contains("Unauthorised", StringComparison.InvariantCultureIgnoreCase));
        }

        [Test]
        public void ThrowsWithForbidden()
        {
            FakeHttpMessageHandler fakeHttpMessageHandler = new(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden,
                Content = new StringContent("", Encoding.UTF8, "application/json")
            });
            HttpClient fakeHttpClient = new(fakeHttpMessageHandler) { BaseAddress = new Uri("https://fakebaseAddress.com/") };
            IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
            _ = httpClientFactory.CreateClient().Returns(fakeHttpClient);
            AzureDevOpsRestClient restClient = new(httpClientFactory, Substitute.For<INuKeeperLogger>(), "PAT", fakeHttpClient.BaseAddress);
            NuKeeperException exception = Assert.ThrowsAsync<NuKeeperException>(async () => await restClient.GetGitRepositories("Project"));
            Assert.That(exception.Message.Contains("Forbidden", StringComparison.InvariantCultureIgnoreCase));
        }

        [Test]
        public void ThrowsWithBadStatusCode()
        {
            FakeHttpMessageHandler fakeHttpMessageHandler = new(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("", Encoding.UTF8, "application/json")
            });
            HttpClient fakeHttpClient = new(fakeHttpMessageHandler) { BaseAddress = new Uri("https://fakebaseAddress.com/") };
            IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
            _ = httpClientFactory.CreateClient().Returns(fakeHttpClient);
            AzureDevOpsRestClient restClient = new(httpClientFactory, Substitute.For<INuKeeperLogger>(), "PAT", fakeHttpClient.BaseAddress);
            NuKeeperException exception = Assert.ThrowsAsync<NuKeeperException>(async () => await restClient.GetGitRepositories("Project"));
            Assert.That(exception.Message.Contains("Error", StringComparison.InvariantCultureIgnoreCase));
        }

        [Test]
        public async Task GetsProjects()
        {
            ProjectResource projectResource = new()
            {
                Count = 3,
                value = new List<Project>
                {
                    new() {
                        id = "eb6e4656-77fc-42a1-9181-4c6d8e9da5d1",
                        name = "Fabrikam-Fiber-TFVC",
                        description = "Team Foundation Version Control projects.",
                        url = "https://dev.azure.com/fabrikam/_apis/projects/eb6e4656-77fc-42a1-9181-4c6d8e9da5d1",
                        state = "wellFormed",
                        visibility = "private",
                    },
                    new() {
                        id = "6ce954b1-ce1f-45d1-b94d-e6bf2464ba2c",
                        name = "Fabrikam-Fiber-Git",
                        description = "Git projects.",
                        url = "https://dev.azure.com/fabrikam/_apis/projects/6ce954b1-ce1f-45d1-b94d-e6bf2464ba2c",
                        state = "wellFormed",
                        visibility = "private",
                        revision = 51
                    },
                    new() {
                        id = "281f9a5b-af0d-49b4-a1df-fe6f5e5f84d0",
                        name = "TestGit",
                        url = "https://dev.azure.com/fabrikam/_apis/projects/281f9a5b-af0d-49b4-a1df-fe6f5e5f84d0",
                        state = "wellFormed",
                        visibility = "private",
                        revision = 2
                    }
                }
            };

            AzureDevOpsRestClient restClient = GetFakeClient(projectResource);
            List<Project> projects = (await restClient.GetProjects()).ToList();

            Assert.That(projects, Is.Not.Null);
            Assert.That(projects.Count == 3);
        }

        [Test]
        public async Task GetsGitRepositories()
        {
            GitRepositories gitRepositories = new()
            {
                count = 3,
                value = new List<AzureRepository>
                {
                    new() {
                        id = "5febef5a-833d-4e14-b9c0-14cb638f91e6",
                        name = "AnotherRepository",
                        url = "https://dev.azure.com/fabrikam/_apis/git/repositories/5febef5a-833d-4e14-b9c0-14cb638f91e6",
                        project = new Project
                        {
                            id = "6ce954b1-ce1f-45d1-b94d-e6bf2464ba2c",
                            name = "Fabrikam-Fiber-Git",
                            url = "https://dev.azure.com/fabrikam/_apis/projects/6ce954b1-ce1f-45d1-b94d-e6bf2464ba2c",
                            state = "wellFormed"
                        },
                        remoteUrl = "https://dev.azure.com/fabrikam/Fabrikam-Fiber-Git/_git/AnotherRepository"
                    },
                    new() {
                        id = "278d5cd2-584d-4b63-824a-2ba458937249",
                        name = "Fabrikam-Fiber-Git",
                        url = "https://dev.azure.com/fabrikam/_apis/git/repositories/278d5cd2-584d-4b63-824a-2ba458937249",
                        project = new Project
                        {
                            id = "6ce954b1-ce1f-45d1-b94d-e6bf2464ba2c",
                            name = "Fabrikam-Fiber-Git",
                            url = "https://dev.azure.com/fabrikam/_apis/projects/6ce954b1-ce1f-45d1-b94d-e6bf2464ba2c",
                            state = "wellFormed"
                        },
                        remoteUrl = "https://dev.azure.com/fabrikam/_git/Fabrikam-Fiber-Git",
                        defaultBranch = "refs/heads/master"
                    },
                    new() {
                        id = "66efb083-777a-4cac-a350-a24b046be6be",
                        name = "AnotherRepository",
                        url = "https://dev.azure.com/fabrikam/_apis/git/repositories/66efb083-777a-4cac-a350-a24b046be6be",
                        project = new Project
                        {
                            id = "281f9a5b-af0d-49b4-a1df-fe6f5e5f84d0",
                            name = "TestGit",
                            url = "https://dev.azure.com/fabrikam/_apis/projects/281f9a5b-af0d-49b4-a1df-fe6f5e5f84d0",
                            state = "wellFormed"
                        },
                        remoteUrl = "https://dev.azure.com/fabrikam/_git/TestGit",
                        defaultBranch = "refs/heads/master"
                    }
                }
            };

            AzureDevOpsRestClient restClient = GetFakeClient(gitRepositories);
            List<AzureRepository> azureRepositories = (await restClient.GetGitRepositories("ProjectName")).ToList();

            Assert.That(azureRepositories, Is.Not.Null);
            Assert.That(azureRepositories.Count == 3);
        }

        [Test]
        public async Task GetsGitRefs()
        {
            GitRefsResource gitRefsResource = new()
            {
                count = 3,
                value =
                [
                    new()
                    {
                        name = "refs/heads/develop",
                        objectId = "67cae2b029dff7eb3dc062b49403aaedca5bad8d",
                        url = "https://dev.azure.com/fabrikam/_apis/git/repositories/278d5cd2-584d-4b63-824a-2ba458937249/refs/heads/develop"
                    },
                    new()
                    {
                        name = "refs/heads/master",
                        objectId = "23d0bc5b128a10056dc68afece360d8a0fabb014",
                        url = "https://dev.azure.com/fabrikam/_apis/git/repositories/278d5cd2-584d-4b63-824a-2ba458937249/refs/heads/master"
                    },
                    new()
                    {
                        name = "refs/tags/v1.0",
                        objectId = "23d0bc5b128a10056dc68afece360d8a0fabb014",
                        url = "https://dev.azure.com/fabrikam/_apis/git/repositories/278d5cd2-584d-4b63-824a-2ba458937249/refs/tags/v1.0"
                    }
                ]
            };

            AzureDevOpsRestClient restClient = GetFakeClient(gitRefsResource);
            List<GitRefs> gitRefs = (await restClient.GetRepositoryRefs("ProjectName", "RepoId")).ToList();

            Assert.That(gitRefs, Is.Not.Null);
            Assert.That(gitRefs.Count == 3);
        }

        [Test]
        public async Task GetPullRequests()
        {
            PullRequestResource pullRequestResource = new()
            {
                Count = 1,
                value = new[]
                {
                    new PullRequest
                    {
                        AzureRepository = new AzureRepository
                        {
                            id = "3411ebc1-d5aa-464f-9615-0b527bc66719",
                            name = "2016_10_31",
                            url = "https://dev.azure.com/fabrikam/_apis/git/repositories/3411ebc1-d5aa-464f-9615-0b527bc66719",
                            project = new Project
                            {
                                id = "a7573007-bbb3-4341-b726-0c4148a07853",
                                name = "2016_10_31",
                                description = "test project created on Halloween 2016",
                                url = "https://dev.azure.com/fabrikam/_apis/projects/a7573007-bbb3-4341-b726-0c4148a07853",
                                state = "wellFormed",
                                revision = 7
                            },
                            remoteUrl = "https://dev.azure.com/fabrikam/_git/2016_10_31"
                        },
                        PullRequestId = 22,
                        CodeReviewId = 22,
                        Status = "active",
                        CreationDate = new DateTime(2016, 11, 01, 16, 30, 31),
                        Title = "A new feature",
                        Description = "Adding a new feature",
                        SourceRefName = "refs/heads/npaulk/my_work",
                        TargetRefName = "refs/heads/new_feature",
                        MergeStatus = "queued",
                        MergeId = "f5fc8381-3fb2-49fe-8a0d-27dcc2d6ef82",
                        Url = "https: //dev.azure.com/fabrikam/_apis/git/repositories/3411ebc1-d5aa-464f-9615-0b527bc66719/commits/b60280bc6e62e2f880f1b63c1e24987664d3bda3",
                        SupportsIterations = true,
                    }
                }
            };

            AzureDevOpsRestClient restClient = GetFakeClient(pullRequestResource);
            IEnumerable<PullRequest> foundPullRequests = await restClient.GetPullRequests("ProjectName", "RepoId", "head", "base");
            Assert.That(foundPullRequests, Is.Not.Null);
            Assert.That(1 == foundPullRequests.Count());
        }

        [Test]
        public async Task CreatesPullRequest()
        {
            PullRequest pullRequest = new()
            {
                AzureRepository = new AzureRepository
                {
                    id = "3411ebc1-d5aa-464f-9615-0b527bc66719",
                    name = "2016_10_31",
                    url = "https://dev.azure.com/fabrikam/_apis/git/repositories/3411ebc1-d5aa-464f-9615-0b527bc66719",
                    project = new Project
                    {
                        id = "a7573007-bbb3-4341-b726-0c4148a07853",
                        name = "2016_10_31",
                        description = "test project created on Halloween 2016",
                        url = "https://dev.azure.com/fabrikam/_apis/projects/a7573007-bbb3-4341-b726-0c4148a07853",
                        state = "wellFormed",
                        revision = 7
                    },
                    remoteUrl = "https://dev.azure.com/fabrikam/_git/2016_10_31"
                },
                PullRequestId = 22,
                CodeReviewId = 22,
                Status = "active",
                CreationDate = new DateTime(2016, 11, 01, 16, 30, 31),
                Title = "A new feature",
                Description = "Adding a new feature",
                SourceRefName = "refs/heads/npaulk/my_work",
                TargetRefName = "refs/heads/new_feature",
                MergeStatus = "queued",
                MergeId = "f5fc8381-3fb2-49fe-8a0d-27dcc2d6ef82",
                Url = "https: //dev.azure.com/fabrikam/_apis/git/repositories/3411ebc1-d5aa-464f-9615-0b527bc66719/commits/b60280bc6e62e2f880f1b63c1e24987664d3bda3",
                SupportsIterations = true,
            };

            AzureDevOpsRestClient restClient = GetFakeClient(pullRequest);
            PRRequest request = new() { title = "A Pr" };
            PullRequest createdPullRequest = await restClient.CreatePullRequest(request, "ProjectName", "RepoId");
            Assert.That(createdPullRequest, Is.Not.Null);
        }

        [Test]
        public async Task CreatesPullRequestLabel()
        {
            LabelResource labelResource = new()
            {
                value = new List<Label>
                {
                    new() {
                        active = true,
                        id = "id",
                        name = "nukeeper"
                    }
                }
            };

            AzureDevOpsRestClient restClient = GetFakeClient(labelResource);
            LabelRequest request = new() { name = "nukeeper" };
            LabelResource pullRequestLabel = await restClient.CreatePullRequestLabel(request, "ProjectName", "RepoId", 100);
            Assert.That(pullRequestLabel, Is.Not.Null);
        }

        [Test]
        public async Task SetAutoComplete()
        {
            PullRequest pullRequest = new()
            {
                AzureRepository = new AzureRepository
                {
                    id = "3411ebc1-d5aa-464f-9615-0b527bc66719",
                    name = "2016_10_31",
                    url = "https://dev.azure.com/fabrikam/_apis/git/repositories/3411ebc1-d5aa-464f-9615-0b527bc66719",
                    project = new Project
                    {
                        id = "a7573007-bbb3-4341-b726-0c4148a07853",
                        name = "2016_10_31",
                        description = "test project created on Halloween 2016",
                        url = "https://dev.azure.com/fabrikam/_apis/projects/a7573007-bbb3-4341-b726-0c4148a07853",
                        state = "wellFormed",
                        revision = 7
                    },
                    remoteUrl = "https://dev.azure.com/fabrikam/_git/2016_10_31"
                },
                PullRequestId = 22,
                CodeReviewId = 22,
                Status = "active",
                CreationDate = new DateTime(2016, 11, 01, 16, 30, 31),
                Title = "A new feature",
                Description = "Adding a new feature",
                SourceRefName = "refs/heads/npaulk/my_work",
                TargetRefName = "refs/heads/new_feature",
                MergeStatus = "queued",
                MergeId = "f5fc8381-3fb2-49fe-8a0d-27dcc2d6ef82",
                Url = "https: //dev.azure.com/fabrikam/_apis/git/repositories/3411ebc1-d5aa-464f-9615-0b527bc66719/commits/b60280bc6e62e2f880f1b63c1e24987664d3bda3",
                SupportsIterations = true,
            };

            AzureDevOpsRestClient restClient = GetFakeClient(pullRequest);
            PRRequest prRequest = new()
            {
                autoCompleteSetBy = new Creator()
                {
                    id = "3"
                }
            };

            PullRequest pullRequestResponse = await restClient.SetAutoComplete(prRequest, "ProjectName", "RepoId", 100);
            Assert.That(pullRequestResponse, Is.Not.Null);
        }

        [Test]
        public async Task RetrievesFileNames()
        {
            GitItemResource gitItemResource = new()
            {
                value = new List<GitItem>
                {
                    new() { path = "/src/file.cs"},
                    new() { path = "/src/project.csproj"},
                    new() { path = "/README.md"},

                },
                count = 3
            };

            AzureDevOpsRestClient restClient = GetFakeClient(gitItemResource);
            _ = new LabelRequest() { name = "nukeeper" };
            IEnumerable<string> fileNames = await restClient.GetGitRepositoryFileNames("ProjectName", "RepoId");
            Assert.That(fileNames, Is.Not.Null);
            Assert.That(fileNames, Is.EquivalentTo(new[] { "/src/file.cs", "/src/project.csproj", "/README.md" }));
        }

        [TestCase("proj/_apis/git/repositories/Id/pullrequests", false, "proj/_apis/git/repositories/Id/pullrequests?api-version=4.1")]
        [TestCase("proj/_apis/git/repositories/Id/pullrequests", true, "proj/_apis/git/repositories/Id/pullrequests?api-version=4.1-preview.1")]
        [TestCase("proj/_apis/git/repositories/Id/pullrequests?searchCriteria.sourceRefName=head", false, "proj/_apis/git/repositories/Id/pullrequests?searchCriteria.sourceRefName=head&api-version=4.1")]
        [TestCase("proj/_apis/git/repositories/Id/pullrequests?searchCriteria.sourceRefName=head", true, "proj/_apis/git/repositories/Id/pullrequests?searchCriteria.sourceRefName=head&api-version=4.1-preview.1")]
        public void BuildAzureDevOpsUri(string relativePath, bool previewApi, Uri expectedUri)
        {
            Uri uri = AzureDevOpsRestClient.BuildAzureDevOpsUri(relativePath, previewApi);

            Assert.That(expectedUri == uri);
        }

        private static AzureDevOpsRestClient GetFakeClient(object returnObject)
        {
            _ = JsonConvert.SerializeObject(returnObject);
            FakeHttpMessageHandler fakeHttpMessageHandler = new(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(returnObject), Encoding.UTF8, "application/json")
            });

            HttpClient fakeHttpClient = new(fakeHttpMessageHandler) { BaseAddress = new Uri("https://fakebaseAddress.com/") };
            IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
            _ = httpClientFactory.CreateClient().Returns(fakeHttpClient);
            return new AzureDevOpsRestClient(httpClientFactory, Substitute.For<INuKeeperLogger>(), "PAT", fakeHttpClient.BaseAddress);
        }
    }
}
