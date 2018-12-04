using ErrorCore;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
/*
 * LexicalCore的内容可能有：demo分析器、文法转dfa算法、dfa极小化
 * 这里实现了一个简单的词法分析，可以用于将来的demo解释器
 * **/
namespace CLK.LexicalCore
{
    namespace DemoLexer
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

        public class WordSetFactory
        {

            private static string wordSetFileName = "DemoLexerWords.json";
            public static void SerializeWordSet()
            {
                var special = new LexicalCore.DemoLexer.SpecialWordSet();
                special.Stepup();
                string configFilePath = GlobalConfig.ConfigManager.GetConfigFilePath()
                    + wordSetFileName;
                string json = JsonConvert.SerializeObject(special, Formatting.Indented);
                System.IO.File.WriteAllText(configFilePath, json);
            }
            public static SpecialWordSet GetWordSet()
            {
                string wordFile = GlobalConfig.ConfigManager.GetConfigFilePath() +
                    System.IO.Path.DirectorySeparatorChar + wordSetFileName;
                string json = System.IO.File.ReadAllText(wordFile);
                return JsonConvert.DeserializeObject<SpecialWordSet>(json);
            }
        }

        // 这个类应该从json创建
        public class SpecialWordSet
        {
            private List<char> delimiterChars;
            private List<char> blankChars;
            // 这里列举了支持复合运算的运算符 还有部分未列出
            private List<char> unaryOperator;
            private List<string> keywordsList;
            public List<char> DelimiterChars { get => delimiterChars; set => delimiterChars = value; }
            public List<char> BlankChars { get => blankChars; set => blankChars = value; }
            public List<char> UnaryOperator { get => unaryOperator; set => unaryOperator = value; }
            public List<string> KeywordsList { get => keywordsList; set => keywordsList = value; }

            // 也可以使用这里的默认值
            public void Stepup()
            {
                delimiterChars = new List<char> { ',', ';', '{', '}', '[', ']', '(', ')' };
                blankChars = new List<char> { ' ', '\r', '\n', '\t' };
                unaryOperator = new List<char> { '+', '-', '*', '/', '=', '&', '|', '!', '%', '>', '<' };
                var path = Environment.GetEnvironmentVariable("CLK_HOME") + @"\GlobalConfig\" + "keywords";
                var spliter = new char[3] { ' ', '\r', '\n', };
                keywordsList = System.IO.File.ReadAllText(path).    //判断文件由ReadAllText执行，这里可能抛异常
                             Split(spliter, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim(spliter))
                           .ToList();
            }
        }


        public class SampleLexer
        {
            public static EnumerableWord parse(string fileName)
            {
                return new EnumerableWord(fileName);
            }
            public static EnumerableWord parse(char[] data)
            {
                return new EnumerableWord(data);
            }
        }
        // 实现了IEnumerable，可以当作迭代器使用
        public class EnumerableWord : IEnumerable
        {
            private static SpecialWordSet wordSet = WordSetFactory.GetWordSet();
            private static SampleInterpreterError errorHandler = SampleInterpreterError.GetSampleInterpreterError();
            //分析器相关
            private ITakenReader takenReader;
            private uint rowNo = 1;
            private uint colNo = 1;
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
            public EnumerableWord(string fileName)
            {
                takenReader = TakenReaderFactory.GetFromFile(fileName);
                WrapRead();
            }
            public EnumerableWord(char[] data)
            {
                takenReader = TakenReaderFactory.GetFromByteStream(data);
                WrapRead();
            }

            private bool WrapRead()
            {
                isFinish = !(lastCh = takenReader.Next()).HasValue;
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
                    if (!IsSatisfyKeywordOrId() || isFinish)
                    {
                        var word = takenReader.GetWord(isFinish);
                        return wordSet.KeywordsList.Contains(word) ? WrapTaken(TakenType.Keyword, word) : WrapTaken(TakenType.Id, word);
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
            public Taken Analyze()
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
                    else if (wordSet.UnaryOperator.Contains(lastCh.Value) || lastCh.Equals('.'))
                    {
                        return AnalyzeOp();
                    }
                    else if (wordSet.DelimiterChars.Contains(lastCh.Value))
                    {
                        var tmp = WrapTaken(TakenType.delimiterChars, takenReader.GetWord(isFinish));
                        WrapRead();
                        return tmp;
                    }
                    else if (wordSet.BlankChars.Contains(lastCh.Value))
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
                        takenReader.Pass(isFinish);
                    }
                    else
                    {
                        errorHandler.AddError($"Illegal Character:{lastCh},At {rowNo}:{colNo}");
                        WrapRead();
                        takenReader.Pass(isFinish);
                    }
                }
                return WrapTaken(TakenType.EOF, takenReader.GetWord(isFinish));
            }

            private bool IsSatisfyKeywordOrId()
            {
                return IsInRange(lastCh.Value, 'a', 'z') || IsInRange(lastCh.Value, 'A', 'Z') ||
                    lastCh.Equals('_') || IsInRange(lastCh.Value, '0', '9');
            }
            private bool IsSatisfyNum()
            {
                return IsInRange(lastCh.Value, '0', '9');
            }

            public IEnumerator GetEnumerator()
            {
                Taken current;
                while ((current = Analyze()).Type != TakenType.EOF)
                {
                    yield return current;
                }
            }
        }
    }
    namespace GlobalConfig
    {
        public class ConfigManager
        {
            public static string CLK_HOME;
            public static void SetupGlabalConfiguation()
            {
                string pwd = System.Environment.CurrentDirectory;
                string path = pwd.Substring(0, pwd.IndexOf("CLK") + "CLK".Length);
                CLK_HOME = path;
                System.Environment.SetEnvironmentVariable("CLK_HOME", path);
            }
            public static string GetConfigFilePath()
            {
                return System.Environment.GetEnvironmentVariable("CLK_HOME") + System.IO.Path.DirectorySeparatorChar + "GlobalConfig";
            }
        }
    }
}
