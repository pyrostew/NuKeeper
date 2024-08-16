using NuKeeper.Abstractions.Logging;
using NuKeeper.Inspection.Logging;

using NUnit.Framework;

using System;

namespace NuKeeper.Inspection.Tests.Logging
{
    [TestFixture]
    public class ConfigurableLoggerTests
    {
        [TestCase(LogLevel.Detailed)]
        [TestCase(LogLevel.Normal)]
        [TestCase(LogLevel.Minimal)]
        [TestCase(LogLevel.Quiet)]
        public void CanLogMessage(LogLevel loggerLevel)
        {
            INuKeeperLogger logger = MakeLogger(loggerLevel);

            logger.Detailed("test message");
        }

        [TestCase(LogLevel.Detailed)]
        [TestCase(LogLevel.Normal)]
        [TestCase(LogLevel.Minimal)]
        [TestCase(LogLevel.Quiet)]
        public void CanLogMinimal(LogLevel loggerLevel)
        {
            INuKeeperLogger logger = MakeLogger(loggerLevel);

            logger.Minimal("test message");
        }

        [TestCase(LogLevel.Detailed)]
        [TestCase(LogLevel.Normal)]
        [TestCase(LogLevel.Minimal)]
        [TestCase(LogLevel.Quiet)]
        public void CanLogNormal(LogLevel loggerLevel)
        {
            INuKeeperLogger logger = MakeLogger(loggerLevel);

            logger.Normal("test message");
        }

        [TestCase(LogLevel.Detailed)]
        [TestCase(LogLevel.Normal)]
        [TestCase(LogLevel.Minimal)]
        [TestCase(LogLevel.Quiet)]
        public void CanLogError(LogLevel loggerLevel)
        {
            INuKeeperLogger logger = MakeLogger(loggerLevel);

            logger.Error("test message");
        }

        [TestCase(LogLevel.Detailed)]
        [TestCase(LogLevel.Normal)]
        [TestCase(LogLevel.Minimal)]
        [TestCase(LogLevel.Quiet)]
        public void CanLogErrorWithException(LogLevel loggerLevel)
        {
            INuKeeperLogger logger = MakeLogger(loggerLevel);

            logger.Error("test message", new ArgumentException("test"));
        }

        [TestCase(LogLevel.Detailed)]
        [TestCase(LogLevel.Normal)]
        [TestCase(LogLevel.Minimal)]
        [TestCase(LogLevel.Quiet)]
        public void CanLogErrorWithInnerException(LogLevel loggerLevel)
        {
            INuKeeperLogger logger = MakeLogger(loggerLevel);

            logger.Error("test message", new InvalidOperationException("op test",
                new ArgumentException("arg test")));
        }

        private static INuKeeperLogger MakeLogger(LogLevel logLevel)
        {
            ConfigurableLogger logger = new();
            logger.Initialise(logLevel, LogDestination.Console, string.Empty);
            return logger;
        }
    }
}
