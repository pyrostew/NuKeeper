using NuKeeper.Abstractions;
using NuKeeper.Abstractions.Logging;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NuKeeper.Update.ProcessRunner
{
    public class ExternalProcess : IExternalProcess
    {
        private readonly INuKeeperLogger _logger;

        public ExternalProcess(INuKeeperLogger logger)
        {
            _logger = logger;
        }

        public async Task<ProcessOutput> Run(string workingDirectory, string command, string arguments, bool ensureSuccess)
        {
            _logger.Detailed($"In path {workingDirectory}, running command: {command} {arguments}");

            System.Diagnostics.Process process;

            try
            {
                ProcessStartInfo processInfo = MakeProcessStartInfo(workingDirectory, command, arguments);
                process = System.Diagnostics.Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                _logger.Error($"External command failed:{command} {arguments}", ex);

                if (ensureSuccess)
                {
                    throw;
                }

                string message = $"Error starting external process for {command}: {ex.GetType().Name} {ex.Message}";
                return new ProcessOutput(string.Empty, message, 1);
            }

            if (process == null)
            {
                throw new NuKeeperException($"Could not start external process for {command}");
            }

            string[] outputs = await Task.WhenAll(
                process.StandardOutput.ReadToEndAsync(),
                process.StandardError.ReadToEndAsync()
            );

            string textOut = outputs[0];
            string errorOut = outputs[1];

            process.WaitForExit();

            int exitCode = process.ExitCode;

            if (exitCode != 0)
            {
                string message = $"Command {command} failed with exit code: {exitCode}\n\n{textOut}\n\n{errorOut}";
                _logger.Detailed(message);

                if (ensureSuccess)
                {
                    throw new NuKeeperException(message);
                }
            }

            return new ProcessOutput(textOut, errorOut, exitCode);
        }

        private static ProcessStartInfo MakeProcessStartInfo(string workingDirectory, string command, string arguments)
        {
            return new ProcessStartInfo(command, arguments)
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory
            };
        }
    }
}
