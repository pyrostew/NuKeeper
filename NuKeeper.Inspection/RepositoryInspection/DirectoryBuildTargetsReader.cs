using NuKeeper.Abstractions;
using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.RepositoryInspection;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NuKeeper.Inspection.RepositoryInspection
{
    public class DirectoryBuildTargetsReader : IPackageReferenceFinder
    {
        private readonly INuKeeperLogger _logger;
        private readonly PackageInProjectReader _packageInProjectReader;

        public DirectoryBuildTargetsReader(INuKeeperLogger logger)
        {
            _logger = logger;
            _packageInProjectReader = new PackageInProjectReader(logger);
        }

        public IReadOnlyCollection<PackageInProject> ReadFile(string baseDirectory, string relativePath)
        {
            PackagePath packagePath = new(baseDirectory, relativePath, PackageReferenceType.DirectoryBuildTargets);
            try
            {
                using FileStream fileContents = File.OpenRead(packagePath.FullName);
                return Read(fileContents, packagePath);
            }
            catch (IOException ex)
            {
                throw new NuKeeperException($"Unable to parse file {packagePath.FullName}", ex);
            }
        }

        public IReadOnlyCollection<string> GetFilePatterns()
        {
            return new[] { "Directory.Build.props", "Directory.Packages.props", "Directory.Build.targets", "Packages.props" };
        }

        public IReadOnlyCollection<PackageInProject> Read(Stream fileContents, PackagePath path)
        {
            XDocument xml = XDocument.Load(fileContents);

            IEnumerable<XElement> packagesNode = xml.Element("Project")?.Elements("ItemGroup");
            if (packagesNode == null)
            {
                return Array.Empty<PackageInProject>();
            }

            IEnumerable<XElement> packageRefs = packagesNode.Elements("PackageReference");
            IEnumerable<XElement> packageDownloads = packagesNode.Elements("PackageDownload");
            IEnumerable<XElement> packageVersions = packagesNode.Elements("PackageVersion");

            return packageRefs
                .Concat(packageDownloads)
                .Concat(packageVersions)
                .Select(el => XmlToPackage(el, path))
                .Where(el => el != null)
                .ToList();
        }

        private PackageInProject XmlToPackage(XElement el, PackagePath path)
        {
            string id = el.Attribute("Include")?.Value;
            id ??= el.Attribute("Update")?.Value;
            string version = GetVersion(el);
            return _packageInProjectReader.Read(id, version, path, null);
        }

        private static string GetVersion(XElement el)
        {
            return el.Attribute("Version")?.Value ?? el.Element("Version")?.Value;
        }
    }
}
