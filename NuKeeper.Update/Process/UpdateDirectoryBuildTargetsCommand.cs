using NuGet.Configuration;
using NuGet.Versioning;

using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NuKeeper.Update.Process
{
    public class UpdateDirectoryBuildTargetsCommand : IUpdateDirectoryBuildTargetsCommand
    {
        private readonly INuKeeperLogger _logger;

        public UpdateDirectoryBuildTargetsCommand(INuKeeperLogger logger)
        {
            _logger = logger;
        }

        public Task Invoke(PackageInProject currentPackage, NuGetVersion newVersion, PackageSource packageSource,
            NuGetSources allSources)
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
                UpdateFile(xmlOutput, newVersion, currentPackage, xml);
            }

            return Task.CompletedTask;
        }

        private void UpdateFile(Stream fileContents, NuGetVersion newVersion,
            PackageInProject currentPackage, XDocument xml)
        {
            IEnumerable<XElement> packagesNode = xml.Element("Project")?.Elements("ItemGroup");
            if (packagesNode == null)
            {
                return;
            }

            IEnumerable<XElement> packageRefs = IncludesOrUpdates(currentPackage, packagesNode.Elements("PackageReference"));
            UpdateVersionTo(currentPackage, packageRefs, newVersion.ToString());
            IEnumerable<XElement> packageVersions = IncludesOrUpdates(currentPackage, packagesNode.Elements("PackageVersion"));
            UpdateVersionTo(currentPackage, packageVersions, newVersion.ToString());
            IEnumerable<XElement> packageDownloads = IncludesOrUpdates(currentPackage, packagesNode.Elements("PackageDownload"));
            UpdateVersionTo(currentPackage, packageDownloads, $"[{newVersion}]");

            xml.Save(fileContents);
        }

        private void UpdateVersionTo(PackageInProject currentPackage, IEnumerable<XElement> elements, string newVersion)
        {
            foreach (XElement dependencyToUpdate in elements)
            {
                _logger.Detailed(
                    $"Updating directory-level dependencies: {currentPackage.Id} in path {currentPackage.Path.FullName}");
                XAttribute attribute = dependencyToUpdate.Attribute("Version");
                if (attribute != null)
                {
                    attribute.Value = newVersion;
                }
                else
                {
                    dependencyToUpdate.Element("Version").Value = newVersion;
                }
            }
        }

        private static IEnumerable<XElement> IncludesOrUpdates(PackageInProject currentPackage, IEnumerable<XElement> elements)
        {
            return elements.Where(el =>
                el.Attributes("Include").Any(a => a.Value.Equals(currentPackage.Id, StringComparison.InvariantCultureIgnoreCase))
                || el.Attributes("Update").Any(a => a.Value.Equals(currentPackage.Id, StringComparison.InvariantCultureIgnoreCase))
            );
        }
    }
}
