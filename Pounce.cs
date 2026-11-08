using System;

namespace Pounce.Core
{
    class PounceException : Exception
    {
        public PounceException(string message) : base(message) { }
    }


    class Pounce
    {
        public static void Main(string[] args)
        {
            // if (args.Length != 1)
            // {
            //     Console.Error.WriteLine("Expected Pounce file, found none.");
            // }

            // string filename = args[0];

            // Debug:
            string filename = "Tests/operator∧.pounce";

            CommentRemover cr = new CommentRemover();
            Sectioner sr = new Sectioner();
            try
            {
                List<string> lines = cr.RemoveInFile(filename);
                
                List<Section> sections = sr.DetermineSections(lines);
                for (int i = 0; i < sections.Count; i++)
                {
                    for (int j = 0; j < sections[i].Count; j++)
                    {
                        Console.WriteLine(sections[i][j]);
                    }
                    Console.WriteLine("***");
                }
            }
            catch (FileNotFoundException e)
            {
                Console.Error.WriteLine(e.GetType().Name + ": " + filename);
                System.Environment.Exit(1);
                return;
            }
            catch (PounceException e)
            {
                Console.Error.WriteLine(e.GetType().Name + ": " + e.Message);
                System.Environment.Exit(1);
                return;
            }
        }
    }
}