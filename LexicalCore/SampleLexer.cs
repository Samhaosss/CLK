using ErrorCore;
using System;
using System.Collections.Generic;
using System.Linq;
/*
 * LexicalCore的内容可能有：demo分析器、文法转dfa算法、dfa极小化
 * 这里实现了一个简单的词法分析，可以用于将来的demo解释器
 * **/
namespace CLK.LexicalCore
{
    public enum TakenType { Keyword, Id, Op, delimiterChars, Num, EOF };
    public class Taken
    {
        private TakenType type;
        private string value;
        private long rowNo;
        private long colNo;

        public Taken(TakenType type, string value, long rowNo, long colNo)
        {
            this.type = type;
            this.value = value;
            this.rowNo = rowNo;
            this.colNo = colNo;
        }

        public TakenType Type => type;
        public string Value => value;
        public long RowNo { get => rowNo; }
        public long ColNo { get => colNo; }
        public override string ToString() => "[Type=>\"" + type.ToString() + "\"  Value=>\"" + value + "\"]";
    }


    public class SampleLexer
    {
        // 这里应该使用json 方便起见直接硬编码
        private static List<char> delimiterChars = new List<char> { ',', ';', '{', '}', '[', ']', '(', ')' };
        private static List<char> blankChars = new List<char> { ' ', '\r', '\n', '\t' };
        // 这里列举了支持复合运算的运算符 还有部分未列出
        private static List<char> unaryOperator = new List<char> { '+', '-', '*', '/', '=', '&', '|', '!', '%', '>', '<' };
        private static List<string> keywordsList;
        private const string keywordFileName = "keywords";
        /*
         * keyword文件应该和可执行文件同一目录
         * **/
        static SampleLexer()
        {
            var path = Environment.GetEnvironmentVariable("CLK_HOME") + @"\GlobalConfig\" + keywordFileName;
            var spliter = new char[3] { ' ', '\r', '\n', };
            keywordsList = System.IO.File.ReadAllText(path).    //判断文件由ReadAllText执行，这里可能抛异常
                           Split(spliter, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim(spliter))
                           .ToList();
        }
        private SampleInterpreterError errorHandler = SampleInterpreterError.GetSampleInterpreterError();
        //分析器相关
        private TakenReader takenReader;
        private uint rowNo;
        private uint colNo;
        private bool isFinish;
        private char? lastCh;

        public uint ColNo { get => colNo; }
        public uint RowNo { get => rowNo; }
        /*
         * 创建lexer的流程：
         *      加载文件中的关键字，如果文件不存在抛异常
         *      创建takenReader，如果目标文件不存在或存在权限问题，抛异常
         *      初始化行列，从文件预读一个char
         * **/
        public SampleLexer(string targetFile)
        {
            takenReader = new TakenReader(targetFile);
            colNo = rowNo = 1;
            WrapRead();
        }

        private bool WrapRead()
        {
            isFinish = !(lastCh = takenReader.next()).HasValue;
            return isFinish;
        }

        private Taken WrapTaken(TakenType takenType, string value)
        {
            return new Taken(takenType, value, rowNo, colNo);
        }
        private bool IsInRange(char tar, char start, char end)
        {
            return (tar >= start) && (tar <= end);
        }
        private Taken AnalyzeKeywordOrId()
        {
            while (true)
            {
                WrapRead();
                if (!isSatisfyKeywordOrId() || isFinish)
                {
                    var word = takenReader.GetWord(isFinish);
                    return keywordsList.Contains(word) ? WrapTaken(TakenType.Keyword, word) : WrapTaken(TakenType.Id, word);
                }
            }
        }
        private Taken AnalyzeNum()
        {
            bool hasDot = false;
            while (true)
            {
                WrapRead();
                if (lastCh.Equals('.') && !hasDot)
                {
                    hasDot = true;
                }
                else if (!IsSatisfyNum() || isFinish)
                {
                    return WrapTaken(TakenType.Num, new string(takenReader.GetWord(isFinish).ToArray()));
                }
            }
        }
        private Taken AnalyzeOp()
        {
            // 不管哪种情况 都需要向前预读
            var tmp = lastCh;
            WrapRead();
            if (tmp.Equals('.'))
            {
                return WrapTaken(TakenType.Op, takenReader.GetWord(isFinish));
            }
            else if (lastCh.Equals('='))
            {
                WrapRead();
                return WrapTaken(TakenType.Op, takenReader.GetWord(isFinish));
            }
            else
            {
                return WrapTaken(TakenType.Op, takenReader.GetWord(isFinish));
            }
        }
        public Taken analyze()
        {
            while (!isFinish)
            {
                if (IsInRange(lastCh.Value, 'a', 'z') || IsInRange(lastCh.Value, 'A', 'Z') || lastCh.Equals('_'))
                {
                    return AnalyzeKeywordOrId();
                }
                else if (IsSatisfyNum())
                {
                    return AnalyzeNum();
                }
                else if (unaryOperator.Contains(lastCh.Value) || lastCh.Equals('.'))
                {
                    return AnalyzeOp();
                }
                else if (delimiterChars.Contains(lastCh.Value))
                {
                    var tmp = WrapTaken(TakenType.delimiterChars, takenReader.GetWord(isFinish));
                    WrapRead();
                    return tmp;
                }
                else if (blankChars.Contains(lastCh.Value))
                {
                    if (lastCh.Equals('\n'))
                    {
                        rowNo++; colNo = 1;
                    }
                    else if (lastCh.HasValue)
                    {
                        colNo++;
                    }
                    WrapRead();
                    takenReader.pass(isFinish);
                }
                else
                {
                    errorHandler.addError($"Illegal Character:{lastCh},At {rowNo}:{colNo}");
                    WrapRead();
                    takenReader.pass(isFinish);
                }
            }
            return WrapTaken(TakenType.EOF, takenReader.GetWord(isFinish));
        }

        private bool isSatisfyKeywordOrId()
        {
            return IsInRange(lastCh.Value, 'a', 'z') || IsInRange(lastCh.Value, 'A', 'Z') ||
                lastCh.Equals('_') || IsInRange(lastCh.Value, '0', '9');
        }
        private bool IsSatisfyNum()
        {
            return IsInRange(lastCh.Value, '0', '9');
        }

    }
}
