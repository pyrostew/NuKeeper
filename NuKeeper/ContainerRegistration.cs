using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Git;
using NuKeeper.AzureDevOps;
using NuKeeper.BitBucket;
using NuKeeper.BitBucketLocal;
using NuKeeper.Collaboration;
using NuKeeper.Commands;
using NuKeeper.Engine;
using NuKeeper.Engine.Packages;
using NuKeeper.Git;
using NuKeeper.Gitea;
using NuKeeper.GitHub;
using NuKeeper.Gitlab;
using NuKeeper.Local;
using NuKeeper.Update.Process;
using NuKeeper.Update.Selection;

using SimpleInjector;

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;

namespace NuKeeper
{
    public static class ContainerRegistration
    {
        public static Container Init()
        {
            Container container = new();

            RegisterHttpClient(container);

            Register(container);
            RegisterCommands(container);
            ContainerInspectionRegistration.Register(container);
            ContainerUpdateRegistration.Register(container);

            container.Verify();

            return container;
        }

        private static void RegisterHttpClient(Container container)
        {
            ServiceCollection services = new();
            _ = services.AddHttpClient(Options.DefaultName)
                .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
                {
                    HttpClientHandler httpMessageHandler = new();
                    if (httpMessageHandler.SupportsAutomaticDecompression)
                    {
                        // TODO: change to All when moving to .NET 5.0
                        httpMessageHandler.AutomaticDecompression =
                            DecompressionMethods.GZip | DecompressionMethods.Deflate;
                    }

                    return httpMessageHandler;
                });
            _ = services
                .AddSimpleInjector(container)
                .BuildServiceProvider(validateScopes: true)
                .UseSimpleInjector(container);
        }

        private static void RegisterCommands(Container container)
        {
            container.Register<GlobalCommand>();
            container.Register<InspectCommand>();
            container.Register<OrganisationCommand>();
            container.Register<RepositoryCommand>();
            container.Register<UpdateCommand>();
        }

        private static void Register(Container container)
        {
            container.Register<ILocalEngine, LocalEngine>();
            container.Register<ICollaborationEngine, CollaborationEngine>();
            container.Register<IGitRepositoryEngine, GitRepositoryEngine>();
            container.Register<IRepositoryUpdater, RepositoryUpdater>();
            container.Register<IPackageUpdateSelection, PackageUpdateSelection>();
            container.Register<IExistingCommitFilter, ExistingCommitFilter>();
            container.Register<IPackageUpdater, PackageUpdater>();
            container.Register<IRepositoryFilter, RepositoryFilter>();
            container.Register<ISolutionRestore, SolutionRestore>();

            container.Register<ILocalUpdater, LocalUpdater>();
            container.Register<IUpdateSelection, UpdateSelection>();
            container.Register<IFileSettingsCache, FileSettingsCache>();
            container.Register<IFileSettingsReader, FileSettingsReader>();

            container.RegisterSingleton<IEnvironmentVariablesProvider, EnvironmentVariablesProvider>();

            container.RegisterSingleton<IGitDiscoveryDriver, LibGit2SharpDiscoveryDriver>();

            container.RegisterSingleton<ICollaborationFactory, CollaborationFactory>();

            Registration[] settingsRegistration = RegisterMultipleSingletons<ISettingsReader>(container, new[]
            {
                typeof(GitHubSettingsReader).Assembly,
                typeof(AzureDevOpsSettingsReader).Assembly,
                typeof(VstsSettingsReader).Assembly,
                typeof(BitbucketSettingsReader).Assembly,
                typeof(BitBucketLocalSettingsReader).Assembly,
                typeof(GitlabSettingsReader).Assembly,
                typeof(GiteaSettingsReader).Assembly
            });

            container.Collection.Register<ISettingsReader>(settingsRegistration);
        }

        private static Registration[] RegisterMultipleSingletons<T>(Container container, Assembly[] assemblies)
        {
            System.Collections.Generic.IEnumerable<System.Type> types = container.GetTypesToRegister(typeof(T), assemblies);

            return (from type in types
                    select Lifestyle.Singleton.CreateRegistration(type, container)
            ).ToArray();
        }
    }
}
