using NuKeeper.Abstractions.Formats;

using NUnit.Framework;

using System;

namespace NuKeeper.Abstractions.Tests.Formats
{
    [TestFixture]
    public class UriFormatsTests
    {
        [Test]
        public void TrailingSlashIsKeptWhenPresent()
        {
            Uri input = new("http://test.com/api/path/");

            Uri output = UriFormats.EnsureTrailingSlash(input);

            Assert.That(output.ToString(), Is.EqualTo("http://test.com/api/path/"));
        }

        [Test]
        public void TrailingSlashIsAddedWhenMissing()
        {
            Uri input = new("http://test.com/api/path");

            Uri output = UriFormats.EnsureTrailingSlash(input);

            Assert.That(output.ToString(), Is.EqualTo("http://test.com/api/path/"));
        }

        [Test]
        public void IsLocalUri()
        {
            string input = ".";
            Uri output = input.ToUri();

            Assert.That(output.IsFile, Is.EqualTo(true));
        }

        [Test]
        public void IsRemoteUri()
        {
            string input = "https://www.google.com";
            Uri output = input.ToUri();

            Assert.That(output.Host, Is.EqualTo("www.google.com"));
        }

        [Test]
        public void IsNonExistingUri()
        {
            string input = "../../../invalidpath/test/1234/abcde";

            Assert.That(input.ToUri,
                Throws.Exception
                    .TypeOf<NuKeeperException>());
        }
    }
}
