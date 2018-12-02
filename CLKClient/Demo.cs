using CLK.LexicalCore;
using System.Collections.Generic;
using System.Linq;
/*
 * 将来这个模块应该被改为winform程序，目前只是控制台应用程序，仅仅用于展示库用法
 * **/
namespace CLK.Client
{
    class LexerLibUsageDemo
    {
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
            TakenReader takenReader = new TakenReader(System.Environment.
                GetEnvironmentVariable("CLK_HOME") + @"\GlobalConfig\keywords"); ;
            List<string> takens = new List<string>();
            while (takenReader.hasNext())
            {
                var ch = takenReader.next();
                if (ch.Equals('\n'))
                {
                    takens.Add(takenReader.GetWord());
                }
            }
            takens.Add(new string(takenReader.GetWord().ToArray()));
            takens.ForEach(element => System.Console.Write($"{element}"));
        }
        // lexer实例
        public static void SampleLexerUsage(string fileName)
        {
            SampleLexer sampleLexer = new LexicalCore.SampleLexer(fileName);
            ErrorCore.SampleInterpreterError sampleInterpreterError = ErrorCore.SampleInterpreterError.GetSampleInterpreterError();
            long lastLine = 1;
            while (true)
            {
                var taken = sampleLexer.analyze();
                if (taken.RowNo != lastLine)
                {
                    System.Console.WriteLine($"    =>Line:{lastLine}");
                    lastLine = taken.RowNo;
                }
                switch (taken.Type)
                {
                    case TakenType.EOF:
                        System.Console.Write(taken);
                        sampleInterpreterError.reportError();
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
            TakenReaderUsage();
            SampleLexerUsage(@"C:\Users\sam\source\repos\CLK\LexicalCore\TakenReader.cs");
        }
    }
}
