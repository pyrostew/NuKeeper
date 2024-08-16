using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Inspection.Report;
using NuKeeper.Inspection.Report.Formats;

using NUnit.Framework;

using System;
using System.Collections.Generic;

namespace NuKeeper.Inspection.Tests.Report.Formats
{
    [TestFixture]
    public class ReportFormatTests
    {
        [Test, TestCaseSource(nameof(AllReportFormats))]
        public void EmptyUpdateListCanBeWritten(Type reportType)
        {
            List<PackageUpdateSet> rows = [];

            string outData = ReportToString(reportType, rows);

            Assert.That(outData, Is.Not.Null);
        }

        [Test, TestCaseSource(nameof(AllReportFormats))]
        public void OneUpdateInListCanBeWritten(Type reportType)
        {
            List<PackageUpdateSet> rows = PackageUpdates.OnePackageUpdateSet();

            string outData = ReportToString(reportType, rows);

            Assert.That(outData, Is.Not.Null);
            AssertExpectedEmpty(reportType, outData);
        }

        [Test, TestCaseSource(nameof(AllReportFormats))]
        public void MultipleUpdatesInListCanBeWritten(Type reportType)
        {
            List<PackageUpdateSet> rows = PackageUpdates.PackageUpdateSets(5);

            string outData = ReportToString(reportType, rows);

            Assert.That(outData, Is.Not.Null);

            AssertExpectedEmpty(reportType, outData);
        }

        private static void AssertExpectedEmpty(Type reportType, string outData)
        {
            bool expectEmpty = reportType == typeof(NullReportFormat);
            if (expectEmpty)
            {
                Assert.That(outData, Is.Empty);
            }
            else
            {
                Assert.That(outData, Is.Not.Empty);
            }
        }

        public static Type[] AllReportFormats()
        {
            return new[]
            {
                typeof(NullReportFormat),
                typeof(TextReportFormat),
                typeof(CsvReportFormat),
                typeof(MetricsReportFormat),
                typeof(LibYearsReportFormat)
            };
        }

        private static string ReportToString(Type reportType, List<PackageUpdateSet> rows)
        {
            using TestReportWriter output = new();
            IReportFormat reporter = MakeInstance(reportType, output);
            reporter.Write("test", rows);
            return output.Data();
        }

        private static IReportFormat MakeInstance(Type reportType, IReportWriter writer)
        {
            System.Reflection.ConstructorInfo noArgCtor = reportType.GetConstructor(Array.Empty<Type>());
            if (noArgCtor != null)
            {
                return (IReportFormat)noArgCtor.Invoke(Array.Empty<object>());
            }

            System.Reflection.ConstructorInfo oneArgCtor = reportType.GetConstructor(new[] { typeof(IReportWriter) });
            return (IReportFormat)oneArgCtor.Invoke(new object[] { writer });
        }
    }
}
