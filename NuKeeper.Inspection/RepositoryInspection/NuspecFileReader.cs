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
    public class NuspecFileReader : IPackageReferenceFinder
    {
        private readonly INuKeeperLogger _logger;
        private readonly PackageInProjectReader _packageInProjectReader;

        public NuspecFileReader(INuKeeperLogger logger)
        {
            _logger = logger;
            _packageInProjectReader = new PackageInProjectReader(logger);
        }

        public IReadOnlyCollection<PackageInProject> ReadFile(string baseDirectory, string relativePath)
        {
            PackagePath packagePath = new(baseDirectory, relativePath, PackageReferenceType.Nuspec);
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
            return new[] { "*.nuspec" };
        }

        public IReadOnlyCollection<PackageInProject> Read(Stream fileContents, PackagePath path)
        {
            XDocument xml = XDocument.Load(fileContents);

            XElement packagesNode = xml.Element("package")?.Element("metadata")?.Element("dependencies");
            if (packagesNode == null)
            {
                return Array.Empty<PackageInProject>();
            }

            IEnumerable<XElement> packageNodeList = packagesNode.Elements()
                .Where(x => x.Name == "dependency");

            return packageNodeList
                .Select(el => XmlToPackage(el, path))
                .Where(el => el != null)
                .ToList();
        }

        private PackageInProject XmlToPackage(XElement el, PackagePath path)
        {
            string id = el.Attribute("id")?.Value;
            string version = el.Attribute("version")?.Value;

            return _packageInProjectReader.Read(id, version, path, null);
        }
    }
}
