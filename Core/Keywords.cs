namespace Pounce.Core
{
    static class Keywords
    {
        public static readonly HashSet<string> SectionHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "define",
            "theorem",
            "rule"
        };

        public static HashSet<string> StatementHeaders = new HashSet<string>()
        {
            "using"
        };
    }
}