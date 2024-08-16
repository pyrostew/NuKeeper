using NSubstitute;

using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Logging;
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
    public class InspectCommandTests
    {
        [Test]
        public async Task ShouldCallEngineAndSucceed()
        {
            ILocalEngine engine = Substitute.For<ILocalEngine>();
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();

            _ = fileSettings.GetSettings().Returns(FileSettings.Empty());

            InspectCommand command = new(engine, logger, fileSettings);

            int status = await command.OnExecute();

            Assert.That(status, Is.EqualTo(0));
            await engine
                .Received(1)
                .Run(Arg.Any<SettingsContainer>(), false);
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
            Assert.That(settings.PackageFilters.MaxPackageUpdates, Is.EqualTo(0));

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
        public async Task InvalidMaxAgeWillFail()
        {
            FileSettings fileSettings = new()
            {
                Age = "fish"
            };

            SettingsContainer settings = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Null);
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
        public async Task WillReadBranchNamePrefixFromFile()
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

        [Test]
        public async Task LogLevelIsNormalByDefault()
        {
            ILocalEngine engine = Substitute.For<ILocalEngine>();
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();

            _ = fileSettings.GetSettings().Returns(FileSettings.Empty());

            InspectCommand command = new(engine, logger, fileSettings);

            _ = await command.OnExecute();

            logger
                .Received(1)
                .Initialise(LogLevel.Normal, LogDestination.Console, Arg.Any<string>());
        }

        [Test]
        public async Task ShouldSetLogLevelFromCommand()
        {
            ILocalEngine engine = Substitute.For<ILocalEngine>();
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();

            _ = fileSettings.GetSettings().Returns(FileSettings.Empty());

            InspectCommand command = new(engine, logger, fileSettings)
            {
                Verbosity = LogLevel.Minimal
            };

            _ = await command.OnExecute();

            logger
                .Received(1)
                .Initialise(LogLevel.Minimal, LogDestination.Console, Arg.Any<string>());
        }

        [Test]
        public async Task ShouldSetLogLevelFromFile()
        {
            ILocalEngine engine = Substitute.For<ILocalEngine>();
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();

            FileSettings settings = new()
            {
                Verbosity = LogLevel.Detailed
            };

            _ = fileSettings.GetSettings().Returns(settings);

            InspectCommand command = new(engine, logger, fileSettings);

            _ = await command.OnExecute();

            logger
                .Received(1)
                .Initialise(LogLevel.Detailed, LogDestination.Console, Arg.Any<string>());
        }

        [Test]
        public async Task CommandLineLogLevelOverridesFile()
        {
            ILocalEngine engine = Substitute.For<ILocalEngine>();
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();

            FileSettings settings = new()
            {
                Verbosity = LogLevel.Detailed
            };

            _ = fileSettings.GetSettings().Returns(settings);

            InspectCommand command = new(engine, logger, fileSettings)
            {
                Verbosity = LogLevel.Minimal
            };

            _ = await command.OnExecute();

            logger
                .Received(1)
                .Initialise(LogLevel.Minimal, LogDestination.Console, Arg.Any<string>());
        }

        [Test]
        public async Task LogToFileBySettingFileName()
        {
            ILocalEngine engine = Substitute.For<ILocalEngine>();
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();

            FileSettings settings = FileSettings.Empty();

            _ = fileSettings.GetSettings().Returns(settings);

            InspectCommand command = new(engine, logger, fileSettings)
            {
                LogFile = "somefile.log"
            };

            _ = await command.OnExecute();

            logger
                .Received(1)
                .Initialise(LogLevel.Normal, LogDestination.File, "somefile.log");
        }

        [Test]
        public async Task LogToFileBySettingLogDestination()
        {
            ILocalEngine engine = Substitute.For<ILocalEngine>();
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();

            FileSettings settings = FileSettings.Empty();

            _ = fileSettings.GetSettings().Returns(settings);

            InspectCommand command = new(engine, logger, fileSettings)
            {
                LogDestination = LogDestination.File
            };

            _ = await command.OnExecute();

            logger
                .Received(1)
                .Initialise(LogLevel.Normal, LogDestination.File, "nukeeper.log");
        }

        [Test]
        public async Task ShouldSetOutputOptionsFromFile()
        {
            FileSettings fileSettings = new()
            {
                OutputDestination = OutputDestination.File,
                OutputFormat = OutputFormat.Csv,
                OutputFileName = "foo.csv"
            };

            SettingsContainer settings = await CaptureSettings(fileSettings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.UserSettings.OutputDestination, Is.EqualTo(OutputDestination.File));
            Assert.That(settings.UserSettings.OutputFormat, Is.EqualTo(OutputFormat.Csv));
            Assert.That(settings.UserSettings.OutputFileName, Is.EqualTo("foo.csv"));
        }

        [Test]
        public async Task WhenFileNameIsExplicit_ShouldDefaultOutputDestToFile()
        {
            FileSettings fileSettings = new()
            {
                OutputDestination = null,
                OutputFormat = OutputFormat.Csv
            };

            SettingsContainer settings = await CaptureSettings(fileSettings, null, null, "foo.csv");

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.UserSettings.OutputDestination, Is.EqualTo(OutputDestination.File));
            Assert.That(settings.UserSettings.OutputFormat, Is.EqualTo(OutputFormat.Csv));
            Assert.That(settings.UserSettings.OutputFileName, Is.EqualTo("foo.csv"));
        }

        [Test]
        public async Task WhenFileNameIsExplicit_ShouldKeepOutputDest()
        {
            FileSettings fileSettings = new()
            {
                OutputDestination = OutputDestination.Off,
                OutputFormat = OutputFormat.Csv
            };

            SettingsContainer settings = await CaptureSettings(fileSettings, null, null, "foo.csv");

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.UserSettings.OutputDestination, Is.EqualTo(OutputDestination.Off));
            Assert.That(settings.UserSettings.OutputFormat, Is.EqualTo(OutputFormat.Csv));
            Assert.That(settings.UserSettings.OutputFileName, Is.EqualTo("foo.csv"));
        }

        [Test]
        public async Task ShouldSetOutputOptionsFromCommand()
        {
            SettingsContainer settingsOut = await CaptureSettings(FileSettings.Empty(),
                OutputDestination.File,
                OutputFormat.Csv);

            Assert.That(settingsOut.UserSettings.OutputDestination, Is.EqualTo(OutputDestination.File));
            Assert.That(settingsOut.UserSettings.OutputFormat, Is.EqualTo(OutputFormat.Csv));
        }

        public static async Task<SettingsContainer> CaptureSettings(FileSettings settingsIn,
            OutputDestination? outputDestination = null,
            OutputFormat? outputFormat = null,
            string outputFileName = null)
        {
            IConfigureLogger logger = Substitute.For<IConfigureLogger>();
            IFileSettingsCache fileSettings = Substitute.For<IFileSettingsCache>();

            SettingsContainer settingsOut = null;
            ILocalEngine engine = Substitute.For<ILocalEngine>();
            await engine.Run(Arg.Do<SettingsContainer>(x => settingsOut = x), false);


            _ = fileSettings.GetSettings().Returns(settingsIn);

            InspectCommand command = new(engine, logger, fileSettings);

            if (outputDestination.HasValue)
            {
                command.OutputDestination = outputDestination.Value;
            }
            if (outputFormat.HasValue)
            {
                command.OutputFormat = outputFormat.Value;
            }

            if (!string.IsNullOrWhiteSpace(outputFileName))
            {
                command.OutputFileName = outputFileName;
            }

            _ = await command.OnExecute();

            return settingsOut;
        }
    }
}
