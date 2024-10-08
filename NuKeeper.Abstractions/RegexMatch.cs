using System.Text.RegularExpressions;

namespace NuKeeper.Abstractions
{
    public static class RegexMatch
    {
        public static bool IncludeExclude(string target, Regex include, Regex exclude)
        {
            return IsIncluded(target, include) && !IsExcluded(target, exclude);
        }

        private static bool IsIncluded(string target, Regex include)
        {
            return include?.IsMatch(target) ?? true;
        }

        private static bool IsExcluded(string target, Regex exclude)
        {
            return exclude?.IsMatch(target) ?? false;
        }
    }
}
