using CLK.GrammarCore.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
/*
 *  之前对文法符号、文法单元、产生式、文法的定义都是一种抽象的表达，这里的各个相应的类场实现了某种定义规范 如对文法非终结符要求以 大写字符开头
 *  基本运行稳定
 * **/
namespace CLK.GrammarCore.Factory
{
    // 通过串构建各类文法
    // 由于串的模式比较简单，因此不适用单独的语法分析器来解析 而使用固定的方式
    // 串模式:
    //  Structure => Structure | [ ... ]    [\]
    // 类厂目前无状态 所以所有的方法都是静态方法
    /// <summary>
    ///  通过串创建文法的一种默认实现，定义自然语言描述如下:
    ///  文法由多个文法产生式构成， 一个文法产生式可以由多行构成，未结束前用 '\' 分割
    ///  一个文法产生式 由左部文法单元和多个右部文法单元和分割符构成，分割为"=>"，该内容中间不可包含空格，多个右部文法单元由'|'分割
    ///  一个文法单元由多个终结符和非终结构成，文法符号之间通过空格分割
    ///  一个非终结符是以大写英文字母开头的串，终结符是不能只包含单个'$的串,使用'^'表示空
    /// </summary>
    public class DefaultGrammarFactory
    {

        // 这里定义了文法中包含的一些特殊字符
        public static string defaultLeftRigtSp = "=>";   //左右部文法单元的分割
        public static char defaultSymbolSp = ' ';  // 文法符号之间的分隔符 
        public static char defaultStructureSp = '|';     // 文法单元之间的分割符
        public static string defaultUnfinishedLineFlag = @"\";   //行未结束标志
        public static char[] invalidCh = { ' ', '\n', '\r', '\t' };
        /// <summary>
        /// 从文件读取文法
        /// </summary>
        /// <param name="fileName">文件路径</param>
        /// <returns>返回文法</returns>
        public static Grammar CreateFromFile(string fileName)
        {
            string rowInput = System.IO.File.ReadAllText(fileName);
            return CreateFromStr(rowInput);
        }
        public static Grammar CreateFromChArray(char[] rowData)
        {
            return CreateFromStr(new string(rowData));
        }
        public static CFG CreateCFGFromFile(string fileName)
        {
            return (CFG)CreateFromFile(fileName);
        }
        public static CFG CreateCFGFromStR(string rowData)
        {
            return (CFG)CreateFromStr(rowData) as CFG;
        }

        //想了想还是这么处理比较简单
        public static Grammar CreateFromStr(string rowData)
        {
            // 首先判断输入串是否值得分析
            if (rowData == null) { throw new ArgumentNullException("构造文法的输出串为空"); }
            rowData = rowData.Trim(invalidCh);
            if (rowData.Count() == 0)
            {
                throw new ArgumentException("构建文法的输入串不包含有效字符");
            }
            // 必须转为list才能真正迭代str
            char[] de = new char[] { '\n', '\r', '\t' };
            //注意这里不能去掉空格
            var lines = rowData.Split('\n').Select(x => x.Trim(invalidCh)).ToList();    //无论那种操作系统 换行前的最后一个字符均为/n
            var itr = lines.GetEnumerator();
            itr.MoveNext();//初始化迭代器
            List<GrammarProduction> productions = new List<GrammarProduction>();
            int lineNo = 0;
            int sentenceNo = 1;
            // 下面的构建 依赖了structure的从串构建方法并没有依赖production的从串构建方法 因为后者当初设计考虑较少
            do
            {
                lineNo++;
                // 文法符号
                // 下面分析每一行
                // 首先合并多个以 \ 结尾的串， 然后判断一个完整的串是否包含左右部分隔符 进而构建各个文法单元;
                string sentence = "";
                while ((sentence += itr.Current).EndsWith(defaultUnfinishedLineFlag))
                {
                    if (!itr.MoveNext())
                    {
                        throw new ArgumentException(@"构造文法的输出串不能以 '\' 结束，否则被认为当前产生式未完成, 行数:" + lineNo);
                    }
                    sentence = sentence.Remove(sentence.Count() - 1);  //去掉最后的行未结束标志
                    sentence += " "; //添加一个空格代替行未结束标志 认为换行后的内容绝对不属于同一个文法符号
                    lineNo += 1;
                }
                if (sentence.Contains(defaultUnfinishedLineFlag))
                {
                    throw new ArgumentException($"构造产生式的语句包含了用于标识行未结束的符号:{defaultUnfinishedLineFlag},sentenceNo:{sentenceNo}\n:\r{sentence}\n");
                }
                // 到了这里就合理的分开了各个句子 接下来处理句子的逻辑委托给创建产生式
                try
                {
                    productions.Add(DefaultProductionFactory.CreateFromStr(sentence));
                }
                catch (ArgumentNullException e)
                {
                    throw new ArgumentNullException($"{e.Message},sentenceNo:{sentenceNo},lineNo:{lineNo} :\n{sentence}");
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException($"{e.Message},sentenceNo:{sentenceNo},lineNo:{lineNo} :\n{sentence}");
                }
                ++sentenceNo;
            } while (itr.MoveNext());
            Grammar result = null;
            switch (AnalyseGType(productions))
            {
                case GrammarType.ZeroType:
                    result = new Grammar(productions);
                    break;
                case GrammarType.ContextSensitive:
                    result = new CSG(productions);
                    break;
                case GrammarType.ContextFree:
                    result = new CFG(productions);
                    break;
                case GrammarType.Regular:
                    result = new RG(productions);
                    break;

            };
            return result;
        }
        private static int SubStrTimes(string origin, string sub)
        {
            int count = 0;
            int startIndex = 0;
            while ((startIndex = origin.IndexOf(sub, startIndex)) != -1)
            {
                startIndex += sub.Count();
                if (startIndex == origin.Count()) { break; }
                count++;
            }
            return count;
        }
        // 这里的输入实际上可以是不合法的文法 此过程只是判断左友部文法单元长度关系
        private static GrammarType AnalyseGType(List<GrammarProduction> productions)
        {
            GrammarType grammarType = GrammarType.ZeroType;
            if (productions.All(x => x.LeftStructure.Length() == 1))
            {
                if (productions.All(x => x.RightStructures.All(y => y.IsSatisfyRG())))
                {
                    grammarType = GrammarType.Regular;
                }
                else
                {
                    grammarType = GrammarType.ContextFree;
                }
            }
            else
            {
                if (productions.All(X => X.RightStructures.All(y => y.Length() <= X.LeftStructure.Length())))
                {
                    grammarType = GrammarType.ContextSensitive;
                }
                else
                {
                    grammarType = GrammarType.ZeroType;
                }
            }
            return grammarType;
        }
    }


    public class DefaultProductionFactory
    {
        public static string defaultLeftRightSp = "=>";
        public static char defaultStructureSp = '|';
        /// <summary>
        /// 从串创建产生式，产生式形式  [ GrammarStructure ] => [GrammarStructure] | ...
        /// </summary>
        /// <param name="rowData"></param>
        /// <returns></returns>
        public static GrammarProduction CreateFromStr(string rowData)
        {
            if (rowData == null)
            {
                throw new ArgumentNullException();
            }
            rowData = rowData.Trim(DefaultSymbolFactory.invalidCh);
            if (rowData.Count() == 0)
            {
                throw new ArgumentException("用于构建文法产生式的串不包含有效字符");
            }
            int index;
            // 如果包含多次 => 或一次也不包含 
            if (SubStrTimes(rowData, defaultLeftRightSp, out index) != 1)
            {
                throw new ArgumentException($"用于构建文法产生式的串不包含或包含多次左右部分割符:{defaultLeftRightSp}");
            }
            string[] sp = new string[] { defaultLeftRightSp };
            //这里很烦 c#提供的api不支持直接按照字符串分割
            //sp, StringSplitOptions.RemoveEmptyEntries
            var strcs = rowData.Split(sp, StringSplitOptions.RemoveEmptyEntries);
            // 防止不包含右部或右部文法单元
            if (strcs.Length != 2)
            {
                throw new ArgumentException($"构造产生式的语句格式不正确:{rowData}");
            }
            GrammarStructure left = DefaultStructureFactory.CreateFromStr(strcs[0]);
            var rightStr = strcs[1].Split(defaultStructureSp).Select(x => x.Trim(DefaultGrammarFactory.invalidCh)).ToList();
            if (rightStr.Any(X => X.Equals("")))
            {
                throw new ArgumentException($"构造产生式的语句中，分隔符之间必须包含有效符号:{rowData}");
            }

            List<GrammarStructure> right = rightStr.Select(x => DefaultStructureFactory.CreateFromStr(x)).ToList();
            return new GrammarProduction(left, new HashSet<GrammarStructure>(right));
        }
        private static int SubStrTimes(string origin, string sub, out int index)
        {
            int count = 0;
            int startIndex = 0;
            while ((startIndex = origin.IndexOf(sub, startIndex)) != -1)
            {
                startIndex += sub.Count();
                if (startIndex == origin.Count()) { break; }
                count++;
            }
            index = startIndex;
            return count;
        }
    }
    public class DefaultStructureFactory
    {
        public static char[] defaultLIllegalCh = new char[] { DefaultGrammarFactory.defaultStructureSp };
        public static char[] defaultSymbolSp = new char[] { ' ', '\r', '\t' };
        /// <summary>
        /// 将使用默认的文法符号定义创建文法单元，文法符号之间以空格分割
        /// </summary>
        public static GrammarStructure CreateFromStr(string rowData)
        {
            if (rowData == null) { throw new ArgumentNullException("用于构建文法单元的串不可为Null"); }
            rowData = rowData.Trim(DefaultSymbolFactory.invalidCh);
            if (rowData.Count() == 0)
            {
                throw new ArgumentException("用于构建文法单元的串不包含有效字符");
            }
            var structure = new List<GrammarSymbol>();
            var symbols = rowData.Split(defaultSymbolSp).Where(x => !x.Equals(""));
            foreach (var sym in symbols)
            {
                structure.Add(DefaultSymbolFactory.CreateSymbol(sym));
            }
            return new GrammarStructure(structure);
        }
    }
    public class DefaultSymbolFactory
    {
        /// <summary>
        /// 空格用于分割文法符号，$用于内部,|用于分割文法单元, \用于标识行未结束
        /// </summary>
        public static char[] defaultIllegalCh = new char[] { ' ', '$', '|', '\\' };
        public static char[] invalidCh = { ' ', '\n', '\r', '\t' };

        private static Dictionary<string, Terminal> terminalsBuf = new Dictionary<string, Terminal>();
        private static Dictionary<string, Nonterminal> nonterminalBuf = new Dictionary<string, Nonterminal>();
        /// <summary>
        /// 通过串构建文法符号，如果串开头大写则创建非终结符，否则创建终结符
        /// 终结符不可包含非法字符
        /// </summary>
        public static GrammarSymbol CreateSymbol(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }
            value = value.Trim(invalidCh);
            if (value.Count() == 0)
            {
                throw new ArgumentException("用于构建文法符号的串不包含有效字符");
            }

            if (char.IsLetter(value[0]) && !char.IsLower(value[0]))
            {
                return CreateNonterminal(value);
            }
            else
            {
                return CreateTerminal(value);
            }
        }
        public static Terminal CreateTerminal(string value)
        {
            if (value != null && value.Count() == 1 && defaultIllegalCh.Contains(value[0]))
            {
                throw new ArgumentException($"用于构建终结符的串:{value}不能是: ' ', '$', '|', '\\'");
            }
            if (terminalsBuf.ContainsKey(value))
            {
                return terminalsBuf[value];
            }
            else
            {
                var result = new Terminal(value);
                terminalsBuf.Add(value, result);

                return result;
            }
        }
        public static Terminal CreateTerminal(char ch)
        {
            return CreateTerminal(ch.ToString());
        }
        public static Terminal CreateEmptyTerminal()
        {
            return Terminal.Empty;
        }
        public static Nonterminal CreateNonterminal(string value)
        {
            if (value != null && value.Count() != 0 && char.IsLower(value[0]))
            {
                throw new ArgumentException("用于构建非终结符的串必须以大写字符开头");
            }
            foreach (var ich in defaultIllegalCh)
            {
                if (value.Contains(ich))
                {
                    throw new ArgumentException("用于构建非终结符的串不可包含非法字符");
                }
            }
            if (nonterminalBuf.ContainsKey(value))
            {
                return nonterminalBuf[value];
            }
            else
            {
                var result = new Nonterminal(value);
                nonterminalBuf.Add(value, result);
            }
            return new Nonterminal(value);
        }
    }
    public class DFAFactory
    {
        // TODO:未来可能添加一些其他创建DFA的方式
    }

    /// <summary>
    /// 创建输入流，目前支持从文件、串获取
    /// 由于当前得实现允许长度大于一的终结符，因此输入串中终结符之间应当使用空格分离，否则无法创建输入流
    /// 如： 终结符集合为{ a, ad, c,d} , 当出现句子 aadccc时，无法判断应该分割为 [a ad c c c] 还是 [ a a d c c c]
    /// 如果终结符之间互相不为前缀则可以不以空格分割
    /// </summary>
    public class DefaultSymbolStreamFactory
    {
        public static SymbolStream CreateFromStr(CFG grammar, string input)
        {
            List<Terminal> stream = new List<Terminal>();
            if (grammar.Terminals.All(X => X.Value.Count() == 1))
            {
                foreach (var ch in input)
                {
                    Terminal terminal = DefaultSymbolFactory.CreateTerminal(ch);
                    if (!grammar.Terminals.Contains(terminal))
                    {
                        throw new ErrorCore.IllegalChException("输入流包含不存在于文法终结符集的字符");
                    }
                    stream.Add(terminal);
                }
                stream.Add(Terminal.End);
            }
            else
            {
                throw new System.NotImplementedException("未完成复杂输入流处理");
            }
            return new SymbolStream(stream);
        }
    }
}
