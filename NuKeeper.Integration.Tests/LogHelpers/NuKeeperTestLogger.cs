using NuKeeper.Abstractions.Logging;

using NUnit.Framework;

using System;
using System.Collections.Concurrent;

namespace NuKeeper.Integration.Tests.LogHelpers
{
    public class NuKeeperTestLogger : INuKeeperLogger
    {
        private readonly LogLevel _logLevel;

        private readonly ConcurrentQueue<string> _buffer = new();

        public NuKeeperTestLogger(LogLevel logLevel = LogLevel.Detailed)
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

        public void Error(string message, Exception ex = null)
        {
            Log(message, ex: ex);
        }

        public void Detailed(string message)
        {
            Log(message, LogLevel.Detailed);
        }

        public void Minimal(string message)
        {
            Log(message, LogLevel.Minimal);
        }

        public void Normal(string message)
        {
            Log(message, LogLevel.Normal);
        }

        private void Log(string message, LogLevel? level = null, Exception ex = null)
        {
            string test = TestContext.CurrentContext.Test.Name;

            if (_logLevel >= (level ?? LogLevel.Detailed))
            {
                string levelString = level?.ToString() ?? "Error";

                _buffer.Enqueue($"{test}: {levelString} - {message}");

                if (ex != null)
                {
                    _buffer.Enqueue($"{test}:   {ex.GetType().Name} : {ex.Message}");
                    foreach (string line in ex.StackTrace.Split(Environment.NewLine.ToCharArray()))
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            _buffer.Enqueue($"{test}:     {line}");
                        }
                    }
                }
            }
        }
    }
}
