using CLK.GrammarCore;
using CLK.GrammarCore.Factory;
using CLK.GrammarCore.Parser;
using CLK.LexicalCore.DemoLexer;
using CLK.SyntaxCore;
using System;
using System.Collections.Generic;
using System.Linq;

//下一步计划： 完成多个分析程序
// 
namespace CLK.Client
{

    class LexerLibUsageDemo
    {
        public static void Print<T>(IEnumerable<T> data)
        {
            foreach (T d in data)
            {
                Console.Write(d);
            }
            Console.Write("\n");
        }
        // grammar相关数据结构的用法
        public static void GrammarDSUsage()
        {
            // 清华大学出版社 《编译原理》： 文法4.4

            SymbolStream symbolIter = new SymbolStream(new List<Terminal> { });

            Grammar grammar = DefaultGrammarFactory.CreateFromFile(@"C:\Users\sam\source\repos\CLK\SyntaxCore\demoGrammar.txt");
            CFG cfg = (CFG)grammar;
            // var table = cfg.GetLRTable();
            Console.WriteLine("LRTable");
            var lrTable = cfg.GetLRTable();
            lrTable.Print();
            var items = cfg.GetItemsSet();
            Console.WriteLine($"Ltems:\n {items}");
            LRParser lRParser = new LRParser(cfg);
            SymbolStream lrInput = DefaultSymbolStreamFactory.CreateFromStr(cfg, "ccdcd");
            lRParser.Init(lrInput);
            do
            {
                lRParser.Walk();
                lRParser.PrintState();
            } while (lRParser.GetState() == ParserState.Unfinished);
            var atl = lRParser.GetParseResult();
            atl.Print();
            /* Console.WriteLine(grammar);
             Console.WriteLine($"First:\n{cfg.GetFirstSetOfNonterminals()}");
             Console.WriteLine($"FirstSet:\n{cfg.GetFirstSetOfStructure()}");
             Console.WriteLine($"Follow:\n{cfg.GetFollow()}");
             Console.WriteLine("PATABLE");
             //var table = cfg.GetPATable();
             //table.Print();
             Console.WriteLine("itemsSet");
             var itemsSet = cfg.GetItemsSet();
             Console.WriteLine(itemsSet);*/

            /*LLParser llParser = new LLParser(cfg);
            SymbolStream symbolStream = DefaultSymbolStreamFactory.CreateFromStr(cfg, "d+(d+d)");
            llParser.Init(symbolStream);
            int tmp = 0;
            do
            {
                llParser.Walk();
                llParser.PrintState();
            } while (llParser.GetState() == ParserState.Unfinished);

            var atl = llParser.GetParseResult();
            Console.WriteLine($"Final:state{llParser.GetState()}");
            if (atl != null)
            {
                atl.Print();
            }*/
            /*  RG newG = (RG)grammar;
              if (grammar.IsLeftRecursive())
              {
                  Console.WriteLine("Grammar is LeftRecursive,eliminate it  ");
              }
              if (grammar.IsSatisfyNonrecuPredictionAnalysis())
              {
                  Console.WriteLine("文法满足非递归调用分析要求");
              }
              Console.WriteLine($"Origin grammar:{grammar}");
              Console.WriteLine($"New grammar:{newG}");

              var fst = newG.GetFirstSetOfStructure();
              var first = newG.GetFirstSetOfNonterminals();
              var follow = newG.GetFollow();
              Console.WriteLine("First:" + first);
              Console.WriteLine("Structure" + fst);
              Console.WriteLine("Follow" + follow);
              // 左递归判断
              if (newG.IsLeftRecursive())
              {
                  Console.WriteLine("grammar is left recursive");
              }
              Console.WriteLine("EliminateCR:");
              // 递归下降分析
              Console.WriteLine($"TEST:{symbolIter}");
              if (newG.RecursiveAnalyze(symbolIter))
              {
                  Console.WriteLine($"{symbolIter} is the sentence of Grammar");
              }
              else
              {
                  Console.WriteLine($"{symbolIter} is not the  sentence of Grammar");
              }
              //newG.GetPATable().Print();
              if (newG.GetPATable() != null)
              {
                  newG.GetPATable().Print();
              }
              DFA dfa = newG.ToDFA();
              dfa.Print();
              */
        }
        public static void PrintDic<C, V>(Dictionary<C, HashSet<V>> dic, String prefix)
        {
            Console.WriteLine(prefix);
            foreach (var fi in dic)
            {
                Console.Write($"{fi.Key} => [");
                foreach (var key in fi.Value)
                {
                    Console.Write($" {key}");
                }
                Console.WriteLine(" ]");
            }
        }

        public static void SampleSyntaxTest()
        {
            SampleSyntaxParser sampleSyntaxParser = new SampleSyntaxParser();
            while (true)
            {
                try
                {
                    Console.Write("<<");
                    sampleSyntaxParser.Parse(Console.ReadLine().ToArray());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
        public static void SetupKeyFile()
        {
            WordSetFactory.SerializeWordSet();
        }
        // 设置CLK_HOME环境变量 CLK_HOME环境变量未整个项目的根目录，随后可以考虑写一个单独的配置处理程序
        public static void SetupEnv()
        {
            string pwd = System.Environment.CurrentDirectory;
            string path = pwd.Substring(0, pwd.IndexOf("CLK") + "CLK".Length);
            System.Environment.SetEnvironmentVariable("CLK_HOME", path);
        }
        // takenReader实列
        public static void TakenReaderUsage()
        {
            ITokenReader takenReader = TokenReaderFactory.GetFromFile(System.Environment.
                GetEnvironmentVariable("CLK_HOME") + @"\GlobalConfig\keywords"); ;
            List<string> takens = new List<string>();
            while (takenReader.HasNext())
            {
                var ch = takenReader.Next();
                if (ch.Equals('\n'))
                {
                    takens.Add(takenReader.GetWord());
                }
            }
            takens.Add(new string(takenReader.GetWord().ToArray()));
            takens.ForEach(element => System.Console.Write($"{element}"));
        }
        public static void SampleLexerAsIter(string fileName)
        {
            EnumerableWord sampleLexer = new LexicalCore.DemoLexer.EnumerableWord(fileName);
            long lastLine = 1;
            foreach (Taken taken in sampleLexer)
            {
                if (taken.RowNo != lastLine)
                {
                    System.Console.WriteLine($"    =>Line:{lastLine}");
                    lastLine = taken.RowNo;
                }
                System.Console.Write(taken);
            }
        }
        // lexer实例
        public static void SampleLexerUsage(string fileName)
        {
            EnumerableWord sampleLexer = new LexicalCore.DemoLexer.EnumerableWord(fileName);
            ErrorCore.SampleInterpreterError sampleInterpreterError = ErrorCore.SampleInterpreterError.GetSampleInterpreterError();
            long lastLine = 1;
            while (true)
            {
                var taken = sampleLexer.Next();
                if (taken.RowNo != lastLine)
                {
                    System.Console.WriteLine($"    =>Line:{lastLine}");
                    lastLine = taken.RowNo;
                }
                switch (taken.Type)
                {
                    case TakenType.EOF:
                        System.Console.Write(taken);
                        sampleInterpreterError.ReportError();
                        return;
                    default:
                        System.Console.Write(taken);
                        break;
                }
            }
        }
        static void Main(string[] args)
        {
            SetupEnv();
            GrammarDSUsage();
            //SampleSyntaxTest();
            //GrammarTest();
            //SetupKeyFile();
            //TakenReaderUsage();
            //SampleLexerAsIter(@"C:\Users\sam\source\repos\CLK\LexicalCore\TakenReader.cs");
            //SampleLexerUsage(@"C:\Users\sam\source\repos\CLK\LexicalCore\TakenReader.cs");
        }
    }
}
