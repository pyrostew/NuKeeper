using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using NuGet.Common;

using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Inspections.Files;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Inspection;
using NuKeeper.Inspection.Files;
using NuKeeper.Inspection.Report;
using NuKeeper.Inspection.Sort;
using NuKeeper.Inspection.Sources;
using NuKeeper.Local;

using NUnit.Framework;

namespace NuKeeper.Tests.Local
{
   [TestFixture]
   public class LocalEngineTests
   {
      [Test]
      public async Task CanRunInspect()
      {
         IUpdateFinder finder = Substitute.For<IUpdateFinder>();
         ILocalUpdater updater = Substitute.For<ILocalUpdater>();
         LocalEngine engine = MakeLocalEngine(finder, updater);

         SettingsContainer settings = new()
         {
            UserSettings = new UserSettings()
         };

         await engine.Run(settings, false);

         _ = await finder.Received()
             .FindPackageUpdateSets(Arg.Any<IFolder>(),
                 Arg.Any<NuGetSources>(),
                 Arg.Any<VersionChange>(),
                 Arg.Any<UsePrerelease>());

         await updater.Received(0)
             .ApplyUpdates(
                 Arg.Any<IReadOnlyCollection<PackageUpdateSet>>(),
                 Arg.Any<IFolder>(),
                 Arg.Any<NuGetSources>(),
                 Arg.Any<SettingsContainer>());
      }

      [Test]
      public async Task CanRunUpdate()
      {
         IUpdateFinder finder = Substitute.For<IUpdateFinder>();
         ILocalUpdater updater = Substitute.For<ILocalUpdater>();
         LocalEngine engine = MakeLocalEngine(finder, updater);

         SettingsContainer settings = new()
         {
            UserSettings = new UserSettings()
         };

         await engine.Run(settings, true);

         _ = await finder.Received()
             .FindPackageUpdateSets(Arg.Any<IFolder>(),
                 Arg.Any<NuGetSources>(),
                 Arg.Any<VersionChange>(),
                 Arg.Any<UsePrerelease>());

         await updater
             .Received(1)
             .ApplyUpdates(
                 Arg.Any<IReadOnlyCollection<PackageUpdateSet>>(),
                 Arg.Any<IFolder>(),
                 Arg.Any<NuGetSources>(),
                 Arg.Any<SettingsContainer>());
      }

      [Test]
      public async Task RunUpdate()
      {
         ServiceProvider services = ServicesRegistration.BuildProvider(sc =>
         {
            ServicesRegistration.RegisterServices(sc);
            ServicesRegistration.RegisterInspectionServices(sc);
            ServicesRegistration.RegisterUpdateServices(sc);
         });

         ILocalEngine engine = services.GetRequiredService<ILocalEngine>();
         SettingsContainer settings = new();

         settings.WorkingFolder = new Folder(services.GetRequiredService<INuKeeperLogger>(), new DirectoryInfo("..\\..\\..\\..\\TestData"));
         settings.UserSettings = new()
         {
            Directory = settings.WorkingFolder.FullPath,
            AllowedChange = VersionChange.Major
         };
         settings.PackageFilters = new() { MaxPackageUpdates = 10 };

         await engine.Run(settings, true);
      }

      private static LocalEngine MakeLocalEngine(IUpdateFinder finder, ILocalUpdater updater)
      {
         INuGetSourcesReader reader = Substitute.For<INuGetSourcesReader>();
         _ = finder.FindPackageUpdateSets(
                 Arg.Any<IFolder>(), Arg.Any<NuGetSources>(),
                 Arg.Any<VersionChange>(),
                 Arg.Any<UsePrerelease>())
             .Returns([]);

         IPackageUpdateSetSort sorter = Substitute.For<IPackageUpdateSetSort>();
         _ = sorter.Sort(Arg.Any<IReadOnlyCollection<PackageUpdateSet>>())
             .Returns(x => x.ArgAt<IReadOnlyCollection<PackageUpdateSet>>(0));

         INuKeeperLogger logger = Substitute.For<INuKeeperLogger>();

         NuGet.Common.ILogger nugetLogger = Substitute.For<NuGet.Common.ILogger>();

         IReporter reporter = Substitute.For<IReporter>();

         LocalEngine engine = new(reader, finder, sorter, updater,
             reporter, logger, nugetLogger);
         Assert.That(engine, Is.Not.Null);
         return engine;
      }
   }
}
