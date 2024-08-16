using NuKeeper.Abstractions.Output;
using NuKeeper.Abstractions.RepositoryInspection;

using System.Collections.Generic;

namespace NuKeeper.Inspection.Report
{
    public interface IReporter
    {
        void Report(
            OutputDestination destination,
            OutputFormat format,
            string reportName,
            string fileName,
            IReadOnlyCollection<PackageUpdateSet> updates);
    }
}
