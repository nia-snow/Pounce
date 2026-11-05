namespace Pounce.String
{
    static class StringManip
    {
        public static bool IsBlank(string s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        // Split On Whitespace
        public static string[] SplitOnWhite(string s)
        {
            return s.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries);
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