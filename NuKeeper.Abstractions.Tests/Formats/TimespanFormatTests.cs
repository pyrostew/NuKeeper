using NuKeeper.Abstractions.Formats;

using NUnit.Framework;

using System;

namespace NuKeeper.Abstractions.Tests.Formats
{
    [TestFixture]
    public class TimespanFormatTests
    {
        [Test]
        public void TestSomeSeconds()
        {
            TimeSpan duration = new(0, 0, 32);

            string result = TimeSpanFormat.Ago(duration);

            Assert.That(result, Is.EqualTo("32 seconds ago"));
        }

        [Test]
        public void TestSomeMinutes()
        {
            TimeSpan duration = new(0, 12, 3);

            string result = TimeSpanFormat.Ago(duration);

            Assert.That(result, Is.EqualTo("12 minutes ago"));
        }

        [Test]
        public void TestAnHour()
        {
            TimeSpan duration = new(1, 1, 1);

            string result = TimeSpanFormat.Ago(duration);

            Assert.That(result, Is.EqualTo("1 hour ago"));
        }

        [Test]
        public void TestSomeHours()
        {
            TimeSpan duration = new(9, 12, 3);

            string result = TimeSpanFormat.Ago(duration);

            Assert.That(result, Is.EqualTo("9 hours ago"));
        }

        [Test]
        public void TestSomeDays()
        {
            TimeSpan duration = new(12, 9, 12, 3);

            string result = TimeSpanFormat.Ago(duration);

            Assert.That(result, Is.EqualTo("12 days ago"));
        }

        [Test]
        public void TestTwoDatesSeperatedByOneDays()
        {
            DateTime end = DateTime.UtcNow;
            DateTime start = end.AddDays(-1);

            string result = TimeSpanFormat.Ago(start, end);

            Assert.That(result, Is.EqualTo("1 day ago"));
        }

        [Test]
        public void TestTwoDatesSeperatedByTwoDays()
        {
            DateTime end = DateTime.UtcNow;
            DateTime start = end.AddDays(-2);

            string result = TimeSpanFormat.Ago(start, end);

            Assert.That(result, Is.EqualTo("2 days ago"));
        }

        [Test]
        public void TestTwoDatesYearsApart()
        {
            DateTime end = DateTime.UtcNow;
            DateTime start = end.AddYears(-2);

            string result = TimeSpanFormat.Ago(start, end);

            Assert.That(result, Is.EqualTo("2 years ago"));
        }

        [Test]
        public void TestTwoDatesMonthsApart()
        {
            DateTime end = DateTime.UtcNow;
            DateTime start = end.AddMonths(-4);

            string result = TimeSpanFormat.Ago(start, end);

            Assert.That(result, Is.EqualTo("4 months ago"));
        }

        [Test]
        public void TestMonthStart()
        {
            DateTime end = new(2018, 2, 1);
            DateTime start = end.AddDays(-1);

            string result = TimeSpanFormat.Ago(start, end);

            Assert.That(result, Is.EqualTo("1 day ago"));
        }

        [Test]
        public void TestYearStart()
        {
            DateTime end = new(2018, 1, 1);
            DateTime start = end.AddDays(-1);

            string result = TimeSpanFormat.Ago(start, end);

            Assert.That(result, Is.EqualTo("1 day ago"));
        }

        [Test]
        public void TestTwoDatesTenMonthsApart()
        {
            DateTime end = new(2018, 4, 5);
            DateTime start = end.AddMonths(-10);

            string result = TimeSpanFormat.Ago(start, end);

            Assert.That(result, Is.EqualTo("10 months ago"));
        }

        [Test]
        public void TestTwoDatesFourteenApart()
        {
            DateTime end = new(2018, 3, 4);
            DateTime start = end.AddMonths(-14);

            string result = TimeSpanFormat.Ago(start, end);

            Assert.That(result, Is.EqualTo("1 year and 2 months ago"));
        }


        [Test]
        public void TestTwoDatesYearsAndMonthsApart()
        {
            DateTime end = DateTime.UtcNow;
            DateTime start = end
                .AddYears(-2)
                .AddMonths(-10);

            string result = TimeSpanFormat.Ago(start, end);

            Assert.That(result, Is.EqualTo("2 years and 10 months ago"));
        }

        [Test]
        public void TestTwoDatesThreeYearsAndOneMonthApart()
        {
            DateTime end = DateTime.UtcNow;
            DateTime start = end
                .AddYears(-3)
                .AddMonths(-1);

            string result = TimeSpanFormat.Ago(start, end);

            Assert.That(result, Is.EqualTo("3 years and 1 month ago"));
        }
    }
}
