using NuGet.Common;

using NUnit.Framework;

using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NuKeeper.Integration.Tests.LogHelpers
{
    internal class NugetTestLogger : ILogger
    {
        private readonly LogLevel _logLevel;

        private readonly ConcurrentQueue<string> _buffer = new();

        public NugetTestLogger(LogLevel logLevel = LogLevel.Verbose)
        {
            _logLevel = logLevel;
        }

        public void ClearLog()
        {
            _buffer.Clear();
        }

        public void DumpLogToTestOutput()
        {
            string test = TestContext.CurrentContext.Test.Name;

            if (!_buffer.IsEmpty)
            {
                TestContext.Error.WriteLine($"{test}: NuKeeper Log:");
                while (_buffer.TryDequeue(out string line))
                {
                    TestContext.Error.WriteLine(line);
                }
            }
        }

        public void Log(LogLevel level, string data)
        {
            string test = TestContext.CurrentContext.Test.Name;

            if (level >= _logLevel)
            {
                _buffer.Enqueue($"{test}: {level} - {data}");
            }
        }

        public void Log(ILogMessage message)
        {
            Log(message.Level, message.Message);
        }

        public Task LogAsync(LogLevel level, string data)
        {
            return Task.Run(() =>
            {
                Log(level, data);
            });
        }

        public Task LogAsync(ILogMessage message)
        {
            return Task.Run(() =>
            {
                Log(message);
            });
        }

        public void LogDebug(string data)
        {
            Log(LogLevel.Debug, data);
        }

        public void LogError(string data)
        {
            Log(LogLevel.Error, data);
        }

        public void LogInformation(string data)
        {
            Log(LogLevel.Information, data);
        }

        public void LogInformationSummary(string data)
        {
            Log(LogLevel.Information, data);
        }

        public void LogMinimal(string data)
        {
            Log(LogLevel.Minimal, data);
        }

        public void LogVerbose(string data)
        {
            Log(LogLevel.Verbose, data);
        }

        public void LogWarning(string data)
        {
            Log(LogLevel.Warning, data);
        }
    }
}
