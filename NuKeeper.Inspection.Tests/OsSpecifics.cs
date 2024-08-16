using System.Runtime.InteropServices;

namespace NuKeeper.Inspection.Tests
{
    public static class OsSpecifics
    {
        public static string GenerateBaseDirectory()
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            string baseDirectory = isWindows ? "c:\\temp\\somewhere" : "/temp/somewhere";
            return baseDirectory;
        }
    }
}
