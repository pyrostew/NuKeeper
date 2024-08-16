using NuGet.Configuration;
using NuGet.Versioning;

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
    public class UpdateProjectImportsCommand : IUpdateProjectImportsCommand
    {
        public Task Invoke(PackageInProject currentPackage,
            NuGetVersion newVersion, PackageSource packageSource, NuGetSources allSources)
        {
            if (currentPackage == null)
            {
                throw new ArgumentNullException(nameof(currentPackage));
            }

            Stack<string> projectsToUpdate = new();
            projectsToUpdate.Push(currentPackage.Path.FullName);

            while (projectsToUpdate.Count > 0)
            {
                string currentProject = projectsToUpdate.Pop();

                XDocument xml;
                using (FileStream projectContents = File.Open(currentProject, FileMode.Open, FileAccess.ReadWrite))
                {
                    xml = XDocument.Load(projectContents);
                }

                IEnumerable<string> projectsToCheck = UpdateConditionsOnProjects(xml, currentProject);

                foreach (string potentialProject in projectsToCheck)
                {
                    string fullPath =
                        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(currentProject), potentialProject));
                    if (File.Exists(fullPath))
                    {
                        projectsToUpdate.Push(fullPath);
                    }
                }
            }

            return Task.CompletedTask;
        }

        private static IEnumerable<string> UpdateConditionsOnProjects(XDocument xml, string savePath)
        {
            XNamespace ns = xml.Root.GetDefaultNamespace();

            XElement project = xml.Element(ns + "Project");

            if (project == null)
            {
                return Enumerable.Empty<string>();
            }

            IEnumerable<XElement> imports = project.Elements(ns + "Import");
            List<XElement> importsWithToolsPath = imports
                .Where(i => i.Attributes("Project").Any(a => a.Value.Contains("$(VSToolsPath)", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            IEnumerable<XElement> importsWithoutCondition = importsWithToolsPath.Where(i => !i.Attributes("Condition").Any());
            IEnumerable<XElement> importsWithBrokenVsToolsCondition = importsWithToolsPath.Where(i =>
                i.Attributes("Condition").Any(a => a.Value == "\'$(VSToolsPath)\' != \'\'"));

            bool saveRequired = false;
            foreach (XElement importToFix in importsWithBrokenVsToolsCondition.Concat(importsWithoutCondition))
            {
                saveRequired = true;
                UpdateImportNode(importToFix);
            }

            if (saveRequired)
            {
                using FileStream xmlOutput = File.Open(savePath, FileMode.Truncate);
                xml.Save(xmlOutput);
            }

            return FindProjectReferences(project, ns);
        }

        private static IEnumerable<string> FindProjectReferences(XElement project, XNamespace ns)
        {
            IEnumerable<XElement> itemGroups = project.Elements(ns + "ItemGroup");
            IEnumerable<XElement> projectReferences = itemGroups.SelectMany(ig => ig.Elements(ns + "ProjectReference"));
            IEnumerable<string> includes = projectReferences.Attributes("Include").Select(a => a.Value);
            return includes;
        }

        private static void UpdateImportNode(XElement importToFix)
        {
            string importPath = importToFix.Attribute("Project").Value;
            string condition = $"Exists('{importPath}')";
            if (!importToFix.Attributes("Condition").Any())
            {
                importToFix.Add(new XAttribute("Condition", condition));
            }
            else
            {
                importToFix.Attribute("Condition").Value = condition;
            }
        }
    }
}
