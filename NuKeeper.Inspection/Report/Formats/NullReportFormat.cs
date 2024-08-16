using NuKeeper.Abstractions.RepositoryInspection;

using System.Collections.Generic;

namespace NuKeeper.Inspection.Report.Formats
{
    public class NullReportFormat : IReportFormat
    {
        public void Write(string name, IReadOnlyCollection<PackageUpdateSet> updates)
        {
        }
    }
}
