using NuKeeper.Abstractions.RepositoryInspection;

using System.Collections.Generic;

namespace NuKeeper.Inspection.Report.Formats
{
    public interface IReportFormat
    {
        void Write(
            string name,
            IReadOnlyCollection<PackageUpdateSet> updates);
    }
}
