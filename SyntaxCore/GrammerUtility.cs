using ErrorCore;
using System.Collections;
using System.Collections.Generic;
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
/// </summary>
namespace CLK.GrammarDS
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
        protected internal static char delem = '|';
        protected internal static char empty = '^';
        protected internal static char endSper = '$';
        protected internal static string EmptyTerminal = "^";
        protected string value;
        /// <summary>
        /// 获取文法符号类别
        /// </summary>
        public abstract SymbolType GetSymbolType();
        /// <summary>
        /// 通过串创建文法符号，不允许串中间包含空格和默认分割符'|'和内部用于分割句子的'$'
        /// </summary>
        /// <param name="value">符号值</param>
        /// <exception cref="IllegalChException">包含非法字符如: ' ' '|' </exception>
        public GrammarSymbol(string value)
        {
            this.value = value.Trim(' ');
            //空、分割符在解析时会被处理
            /*//内部不能包含空格或| 否则导致语句混乱
            if (value.Contains(delem) || value.Contains(empty))
            {
                throw new IllegalChException($"文法符号:{value}中不允许包含空格或{delem}");
            }//如果包含$或^则当前输入长度只能为一
            else if ((value.Contains(endSper) || value.Contains(EmptyTerminal)) && value.Length != 1)
            {
                throw new IllegalChException($"文法符号:{value}中不允许包含语句分隔符{delem}或用于表示空的{EmptyTerminal}");
            }*/
        }
        // 存入hashtable需要的方法
        /// <summary>
        /// 用于判断文法符号是否相同，首先判断符号类型：终结符或非终结符，
        /// 相同类型的符号值也相同则返回true
        /// </summary>
        public override bool Equals(object obj)
        {
            GrammarSymbol sym = obj as GrammarSymbol;
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
        /// <summary>
        /// 空字符
        /// </summary>
        public static Terminal Empty = new Terminal(EmptyTerminal);
        /// <summary>
        /// 分隔符
        /// </summary>
        public static Terminal Sperator = new Terminal(delem);
        /// <summary>
        /// 语句分隔符
        /// </summary>
        public static Terminal End = new Terminal(endSper);
        /// <summary>
        /// 获取空字符
        /// </summary>
        public static Terminal GetEmpty()
        {
            return new Terminal(EmptyTerminal);
        }

        public string Value { get => value; }
        /// <summary>
        /// 通过串创建终结符
        /// </summary>
        public Terminal(string value) : base(value)
        {

        }
        public Terminal(char value) : base(value.ToString())
        {
        }
        /// <summary>
        /// 什么都不传递则返回空字符
        /// </summary>
        public Terminal() : base(EmptyTerminal)
        {
        }
        /// <summary>
        /// 判断是否未空字符
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            return value.Equals(EmptyTerminal);
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
        public Nonterminal(string name) : base(name)
        {
        }
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
        ///  通过List构造文法单元,将去除list中的多个空字符
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

        // TODO: 下面这个方法还需要被检查 并没有处理所有可能输入的串类型
        /// <summary>
        /// 通过字符串创建结构，各个文法符号之间默认通过空格分割，如文法单元'aAb'，需要表示为'a A b',文法单元'dight ch'表示由两个非终结符构成
        /// 符号开头只能是英文字母，大写开头将创建非终结符，小写开头创建终结符
        /// 传递只包含空格的串以构建用于空产生式的文法单元
        /// </summary>
        /// <param name="structureInString">文法符号串</param>
        /// <param name="delem">文法符号之间的分隔符，默认为空格</param>
        public GrammarStructure(string structureInString, char delem = ' ')
        {
            // 输入的等价划分集合 可用于考虑数据处理
            if (structureInString == null) { throw new System.ArgumentNullException("用于构建文法单元的串不可为Null"); }
            if (structureInString.Trim(' ').Length > 0)
            {
                structureInString = structureInString.Trim(' ');
                structure = new List<GrammarSymbol>();
                var symbols = structureInString.Split(delem);
                foreach (var sym in symbols)
                {
                    if (sym.Length > 0 && char.IsLetter(sym[0]))
                    {
                        if (char.IsLower(sym[0]))
                        {
                            structure.Add(new Terminal(sym));
                        }
                        else
                        {
                            structure.Add(new Nonterminal(sym));
                        }
                    }
                    else if (sym.Length > 0 && sym[0] == GrammarSymbol.empty)
                    {
                        structure.Add(Terminal.Empty);
                    }
                    else
                    {
                        throw new IllegalChException($"用于构建文法单元的串中的文法符号<{structureInString}>必须以合法字符开头");
                    }
                }
            }
            else
            {
                structure = new List<GrammarSymbol> { new Terminal("") };
            }
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
            GenSymbol();
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
        private void RemoveMutiEmpty()
        {
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
        /// 判断一个符号是否在当前文法单元
        /// </summary>
        /// <param name="sym">可以是终结或非终结符</param>
        /// <returns></returns>
        public bool Contains(GrammarSymbol sym)
        {
            foreach (var sy in structure)
            {
                if (sy.GetSymbolType() == sym.GetSymbolType() && sym.Equals(sy))
                {
                    return true;
                }
            }
            return false;
        }
        public int Length() { return structure.Count; }
        public HashSet<Nonterminal> Nonterminals => nonterminals;
        public HashSet<Terminal> Terminals => terminals;
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
        /// 向结尾添加非终结符
        /// </summary>
        /// <param name="nt">非终结符</param>
        public GrammarStructure AppendNt(Nonterminal nt)
        {
            // 这里修改了structure的不变性 但目前看来没问题
            structure.Add(nt);
            nonterminals.Add(nt);
            return this;
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
        public Nonterminal GetFirstNT()
        {
            return structure[0].GetSymbolType() == SymbolType.Nonterminal ? (Nonterminal)structure[0] : null;
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
        /// 由一个左部文法单元和多个右部文法单元构成产生式
        /// </summary>
        /// <param name="leftStructure">左部文法单元</param>
        /// <param name="rightStructures">多个右部文法单元</param>
        public GrammarProduction(GrammarStructure leftStructure, HashSet<GrammarStructure> rightStructures)
        {
            // 这里在传入时就防止了多个可能重复的右部文法单元
            CheckPara(leftStructure, rightStructures);
            this.leftStructure = leftStructure;
            this.rightStructures = rightStructures;
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
        /// <summary>
        /// 通过字符串构建产生式
        /// </summary>
        /// <param name="left">左部文法符号的字符串表示，需要满足构建文法单元的要求,不可为null</param>
        /// <param name="right">多个合并的文法单元的字符串表示,不可为null</param>
        /// <param name="delma">分割多个文法单元的符号，默认为'|'</param>
        public GrammarProduction(string left, string right, char delma = '|')
        {
            // TODO: 完善从串创建产生式
            leftStructure = new GrammarStructure(left);
            if (!leftStructure.IsContrainNonterminals())
            {
                throw new System.ArgumentException($"用于构造文法的左部文法符号{LeftStructure}必须包含非终结符号");
            }
            if (right == null)
            {
                throw new System.ArgumentNullException($"用于构造文法的右部文法单元不可为null");
            }
            right = right.Trim(' ');
            rightStructures = new HashSet<GrammarStructure>();
            if (right.Length != 0)
            {
                foreach (var stc in right.Split(delma))
                {
                    rightStructures.Add(new GrammarStructure(stc));
                }
            }
            else
            {
                rightStructures.Add(new GrammarStructure(Terminal.GetEmpty()));
            }
            rightStructures.TrimExcess();
            GenSymbols();
        }

        // 生成终结符集合与非终结符集
        private void GenSymbols()
        {
            // 选出所有文法单元的终结符hashset 再执行累加 最后加入左部文法单元的终结符
            var ts = from termi in rightStructures select termi.Terminals;
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
        public HashSet<GrammarStructure> RightStructures => rightStructures;
        public HashSet<Terminal> Terminals => terminals;
        public HashSet<Nonterminal> Nonterminals => nonterminals;
        /// <summary>
        /// 如果左部文法单元以非终结符开头则返回该符号，否则返回Null
        /// </summary>
        public Nonterminal GetFirstNT()
        {
            var result = leftStructure.GetFirstNT();
            if (result == null)
            {
                throw new System.NotImplementedException("内部错误:构建出无非终结符的左部文法单元");
            }

            return result;
        }
        /// <summary>
        /// 增加一个文法单元到当前产生式,应该尽量少用，目前实现不够好
        /// </summary>
        public bool AddStructure(GrammarStructure structure)
        {
            var result = rightStructures.Add(structure);
            GenSymbols();
            return result;
        }
        /// <summary>
        /// 删除一个文法单元到当前产生式,应该尽量少用，目前实现不够好
        /// </summary>
        public bool Remove(GrammarStructure structure)
        {
            var result = rightStructures.Remove(structure);
            GenSymbols();
            return result;
        }
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
        public HashSet<Nonterminal> Nonterminals => nonterminals;
        public HashSet<Terminal> Terminals => terminals;
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
            this.grammarProductions = new Dictionary<GrammarStructure, HashSet<GrammarStructure>>();
            AddToDic(grammarProductions);
            this.startNonterminalSymbol = startNonterminalSymbol ?? grammarProductions[0].GetFirstNT();
            GenSymbols();//合并终结符 非终结符号
            if (!GrammarValidate())
            {
                throw new IllegalGrammarException("不符合文法定义：输入文法异常，必须确保所有的非终结符出现在某个文法产生式的左部");
            }
            grammarType = GType();
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
            {
                if (grammarProductions.Keys.All(x => grammarProductions[x].All(y => y.Length() <= 2 && y.Nonterminals.Count <= 1)))
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
                if (grammarProductions.Keys.All(x => grammarProductions[x].All(y => y.Length() <= x.Length())))
                {
                    type = GrammarType.ContextSensitive;
                }
                else
                {
                    type = GrammarType.ZeroType;
                }
            }
            /*
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
                        }*/
            return type;
        }
        //这里可以写的好看点 但没必要
        protected void GenSymbols()
        {
            if (nonterminals == null)
            {
                nonterminals = new HashSet<Nonterminal>();
            }
            if (terminals == null)
            {
                terminals = new HashSet<Terminal>();
            }
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
    }
    /// <summary>
    /// 上下文有关文法，仅仅继承了零型文法的方法
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
    }

}
