using NuGet.Versioning;

namespace NuKeeper.Abstractions.NuGet
{
    public static class VersionRanges
    {
        public static NuGetVersion SingleVersion(VersionRange range)
        {
            return range == null || range.IsFloating || (range.HasLowerAndUpperBounds && range.MinVersion != range.MaxVersion)
                ? null
                : range.MinVersion;
        }
    }
}
