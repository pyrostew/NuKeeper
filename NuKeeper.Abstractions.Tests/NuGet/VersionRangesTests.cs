using NuGet.Versioning;

using NuKeeper.Abstractions.NuGet;

using NUnit.Framework;

namespace NuKeeper.Abstractions.Tests.NuGet
{
    public class VersionRangesTests
    {
        [TestCase("1.2.3")]
        [TestCase("1.2.3.4")]
        [TestCase("[1.2.3.4]")]
        [TestCase("1.2.3-beta04")]
        [TestCase("1.2.3.4-beta05")]
        public void ParseableToSingleVersion(string rangeString)
        {
            bool canParseAsRange = VersionRange.TryParse(rangeString, out VersionRange versionRange);
            Assert.That(canParseAsRange, Is.True);

            NuGetVersion singleVersion = VersionRanges.SingleVersion(versionRange);

            Assert.That(versionRange, Is.Not.Null);
            Assert.That(singleVersion, Is.Not.Null);
        }

        [TestCase("1.*")]
        [TestCase("1.2.*")]
        [TestCase("1.2.3.*")]
        [TestCase("[1.*, 2.0.0)")]
        public void ParseableButNotSingleVersion(string rangeString)
        {
            bool canParseAsRange = VersionRange.TryParse(rangeString, out VersionRange versionRange);
            Assert.That(canParseAsRange, Is.True);

            NuGetVersion singleVersion = VersionRanges.SingleVersion(versionRange);

            Assert.That(versionRange, Is.Not.Null);
            Assert.That(singleVersion, Is.Null);
        }

        [TestCase("1.2.3")]
        [TestCase("1.2.3.4")]
        [TestCase("[1.2.3.4]")]
        [TestCase("1.2.3-beta04")]
        [TestCase("1.2.3.4-beta05")]
        public void ParseableToPackageIdentity(string rangeString)
        {
            PackageVersionRange rangeIdentity = PackageVersionRange.Parse("testPackage", rangeString);
            global::NuGet.Packaging.Core.PackageIdentity singleVersion = rangeIdentity.SingleVersionIdentity();

            Assert.That(rangeIdentity, Is.Not.Null);
            Assert.That(rangeIdentity.Id, Is.EqualTo("testPackage"));
            Assert.That(rangeIdentity.Version, Is.Not.Null);

            Assert.That(singleVersion, Is.Not.Null);
            Assert.That(singleVersion.Id, Is.EqualTo("testPackage"));
            Assert.That(singleVersion.Version, Is.Not.Null);
        }

        [TestCase("1.*")]
        [TestCase("1.2.*")]
        [TestCase("1.2.3.*")]
        [TestCase("[1.*, 2.0.0)")]
        public void ParseableButNotToPackageIdentity(string rangeString)
        {
            PackageVersionRange rangeIdentity = PackageVersionRange.Parse("testPackage", rangeString);
            global::NuGet.Packaging.Core.PackageIdentity singleVersion = rangeIdentity.SingleVersionIdentity();

            Assert.That(rangeIdentity, Is.Not.Null);
            Assert.That(rangeIdentity.Id, Is.EqualTo("testPackage"));
            Assert.That(rangeIdentity.Version, Is.Not.Null);

            Assert.That(singleVersion, Is.Null);
        }
    }
}
