using NuKeeper.Abstractions.Formats;

using NUnit.Framework;

using System;

namespace NuKeeper.Abstractions.Tests.Formats
{
    [TestFixture]
    public class DurationParserTests
    {
        [Test]
        public void NullStringIsNotParsed()
        {
            TimeSpan? value = DurationParser.Parse(null);
            Assert.That(value, Is.Null);
        }

        [Test]
        public void EmptyStringIsNotParsed()
        {
            TimeSpan? value = DurationParser.Parse(string.Empty);
            Assert.That(value, Is.Null);
        }

        [Test]
        public void BadStringIsNotParsed()
        {
            TimeSpan? value = DurationParser.Parse("ghoti");
            Assert.That(value, Is.Null);
        }

        [Test]
        public void ZeroIsParsed()
        {
            // when you send a zero, no point in specifying the units
            TimeSpan? value = DurationParser.Parse("0");
            Assert.That(value, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void UnknownUnitsIsNotParsed()
        {
            TimeSpan? value = DurationParser.Parse("37x");
            Assert.That(value, Is.Null);
        }

        [TestCase("1d", 1)]
        [TestCase("3d", 3)]
        [TestCase("12d", 12)]
        [TestCase("123d", 123)]
        public void DaysAreParsed(string input, int expectedDays)
        {
            TimeSpan? parsed = DurationParser.Parse(input);
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.Value, Is.EqualTo(TimeSpan.FromDays(expectedDays)));
        }

        [TestCase("1h", 1)]
        [TestCase("3h", 3)]
        [TestCase("12h", 12)]
        [TestCase("123h", 123)]
        public void HoursAreParsed(string input, int expectedHours)
        {
            TimeSpan? parsed = DurationParser.Parse(input);
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.Value, Is.EqualTo(TimeSpan.FromHours(expectedHours)));
        }

        [TestCase("1w", 1)]
        [TestCase("3w", 3)]
        [TestCase("12w", 12)]
        [TestCase("123w", 123)]
        public void WeeksAreParsed(string input, int expectedWeeks)
        {
            TimeSpan? parsed = DurationParser.Parse(input);
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.Value, Is.EqualTo(TimeSpan.FromDays(expectedWeeks * 7)));
        }
    }
}
