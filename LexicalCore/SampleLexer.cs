using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public override bool Equals(object obj)
        {
            var taken = obj as Taken;
            return taken != null &&
                   type == taken.type &&
                   value == taken.value;
        }

        public override int GetHashCode()
        {
            var hashCode = 1148455455;
            hashCode = hashCode * -1521134295 + type.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(value);
            return hashCode;
        }
        public override string ToString() => "[Type=>" + type.ToString() + "  Value=>" + value + "]";
    }


    public class SampleLexer
    {
        private TakenReader takenReader;
        private uint rowNo;
        private uint colNo;
        private List<string> keywordsList;
        private const string keywordFileName = "keywords";
        private bool isFinish;
        private char? lastCh;
        // ' ', ',', ';', '\r', '\t'
        private static List<char> delimiterChars = new List<char> { ';' };
        // 这里列举了支持复合运算的运算符 还有部分未列出
        private static List<char> unaryOperator = new List<char> { '+', '-', '*', '/', '=', '&', '|', '!', '%', '>', '<' };

        public uint ColNo { get => colNo; }
        public uint RowNo { get => rowNo; }

        public SampleLexer(string targetFile)
        {
            // ADD keyword 
            ReadKeywords();
            takenReader = new TakenReader(targetFile);
            colNo = rowNo = 1;
            WrapRead();
        }
        private void ReadKeywords()
        {
            var path = Environment.CurrentDirectory + @"\" + keywordFileName;
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"File:{path} does not exist");
            }
            keywordsList = new List<string>();
            var keywords = System.IO.File.ReadAllText(path);
            var spliter = new char[3] { ' ', '\r', '\n', };
            foreach (var key in keywords.Split(spliter, StringSplitOptions.RemoveEmptyEntries))
            {
                keywordsList.Add(key.Trim(spliter));
                //Console.Write(key.Trim(spliter) + ";");
            }

        }
        private bool WrapRead()
        {
            isFinish = !(lastCh = takenReader.next()).HasValue;
            if (lastCh.Equals('\n'))
            {
                rowNo++; colNo = 1;
            }
            else
            {
                colNo++;
            }
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
                if (!WrapRead())
                {
                    if (!isSatisfyKeywordOrId())
                    {
                        var word = takenReader.GetWord();
                        return keywordsList.Contains(word) ? WrapTaken(TakenType.Keyword, word) : WrapTaken(TakenType.Id, word);
                    }
                }
                else
                {
                    var word = takenReader.GetWord(true);
                    return keywordsList.Contains(word) ? WrapTaken(TakenType.Keyword, word) : WrapTaken(TakenType.Id, word);
                }
            }
        }
        private Taken AnalyzeNum()
        {
            bool hasDot = false;
            while (true)
            {
                if (WrapRead())
                {
                    if (IsSatisfyNum()) { }
                    else if (lastCh.Equals('.') && !hasDot)
                    {
                        hasDot = true;
                    }
                    else
                    {
                        return WrapTaken(TakenType.Num, new string(takenReader.GetWord().ToArray()));
                    }
                }
                else
                {
                    return WrapTaken(TakenType.Num, new string(takenReader.GetWord(true).ToArray()));
                }
            }
        }
        private Taken AnalyzeOp()
        {
            // 不管哪种情况 都需要向前预读
            WrapRead();
            if (lastCh.Equals('.'))
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
                    WrapRead();
                    return WrapTaken(TakenType.delimiterChars, takenReader.GetWord(isFinish));
                }
                else
                {
                    WrapRead();
                    takenReader.pass();
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
