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
        public enum TakenType { Keyword, Id, Op, DelimiterChars, InternalFunction, Num, StringLiteral, IntegerLiteral, FloatLiteral, EOF };
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
                string configFilePath = GlobalConfig.ConfigManager.GetConfigFilePath() + System.IO.Path.DirectorySeparatorChar
                    + wordSetFileName;
                string json = JsonConvert.SerializeObject(special, Formatting.Indented);
                Console.Write(json);
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
            private List<string> internalFunction;

            public List<char> DelimiterChars { get => delimiterChars; set => delimiterChars = value; }
            public List<char> BlankChars { get => blankChars; set => blankChars = value; }
            public List<char> UnaryOperator { get => unaryOperator; set => unaryOperator = value; }
            public List<string> KeywordsList { get => keywordsList; set => keywordsList = value; }
            public List<string> InternalFunction { get => internalFunction; set => internalFunction = value; }

            // 也可以使用这里的默认值
            public void Stepup()
            {
                delimiterChars = new List<char> { ',', ';', '{', '}', '[', ']', '(', ')' };
                internalFunction = new List<string> { "Print", "Input" };
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
            public static EnumerableWord Parse(string fileName)
            {
                return new EnumerableWord(fileName);
            }
            public static EnumerableWord Parse(char[] data)
            {
                return new EnumerableWord(data);
            }
        }
        // 实现了IEnumerable，可以当作迭代器使用
        // 目前看来识别非常准确
        public class EnumerableWord : IEnumerable
        {
            private static SpecialWordSet wordSet = WordSetFactory.GetWordSet();
            private static SampleInterpreterError errorHandler = SampleInterpreterError.GetSampleInterpreterError();
            //分析器相关
            private ITokenReader takenReader;
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
                takenReader = TokenReaderFactory.GetFromFile(fileName);
                WrapRead();
            }
            public EnumerableWord(char[] data)
            {
                takenReader = TokenReaderFactory.GetFromByteStream(data);
                WrapRead();
            }

            private bool WrapRead()
            {
                isFinish = !(lastCh = takenReader.Next()).HasValue;
                if (!isFinish)
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
                    WrapRead();
                    // 这里顺序不能反， 因为如果结束lastch则为null
                    if (isFinish || !IsSatisfyKeywordOrId())
                    {
                        var word = takenReader.GetWord(isFinish);
                        var type = TakenType.Id;
                        if (wordSet.KeywordsList.Contains(word))
                        {
                            type = TakenType.Keyword;
                        }
                        else if (wordSet.InternalFunction.Contains(word))
                        {
                            type = TakenType.InternalFunction;
                        }

                        return WrapTaken(type, word);
                    }
                }
            }
            private Taken AnalyzeNum()
            {
                bool hasDot = false;
                while (true)
                {
                    WrapRead();
                    if (isFinish)
                    {
                        var type = hasDot ? TakenType.FloatLiteral : TakenType.IntegerLiteral;
                        return WrapTaken(type, takenReader.GetWord(isFinish));
                    }
                    else if (lastCh.Equals('.') && !hasDot)
                    {
                        hasDot = true;
                    }
                    else if (!IsSatisfyNum())
                    {
                        var type = hasDot ? TakenType.FloatLiteral : TakenType.IntegerLiteral;
                        return WrapTaken(type, takenReader.GetWord(false));
                    }
                }
            }
            private Taken AnalyzeOp()
            {
                if (lastCh.Equals('.'))
                {
                    var result = WrapTaken(TakenType.Op, takenReader.GetWord(true));
                    WrapRead();
                    return result;
                }
                WrapRead();
                if (isFinish)
                {
                    return WrapTaken(TakenType.Op, takenReader.GetWord(true));
                }
                else if (lastCh.Equals('='))
                {
                    var op = WrapTaken(TakenType.Op, takenReader.GetWord(true));
                    WrapRead();
                    return op;
                }
                else
                {
                    return WrapTaken(TakenType.Op, takenReader.GetWord(isFinish));
                }
            }
            public Taken Next()
            {
                while (!isFinish)
                {
                    // 识别标识符、关键字、内部函数
                    if (IsInRange(lastCh.Value, 'a', 'z') || IsInRange(lastCh.Value, 'A', 'Z') || lastCh.Equals('_'))
                    {
                        return AnalyzeKeywordOrId();
                    }
                    // 识别整型 浮点型
                    else if (IsSatisfyNum())
                    {
                        return AnalyzeNum();
                    }
                    // 识别运算符
                    else if (wordSet.UnaryOperator.Contains(lastCh.Value) || lastCh.Equals('.'))
                    {
                        return AnalyzeOp();
                    }
                    // 识别字符串字面值
                    else if (lastCh.Equals('"'))
                    {
                        throw new NotImplementedException("未完成字符串字面值识别");
                    }
                    // 识别分隔符
                    else if (wordSet.DelimiterChars.Contains(lastCh.Value))
                    {
                        // 应该先读？还是先输出？ 设置为true 因为分隔符均为一个长度
                        var tmp = WrapTaken(TakenType.DelimiterChars, takenReader.GetWord(true));
                        WrapRead();//读一个字符 供下次处理
                        return tmp;
                    }
                    // 空白字符
                    else if (wordSet.BlankChars.Contains(lastCh.Value))
                    {
                        if (lastCh.Equals('\n'))
                        {
                            rowNo++; colNo = 1;
                        }
                        /* else if (lastCh.HasValue)
                         {
                             colNo++;
                         }*/
                        takenReader.Pass(true);
                        WrapRead();
                    }
                    // 非法字符
                    else
                    {
                        errorHandler.AddError($"Illegal Character:{lastCh},At {rowNo}:{colNo}");
                        takenReader.Pass(true);
                        WrapRead();
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
                while ((current = Next()).Type != TakenType.EOF)
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
