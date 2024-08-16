using NuKeeper.Abstractions.Logging;
using NuKeeper.Abstractions.Output;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Inspection.Report.Formats;

using System;
using System.Collections.Generic;

namespace NuKeeper.Inspection.Report
{
    public class Reporter : IReporter
    {
        private readonly INuKeeperLogger _logger;

        public Reporter(INuKeeperLogger logger)
        {
            _logger = logger;
        }

        public void Report(
            OutputDestination destination,
            OutputFormat format,
            string reportName,
            string fileName,
            IReadOnlyCollection<PackageUpdateSet> updates)
        {
            if (updates == null)
            {
                throw new ArgumentNullException(nameof(updates));
            }

            string destinationDesc = destination == OutputDestination.File ?
                $" File '{fileName}'" :
                destination.ToString();

            _logger.Detailed($"Output report named {reportName}, is {format} to {destinationDesc}");

            using (IReportWriter writer = MakeReportWriter(destination, fileName))
            {
                IReportFormat reporter = MakeReporter(format, writer);
                reporter.Write(reportName, updates);
            }

            _logger.Detailed($"Wrote report for {updates.Count} updates");
        }

        private static IReportFormat MakeReporter(
            OutputFormat format,
            IReportWriter writer)
        {
            return format switch
            {
                OutputFormat.Off => new NullReportFormat(),
                OutputFormat.Text => new TextReportFormat(writer),
                OutputFormat.Csv => new CsvReportFormat(writer),
                OutputFormat.Metrics => new MetricsReportFormat(writer),
                OutputFormat.LibYears => new LibYearsReportFormat(writer),
                _ => throw new ArgumentOutOfRangeException($"Invalid OutputFormat: {format}"),
            };
        }

        private static IReportWriter MakeReportWriter(
            OutputDestination destination,
            string fileName)
        {
            return destination switch
            {
                OutputDestination.Console => new ConsoleReportWriter(),
                OutputDestination.File => new FileReportWriter(fileName),
                OutputDestination.Off => new NullReportWriter(),
                _ => throw new ArgumentOutOfRangeException($"Invalid OutputDestination: {destination}"),
            };
        }
    }
}
