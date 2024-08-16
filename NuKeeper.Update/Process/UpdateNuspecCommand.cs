using NuGet.Configuration;
using NuGet.Versioning;

using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NuKeeper.Update.Process
{
    public class UpdateNuspecCommand : IUpdateNuspecCommand
    {
        private readonly INuKeeperLogger _logger;

        public UpdateNuspecCommand(INuKeeperLogger logger)
        {
            _logger = logger;
        }

        public Task Invoke(PackageInProject currentPackage,
            NuGetVersion newVersion, PackageSource packageSource, NuGetSources allSources)
        {
            if (currentPackage == null)
            {
                throw new ArgumentNullException(nameof(currentPackage));
            }

            if (newVersion == null)
            {
                throw new ArgumentNullException(nameof(newVersion));
            }

            XDocument xml;
            using (FileStream xmlInput = File.OpenRead(currentPackage.Path.FullName))
            {
                xml = XDocument.Load(xmlInput);
            }

            using (FileStream xmlOutput = File.Open(currentPackage.Path.FullName, FileMode.Truncate))
            {
                UpdateNuspec(xmlOutput, newVersion, currentPackage, xml);
            }

            return Task.CompletedTask;
        }

        private void UpdateNuspec(Stream fileContents, NuGetVersion newVersion,
            PackageInProject currentPackage, XDocument xml)
        {
            XElement packagesNode = xml.Element("package")?.Element("metadata")?.Element("dependencies");
            if (packagesNode == null)
            {
                return;
            }

            System.Collections.Generic.IEnumerable<XElement> packageNodeList = packagesNode.Elements()
                .Where(x => x.Name == "dependency" && x.Attributes("id")
                .Any(a => a.Value == currentPackage.Id));

            foreach (XElement dependencyToUpdate in packageNodeList)
            {
                _logger.Detailed($"Updating nuspec depenencies: {currentPackage.Id} in path {currentPackage.Path.FullName}");
                dependencyToUpdate.Attribute("version").Value = newVersion.ToString();
            }

            xml.Save(fileContents);
        }
    }
}
