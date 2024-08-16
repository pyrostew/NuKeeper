using System;

namespace NuKeeper.Abstractions.Formats
{
    public static class TimeSpanFormat
    {
        public static string Ago(DateTime start, DateTime end)
        {
            TimeSpan duration = end.Subtract(start);

            if (start.Year == end.Year && start.Month == end.Month)
            {
                return Ago(duration);
            }

            if (duration.TotalDays < 29)
            {
                // no exact size for "a month", but this is a lower bound
                return Ago(duration);
            }

            int months = MonthsBetween(start, end);
            int years = months / 12;
            int remainderMonth = months % 12;

            return years == 0
                ? Plural(remainderMonth, "month") + " ago"
                : remainderMonth == 0
                ? Plural(years, "year") + " ago"
                : Plural(years, "year") + " and " + Plural(remainderMonth, "month") + " ago";
        }

        private static int MonthsBetween(DateTime start, DateTime end)
        {
            if (start.Year == end.Year)
            {
                return end.Month - start.Month;
            }

            int fullYears = end.Year - start.Year - 1;
            int months = end.Month + 12 - start.Month;
            return (fullYears * 12) + months;
        }

        public static string Ago(TimeSpan ago)
        {
            if (ago.TotalSeconds < 1)
            {
                return "now";
            }

            if (ago.TotalMinutes < 1)
            {
                int secs = (int)ago.TotalSeconds;
                return Plural(secs, "second") + " ago";
            }

            if (ago.TotalHours < 1)
            {
                int mins = (int)ago.TotalMinutes;
                return Plural(mins, "minute") + " ago";
            }

            if (ago.TotalDays < 1)
            {
                int hours = (int)ago.TotalHours;
                return Plural(hours, "hour") + " ago";
            }

            int days = (int)ago.TotalDays;
            return Plural(days, "day") + " ago";
        }

        private static string Plural(int value, string metric)
        {
            return value == 1 ? $"1 {metric}" : $"{value} {metric}s";
        }
    }
}
