using ErrorCore;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
/*
*  这一部分目前完成基本非终结符、终结符、文法数据结构
*  并未完成所有需要的方法
*  文法创建：首先创建各个组成的structure,每个structure由终结符、非终结符组合而成
*  目前未提供更为简单的创建文法的方式，比较方便的是从串构建，随后可以考虑通过模板生成
*  不过内部的文法表示由文法的文法分析程序递归构建，需要手动构建文法的部分并不多
* **/

/// <summary>
/// 包含构建文法的基本数据结构: 终结符、非终结符、文法单元、单个产生式、文法。
/// 实现了部分文法相关算法:文法类别判断
/// 目前对所有数据结构都实现未内部不可变
/// </summary>
namespace CLK.GrammarCore
{

    /// <summary>
    /// 终结符 非终结符的枚举类型
    /// </summary>
    public enum SymbolType { Terminal, Nonterminal };
    /// <summary>
    /// 文法符号，终结符与非终结符的抽象基类，用于表示文法符号这一抽象概念
    /// </summary>
    public abstract class GrammarSymbol
    {
        /// <summary>
        /// 内部用于分割句子的字符 
        /// </summary>
        protected internal static char endSperValue = '$';
        /// <summary>
        /// 用于表示空的字符串
        /// </summary>
        protected internal static string EmptyTerminalValue = "^";
        protected readonly string value;
        /// <summary>
        /// 获取文法符号类别
        /// </summary>
        public abstract SymbolType GetSymbolType();
        // 抽象基类 不会被用户调用 
        public GrammarSymbol(string value)
        {
            this.value = value.Trim(' ');
        }
        // 存入hashtable需要的方法
        /// <summary>
        /// 用于判断文法符号是否相同，首先判断符号类型：终结符或非终结符，
        /// 相同类型的符号值也相同则返回true
        /// </summary>
        public override bool Equals(object obj)
        {
            //如果转换失败 直接抛出异常
            GrammarSymbol sym = (GrammarSymbol)obj;
            // 这样允许将非终结符与终结符比较 比较合理
            if (sym.GetSymbolType() != GetSymbolType())
            {
                return false;
            }
            return value.Equals(sym.value);
        }
        /// <summary>
        /// 根据符号值返回hashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var hashCode = 1927018180;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(value);
            return hashCode;
        }
        /// <summary>
        /// 返回文法符号值
        /// </summary>
        public override string ToString()
        {
            return value;
        }
    }
    /// <summary>
    /// 终结符,应该把所有的终结符的值视为串
    /// </summary>
    public class Terminal : GrammarSymbol
    {
        /*
         *  尽管空句子 在定义上不是一个终结符，但在内部将其实现为一个特殊的终结符会很方便，也不影响概念上的理解
         * **/
        /// <summary>
        /// 空字符
        /// </summary>
        public static Terminal Empty = new Terminal(EmptyTerminalValue);
        /// <summary>
        /// 语句分隔符
        /// </summary>
        public static Terminal End = new Terminal(endSperValue);
        /// <summary>
        /// 获取空字符 这个方法本不应该存在，要获取空，直接获取terminal类得静态成员 存在得原因是一开始得失误
        /// </summary>
        internal static Terminal GetEmpty()
        {
            return new Terminal(EmptyTerminalValue);
        }

        public string Value { get => value; }
        /// <summary>
        /// 通过串创建终结符, 不允许创建只包含$(内部使用)的terminal 
        /// </summary>
        public Terminal(string value) : base(value)
        {

        }
        /// <summary>
        /// 通过单个字符创建终结符
        /// </summary>
        /// <param name="value"></param>
        public Terminal(char value) : base(value.ToString())
        {
        }
        /// <summary>
        /// 判断是否未空字符
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            return value.Equals(EmptyTerminalValue);
        }
        public override SymbolType GetSymbolType()
        {
            return SymbolType.Terminal;
        }

    }
    // 将来可能拓展此类，加入属性，从而支持语法制导
    /// <summary>
    /// 对于非终结符，并没有实际的值，仅仅使用name标识
    /// </summary>
    public class Nonterminal : GrammarSymbol
    {
        /// <summary>
        /// 通过串创建非终结符, 此串仅仅用于标识
        /// </summary>
        public Nonterminal(string name) : base(name)
        {
        }
        public Nonterminal(char value) : base(value.ToString()) { }
        public string Value => value;


        public override SymbolType GetSymbolType()
        {
            return SymbolType.Nonterminal;
        }

    }

    /// <summary>
    /// 文法单元，如在文法:bA => aA|abA中， bA、aA、abA都是GrammarStructure,
    /// 此结构将作为产生式的组成,可被迭代查看每个文法符号
    /// </summary>
    public class GrammarStructure : IEnumerable
    {
        private List<GrammarSymbol> structure;
        private HashSet<Terminal> terminals;
        private HashSet<Nonterminal> nonterminals;

        /// <summary>
        ///  通过List构造文法单元,将去除list中的多余的空字符
        /// </summary>
        public GrammarStructure(List<GrammarSymbol> structure)
        {
            if (structure == null)
            {
                throw new System.ArgumentNullException("构建文法单元的文法符号列表不可为null");
            }
            else if (structure.Count == 0)
            {
                throw new System.ArgumentException("构建文法单元的文法符号列表不可为空");
            }
            //防止用户不小心修改外部list 导致structure内部结构被修改
            this.structure = new List<GrammarSymbol>(structure);
            //移除文法中多余的空 
            RemoveMutiEmpty();
            GenSymbol();
        }

        /// <summary>
        /// 根据单个非终结符构建stucture
        /// </summary>
        public GrammarStructure(Nonterminal nonterminal)
        {
            if (nonterminal == null)
            {
                throw new System.ArgumentNullException();
            }

            structure = new List<GrammarSymbol> { nonterminal };

            nonterminals = new HashSet<Nonterminal> { nonterminal };
            terminals = new HashSet<Terminal>();
            terminals.TrimExcess(); nonterminals.TrimExcess(); structure.TrimExcess();
            GenSymbol();
        }
        /// <summary>
        /// 根据单个终结符构建stucture
        /// </summary>
        public GrammarStructure(Terminal terminal)
        {
            if (terminal == null)
            {
                throw new System.ArgumentNullException();
            }

            structure = new List<GrammarSymbol> { terminal };
            nonterminals = new HashSet<Nonterminal>();
            terminals = new HashSet<Terminal> { terminal };
            terminals.TrimExcess(); nonterminals.TrimExcess(); structure.TrimExcess();
        }
        private void GenSymbol()
        {
            // c#的linq 真好用 *_*
            terminals = new HashSet<Terminal>((from symbol in structure
                                               where symbol.GetSymbolType() == SymbolType.Terminal
                                               select symbol as Terminal).ToList());
            nonterminals = new HashSet<Nonterminal>((from symbol in structure
                                                     where symbol.GetSymbolType() == SymbolType.Nonterminal
                                                     select symbol as Nonterminal).ToList());
            terminals.TrimExcess();
            nonterminals.TrimExcess();
            structure.TrimExcess();
        }
        /// <summary>
        /// 标准化的文法单元要么只包含一个空要么不包含空包含多个终结或非终结序列
        /// </summary>
        private void RemoveMutiEmpty()
        {
            // 如果文法单元包含空，但长度不为0，则该空无意义,如 a^B， 其中的^没有意义
            if (structure.Contains(Terminal.GetEmpty()) && structure.Count != 1)
            {
                structure.RemoveAll(x => x.Equals(Terminal.GetEmpty()));
                if (structure.Count == 0)  //避免多个空
                {
                    structure.Add(Terminal.GetEmpty());
                }
            }
        }
        /// <summary>
        ///  是否以非终结符开头
        /// </summary>
        public bool IsStartWithNonterminals()
        {
            return structure[0].GetSymbolType() == SymbolType.Nonterminal;
        }
        /// <summary>
        /// 判断是否包含非终结符
        /// </summary>
        public bool IsContrainNonterminals()
        {
            return nonterminals.Count != 0;
        }
        /// <summary>
        /// 判断是否仅包含空
        /// </summary>
        public bool IsEmpty() { return structure.Count == 1 && structure[0].Equals(Terminal.Empty); }
        /// <summary>
        /// 是否包含终结符
        /// </summary>
        public bool IsContrainTerminals() { return terminals.Count != 0; }
        /// <summary>
        /// 判断一个符号是否在当前文法单元
        /// </summary>
        /// <param name="sym">可以是终结或非终结符</param>
        /// <returns></returns>
        public bool Contains(GrammarSymbol sym)
        {
            return structure.Contains(sym);
        }
        /// <summary>
        /// 判断该产生式是否满足正则文法的要求
        /// </summary>
        /// <returns></returns>
        public bool IsSatisfyRG()
        {
            // 长度不大二 终结符非终结符任何一个不超过2
            bool result = structure.Count <= 2 && terminals.Count <= 1 && nonterminals.Count <= 1;
            if (structure.Count == 2)//如果长度为2 则必须为 aA 形式
            {
                result = result && structure[0].GetSymbolType() == SymbolType.Terminal && structure[1].GetSymbolType() == SymbolType.Nonterminal;
            }
            return result;
        }
        public int Length() { return structure.Count; }
        //这里new 的目的是为了防止用户修改内部
        public HashSet<Nonterminal> Nonterminals => new HashSet<Nonterminal>(nonterminals);
        public HashSet<Terminal> Terminals => new HashSet<Terminal>(terminals);
        // 这里这样做是为了后面实现上下文无关文法相关算法方便 这样的返回 不会修改当前终结符内部的结构 而文法符号又是不变的所以这么做完全没影响
        internal List<GrammarSymbol> Structure { get => new List<GrammarSymbol>(structure); }

        public override string ToString()
        {
            string tmp = "";
            foreach (var syb in structure)
            {
                tmp += (" " + syb.ToString());
            }
            return tmp;
        }
        /// <summary>
        /// 两个包含完全相同文法符号序列的文法单元等价
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            GrammarStructure ob = obj as GrammarStructure;
            if (ob.structure.Count != structure.Count)
            {
                return false;
            }
            return ob.structure.SequenceEqual(structure);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int tmp = 0;
                foreach (var sym in structure)
                {
                    tmp += (sym == null ? 0 : sym.GetHashCode());
                }
                var hashCode = 2115373340;
                hashCode = hashCode * -1521134295 + tmp;
                return hashCode;
            }
        }

        /// <summary>
        /// 向结尾添加非终结符, 仅供内部使用,内部的用例并没有破坏设计理念 只是为了方便
        /// </summary>
        /// <param name="nt">非终结符</param>
        internal GrammarStructure AppendNt(Nonterminal nt)
        {
            //TODO:此方法破坏了structure的不可变性，随后可能移走
            structure.Add(nt);
            nonterminals.Add(nt);
            return this;
        }
        /// <summary>
        /// 尾处添加一个文法符号 返回一个新structure
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public GrammarStructure Append(GrammarSymbol symbol)
        {
            if (symbol == null)
            {
                throw new System.ArgumentNullException();
            }
            if (symbol.Equals(Terminal.Empty))
            {
                return this; //如果添加空，则和当前等价
            }

            var newS = new List<GrammarSymbol>(structure);
            newS.Add(symbol);
            return new GrammarStructure(newS);
        }

        public IEnumerator GetEnumerator()
        {
            foreach (GrammarSymbol sym in structure)
            {
                yield return sym;
            }
        }
        /// <summary>
        /// 获取范围内文法符号
        /// </summary>
        /// <param name="start">开始坐标</param>
        /// <param name="len">长度</param>
        public GrammarStructure GetRange(int start, int len)
        {
            return new GrammarStructure(structure.GetRange(start, len));
        }
        /// <summary>
        /// 获取某个文法符号
        /// </summary>
        /// <param name="index">文法符号位置</param>
        /// <returns></returns>
        public GrammarSymbol this[int index]
        {
            get
            {
                return structure[index];
            }
        }
        /// <summary>
        /// 如果文法单元第一个符号为非终结符则返回，否则返回null
        /// </summary>
        /// <returns></returns>
        internal Nonterminal FirstNTOrNull()
        {
            return structure[0].GetSymbolType() == SymbolType.Nonterminal ? (Nonterminal)structure[0] : null;
        }
        /// <summary>
        /// 获取文法单元中第一个出现的非终结符，如果没有返回Null
        /// </summary>
        /// <returns></returns>
        public Nonterminal GetFirstNt()
        {
            foreach (var t in structure)
            {
                if (t.GetSymbolType() == SymbolType.Nonterminal)
                {
                    return (Nonterminal)t;
                }
            }
            return null;
        }
        internal GrammarStructure() { }
    }
    // TODO:structure具有内部不变性但production目前的设计是允许被修改
    /// <summary>
    /// 文法产生式,由一个左部文法单元和一个或多个右部文法单元构成
    /// 可以是乔姆斯基定义的四种文法中的任意一个
    /// </summary>
    public class GrammarProduction
    {
        private GrammarStructure leftStructure;
        private HashSet<GrammarStructure> rightStructures; //这里用于确保各个文法单元之间不重复
        private HashSet<Terminal> terminals;
        private HashSet<Nonterminal> nonterminals;
        /// <summary>
        /// 由一个左部文法单元和多个右部文法单元构成产生式,传递hashSet保证structures之间不重复
        /// </summary>
        /// <param name="leftStructure">左部文法单元</param>
        /// <param name="rightStructures">多个右部文法单元</param>
        public GrammarProduction(GrammarStructure leftStructure, HashSet<GrammarStructure> rightStructures)
        {
            // 这里在传入时就防止了多个可能重复的右部文法单元
            CheckPara(leftStructure, rightStructures);
            this.leftStructure = leftStructure;
            this.rightStructures = new HashSet<GrammarStructure>(rightStructures);
            GenSymbols();
        }
        private void CheckPara(GrammarStructure leftStructure, HashSet<GrammarStructure> rightStructures)
        {
            if (leftStructure == null || rightStructures == null || rightStructures.Count == 0)
            {
                throw new System.ArgumentException("用于构建产生式的文法单元不可为Null或空");
            }
            if (!leftStructure.IsContrainNonterminals())
            {
                throw new System.ArgumentException($"用于构建产生式的左部文法单元需要包含非终结符号:{leftStructure}");
            }
        }
        // 生成终结符集合与非终结符集
        private void GenSymbols()
        {
            // 选出所有文法单元的终结符hashset 再执行累加 最后加入左部文法单元的终结符
            var ts = from termi in rightStructures select termi.Terminals;
            // 这里虽然进行了累加 且修改了ts 但由于structure返回是内部terminals的拷贝 所以对structure无影响
            terminals = ts.Aggregate((first, second) => { first.UnionWith(second); return first; });
            terminals.UnionWith(leftStructure.Terminals);
            var nts = from nt in rightStructures select nt.Nonterminals;
            nonterminals = nts.Aggregate((first, second) => { first.UnionWith(second); return first; });
            nonterminals.UnionWith(leftStructure.Nonterminals);
            //节省空间
            nonterminals.TrimExcess();
            terminals.TrimExcess();
        }
        public GrammarStructure LeftStructure => leftStructure;
        public HashSet<GrammarStructure> RightStructures => new HashSet<GrammarStructure>(rightStructures);
        public HashSet<Terminal> Terminals => new HashSet<Terminal>(terminals);
        public HashSet<Nonterminal> Nonterminals => new HashSet<Nonterminal>(nonterminals);
        /// <summary>
        /// 如果左部文法单元以非终结符开头则返回该符号，否则返回Null
        /// </summary>
        internal Nonterminal FirstNTOrNull()
        {
            var result = leftStructure.FirstNTOrNull();
            if (result == null)
            {
                throw new System.NotImplementedException("内部错误:构建出无非终结符的左部文法单元");
            }

            return result;
        }

        /// <summary>
        /// 判断当前产生式是否满足上下文有关文法定义
        /// </summary>
        public bool IsSatisfyCSG()
        {
            // 只要右部文法单元长度不超过左部即可
            return rightStructures.All(x => x.Length() >= leftStructure.Length());
        }
        /// <summary>
        /// 判断当前产生式是否满足上下文无关文法定义
        /// </summary>
        /// <returns></returns>
        public bool IsSatisfyCFG()
        {
            if (!IsSatisfyCSG())
            {
                return false;
            }

            return leftStructure.Length() == 1;
        }
        /// <summary>
        /// 判断当前产生式是否满足正则文法要求
        /// </summary>
        public bool IsSatisfyRG()
        {
            if (!IsSatisfyCFG())
            {
                return false;
            }
            // 右部文法单元长度不超过2 且包含非终结符个数不超过1
            return rightStructures.All(x => x.Nonterminals.Count <= 1 && x.Length() <= 2 && x.Terminals.Count <= 1);
        }
        /// <returns></returns>
        public override string ToString()
        {
            string tmp = "";
            foreach (var stc in rightStructures)
            {
                tmp += (stc + "|");

            }
            return LeftStructure.ToString() + " => " + tmp.Remove(tmp.Length - 1);
        }
    }


    /// <summary>
    ///  ZeroType => 0型文法, ContextSensitive => 上下文有关文法, ContextFree => 上下文无法, Regular => 线性文法
    /// </summary>
    public enum GrammarType { ZeroType, ContextSensitive, ContextFree, Regular }
    /// <summary>
    ///  零型文法，内部并没有很多供调用的算法
    ///  要求非终结符必须出现在某个产生左部，否则非法
    /// </summary>
    public class Grammar
    {
        // 可以直接获取文法的终结符号集 和非终结符
        // 目前想到的还需要加入的操作：
        //          去无用符号、产生式
        //          去空产生式
        //          文法规约
        //          文法正确性
        // 
        // 所有产生式 这里确保聚拢性
        protected Dictionary<GrammarStructure, HashSet<GrammarStructure>> grammarProductions;
        protected Nonterminal startNonterminalSymbol;//开始符号
        protected HashSet<Nonterminal> nonterminals;//所有非终结符
        protected HashSet<Terminal> terminals;//所有终结符
        protected GrammarType grammarType;

        public Nonterminal StartNonterminalSymbol { get => startNonterminalSymbol; }
        //public List<GrammarProduction> GrammarProductions => grammarProductions;
        public HashSet<Nonterminal> Nonterminals => new HashSet<Nonterminal>(nonterminals);
        public HashSet<Terminal> Terminals => new HashSet<Terminal>(terminals);
        /// <summary>
        /// 获取文法实际类别
        /// </summary>
        public GrammarType GrammarType => grammarType;

        /// <summary>
        ///  通过产生式和开始符号构建文法,必须确保所有的非终结符出现在某个文法产生式的左部
        /// </summary>
        /// <param name="grammarProductions">多个产生式，需要确保每个非终结符都有相应的产生式，否在构造抛异常</param>
        /// <param name="startNonterminalSymbol">如果不传递，则默认使用第一个产生式的左部文法单元的第一个非终结符号,如果找不到则抛异常</param>
        /// <exception cref="IllegalGrammarException">文法不符合定义</exception>
        public Grammar(List<GrammarProduction> grammarProductions, Nonterminal startNonterminalSymbol = null)
        {
            // 子类的创建都委托到这里,内部用相对聚合方式的构建
            this.grammarProductions = new Dictionary<GrammarStructure, HashSet<GrammarStructure>>();
            AddToDic(grammarProductions);
            this.startNonterminalSymbol = startNonterminalSymbol ??
                grammarProductions[0].LeftStructure.GetFirstNt();// 这里能确保不为null
            Debug.Assert(this.startNonterminalSymbol != null);
            GenSymbols();//合并终结符 非终结符号
            if (!GrammarValidate())
            {
                throw new IllegalGrammarException("不符合文法定义：输入文法异常，必须确保所有的非终结符出现在某个文法产生式的左部");
            }
            grammarType = GType();
            /*
             * 到了这里就可以确定 创建的文法一定符合要求：每个非终结符必须出现在某个文法产生式左部、右部文法单元之间不重复、文法单元符合要求。
             * **/
        }
        //将传出的production加入dic
        private void AddToDic(List<GrammarProduction> grammarProductions)
        {
            if (grammarProductions == null || grammarProductions.Count == 0)
            {
                throw new System.ArgumentException("构造文法多个的产生式不能为Null或无产生式");
            }
            foreach (var pro in grammarProductions)
            {
                if (this.grammarProductions.TryGetValue(pro.LeftStructure, out HashSet<GrammarStructure> tmp))
                {
                    tmp.UnionWith(pro.RightStructures);
                }
                else
                {
                    this.grammarProductions.Add(pro.LeftStructure, pro.RightStructures);
                }
            }
        }
        /// <summary>
        /// 文法有效性验证 目前只想到了所有的非终结符必须出现在左部的某个文法单元这个限制
        /// </summary>
        /// <returns></returns>
        protected bool GrammarValidate()
        {
            // 文法合法性判断
            foreach (var nt in nonterminals)
            {   //如果任何一个左部文法单元均不包含该非终结符 则文法非法
                if (!grammarProductions.Keys.Any(x => x.Contains(nt)))
                {
                    return false;
                }
            }
            foreach (var value in grammarProductions.Values)
            {
                value.TrimExcess();
            }
            return true;
        }

        /// <summary>
        /// 文法类别判断,会给出最精确的判断
        /// </summary>
        /// <returns></returns>
        protected GrammarType GType()
        {
            GrammarType type = GrammarType.ZeroType;
            // 更为简洁的写法
            if (grammarProductions.Keys.All(x => x.Length() == 1))
            {   //每个产生式的右部文法单元长度不超过2 终结符非终结符不超过2
                if (grammarProductions.Keys.All(x => grammarProductions[x].All(
                                            y => y.IsSatisfyRG())))
                {
                    type = GrammarType.Regular;
                }
                else
                {
                    type = GrammarType.ContextFree;
                }
            }
            else
            {
                if (grammarProductions.Keys.All(x => grammarProductions[x].All(y => y.Length() >= x.Length())))
                {
                    type = GrammarType.ContextSensitive;
                }
                else
                {
                    type = GrammarType.ZeroType;
                }
            }

            return type;
        }
        //这里可以写的好看点 但没必要
        protected void GenSymbols()
        {
            terminals = new HashSet<Terminal>();
            nonterminals = new HashSet<Nonterminal>();
            foreach (var left in grammarProductions.Keys)
            {
                nonterminals.UnionWith(left.Nonterminals);
                terminals.UnionWith(left.Terminals);
                foreach (var right in grammarProductions[left])
                {
                    nonterminals.UnionWith(right.Nonterminals);
                    terminals.UnionWith(right.Terminals);
                }
            }
            if (terminals.Contains(Terminal.Empty))
            {
                terminals.Remove(Terminal.Empty);   //空不属于终结符集
            }
            nonterminals.TrimExcess();
            terminals.TrimExcess();
        }
        public override string ToString()
        {
            var tmp = "";
            foreach (var left in grammarProductions.Keys)
            {
                tmp += (left + " => ");
                foreach (var right in grammarProductions[left])
                {
                    tmp += (right + " | ");
                }
                var index = tmp.LastIndexOf('|');
                tmp = tmp.Remove(index - 1) + "\n";
            }
            string grammarStr = $"Grammar:\n{tmp}Type:{grammarType}";
            return grammarStr;
        }
        public HashSet<GrammarStructure> GetStructures(GrammarStructure structure)
        {
            if (!grammarProductions.ContainsKey(structure))
            {
                return null;
            }

            return grammarProductions[structure];
        }
        public List<GrammarSymbol> GetAllSymbols()
        {
            List<GrammarSymbol> result = new List<GrammarSymbol>();
            foreach (var ter in terminals)
            {
                result.Add(ter);
            }
            foreach (var nt in nonterminals)
            {
                result.Add(nt);
            }
            return result;
        }
        /// <summary>
        /// 判断文法是否包含空产生式
        /// </summary>
        /// <returns></returns>
        public bool HasEmptyProduction()
        {
            return grammarProductions.Any(p => p.Value.Any(s => s.IsEmpty()));
        }
        // TODO: 需要添加一些适用于所用文法的算法
    }
    /// <summary>
    /// 上下文有关文法，仅仅继承了零型文法的方法，目前未实现相关算法
    /// </summary>
    public class CSG : Grammar
    {
        public CSG(List<GrammarProduction> grammarProductions, Nonterminal startNonterminalSymbol = null) :
            base(grammarProductions, startNonterminalSymbol)
        {
            // 文法合法性交给父类判断，这里只要保证不是零型文法即可
            // 不能限制到上下文有关文法 因为上下文无关、正则文法都是上下文有关文法
            if (grammarType == GrammarType.ZeroType)
            {
                throw new IllegalGrammarException("文法不符合上下文有关文法定义");
            }
        }

        //TODO: 需要添加一些适用于上下文有关文法的算法
    }

}






/*RUBISH
                        bool lLong = true;
                        // 未检测到右部文法符号不符合正规文法
                        bool rLong = true;
                        foreach (var pt in grammarProductions.Keys)
                        {
                            // 左部文法单元长度为1 要么是正规文法 要么是上下文无关
                            if (lLong && pt.Structure.Count == 1)
                            {
                                // 如果尚未检测到不满足正规文法的文法单元 则还需要进行这里的处理 否则直接未无关文法
                                if (rLong)
                                {
                                    foreach (var rpt in grammarProductions[pt])
                                    {
                                        // 如果由任何一个非终结符或终终结符数大于1或总长度大于2 则为上下文无关
                                        if (rpt.Nonterminals.Count > 1 || rpt.Terminals.Count > 1 || rpt.Structure.Count > 2)
                                        {
                                            type = GrammarType.ContextFree;
                                            rLong = false;
                                            break;
                                        }
                                    }
                                    if (rLong)
                                    {
                                        type = GrammarType.Regular;
                                    }
                                }
                                else
                                {
                                    type = GrammarType.ContextFree;
                                }
                            }
                            else
                            {
                                lLong = false;
                                var leftLen = pt.Structure.Count;
                                foreach (var rpt in grammarProductions[pt])
                                {
                                    // 任何一个右部产生式长度大于左部 则为0型文法
                                    if (rpt.Structure.Count > leftLen)
                                    {
                                        return GrammarType.ZeroType;
                                    }
                                }
                                type = GrammarType.ContextSensitive;
                            }
                        }
     */
