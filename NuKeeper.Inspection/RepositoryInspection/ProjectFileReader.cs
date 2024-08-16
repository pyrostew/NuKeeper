using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.RepositoryInspection;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NuKeeper.Inspection.RepositoryInspection
{
    public class ProjectFileReader : IPackageReferenceFinder
    {
        private const string VisualStudioLegacyProjectNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";
        private readonly INuKeeperLogger _logger;
        private readonly PackageInProjectReader _packageInProjectReader;

        public ProjectFileReader(INuKeeperLogger logger)
        {
            _logger = logger;
            _packageInProjectReader = new PackageInProjectReader(logger);
        }

        public IReadOnlyCollection<PackageInProject> ReadFile(string baseDirectory, string relativePath)
        {
            string filePath = Path.Combine(baseDirectory, relativePath);
            try
            {
                using FileStream fileContents = File.OpenRead(filePath);
                return Read(fileContents, baseDirectory, relativePath);
            }
            catch (IOException ex)
            {
                throw new ApplicationException($"Unable to parse file {filePath}", ex);
            }
        }

        public IReadOnlyCollection<string> GetFilePatterns()
        {
            return new[] { "*.csproj", "*.vbproj", "*.fsproj" };
        }

        public IReadOnlyCollection<PackageInProject> Read(Stream fileContents, string baseDirectory, string relativePath)
        {
            XDocument xml = XDocument.Load(fileContents);
            XNamespace ns = xml.Root.GetDefaultNamespace();

            PackagePath path = CreatePackagePath(ns, baseDirectory, relativePath);

            XElement project = xml.Element(ns + "Project");

            if (project == null)
            {
                return Array.Empty<PackageInProject>();
            }

            List<PackageInProject> projectFileResults = [];

            List<XElement> itemGroups = project
                .Elements(ns + "ItemGroup")
                .ToList();

            List<string> projectRefs = itemGroups
                .SelectMany(ig => ig.Elements(ns + "ProjectReference"))
                .Select(el => MakeProjectPath(el, path.FullName))
                .ToList();

            IEnumerable<XElement> packageRefs = itemGroups.SelectMany(ig => ig.Elements(ns + "PackageReference"));
            projectFileResults.AddRange(
                packageRefs
                .Select(el => XmlToPackage(ns, el, path, projectRefs))
                .Where(el => el != null)
            );

            projectFileResults.AddRange(
                itemGroups
                    .SelectMany(ig => ig.Elements(ns + "PackageDownload"))
                    .Select(el => XmlToPackage(ns, el, new PackagePath(baseDirectory, relativePath, PackageReferenceType.DirectoryBuildTargets), null))
                    .Where(el => el != null)
            );

            projectFileResults.AddRange(
                itemGroups
                    .SelectMany(ig => ig.Elements(ns + "PackageVersion"))
                    .Select(el => XmlToPackage(ns, el, new PackagePath(baseDirectory, relativePath, PackageReferenceType.DirectoryBuildTargets), null))
                    .Where(el => el != null)
            );

            return projectFileResults;
        }

        private static string MakeProjectPath(XElement el, string currentPath)
        {
            string relativePath = el.Attribute("Include")?.Value;

            string currentDir = Path.GetDirectoryName(currentPath);
            string combinedPath = Path.Combine(currentDir, relativePath);

            // combined path can still have "\..\" parts to it, need to canonicalise
            return Path.GetFullPath(combinedPath);
        }

        private static PackagePath CreatePackagePath(XNamespace xmlNamespace, string baseDirectory, string relativePath)
        {
            return xmlNamespace.NamespaceName == VisualStudioLegacyProjectNamespace
                ? new PackagePath(baseDirectory, relativePath, PackageReferenceType.ProjectFileOldStyle)
                : new PackagePath(baseDirectory, relativePath, PackageReferenceType.ProjectFile);
        }

        private PackageInProject XmlToPackage(XNamespace ns, XElement el,
            PackagePath path, IEnumerable<string> projectReferences)
        {
            string id = el.Attribute("Include")?.Value;
            string version = GetVersion(el, ns);
            return _packageInProjectReader.Read(id, version, path, projectReferences);
        }

        private static string GetVersion(XElement el, XNamespace ns)
        {
            return el.Attribute("Version")?.Value ?? el.Element(ns + "Version")?.Value;
        }
    }
}
