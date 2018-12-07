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
        public static void GrammarTest()
        {
            Nonterminals GE = new Nonterminals("GE");
            Nonterminals Structure = new Nonterminals("Stucture");
            Terminals Sp = new Terminals("=>");
            Terminals Or = new Terminals("|");
            Nonterminals GEP = new Nonterminals("GEP");
            Terminals None = new Terminals("$");
            Nonterminals T = new Nonterminals("T");
            Nonterminals NT = new Nonterminals("NT");
            Nonterminals STP = new Nonterminals("STP");
            Nonterminals TP = new Nonterminals("TP");
            Nonterminals NTP = new Nonterminals("NTP");
            Nonterminals BigCase = new Nonterminals("BigCase");
            Nonterminals SmallCase = new Nonterminals("SmallCase");

            List<GrammarProduction> grammarProductions = new List<GrammarProduction>
            {
                new GrammarProduction(new GrammarStructure(new List<IGrammarSymbol>{GE}),new List<GrammarStructure>{
                                          new GrammarStructure( new List<IGrammarSymbol>{Structure,Sp, Structure, GEP})}),

                new GrammarProduction(new GrammarStructure(new List<IGrammarSymbol>{ GEP}),
                                        new List<GrammarStructure>{new GrammarStructure(new List<IGrammarSymbol> {Or, Structure,GEP }),
                                            new GrammarStructure(new List<IGrammarSymbol> { None })}),
            };
            Grammar grammar = new Grammar(grammarProductions);
            foreach (var tmp in grammar.Nonterminals)
            {
                Console.WriteLine($"{tmp}");
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
            //SampleSyntaxTest();
            GrammarTest();
            //SetupKeyFile();
            //TakenReaderUsage();
            //SampleLexerAsIter(@"C:\Users\sam\source\repos\CLK\LexicalCore\TakenReader.cs");
            //SampleLexerUsage(@"C:\Users\sam\source\repos\CLK\LexicalCore\TakenReader.cs");
        }
    }
}
