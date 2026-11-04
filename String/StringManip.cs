namespace Pounce.String
{
    static class StringManip
    {
        public static bool IsBlank(string a)
        {
            return string.IsNullOrWhiteSpace(a);
        }

        // String Compare
        // ------
        public static bool StartsWithIgnoreCase(string a, string b)
        {
            return a.StartsWith(b, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ContainsIgnoreCase(string a, string b)
        {
            return a.Contains(b, StringComparison.OrdinalIgnoreCase);
        }

        public static bool EqualsIgnoreCase(string a, string b)
        {
            return a.Equals(b, StringComparison.OrdinalIgnoreCase);
        }
    }
}