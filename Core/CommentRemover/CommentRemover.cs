using System.Text;

namespace Pounce.Core
{
    class CommentRemoverException : PounceException
    {
        public CommentRemoverException(string message) : base(message) { }
    }

    class CommentRemover
    {
        private string Text = "";

        private int Position = 0;

        public CommentRemoverException Error(int line, string message)
        {
            return new CommentRemoverException($"[Line {line}] {message}");
        }

        public List<string> RemoveInFile(string filename)
        {
            string text = File.ReadAllText(filename).Replace("\r\n", "\n").Replace("\r", "\n");
            return Remove(text);
        }

        public List<string> Remove(string text)
        {
            Text = text;
            Position = 0;

            int lastUpdated = 0;
            StringBuilder sb = new StringBuilder(Text.Length);

            while (!IsAtEnd())
            {
                char c = Next();
                if (c == '/')
                {
                    if (Peek() == '/')
                    {
                        sb.Append(Text[lastUpdated..(Position - 1)]).Append("\n");
                        Position = Text.IndexOf('\n', Position)+1;
                        if (Position == 0)
                        {
                            Position = Text.Length;
                        }
                        lastUpdated = Position;
                    }
                    else if (Peek() == '*')
                    {
                        int spot = Position - 1;
                        sb.Append(Text[lastUpdated..(Position - 1)]);
                        sb.Append(" ");
                        Next();
                        while (!IsAtEnd())
                        {
                            char ch = Next();
                            if (ch == '\n')
                            {
                                sb.Append("\n");
                            }
                            if (ch == '*' && Peek() == '/')
                            {
                                Next();
                                lastUpdated = Position;
                                break;
                            }
                        }
                        if (IsAtEnd())
                        {
                            int line = Text[0..spot].Count(c => c == '\n') + 1;
                            throw Error(line, "Unterminated /*");
                        }
                    }
                }
            }
            sb.Append(Text[lastUpdated..]);
            return sb.ToString().Split('\n').ToList();
        }

        private bool IsAtEnd()
        {
            return Position >= Text.Length;
        }

        private char Peek()
        {
            if (Position >= Text.Length) return '\0';
            return Text[Position];
        }

        private char Next()
        {
            if (Position >= Text.Length) return '\0';
            return Text[Position++];
        }
    }
}