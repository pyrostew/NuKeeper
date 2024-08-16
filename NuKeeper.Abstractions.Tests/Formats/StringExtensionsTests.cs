using NUnit.Framework;

using System;

namespace NuKeeper.Abstractions.Tests.Formats
{
    [TestFixture]
    public class StringExtensionsTests
    {
        [TestCase("foobar", "foo")]
        [TestCase("foobar", "FOO")]
        [TestCase("FOOBAR", "foo")]
        [TestCase("FoObAr", "foo")]
        [TestCase("foobar", "bar")]
        [TestCase("foobar", "ob")]
        [TestCase("", "")]
        public void DoesContainOrdinal(string value, string substring)
        {
            Assert.That(value.Contains(substring, StringComparison.OrdinalIgnoreCase));
        }

        [TestCase("foobar", "x")]
        [TestCase("", "bar")]
        [TestCase("", "a")]
        [TestCase("foobar", "foobarfish")]
        public void DoesNotContainOrdinal(string value, string substring)
        {
            Assert.That(value.Contains(substring, StringComparison.OrdinalIgnoreCase), Is.False);
        }

        [Test]
        public void ShouldThrowOnNull()
        {
            _ = Assert.Throws<NullReferenceException>(
                () => ((string)null).Contains("sth", StringComparison.CurrentCulture));
        }
    }
}
