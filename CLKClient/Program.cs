using CLK.LexicalCore;
using System.Collections.Generic;
using System.Linq;

namespace CLK.Client
{
    class Program
    {
        public static void TakenReaderUsage()
        {
            TakenReader takenReader = new TakenReader(@"C:\Users\sam\Desktop\impfile\sss.txt");
            List<string> takens = new List<string>();
            while (takenReader.hasNext())
            {
                var ch = takenReader.next();
                if (ch.Equals('\n'))
                {
                    IEnumerable<char> tmp = takenReader.GetWord();
                    takens.Add(new string(tmp.ToArray()));
                }
            }
            takens.Add(new string(takenReader.GetWord().ToArray()));
            foreach (var tak in takens)
            {
                System.Console.Write($"{tak}");
            }
        }
        public static void SampleLexerUsage(string fileName)
        {
            SampleLexer sampleLexer = new LexicalCore.SampleLexer(fileName);
            while (true)
            {
                var taken = sampleLexer.analyze();
                switch (taken.Type)
                {
                    case TakenType.EOF:
                        System.Console.Write(taken);
                        return;
                    case TakenType.delimiterChars:
                        System.Console.WriteLine(taken + $"    ->Line:{taken.RowNo}");
                        break;
                    default:
                        System.Console.Write(taken);
                        break;
                }
            }
        }
        static void Main(string[] args)
        {
            SampleLexerUsage(@"C:\Users\sam\source\repos\CLK\LexicalCore\TakenReader.cs");

        }
    }
}
