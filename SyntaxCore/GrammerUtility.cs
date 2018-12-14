using ErrorCore;
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
namespace CLK.util
{

    /// <summary>
    /// 终结符 非终结符的枚举类型
    /// </summary>
    public enum SymbolType { Terminals, Nonterminals };
    /// <summary>
    /// 终结符和非终结符的接口 通过getType获取类型 而不是运行时识别
    /// </summary>
    public interface IGrammarSymbol
    {
        SymbolType GetSymbolType();
    }
    /// <summary>
    /// 终结符,应该把所有的终结符的值视为串
    /// </summary>
    public class Terminals : IGrammarSymbol
    {
        public static string EmptyValue = "";
        public static Terminals GetEmpty()
        {
            return new Terminals(EmptyValue);
        }
        private string value;
        public string Value { get => value; }

        public Terminals(string value)
        {
            this.value = value;
        }
        public Terminals()
        {
            value = EmptyValue;
        }
        public bool IsEmpty()
        {
            return value.Equals(EmptyValue);
        }
        public override string ToString()
        {
            return value;
        }

        // 存入hashtable需要的方法
        /// <summary>
        /// 如果传入非终结符，不会抛出转换异常，而是返回false
        /// </summary>
        public override bool Equals(object obj)
        {
            IGrammarSymbol sym = (IGrammarSymbol)obj;
            if (sym.GetSymbolType() != GetSymbolType())
            {
                return false;
            }
            return value.Equals(((Terminals)obj).value);
        }

        public SymbolType GetSymbolType()
        {
            return SymbolType.Terminals;
        }
        public override int GetHashCode()
        {
            var hashCode = 1927018180;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(value);
            return hashCode;
        }
    }
    // 将来可能拓展此类，加入属性，从而支持语法制导
    /// <summary>
    /// 对于非终结符，并没有实际的值，仅仅使用name标识
    /// </summary>
    public class Nonterminals : IGrammarSymbol
    {
        private string name;
        public Nonterminals(string name)
        {
            this.name = name;
        }
        public string Name => name;
        /// <summary>
        /// 获取符号类别
        /// </summary>
        /// <returns>终结符或非终结符</returns>
        public SymbolType GetSymbolType()
        {
            return SymbolType.Nonterminals;
        }

        public override string ToString()
        {
            return name;
        }
        public override bool Equals(object obj)
        {
            IGrammarSymbol sym = (IGrammarSymbol)obj;
            if (sym.GetSymbolType() != GetSymbolType())
            {
                return false;
            }
            return name.Equals(((Nonterminals)obj).name);
        }

        public override int GetHashCode()
        {
            var hashCode = 629881564;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(name);
            return hashCode;
        }
    }

    /// <summary>
    /// 文法单元，如在文法:bA => aA|abA中， bA、aA、abA都是GrammarStructure,
    /// 此结构将作为产生式的组成
    /// </summary>
    public class GrammarStructure
    {
        private List<IGrammarSymbol> structure;
        private HashSet<Terminals> terminals;
        private HashSet<Nonterminals> nonterminals;
        /// <summary>
        ///  通过List构造文法单元
        /// </summary>
        /// <param name="structure">传递Terminal.Empty将创建用于空产生式的部分</param>
        public GrammarStructure(List<IGrammarSymbol> structure)
        {
            if (structure == null)
            {
                throw new System.ArgumentNullException("构建文法单元的文法符号列表不可为null");
            }
            else if (structure.Count == 0)
            {
                throw new System.ArgumentException("构建文法单元的文法符号列表不可为空");
            }
            this.structure = structure;
            GenSymbol();
        }
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
                structure = new List<IGrammarSymbol>();
                var symbols = structureInString.Split(delem);
                foreach (var sym in symbols)
                {
                    if (sym.Length > 0 && char.IsLetter(sym[0]))
                    {
                        if (char.IsLower(sym[0]))
                        {
                            structure.Add(new Terminals(sym));
                        }
                        else
                        {
                            structure.Add(new Nonterminals(sym));
                        }
                    }
                    else
                    {
                        throw new IllegalChException($"用于构建文法单元的串中的文法符号<{structureInString}>必须以合法字符开头");
                    }
                }
            }
            else
            {
                structure = new List<IGrammarSymbol> { new Terminals("") };
            }
            GenSymbol();
        }
        private void GenSymbol()
        {
            if (Terminals == null)
            {
                terminals = new HashSet<Terminals>();
            }

            if (nonterminals == null)
            {
                nonterminals = new HashSet<Nonterminals>();
            }
            // 将符号分类入终结 非终结 hashset
            foreach (var syn in Structure)
            {
                if (syn.GetSymbolType() == SymbolType.Terminals)
                {
                    Terminals.Add((Terminals)syn);
                }
                else
                {
                    nonterminals.Add((Nonterminals)syn);
                }
            }
            terminals.TrimExcess();
            nonterminals.TrimExcess();
        }
        /// <summary>
        ///  是否以非终结符开头
        /// </summary>
        public bool IsStartWithNonterminals()
        {
            return structure[0].GetSymbolType() == SymbolType.Nonterminals;
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
        public bool Contains(IGrammarSymbol sym)
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
        public List<IGrammarSymbol> Structure => structure;
        public HashSet<Nonterminals> Nonterminals => nonterminals;
        public HashSet<Terminals> Terminals => terminals;

        public override string ToString()
        {
            string tmp = "";
            foreach (var syb in Structure)
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
            GrammarStructure ob = (GrammarStructure)obj;
            if (ob.structure.Count != structure.Count)
            {
                return false;
            }
            var result = structure.Zip(ob.structure, (first, second) => first.Equals(second));
            return ob.structure.SequenceEqual(structure);
        }

        public override int GetHashCode()
        {
            var hashCode = 2115373340;
            hashCode = hashCode * -1521134295 + EqualityComparer<List<IGrammarSymbol>>.Default.GetHashCode(structure);
            return hashCode;
        }
    }

    /// <summary>
    /// 文法产生式,由一个左部文法单元和一个或多个右部文法单元构成
    /// 可以是乔姆斯基定义的四种文法中的任意一个
    /// </summary>
    public class GrammarProduction
    {
        // 定义文法的四元组
        private GrammarStructure leftStructure;
        private HashSet<GrammarStructure> rightStructures; //这里用于确保各个文法单元之间不重复
        private HashSet<Terminals> terminals;
        private HashSet<Nonterminals> nonterminals;
        /// <summary>
        /// 由一个左部文法单元和多个右部文法单元构成产生式
        /// </summary>
        /// <param name="leftStructure">左部文法单元</param>
        /// <param name="rightStructures">多个右部文法单元</param>
        public GrammarProduction(GrammarStructure leftStructure, HashSet<GrammarStructure> rightStructures)
        {
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
                rightStructures.Add(new GrammarStructure(right));
            }
            rightStructures.TrimExcess();
            GenSymbols();
        }

        // 生成终结符集合与非终结符集
        private void GenSymbols()
        {
            if (terminals == null)
            {
                terminals = new HashSet<Terminals>();
            }

            if (nonterminals == null)
            {
                nonterminals = new HashSet<Nonterminals>();
            }

            terminals.UnionWith(leftStructure.Terminals);
            nonterminals.UnionWith(leftStructure.Nonterminals);
            foreach (var stu in rightStructures)
            {
                terminals.UnionWith(stu.Terminals);
                nonterminals.UnionWith(stu.Nonterminals);
            }
            nonterminals.TrimExcess();
            terminals.TrimExcess();
        }
        public GrammarStructure LeftStructure => leftStructure;
        public HashSet<GrammarStructure> RightStructures => rightStructures;
        public HashSet<Terminals> Terminals => terminals;
        public HashSet<Nonterminals> Nonterminals => nonterminals;
        /// <summary>
        /// 判断产生式是否直接左递归
        /// </summary>
        public bool IsLeftRecursive()
        {
            throw new System.NotImplementedException("产生式直接左递归判断未完成");
        }
        /// <summary>
        /// 获取第左部文法单元的第一个非终结符
        /// </summary>
        public Nonterminals GetFirstNT()
        {
            foreach (var sym in leftStructure.Structure)
            {
                if (sym.GetSymbolType() == SymbolType.Nonterminals)
                {
                    return (Nonterminals)sym;
                }
            }
            throw new System.NotImplementedException("内部错误:构建出无非终结符的左部文法单元");
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
    ///  文法:由多个文法产生式构成和开始符号构成
    /// </summary>
    public class Grammar
    {
        // 可以直接获取文法的终结符号集 和非终结符
        // 目前想到的还需要加入的操作：
        //          文法类型判断
        //          是否左递归
        //          左递归、左公因子自动消除
        //          文法正确性
        // 
        // 所有产生式
        protected readonly Dictionary<GrammarStructure, HashSet<GrammarStructure>> grammarProductions;
        protected Nonterminals startNonterminalSymbol;//开始符号
        protected HashSet<Nonterminals> nonterminals;
        protected HashSet<Terminals> terminals;
        protected readonly GrammarType grammarType;
        public Nonterminals StartNonterminalSymbol { get => startNonterminalSymbol; }
        //public List<GrammarProduction> GrammarProductions => grammarProductions;
        public HashSet<Nonterminals> Nonterminals => nonterminals;
        public HashSet<Terminals> Terminals => terminals;
        public GrammarType GrammarType => grammarType;

        /// <summary>
        ///  通过产生式和开始符号构建文法,必须确保所有的非终结符出现在某个文法产生式的左部
        /// </summary>
        /// <param name="grammarProductions">多个产生式，需要确保每个非终结符都有相应的产生式，否在构造抛异常</param>
        /// <param name="startNonterminalSymbol">如果不传递，则默认使用第一个产生式的左部文法单元的第一个非终结符号,如果找不到则抛异常</param>
        public Grammar(List<GrammarProduction> grammarProductions, Nonterminals startNonterminalSymbol = null)
        {
            this.grammarProductions = new Dictionary<GrammarStructure, HashSet<GrammarStructure>>();
            AddToDic(grammarProductions);
            this.startNonterminalSymbol = startNonterminalSymbol ?? grammarProductions[0].GetFirstNT();
            InitSet();//合并终结符 非终结符号
            if (!GrammarValidate())
            {
                throw new IllegalGrammarException("不符合文法定义：输入文法异常，必须确保所有的非终结符出现在某个文法产生式的左部");
            }
            grammarType = GType();
        }
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
        private bool GrammarValidate()
        {
            // 文法合法性判断
            bool inLeft = false;
            foreach (var nt in nonterminals)
            {
                inLeft = false;
                foreach (var pt in grammarProductions.Keys)
                {
                    if (pt.Contains(nt))
                    {
                        inLeft = true; break;
                    }
                }
                if (!inLeft)
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 判断文法是否直接或间接左递归
        /// </summary>
        public bool IsLeftRecursive()
        {
            throw new System.NotImplementedException("未完成左递归判断");
        }
        /// <summary>
        /// 通用消除文法直接、简介左递归算法
        /// </summary>
        public void EliminateRecursiveAndCommonItem()
        {
            throw new System.NotImplementedException("未完成左递归左公因子消除");
        }
        /// <summary>
        /// 文法类别判断,会给出最精确的判断
        /// </summary>
        /// <returns></returns>
        private GrammarType GType()
        {
            GrammarType type = GrammarType.ZeroType;
            //未检测到有左部文法符号超过1
            bool lLong = true;
            // 未检测到右部文法符号不符合正规文法
            bool rLong = true;
            // 未检测到右部长度大于左部
            bool rlLong = true;
            // 实现一边扫描判断文法类型
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
            return type;
        }

        private void InitSet()
        {
            if (nonterminals == null)
            {
                nonterminals = new HashSet<Nonterminals>();
            }

            if (terminals == null)
            {
                terminals = new HashSet<Terminals>();
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
                tmp = tmp.Remove(tmp.Length - 1) + "\n";
            }
            /*       foreach (var gra in grammarProductions)
                   {
                       tmp += gra + "\n";
                   }*/
            string grammarStr = $"Grammar:\n{tmp}Type:{grammarType}";
            return grammarStr;
        }
    }

    public class CSG : Grammar
    {
        public CSG(List<GrammarProduction> grammarProductions, Nonterminals startNonterminalSymbol = null) :
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
