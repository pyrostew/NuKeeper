using NuKeeper.Abstractions.NuGet;
using NuKeeper.Abstractions.RepositoryInspection;
using NuKeeper.Inspection.Report.Formats;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Inspection.Tests.Report.Formats
{
    [TestFixture]
    public class CsvReportFormatTests
    {
        [Test]
        public void NoRowsHasHeaderLineInOutput()
        {
            List<PackageUpdateSet> rows = [];

            string output = ReportToString(rows);

            Assert.That(output, Is.Not.Null);
            Assert.That(output, Is.Not.Empty);

            string[] lines = output.Split(Environment.NewLine);
            Assert.That(lines.Length, Is.EqualTo(1));
        }

        [Test]
        public void OneRowHasOutput()
        {
            List<PackageUpdateSet> rows = PackageUpdates.OnePackageUpdateSet();

            string output = ReportToString(rows);

            Assert.That(output, Is.Not.Null);
            Assert.That(output, Is.Not.Empty);

            string[] lines = output.Split(Environment.NewLine);
            Assert.That(lines.Length, Is.EqualTo(2));
        }

        [Test]
        public void OneRowHasMatchedCommas()
        {
            List<PackageUpdateSet> rows = [];

            string output = ReportToString(rows);
            string[] lines = output.Split(Environment.NewLine);

            foreach (string line in lines)
            {
                int commas = line.Count(c => c == ',');
                Assert.That(commas, Is.EqualTo(11), $"Failed on line {line}");
            }
        }

        [Test]
        public void TwoRowsHaveOutput()
        {
            PackageVersionRange package1 = PackageVersionRange.Parse("foo.bar", "1.2.3");
            PackageVersionRange package2 = PackageVersionRange.Parse("fish", "2.3.4");

            List<PackageUpdateSet> rows =
            [
                PackageUpdates.UpdateSetFor(package1, PackageUpdates.MakePackageForV110(package1)),
                PackageUpdates.UpdateSetFor(package2, PackageUpdates.MakePackageForV110(package2))
            ];

            string output = ReportToString(rows);

            Assert.That(output, Is.Not.Null);
            Assert.That(output, Is.Not.Empty);

            string[] lines = output.Split(Environment.NewLine);
            Assert.That(lines.Length, Is.EqualTo(3));
            Assert.That(lines[1], Does.Contain("foo.bar,"));
            Assert.That(lines[2], Does.Contain("fish,"));
        }

        private static string ReportToString(List<PackageUpdateSet> rows)
        {
            TestReportWriter output = new();

            CsvReportFormat reporter = new(output);
            reporter.Write("test", rows);

            return output.Data();
        }
    }
}
