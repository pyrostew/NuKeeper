using NuKeeper.Collaboration;
using NuKeeper.Commands;
using NuKeeper.Local;

using NUnit.Framework;

using System;

namespace NuKeeper.Tests
{
    [TestFixture]
    public class ContainerRegistrationTests
    {
        [Test]
        public void RootCanBeResolved()
        {
            SimpleInjector.Container container = ContainerRegistration.Init();

            ICollaborationEngine engine = container.GetInstance<ICollaborationEngine>();

            Assert.That(engine, Is.Not.Null);
            Assert.That(engine, Is.TypeOf<CollaborationEngine>());
        }

        [Test]
        public void InspectorCanBeResolved()
        {
            SimpleInjector.Container container = ContainerRegistration.Init();

            ILocalEngine inspector = container.GetInstance<ILocalEngine>();

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
            SimpleInjector.Container container = ContainerRegistration.Init();

            object command = container.GetInstance(commandType);

            Assert.That(command, Is.Not.Null);
        }
    }
}
