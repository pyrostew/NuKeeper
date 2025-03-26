using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using NuGet.Common;

using NuKeeper.Abstractions.Logging;
using NuKeeper.Local;

using NUnit.Framework;

namespace NuKeeper.Update.Tests
{
   [TestFixture]
   public class UpdateRunnerIntegrationTests
   {
      public void UpdateSimpleNETProject()
      {
         ServiceProvider services = ServicesRegistration.BuildProvider(s =>
         {
            ServicesRegistration.RegisterServices(s);
            ServicesRegistration.RegisterUpdateServices(s);
            ServicesRegistration.RegisterInspectionServices(s);

            s.Replace(ServiceDescriptor.Transient(s => Substitute.For<INuKeeperLogger>()));
            s.Replace(ServiceDescriptor.Transient(s => Substitute.For<ILogger>()));
         });

         ILocalEngine engine = services.GetRequiredService<ILocalEngine>();

      }
   }
}
