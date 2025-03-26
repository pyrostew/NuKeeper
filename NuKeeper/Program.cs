using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using McMaster.Extensions.CommandLineUtils;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NuKeeper.Commands;

[assembly: InternalsVisibleTo("NuKeeper.Tests")]

#pragma warning disable CA1822

namespace NuKeeper
{
   [Command(
       Name = "NuKeeper",
       FullName = "Automagically update NuGet packages in .NET projects.")]
   [VersionOptionFromMember(MemberName = nameof(GetVersion))]
   [Subcommand(typeof(InspectCommand))]
   [Subcommand(typeof(UpdateCommand))]
   [Subcommand(typeof(RepositoryCommand))]
   [Subcommand(typeof(OrganisationCommand))]
   [Subcommand(typeof(GlobalCommand))]
   public class Program
   {
      public static async Task<int> Main(string[] args)
      {
         return await Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .RunCommandLineApplicationAsync<Program>(args);
      }

      private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
      {
         ServicesRegistration.RegisterServices(services);
         ServicesRegistration.RegisterInspectionServices(services);
         ServicesRegistration.RegisterUpdateServices(services);
      }

      // ReSharper disable once UnusedMember.Global
      protected int OnExecute(CommandLineApplication app)
      {
         if (app == null)
         {
            throw new ArgumentNullException(nameof(app));
         }

         // this shows help even if the --help option isn't specified
         app.ShowHelp();
         return 1;
      }

      private static string GetVersion()
      {
         return typeof(Program)
         .Assembly
         .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
         .InformationalVersion;
      }
   }
}
