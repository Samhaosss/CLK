using CLK.LexicalCore.DemoLexer;
using CLK.SyntaxCore;
using CLK.util;
using System;
using System.Collections.Generic;
using System.Linq;
/*
* 将来这个模块应该被改为winform程序，目前只是控制台应用程序，仅仅用于展示库用法
* **/
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
            // 完整的文法
            GrammarProduction production = new GrammarProduction("A", "    a A |  B b | C c");
            GrammarProduction production2 = new GrammarProduction("B", " b B | d D | A ");
            GrammarProduction production3 = new GrammarProduction("C", "  c  ");
            GrammarProduction production4 = new GrammarProduction("D", " d D |  a A | e B | d E");
            GrammarProduction production5 = new GrammarProduction("E", " a A |  a D | e ");
            SymbolIter symbolIter = new SymbolIter("aaaabbbbdddddaaaaaacc");
            CFG grammar = new CFG(new List<GrammarProduction> { production, production2, production3, production4, production5 });

            Console.WriteLine($"grammar:{grammar}");
            var fst = grammar.GetFirstOfStructure();
            var first = grammar.GetFirstOfNonterminals();
            var follow = grammar.GetFollow();
            PrintDic(first, "First");
            PrintDic(fst, "Structure");
            PrintDic(follow, "Follow");
            // 左递归判断
            if (grammar.IsLeftRecursive())
            {
                Console.WriteLine("grammar is left recursive");
            }
            grammar.EliminateCommonRecursive();
            Console.WriteLine("EliminateCR:\n" + grammar);
            // 递归下降分析
            Console.WriteLine($"TEST:{symbolIter}");
            if (grammar.RecursiveAnalyze(symbolIter))
            {
                Console.WriteLine($"{symbolIter} is the sentence of Grammar");
            }
            else
            {
                Console.WriteLine($"{symbolIter} is not the  sentence of Grammar");
            }

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
