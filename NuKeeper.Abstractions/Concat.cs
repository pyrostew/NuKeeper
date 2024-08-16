using System.Collections.Generic;
using System.Linq;

namespace NuKeeper.Abstractions
{
    public static class Concat
    {
        public static string FirstValue(params string[] values)
        {
            return values.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
        }

        public static T FirstValue<T>(params T?[] values) where T : struct
        {
            return values.FirstOrDefault(i => i.HasValue) ?? default;
        }

        public static IReadOnlyCollection<string> FirstPopulatedList(List<string> list1, List<string> list2, List<string> list3)
        {
            return HasElements(list1) ? list1 : HasElements(list2) ? list2 : HasElements(list3) ? list3 : (IReadOnlyCollection<string>)null;
        }

        private static bool HasElements(List<string> strings)
        {
            return strings != null && strings.Count > 0;
        }
    }
}
