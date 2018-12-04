using CLK.LexicalCore.DemoLexer;
using System;

/*
 * 语法core，目前想到的有：demo语法分析器、first集、follow集、分析表生成算法
 * **/
namespace CLK.SyntaxCore
{
    // 翻译目标 表达式计算
    public class SampleSyntaxParser
    {
        private EnumerableWord sampleLexer;
        public SampleSyntaxParser()
        {
        }

        public void Parse(char[] data)
        {
            sampleLexer = SampleLexer.parse(data);
            Console.WriteLine($"Result: {ProcE()}");
        }
        public int ProcE()
        {
            int es = 0;
            es = ProcT();
            if (sampleLexer.Analyze().Value.Equals('+'))
            {
                es += ProcE();
            }
            return es;
        }
        public int ProcT()
        {
            int ts = 0;
            ts = ProcF();
            if (sampleLexer.Analyze().Value.Equals('*'))
            {
                ts *= ProcT();
            }

            return ts;
        }
        int ProcF()
        {
            Taken tmp = sampleLexer.Analyze();
            int fs = 0;

            if (tmp.Value.Equals('('))
            {
                fs = ProcE();
                if (!sampleLexer.Analyze().Value.Equals(')'))
                {
                    throw new System.NotImplementedException("Error Handling");
                }

            }
            else if (tmp.Type == TakenType.Num)
            {
                fs = int.Parse(tmp.Value);
            }
            else
            {
                throw new System.NotImplementedException("Error Handling");
            }
            return fs;
        }
    }
}
