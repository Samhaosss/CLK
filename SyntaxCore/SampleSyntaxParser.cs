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
        private Taken lastWord;
        public SampleSyntaxParser()
        {
        }
        private void NextWord()
        {
            lastWord = sampleLexer.Next();
        }
        public void Parse(char[] data)
        {
            sampleLexer = SampleLexer.Parse(data);
            NextWord();
            Console.WriteLine($"Result: {ProcE()}");
        }
        private int ProL()
        {
            int el = ProcE();
            return el;
        }
        public int ProcE()
        {
            int es, mi, ts;
            ts = ProcT();
            mi = ts;
            es = ProcM(mi);
            return es;
        }
        private int ProcM(int mi)
        {
            int ts = 0;
            if (lastWord.Type == TakenType.Op && lastWord.Value.Equals("+"))
            {
                NextWord(); //消耗了一个单词 继续获取
                ts = ProcT();
                mi += ts;
                return ProcM(mi);
            }
            else
            {
                return mi;
            }
        }
        public int ProcT()
        {
            int ni = 0;
            ni = ProcF();
            return ProcN(ni);
        }
        int ProcN(int ni)
        {
            if (lastWord.Type == TakenType.Op && lastWord.Value.Equals("*"))
            {
                NextWord();
                ni *= ProcF();
                return ProcN(ni);
            }
            return ni;
        }
        int ProcF()
        {
            int fs = 0;
            if (lastWord.Type == TakenType.DelimiterChars && lastWord.Value.Equals("("))
            {
                NextWord();
                fs = ProcE();
                if (!(lastWord.Type == TakenType.DelimiterChars && lastWord.Value.Equals(")")))
                {
                    Console.Error.WriteLine($"括号不匹配:{lastWord.RowNo}:{lastWord.ColNo}");
                    throw new System.NotImplementedException("Error Handling");
                }
                NextWord();
                return fs;

            }
            else if (lastWord.Type == TakenType.IntegerLiteral)
            {
                int tmp = int.Parse(lastWord.Value);
                NextWord();
                return tmp;
            }
            else if (lastWord.Type == TakenType.FloatLiteral)
            {
                int tmp = (int)float.Parse(lastWord.Value); ;
                NextWord();
                return tmp;
            }
            else
            {

                Console.Error.WriteLine($"表达式不符合文法定义:{lastWord.RowNo}:{lastWord.ColNo}");
                throw new System.NotImplementedException("Error Handling");
            }
        }
    }
}
