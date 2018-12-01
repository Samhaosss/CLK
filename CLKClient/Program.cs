using CLK.LexicalCore;
using System;
using System.Collections.Generic;
using System.Linq;
namespace CLK.Client
{
    class Program
    {
        static void Main(string[] args)
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
                // System.Console.Write($"{tak}");
            }
            Console.WriteLine($"{Environment.GetEnvironmentVariable("PATH")}");

        }
    }
}
