namespace Pounce.Core
{
    static class Keywords
    {
        public static HashSet<string> SectionHeaders = new HashSet<string>()
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