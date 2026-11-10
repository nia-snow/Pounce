namespace Pounce.Core
{
    static class Keywords
    {
        public static readonly HashSet<string> SectionHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "define",
            "theorem",
            "rule",
            "premisegroup",
            "using"
        };

        public static readonly HashSet<string> AllKeywords = new HashSet<string>
        (
            SectionHeaders.Concat
            (new HashSet<string>()
                {
                    // Header Modifiers
                    "overload",
                    "implicit",
                    "infix",
                    "prefix",
                    "template",
                    
                    // Properties
                    "syntax",
                    "signature",
                    "meaning",

                    // In-Signature
                    "derive",

                    // Specificity
                    "via",
                    "through",
                    "considering",
                    "because",
                    "by",
                    "for",
                    "from",

                    // Type Declarations
                    "let",
                    "be",
                }
            ),
            StringComparer.OrdinalIgnoreCase
        );

        public static readonly HashSet<string> Symbols = new HashSet<string>
        {
            ",",
            "→",
            "∴",
            "#",
            "[",
            "]"
        };
    }
}