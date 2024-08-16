using NuKeeper.Abstractions.RepositoryInspection;

using System;
using System.Collections.Generic;

namespace NuKeeper.Inspection.Report.Formats
{
    public class TextReportFormat : IReportFormat
    {
        private readonly IReportWriter _writer;

        public TextReportFormat(IReportWriter writer)
        {
            _writer = writer;
        }

        public void Write(string name, IReadOnlyCollection<PackageUpdateSet> updates)
        {
            if (updates == null)
            {
                throw new ArgumentNullException(nameof(updates));
            }

            _writer.WriteLine(MessageForCount(updates.Count));

            foreach (PackageUpdateSet update in updates)
            {
                _writer.WriteLine(Description.ForUpdateSet(update));
            }
        }

        private static string MessageForCount(int count)
        {
            return count == 0 ? "Found no package updates" : count == 1 ? "Found 1 package update" : $"Found {count} package updates";
        }
    }
}
