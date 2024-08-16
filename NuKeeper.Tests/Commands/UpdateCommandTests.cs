using NSubstitute;

using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Output;
using NuKeeper.Commands;
using NuKeeper.Inspection.Logging;
using NuKeeper.Local;

using NUnit.Framework;

using System;
using System.Threading.Tasks;

namespace NuKeeper.Tests.Commands
{
    [TestFixture]
    public class UpdateCommandTests
    {
        [Test]
        public async Task ShouldCallEngineAndSucceed()
        {
            ILocalEngine engine = Substitute.For<ILocalEngine>();
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();

            _ = fileSettings.GetSettings().Returns(FileSettings.Empty());

            UpdateCommand command = new(engine, logger, fileSettings);

            int status = await command.OnExecute();

            Assert.That(status, Is.EqualTo(0));
            await engine
                .Received(1)
                .Run(Arg.Any<SettingsContainer>(), true);
        }

        [Test]
        public async Task EmptyFileResultsInDefaultSettings()
        {
            FileSettings fileSettings = FileSettings.Empty();

            SettingsContainer settings = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.PackageFilters, Is.Not.Null);
            Assert.That(settings.UserSettings, Is.Not.Null);
            Assert.That(settings.BranchSettings, Is.Not.Null);

            Assert.That(settings.PackageFilters.MinimumAge, Is.EqualTo(TimeSpan.FromDays(7)));
            Assert.That(settings.PackageFilters.Excludes, Is.Null);
            Assert.That(settings.PackageFilters.Includes, Is.Null);
            Assert.That(settings.PackageFilters.MaxPackageUpdates, Is.EqualTo(1));

            Assert.That(settings.UserSettings.AllowedChange, Is.EqualTo(VersionChange.Major));
            Assert.That(settings.UserSettings.NuGetSources, Is.Null);
            Assert.That(settings.UserSettings.OutputDestination, Is.EqualTo(OutputDestination.Console));
            Assert.That(settings.UserSettings.OutputFormat, Is.EqualTo(OutputFormat.Text));

            Assert.That(settings.BranchSettings.BranchNameTemplate, Is.Null);
        }

        [Test]
        public async Task WillReadMaxAgeFromFile()
        {
            FileSettings fileSettings = new()
            {
                Age = "8d"
            };

            SettingsContainer settings = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.PackageFilters, Is.Not.Null);
            Assert.That(settings.PackageFilters.MinimumAge, Is.EqualTo(TimeSpan.FromDays(8)));
        }

        [Test]
        public async Task WillReadIncludeExcludeFromFile()
        {
            FileSettings fileSettings = new()
            {
                Include = "foo",
                Exclude = "bar"
            };

            SettingsContainer settings = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.PackageFilters, Is.Not.Null);
            Assert.That(settings.PackageFilters.Includes.ToString(), Is.EqualTo("foo"));
            Assert.That(settings.PackageFilters.Excludes.ToString(), Is.EqualTo("bar"));
        }

        [Test]
        public async Task WillReadVersionChangeFromCommandLineOverFile()
        {
            FileSettings fileSettings = new()
            {
                Change = VersionChange.Patch
            };

            SettingsContainer settings = await CaptureSettings(fileSettings, VersionChange.Minor);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.UserSettings, Is.Not.Null);
            Assert.That(settings.UserSettings.AllowedChange, Is.EqualTo(VersionChange.Minor));
        }

        [Test]
        public async Task WillReadVersionChangeFromFile()
        {
            FileSettings fileSettings = new()
            {
                Change = VersionChange.Patch
            };

            SettingsContainer settings = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.UserSettings, Is.Not.Null);
            Assert.That(settings.UserSettings.AllowedChange, Is.EqualTo(VersionChange.Patch));
        }

        [Test]
        public async Task WillReadMaxPackageUpdatesFromFile()
        {
            FileSettings fileSettings = new()
            {
                MaxPackageUpdates = 1234
            };

            SettingsContainer settings = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.PackageFilters, Is.Not.Null);
            Assert.That(settings.PackageFilters.MaxPackageUpdates, Is.EqualTo(1234));
        }

        [Test]
        public async Task WillReadMaxPackageUpdatesFromCommandLineOverFile()
        {
            FileSettings fileSettings = new()
            {
                MaxPackageUpdates = 123
            };

            SettingsContainer settings = await CaptureSettings(fileSettings, null, 23456);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.PackageFilters, Is.Not.Null);
            Assert.That(settings.PackageFilters.MaxPackageUpdates, Is.EqualTo(23456));
        }

        [Test]
        public async Task WillReadBranchNameTemplateFromCommandLineOverFile()
        {
            string testTemplate = "nukeeper/MyBranch";

            FileSettings fileSettings = new()
            {
                BranchNameTemplate = testTemplate
            };

            SettingsContainer settings = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.BranchSettings, Is.Not.Null);
            Assert.That(settings.BranchSettings.BranchNameTemplate, Is.EqualTo(testTemplate));
        }

        public static async Task<SettingsContainer> CaptureSettings(FileSettings settingsIn,
            VersionChange? change = null,
            int? maxPackageUpdates = null)
        {
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();

            SettingsContainer settingsOut = null;
            ILocalEngine engine = Substitute.For<ILocalEngine>();
            await engine.Run(Arg.Do<SettingsContainer>(x => settingsOut = x), true);


            _ = fileSettings.GetSettings().Returns(settingsIn);

            UpdateCommand command = new(engine, logger, fileSettings)
            {
                AllowedChange = change,
                MaxPackageUpdates = maxPackageUpdates
            };

            _ = await command.OnExecute();

            return settingsOut;
        }
    }
}
