using NuKeeper.Abstractions;
using NuKeeper.Abstractions.Logging;

using System;

namespace NuKeeper.Inspection.Logging
{
    public class ConfigurableLogger : INuKeeperLogger, IConfigureLogger
    {
        private IInternalLogger _inner;

        public void Initialise(LogLevel logLevel, LogDestination destination, string filePath)
        {
            _inner = CreateLogger(logLevel, destination, filePath);
        }

        public void Error(string message, Exception ex)
        {
            CheckLoggerCreated();
            _inner.LogError(message, ex);
            if (ex?.InnerException != null)
            {
                Error("Inner Exception", ex.InnerException);
            }
        }

        public void Minimal(string message)
        {
            CheckLoggerCreated();
            _inner.Log(LogLevel.Minimal, message);
        }

        public void Normal(string message)
        {
            CheckLoggerCreated();
            _inner.Log(LogLevel.Normal, message);
        }

        public void Detailed(string message)
        {
            CheckLoggerCreated();
            _inner.Log(LogLevel.Detailed, message);
        }

        private void CheckLoggerCreated()
        {
            _inner ??= CreateLogger(LogLevel.Detailed, LogDestination.Console, string.Empty);
        }

        private static IInternalLogger CreateLogger(
            LogLevel logLevel, LogDestination destination,
            string filePath)
        {
            return destination switch
            {
                LogDestination.Console => new ConsoleLogger(logLevel),
                LogDestination.File => new FileLogger(filePath, logLevel),
                LogDestination.Off => new NullLogger(),
                _ => throw new NuKeeperException($"Unknown log destination {destination}"),
            };
        }
    }
}
