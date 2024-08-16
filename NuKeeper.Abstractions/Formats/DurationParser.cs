using System;

namespace NuKeeper.Abstractions.Formats
{
    public static class DurationParser
    {
        public static TimeSpan? Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (value == "0")
            {
                return TimeSpan.Zero;
            }

            char suffix = value[^1];
            string prefix = value[..^1];

            bool parsed = int.TryParse(prefix, out int count);
            return !parsed
                ? null
                : suffix switch
                {
                    'h' => (TimeSpan?)TimeSpan.FromHours(count),
                    'd' => (TimeSpan?)TimeSpan.FromDays(count),
                    'w' => (TimeSpan?)TimeSpan.FromDays(count * 7),
                    _ => null,
                };
        }
    }
}
