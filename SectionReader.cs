namespace Pounce.Core
{
    class SectionReaderException : PounceException
    {
        public SectionReaderException(string message) : base(message) { }
    }


    class SectionReader
    {
        private List<string> Lines = new();

        private int Start;

        private int Position;

        List<Section> Output = new();

        public List<Section> DetermineSections(string filename)
        {
            Lines = File.ReadLines(filename).ToList();
            Output = new();
            Position = 0;

            while (!IsAtEnd())
            {
                SkipBlank();
                Start = Position;
                List<string>? words = LeadingWords();
                // Section Header
                if (words != null)
                {
                    CollectSection(words);
                    continue;
                }


                else
                {
                    // No other options yet
                    throw Error(Start + 1, "Invalid start to section header");
                }
            }
            return Output;
        }

        private SectionReaderException Error(int line, string message) 
        {
            return new SectionReaderException($"[Line {line}] {message}");
        }

        private void CollectSection(List<string> words)
        {
            int line = Position + 1;
            Next();
            if (!ContainsHeaderWord(words))
            {
                throw Error(line, "':' used but no header word contained in line.");
            }

            while (!IsAtEnd() && !ContainsHeaderWord(LeadingWords()))
            {
                Next();
            }
            Output.Add(new Section(Lines[Start..Position], Start));
        }

        private bool ContainsHeaderWord(List<string>? words)
        {
            return words != null && Keywords.SectionHeaders.Overlaps(words.Select(s => s.ToLower()));
        }

        
        private List<string>? LeadingWords()
        {
            string line = Peek();
            List<string> words = line.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries).ToList();
            int spot = -1;
            for (int i = 0; i < words.Count; i++)
            {
                if (words[i].StartsWith("//"))
                {
                    break; 
                }
                if (words[i].EndsWith(":"))
                {
                    words[i] = words[i][0..^1]; // remove ':'
                    spot = i;
                    break;
                }
            }
            return spot != -1 ? words[0..(spot+1)] : null;
        }

        // Line Movement
        // ------
        private string Peek()
        {
            if (Position >= Lines.Count) return "\0";
            return Lines[Position];
        }

        private string Next()
        {
            if (Position >= Lines.Count) return "\0";
            return Lines[Position++];
        }

        private bool IsBlank()
        {
            return Lines[Position] == "";
        }


        private bool IsAtEnd()
        {
            return Position >= Lines.Count;
        }

        private void SkipBlank()
        {
            while (Position < Lines.Count && IsBlank())
            {
                Position++;
            }
        }

        // String Compare
        // ------
        private bool StartsWithIgnoreCase(string a, string b)
        {
            return a.StartsWith(b, StringComparison.OrdinalIgnoreCase);
        }

        private bool ContainsIgnoreCase(string a, string b)
        {
            return a.Contains(b, StringComparison.OrdinalIgnoreCase);
        }

        private bool EqualsIgnoreCase(string a, string b)
        {
            return a.Equals(b, StringComparison.OrdinalIgnoreCase);
        }

    }
}