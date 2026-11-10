using Pounce.Strings;

namespace Pounce.Core
{
    class SectionerException : PounceException
    {
        public SectionerException(string message) : base(message) { }
    }


    class Sectioner
    {
        private List<string> Lines = new();

        private int Start;

        private int Position;

        List<Section> Output = new();

        public List<Section> DetermineSections(List<string> lines)
        {
            Lines = lines;
            Output = new();
            Position = 0;

            SkipBlank();
            while (!IsAtEnd())
            {
                SkipBlank();
                Start = Position;

                if (ContainsHeaderWord())
                {
                    CollectSection();
                }
                else
                {
                    throw Error(Position + 1, $"Expected section header but found \"{Peek()}\"");
                }
            }
            return Output;
        }

        private SectionerException Error(int line, string message) 
        {
            return new SectionerException($"[Line {line}] {message}");
        }

        private void CollectSection()
        {
            int line = Position + 1;
            Next();
            while (!IsAtEnd() && !ContainsHeaderWord())
            {
                Next();
            }
            Output.Add(new Section(Lines[Start..Position], Start));
        }

        private bool ContainsHeaderWord()
        {
            List<string> words = StringManip.SplitOnWhite(Peek()).Select(s => StringManip.RemoveColon(s)).ToList();
            return Keywords.SectionHeaders.Overlaps(words);
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
            return StringManip.IsBlank(Lines[Position]);
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

    }
}