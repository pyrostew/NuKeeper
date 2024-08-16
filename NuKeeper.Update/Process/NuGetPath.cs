using NuKeeper.Abstractions.Logging;

using System;
using System.IO;
using System.Linq;

namespace NuKeeper.Update.Process
{
    public class NuGetPath : INuGetPath
    {
        private readonly INuKeeperLogger _logger;
        private readonly Lazy<string> _executablePath;

        public NuGetPath(INuKeeperLogger logger)
        {
            _logger = logger;
            _executablePath = new Lazy<string>(FindExecutable);
        }

        public string Executable => _executablePath.Value;

        private string FindExecutable()
        {
            string localNugetPath = FindLocalNuget();

            return !string.IsNullOrEmpty(localNugetPath) ? localNugetPath : FindNugetInPackagesUnderProfile();
        }

        private string FindLocalNuget()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.Combine(appDir, "NuGet.exe");
            if (File.Exists(fullPath))
            {
                _logger.Detailed("Found NuGet.exe: " + fullPath);
                return fullPath;
            }

            return string.Empty;
        }

        private string FindNugetInPackagesUnderProfile()
        {
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (string.IsNullOrWhiteSpace(profile))
            {
                _logger.Error("Could not find user profile path");
                return string.Empty;
            }

            string commandlinePackageDir = Path.Combine(profile, ".nuget", "packages", "nuget.commandline");
            _logger.Detailed("Checking for NuGet.exe in packages directory: " + commandlinePackageDir);

            if (!Directory.Exists(commandlinePackageDir))
            {
                _logger.Error("Could not find nuget commandline path: " + commandlinePackageDir);
                return string.Empty;
            }

            string highestVersion = Directory.GetDirectories(commandlinePackageDir)
                .OrderByDescending(n => n)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(highestVersion))
            {
                _logger.Error("Could not find a version of nuget.commandline");
                return string.Empty;
            }

            string nugetProgramPath = Path.Combine(highestVersion, "tools", "NuGet.exe");
            string fullPath = Path.GetFullPath(nugetProgramPath);
            _logger.Detailed("Found NuGet.exe: " + fullPath);

            return fullPath;
        }
    }
}
