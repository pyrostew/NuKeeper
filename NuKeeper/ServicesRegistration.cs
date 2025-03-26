using System;
using System.Net;
using System.Net.Http;
using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NuGet.Common;

using NuKeeper.Abstractions.CollaborationPlatform;
using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Git;
using NuKeeper.Abstractions.Logging;
using NuKeeper.AzureDevOps;
using NuKeeper.BitBucket;
using NuKeeper.BitBucketLocal;
using NuKeeper.Commands;
using NuKeeper.Engine;
using NuKeeper.Engine.Packages;
using NuKeeper.Git;
using NuKeeper.Gitea;
using NuKeeper.GitHub;
using NuKeeper.Gitlab;
using NuKeeper.Inspection;
using NuKeeper.Inspection.Files;
using NuKeeper.Inspection.Logging;
using NuKeeper.Inspection.NuGetApi;
using NuKeeper.Inspection.Report;
using NuKeeper.Inspection.RepositoryInspection;
using NuKeeper.Inspection.Sort;
using NuKeeper.Inspection.Sources;
using NuKeeper.Local;
using NuKeeper.Update;
using NuKeeper.Update.Process;
using NuKeeper.Update.ProcessRunner;
using NuKeeper.Update.Selection;

namespace NuKeeper
{
   public static class ServicesRegistration
   {
      public static ServiceProvider BuildProvider(Action<ServiceCollection> AddServices)
      {
         ServiceCollection services = new();
         AddServices(services);
         return services.BuildServiceProvider();
      }

      public static void RegisterServices(IServiceCollection services)
      {
         RegisterHttpClient(services);
         Register(services);
         RegisterCommands(services);
      }

      private static void RegisterHttpClient(IServiceCollection services)
      {
         services.AddHttpClient(Options.DefaultName)
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
      }

      private static void RegisterCommands(IServiceCollection services)
      {
         services.AddTransient<InspectCommand>();
         services.AddTransient<UpdateCommand>();
      }

      private static void Register(IServiceCollection services)
      {
         services.AddTransient<ILocalEngine, LocalEngine>();
         services.AddTransient<IGitRepositoryEngine, GitRepositoryEngine>();
         services.AddTransient<IRepositoryUpdater, RepositoryUpdater>();
         services.AddTransient<IPackageUpdateSelection, PackageUpdateSelection>();
         services.AddTransient<IExistingCommitFilter, ExistingCommitFilter>();
         services.AddTransient<IPackageUpdater, PackageUpdater>();
         services.AddTransient<IRepositoryFilter, RepositoryFilter>();
         services.AddTransient<ISolutionRestore, SolutionRestore>();

         services.AddTransient<ILocalUpdater, LocalUpdater>();
         services.AddTransient<IUpdateSelection, UpdateSelection>();
         services.AddTransient<IFileSettingsCache, FileSettingsCache>();
         services.AddTransient<IFileSettingsReader, FileSettingsReader>();

         services.AddSingleton<IEnvironmentVariablesProvider, EnvironmentVariablesProvider>();

         services.AddSingleton<IGitDiscoveryDriver, LibGit2SharpDiscoveryDriver>();

         services.AddTransient<ISettingsReader, GitHubSettingsReader>();
         services.AddTransient<ISettingsReader, AzureDevOpsSettingsReader>();
         services.AddTransient<ISettingsReader, VstsSettingsReader>();
         services.AddTransient<ISettingsReader, BitbucketSettingsReader>();
         services.AddTransient<ISettingsReader, BitBucketLocalSettingsReader>();
         services.AddTransient<ISettingsReader, GitlabSettingsReader>();
         services.AddTransient<ISettingsReader, GiteaSettingsReader>();
      }

      public static void RegisterInspectionServices(IServiceCollection services)
      {
         services.AddSingleton(new ConfigurableLogger());
         services.AddTransient<INuKeeperLogger>((s) => s.GetService<ConfigurableLogger>());
         services.AddTransient<IConfigureLogger>((s) => s.GetService<ConfigurableLogger>());
         services.AddTransient<ILogger, NuGetLogger>();

         services.AddTransient<IDirectoryExclusions, DirectoryExclusions>();
         services.AddTransient<IFolderFactory, FolderFactory>();

         services.AddTransient<IPackageUpdatesLookup, PackageUpdatesLookup>();
         services.AddTransient<IBulkPackageLookup, BulkPackageLookup>();
         services.AddTransient<IPackageLookupResultReporter, PackageLookupResultReporter>();
         services.AddTransient<IPackageVersionsLookup, PackageVersionsLookup>();
         services.AddTransient<IApiPackageLookup, ApiPackageLookup>();
         services.AddTransient<IRepositoryScanner, RepositoryScanner>();

         services.AddTransient<IPackageReferenceFinder, ProjectFileReader>();
         services.AddTransient<IPackageReferenceFinder, PackagesFileReader>();
         services.AddTransient<IPackageReferenceFinder, NuspecFileReader>();
         services.AddTransient<IPackageReferenceFinder, DirectoryBuildTargetsReader>();

         services.AddTransient<IUpdateFinder, UpdateFinder>();
         services.AddTransient<INuGetSourcesReader, NuGetSourcesReader>();
         services.AddTransient<INuGetConfigFileReader, NuGetConfigFileReader>();

         services.AddTransient<IReporter, Reporter>();
         services.AddTransient<IPackageUpdateSetSort, PackageUpdateSetSort>();

         Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      }

      public static void RegisterUpdateServices(IServiceCollection services)
      {
         services.AddTransient<IFileRestoreCommand, NuGetFileRestoreCommand>();
         services.AddTransient<INuGetUpdatePackageCommand, NuGetUpdatePackageCommand>();
         services.AddTransient<IDotNetUpdatePackageCommand, DotNetUpdatePackageCommand>();
         services.AddTransient<IUpdateProjectImportsCommand, UpdateProjectImportsCommand>();
         services.AddTransient<IUpdateNuspecCommand, UpdateNuspecCommand>();
         services.AddTransient<IUpdateDirectoryBuildTargetsCommand, UpdateDirectoryBuildTargetsCommand>();
         services.AddTransient<IExternalProcess, ExternalProcess>();
         services.AddTransient<IMonoExecutor, MonoExecutor>();
         services.AddTransient<IUpdateRunner, UpdateRunner>();
         services.AddTransient<INuGetPath, NuGetPath>();
      }
   }
}
