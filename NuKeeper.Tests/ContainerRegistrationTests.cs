using System;
using System.ComponentModel.Design;

using Microsoft.Extensions.DependencyInjection;

using NuKeeper.Collaboration;
using NuKeeper.Commands;
using NuKeeper.Local;

using NUnit.Framework;

namespace NuKeeper.Tests
{
   [TestFixture]
   public class ContainerRegistrationTests
   {
      [Test]
      public void RootCanBeResolved()
      {
         ServiceProvider services = ServicesRegistration.BuildProvider(ServicesRegistration.RegisterServices);

         ICollaborationEngine engine = services.GetService<ICollaborationEngine>();

         Assert.That(engine, Is.Not.Null);
         Assert.That(engine, Is.TypeOf<CollaborationEngine>());
      }

      [Test]
      public void InspectorCanBeResolved()
      {
         ServiceProvider services = ServicesRegistration.BuildProvider(ServicesRegistration.RegisterServices);

         ILocalEngine inspector = services.GetService<ILocalEngine>();

         Assert.That(inspector, Is.Not.Null);
         Assert.That(inspector, Is.TypeOf<LocalEngine>());
      }

      [TestCase(typeof(InspectCommand))]
      [TestCase(typeof(UpdateCommand))]
      [TestCase(typeof(RepositoryCommand))]
      [TestCase(typeof(OrganisationCommand))]
      [TestCase(typeof(GlobalCommand))]
      public void CommandsCanBeResolved(Type commandType)
      {
         ServiceProvider services = ServicesRegistration.BuildProvider(ServicesRegistration.RegisterServices);

         object command = services.GetService(commandType);

         Assert.That(command, Is.Not.Null);
      }
   }
}
