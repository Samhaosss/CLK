using CLK.DemoInterpreter.DemoLexer;
using CLK.GrammarCore;
using CLK.GrammarCore.Factory;
using CLK.GrammarCore.Parser;
using CLK.Interpreter;
using CLK.Interpreter.DemoLexer;
using System;
using System.Collections.Generic;
using System.Linq;

//下一步计划： 完成多个分析程序
// 
namespace CLK.Client
{
    public enum ClientState { Hungry, Init, Running, Finished }
    public class GrammarLibClient
    {
        private ClientState state;
        private Grammar grammar;
        private IParser parse;
        private SymbolStream input;

        public GrammarLibClient()
        {
            state = ClientState.Hungry;
        }
        public GrammarLibClient(string filePath)
        {
            grammar = DefaultGrammarFactory.CreateCFGFromFile(filePath);
            state = ClientState.Init;
        }
        public void Startup()
        {
            PrintInfo();
            while (true)
            {
                switch (state)
                {
                    case ClientState.Hungry:
                        HandleHungry();
                        break;
                    case ClientState.Init:
                        HandleInit();
                        break;
                    case ClientState.Running:
                        HandleRuning();
                        break;
                    case ClientState.Finished:
                        HandleFinished();
                        break;
                }
            }
        }
        private void HandleFinished()
        {
            while (true)
            {
                PrintState();
                string inputStr = Console.ReadLine();
                if (inputStr.Equals("info"))
                {
                    parse.ReportAnalyzeResult();
                }
                else if (inputStr.Equals("restart"))
                {
                    state = ClientState.Hungry;
                    grammar = null;
                    parse = null;
                    input = null;
                    break;
                }
                else if (inputStr.Equals("reload"))
                {
                    state = ClientState.Init;
                    parse = null;
                    input = null;
                    break;
                }
                else
                {
                    HandleError();
                }
            }
        }
        private void HandleRuning()
        {
            while (true)
            {
                PrintState();
                string inputStr = Console.ReadLine();
                var tmp = inputStr.Split(' ').Select(X => X.Trim()).Where(x => !x.Equals("")).ToArray();
                if (inputStr.Equals("next") || inputStr.Equals("n"))
                {
                    var st = parse.Walk();
                    if (st != ParserState.Unfinished)
                    {
                        state = ClientState.Finished;
                        Console.WriteLine("Analyze Finished");
                        break;
                    }
                }
                else if (inputStr.Equals("run") || inputStr.Equals("r"))
                {
                    while (parse.GetState() == ParserState.Unfinished)
                    {
                        parse.Walk();
                    }
                    state = ClientState.Finished;
                    Console.WriteLine("Analyze Finished");
                    break;
                }
                else if (tmp.Count() != 0 && tmp[0].Equals("print"))
                {
                    if (tmp.Count() == 2)
                    {
                        HandlerPrint(tmp[1]);
                    }
                    else if (tmp.Count() == 1)
                    {
                        parse.PrintState();
                    }
                    else
                    {
                        HandleError();
                    }
                }
                else
                {
                    HandleError();
                }
            }
        }
        private void HandleInit()
        {
            string inputStr;
            while (true)
            {
                inputStr = GetInput();
                var tmp = inputStr.Split(' ').Select(X => X.Trim()).Where(x => !x.Equals("")).ToArray();
                if (tmp.Length != 0)
                {
                    if (tmp[0].Equals("print") && tmp.Length == 2)
                    {
                        HandlerPrint(tmp[1]);
                    }
                    else if (tmp[0].Equals("info"))
                    {
                        Console.WriteLine(grammar);
                    }
                    else if (tmp[0].Equals("ll") || tmp[0].Equals("lr"))
                    {
                        CFG tmpCFG = grammar as CFG;
                        if (tmpCFG != null)
                        {
                            string tmpstr;
                            if (tmp.Length < 2)
                            {
                                Console.WriteLine("Input string to be analyzed");
                                tmpstr = Console.ReadLine();
                            }
                            else
                            {
                                tmpstr = tmp.Skip(1).Aggregate("", (x, y) => x + y);
                            }
                            try
                            {
                                parse = tmp[0].Equals("ll") ? new LLParser(tmpCFG) : (IParser)new LRParser(tmpCFG);
                                input = DefaultSymbolStreamFactory.CreateFromStr(tmpCFG, tmpstr);
                                parse.Init(input);
                                state = ClientState.Running;
                                break;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Error: \n" + e.Message);
                                continue;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Error: Only CFG can use parser");
                            continue;
                        }
                    }
                    else
                    {
                        HandleError();
                    }
                }
            }
        }

        private void HandleHungry()
        {
            while (true)
            {
                string input = GetInput();
                var tmp = input.Split(' ');
                if (tmp.Count() == 2 && tmp[0].Equals("load"))
                {
                    try
                    {
                        grammar = DefaultGrammarFactory.CreateCFGFromFile(tmp[1]);
                        Console.WriteLine("Load Successfully");
                        state = ClientState.Init;   //状态迁移
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Load Failed:\n" + e.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Invaild Input");
                    Help();
                }
            }
        }
        private string GetInput()
        {
            string input = "";
            do
            {
                PrintState();
                input = Console.ReadLine();
                try
                {
                    input = input.ToLower();
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error： " + e.Message);
                }
            } while (true);
            return input;
        }
        private void HandleError()
        {
            string errorMsg = "Invail Input";
            Console.WriteLine(errorMsg);
            Help();
        }
        private void HandlerPrint(string target)
        {
            if (state == ClientState.Hungry) { throw new Exception("内部错误"); }
            CFG tmpCfg = grammar as CFG;
            if (tmpCfg == null)
            {
                Console.WriteLine("Error: Only CFG is surpported currently");
                HandleError();
                return;
            }

            if (target.Equals("first"))
            {
                tmpCfg.GetFirstSetOfNonterminals().Print();
            }
            else if (target.Equals("firstset"))
            {
                tmpCfg.GetFirstSetOfStructure().Print();
            }
            else if (target.Equals("follow"))
            {
                tmpCfg.GetFollow().Print();
            }
            else if (target.Equals("lltable"))
            {
                try
                {
                    tmpCfg.GetPATable().Print();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error: " + e.Message);
                }
            }
            else if (target.Equals("itemsset"))
            {
                tmpCfg.GetItemsSet().Print();
            }
            else if (target.Equals("lrtable"))
            {
                tmpCfg.GetLRTable().Print();
            }
            else
            {
                HandleError();
            }
            return;
        }
        private void Help()
        {
            switch (state)
            {
                case ClientState.Hungry:
                    Console.WriteLine("Client in hungry, feed a grammar file to it first;\n" +
                        "\r\rLoad [filePath]");
                    break;
                case ClientState.Init:
                    Console.WriteLine("Inited State:\n" +
                                        "Print [something]     something could be: first, firstset,follow,lrtable,lltable,itemsset\n" +
                                        "CalFirst [structure]  structure should only contain symbols in grammar[not im yet]\n" +
                                        "Info                  print basic info about current grammar\n" +
                                        "Nomalize              eliminateRecursive,empty production [not implemented yet]\n " +
                                        "LR [input]            analyze input using current grammar\n" +
                                        "LL [input]            analyze input using current grammar\n");
                    break;
                case ClientState.Running:
                    Console.WriteLine("Running State:\n" +
                                      "Next                   run one step\n" +
                                      "Run                    run to finished\n" +
                                      "Print                  print current state of parse\n" +
                                      "Reset                  reset input, analyze again[Not im yet]\n");
                    break;
                case ClientState.Finished:
                    Console.WriteLine("Finished State:\n" +
                                      "Info                 report result of last analyze\n" +
                                      "Restart              Back to hungry\n" +
                                      "Stop                 Back to init State\n" +
                                      "Reload               restart analyze of current grammar");
                    break;
            }
        }
        private void PrintInfo()
        {
            Console.WriteLine("This a best practice on  CLK gammar lib,very sample tool, enjoy!");
        }
        private void PrintState()
        {
            switch (state)
            {
                case ClientState.Hungry:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case ClientState.Init:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case ClientState.Running:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case ClientState.Finished:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }
            Console.Write($"[{state}] <<");
            Console.ResetColor();
        }

        static void Main(string[] args)
        {
            GrammarLibClient client = new GrammarLibClient();
            client.Startup();
        }
    }
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
            SymbolStream symbolIter = new SymbolStream(new List<Terminal> { });
            Grammar grammar = DefaultGrammarFactory.CreateFromFile(@"Path to grammar");
            CFG cfg = (CFG)grammar;
            Console.WriteLine($"First:");
            cfg.GetFirstSetOfNonterminals().Print();
            Console.WriteLine($"FirstSet:");
            cfg.GetFirstSetOfStructure().Print();
            Console.WriteLine($"Follow");
            cfg.GetFollow().Print();
            Console.WriteLine("LRTable");
            var lrTable = cfg.GetLRTable();
            lrTable.Print();
            Console.WriteLine("lritems");
            var items = cfg.GetItemsSet();
            items.Print();
            LRParser lRParser = new LRParser(cfg);
            SymbolStream lrInput = DefaultSymbolStreamFactory.CreateFromStr(cfg, "bcd");
            lRParser.Init(lrInput);
            do
            {
                lRParser.Walk();
                lRParser.PrintState();
            } while (lRParser.GetState() == ParserState.Unfinished);
            var atl = lRParser.GetParseResult();
            if (atl != null)
            {
                atl.Print();
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
        public static void SetupEnv()
        {
            string pwd = System.Environment.CurrentDirectory;
            string path = pwd.Substring(0, pwd.IndexOf("CLK") + "CLK".Length);
            System.Environment.SetEnvironmentVariable("CLK_HOME", path);
        }
        // takenReader实列
        public static void TakenReaderUsage()
        {
            ITokenReader takenReader = TokenReaderFactory.GetFromFile(Environment.
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
            EnumerableWord sampleLexer = new DemoInterpreter.DemoLexer.EnumerableWord(fileName);
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
            EnumerableWord sampleLexer = new DemoInterpreter.DemoLexer.EnumerableWord(fileName);
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
        /*   static void Main(string[] args)
           {
               //SetupEnv();
               //GrammarDSUsage();
               //SampleSyntaxTest();
               //GrammarTest();
               //SetupKeyFile();
               //TakenReaderUsage();
               //SampleLexerAsIter(@"C:\Users\sam\source\repos\CLK\LexicalCore\TakenReader.cs");
               //SampleLexerUsage(@"C:\Users\sam\source\repos\CLK\LexicalCore\TakenReader.cs");
           }*/

    }
}
