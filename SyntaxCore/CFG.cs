using ErrorCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CLK.util
{
    using FirstSet = Dictionary<GrammarStructure, HashSet<Terminal>>;
    using FirstSetNT = Dictionary<Nonterminal, HashSet<Terminal>>;
    using FollowSet = Dictionary<Nonterminal, HashSet<Terminal>>;
    using LLTable = Dictionary<Nonterminal, Dictionary<Terminal, GrammarProduction>>;

    /*
     *  上下文无关文法相关的算法实现在此类内部
     *  这里还缺很多文法变换算法 如消除空产生式、文法规约、消除单产生式、消除直接、间接左递归
     * **/
    /// <summary>
    ///  上下文无关文法
    /// </summary>
    public class CFG : CSG
    {
        private FirstSetNT first;   //每个非终结符的first集
        private FirstSet firstSet; //每个文法单元的first集
        private FollowSet follow; //每个非终结符的follow集
        public static Terminal ENDTERMINAL = new Terminal("$"); //文法默认的分隔符 这里设计还不够实用 需要改进
        private new Dictionary<Nonterminal, HashSet<GrammarStructure>> grammarProductions;  // 为方便操作这里故意隐藏了父类实现
        /// <summary>
        /// 从产生式构造上下文无关文法，需要满足文法定义
        /// </summary>
        /// <param name="grammarProductions">产生式列表</param>
        /// <param name="startNonterminalSymbol">开始符号，如果为null则选取第一个产生式左部文法符号</param>
        public CFG(List<GrammarProduction> grammarProductions, Nonterminal startNonterminalSymbol = null) :
            base(grammarProductions, startNonterminalSymbol)
        {
            if (grammarType == GrammarType.ContextSensitive)
            {
                throw new IllegalGrammarException("文法不符合上下文无关文法定义");
            }
            this.grammarProductions = new Dictionary<Nonterminal, HashSet<GrammarStructure>>();
            foreach (var ke in base.grammarProductions.Keys)
            {
                var nt = ke.GetFirstNT();
                Debug.Assert(nt != null);
                this.grammarProductions.Add(nt, base.grammarProductions[ke]);
            }
        }
        /// <summary>
        /// 判断文法是否直接或间接左递归
        /// </summary>
        public bool IsLeftRecursive()
        {
            // 对每一个非终结符，如果其产生式中包含的非终结符包含最左符号为其的产生式 则为左递归
            foreach (var nt in nonterminals)
            {
                if (FindReachable(nt).Contains(nt))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 找出某个非终结符所有可达的非终结符
        /// </summary>
        public HashSet<Nonterminal> FindReachable(Nonterminal target)
        {
            var oldNT = new HashSet<Nonterminal>();
            var newNT = FindDirectReachable(target);
            // 遍历每个新找到的非终结符 记为N，寻找N的直接可达非终结符集合即为S，
            // 对S中的每个非终结符p,如果p既不在oldNT 也不在 newNT则加入tmp，准备下一轮的新终结符进行遍历
            while (true)
            {
                var tmp = new HashSet<Nonterminal>();
                foreach (var nt in newNT)
                {
                    foreach (var nnt in FindDirectReachable(nt))
                    {
                        if (!newNT.Contains(nnt) && !oldNT.Contains(nnt))
                        {
                            tmp.Add(nnt);
                        }
                    }
                    oldNT.Add(nt);
                }
                newNT = tmp;
                if (newNT.Count == 0) { break; }
            }
            return oldNT;
        }

        /// <summary>
        /// 获取直接可达的非终结符
        /// </summary>
        /// <param name="target"></param>
        /// <returns>如果不含直接可达，则返回空的hashSet</returns>
        public HashSet<Nonterminal> FindDirectReachable(Nonterminal target)
        {
            var stcs = grammarProductions[target];
            HashSet<Nonterminal> result = new HashSet<Nonterminal>();
            foreach (var stc in stcs)
            {
                var nt = stc.GetFirstNT();
                if (nt != null)
                {
                    result.Add(nt);
                }
            }
            // 这里如果为空不返回null而是空hashSet
            return result;
        }

        /// <summary>
        /// 通用消除文法直接、间接左递归算法, 会改变原文法
        /// </summary>
        public void EliminateCommonRecursive()
        {
            //将非终结符排序 从前至后，对
            var orderedNT = nonterminals.ToList();
            var len = orderedNT.Count;
            // 将当前非终结符右部文法单元的第一个非终结符替换
            for (int i = 0; i < len; i++)
            {
                var currentNT = orderedNT[i];
                // 变换前面非终结符
                for (int j = 0; j <= i - 1; j++)
                {
                    var newStructure = new HashSet<GrammarStructure>();
                    var oldStructure = new HashSet<GrammarStructure>();
                    foreach (var stc in grammarProductions[currentNT])
                    {
                        if (stc[0].Equals(orderedNT[j]))
                        {
                            // 下面构造新的产生式
                            var tmp = stc.Structure; tmp.RemoveAt(0);
                            foreach (var nstc in grammarProductions[orderedNT[j]])
                            {
                                newStructure.Add(new GrammarStructure(nstc.Structure.Concat(tmp).ToList()));
                            }
                            oldStructure.Add(stc);
                            //在迭代的过程修改列表在c++中会有问题 c#中还未知 先这么试试 
                            // c# 检测出了这个错误
                        }
                    }
                    grammarProductions[currentNT].ExceptWith(oldStructure);
                    grammarProductions[currentNT].UnionWith(newStructure);
                }
                EliminateDirectRecursive(currentNT);
            }
            //重新生成终结符、非终结符
            InitSet();

            // TODO: !!!
            // 这里需要重新计算first follow  目前只是简单的重新置为null 随后得仔细考虑
            first = null;
            firstSet = null;
            follow = null;
        }
        /// <summary>
        /// 去除某个文法符号产生式的直接左递归
        /// </summary>
        /// <param name="target"></param>
        private void EliminateDirectRecursive(Nonterminal target)
        {
            // 先获取所有导致左递归的右部单元
            HashSet<GrammarStructure> recu = new HashSet<GrammarStructure>();
            foreach (var stc in grammarProductions[target])
            {
                if (stc[0].Equals(target))
                {
                    recu.Add(stc);
                }
            }
            // 无直接左递归
            if (recu.Count == 0)
            {
                return;
            }
            grammarProductions[target].ExceptWith(recu);
            // 不断向原名称加入' 产生新非终结符
            Nonterminal newNt = target;
            while (true)
            {
                newNt = new Nonterminal(newNt.Value + "'");
                if (!grammarProductions.Keys.Contains(newNt))
                {
                    break;
                }
            }
            grammarProductions.Add(newNt, new HashSet<GrammarStructure> { new GrammarStructure(Terminal.GetEmpty()) });

            foreach (var stc in recu)
            {
                //构造新产生式
                var tmp = stc.Structure; tmp.RemoveAt(0);
                var nt = new GrammarStructure(tmp.Concat(new List<GrammarSymbol> { newNt }).ToList());
                grammarProductions[newNt].Add(nt);
            }
            foreach (var stc in grammarProductions[target])
            {
                stc.AppendNt(newNt);
            }
            InitSet();

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
            string grammarStr = $"Grammar:\n{tmp}Type:{grammarType}";
            return grammarStr;
        }
        /// <summary>
        /// 获取文法所有右部文法单元的First集
        /// </summary>
        public FirstSet GetFirstOfStructure()
        {
            if (firstSet != null)
            {
                return firstSet;
            }

            firstSet = new FirstSet();
            foreach (var rightSet in grammarProductions.Values)
            {
                foreach (var right in rightSet)
                {
                    if (firstSet.TryGetValue(right, out HashSet<Terminal> tmp))
                    {
                        tmp.UnionWith(CalFirstOfStructure(right));
                    }
                    else
                    {
                        firstSet.Add(right, CalFirstOfStructure(right));
                    }
                }
            }
            return firstSet;
        }
        /// <summary>
        /// 获取文法每个非终结符号First集
        /// </summary>
        /// <returns></returns>
        public FirstSetNT GetFirstOfNonterminals()
        {
            // 懒惰计算
            if (first != null)
            {
                return first;
            }
            else
            {
                first = new FirstSetNT();
                CalFirst();
                return first;
            }
        }
        private bool change = false;
        private void CalFirst()
        {
            do
            {
                change = false;
                foreach (var nt in nonterminals)
                {
                    if (!first.ContainsKey(nt))
                    {
                        first.Add(nt, new HashSet<Terminal>());
                    }
                    DoCal(nt);
                }
            } while (change);
        }
        private void DoCal(Nonterminal nt)
        {
            int startCount = first[nt].Count;
            var right = grammarProductions[nt];
            foreach (var stc in right)
            {
                foreach (GrammarSymbol sym in stc)
                {
                    if (sym.GetSymbolType() == SymbolType.Terminals)
                    {
                        first[nt].Add((Terminal)sym);
                        break;
                    }
                    else
                    {
                        if (first.ContainsKey((Nonterminal)sym))
                        {
                            first[nt].UnionWith(first[(Nonterminal)sym]);
                        }
                        else
                        {
                            first.Add((Nonterminal)sym, new HashSet<Terminal>());
                            DoCal((Nonterminal)sym);
                            first[nt].UnionWith(first[(Nonterminal)sym]);
                        }
                        if (!first[(Nonterminal)sym].Contains(Terminal.GetEmpty()))
                        {
                            break;
                        }
                    }
                }
            }
            if (first[nt].Count != startCount) { change = true; }
        }
        /// <summary>
        /// 计算可由文法推导的句型的First集
        /// </summary>
        /// <param name="structure">文法可推出的语法单元</param>
        /// <exception cref="IllegalStructureException">非法文法单元</exception>
        public HashSet<Terminal> CalFirstOfStructure(GrammarStructure structure)
        {
            if (!(structure.Terminals.IsSubsetOf(terminals) && structure.Nonterminals.IsSubsetOf(Nonterminals)))
            {
                throw new IllegalGrammarException("当前文法无法推倒至输入文法单元");
            }
            var firstSt = new HashSet<Terminal>();
            foreach (GrammarSymbol sym in structure)
            {
                if (sym.GetSymbolType() == SymbolType.Terminals)
                {
                    firstSt.Add((Terminal)sym);
                    break;
                }
                else
                {
                    // 这里调用First() 而不是直接过去first
                    firstSt.UnionWith(GetFirstOfNonterminals()[(Nonterminal)sym]);
                    if (!first[(Nonterminal)sym].Contains(Terminal.GetEmpty()))
                    {
                        break;
                    }
                }
            }
            return firstSt;
        }

        // 如果两个算法使用同一个bool 可能会线程不安全
        private bool change2 = false;
        /// <summary>
        /// 获取所有非终极符的follow集
        /// </summary>
        /// <returns></returns>
        public FollowSet GetFollow()
        {
            if (follow != null)
            {
                return follow;
            }
            follow = new FollowSet
                {
                    { startNonterminalSymbol, new HashSet<Terminal> { ENDTERMINAL } }
                };
            foreach (var nt in Nonterminals)
            {
                if (!follow.ContainsKey(nt)) { follow.Add(nt, new HashSet<Terminal>()); };
            }
            do
            {
                change2 = false;
                CalFollow();

            } while (change);
            return follow;
        }
        private void CalFollow()
        {
            // 对每一个产生式的右部文法单元进行迭代
            foreach (var pro in grammarProductions)
            {
                foreach (var strc in pro.Value)
                {
                    // 因为没找到迭代的写法 所以这里很丑
                    for (int i = 0; i < strc.Length(); i++)
                    {
                        if (strc[i].GetSymbolType() == SymbolType.Nonterminals)
                        {
                            Nonterminal current = (Nonterminal)strc[i];
                            int startCount = follow[current].Count;
                            //如果为最后一个非终结符 则将左部文法符号的follow加入
                            if (i == strc.Length() - 1)
                            {
                                follow[current].UnionWith(follow[pro.Key]);
                            }
                            else
                            {
                                var tmp = strc.GetRange(i + 1, strc.Length() - i - 1);
                                var tf = CalFirstOfStructure(tmp);
                                if (tf.Contains(Terminal.GetEmpty()))
                                {
                                    follow[current].UnionWith(follow[pro.Key]);
                                    tf.Remove(Terminal.GetEmpty());
                                }
                                follow[current].UnionWith(tf);
                            }
                            if (startCount != follow[current].Count) { change2 = true; }
                        }
                    }
                }
            }
        }
        protected new void InitSet()
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
                nonterminals.Add(left);
                foreach (var right in grammarProductions[left])
                {
                    nonterminals.UnionWith(right.Nonterminals);
                    terminals.UnionWith(right.Terminals);
                }
            }
            nonterminals.TrimExcess();
            terminals.TrimExcess();
        }
        //递归下降 使用的变量，其他地方绝对不使用
        private SymbolIter internalSymBols = null;
        private Terminal currentTeminal = null;
        /// <summary>
        /// 通用递归下降分析 要求文法不含左递归，识别过程无需回溯，但一定程度不必严格要求每个first集之间无交集
        /// </summary>
        /// <returns></returns>
        public bool RecursiveAnalyze(SymbolIter symbolIter)
        {
            internalSymBols = symbolIter ?? throw new System.ArgumentNullException();
            currentTeminal = internalSymBols.Next();
            return DoRecs(startNonterminalSymbol);
        }
        // 递归判断，目前还未加入分析树生成，因为还未实现树结构,还未加入处理回溯的逻辑，目前想到的处理回溯的方式会导致搜索爆炸
        private bool DoRecs(Nonterminal nt)
        {
            var stcs = FindTargetStuc(nt, currentTeminal); // 获取first集包含currentTerminal的stucture
            if (stcs == null) //如果不包含这样的右部文法单元 则无需继续分析当前节点
            {
                return false;
            }
            // 尝试可能的产生式
            foreach (var stc in stcs)
            {
                // 方便失配时 还原
                var count = 0;
                var flag = internalSymBols.GetRest();
                var bef = currentTeminal;
                // 这里可能会出现 多个文法单元匹配 但只返回第一个的情况 还未处理假匹配的可能
                foreach (GrammarSymbol sym in stc)
                {
                    // 每一步都需要确保 流还未消耗结束
                    if (currentTeminal == null)
                    {
                        internalSymBols.BackN(count);
                        currentTeminal = bef;
                        break;
                    }
                    if (sym.GetSymbolType() == SymbolType.Terminals)
                    {
                        if (sym.Equals(currentTeminal))
                        {
                            currentTeminal = internalSymBols.Next();
                            count++;
                            continue;
                        }
                        else
                        {
                            internalSymBols.BackN(count);
                            break;
                        }
                    }
                    else
                    {
                        if (!DoRecs((Nonterminal)sym))
                        {
                            internalSymBols.BackN(count);
                            break;
                        }
                    }
                }
                // 当前匹配 且不为开始符号或 为开始符号但流已被消耗完
                if (internalSymBols.GetRest() != flag && (!nt.Equals(startNonterminalSymbol) ||
                    (nt.Equals(startNonterminalSymbol) && !internalSymBols.HasNext())))
                {
                    return true;
                }
                else
                {
                    internalSymBols.BackN(count);
                    currentTeminal = bef;
                }
            }
            return false;
        }
        /// <summary>
        /// 获取左部文法符号为nt的每个产生式中每个first集中包含终结符t的右部文法单元
        /// </summary>
        /// <param name="nt">左部非终结符</param>
        /// <param name="t">右部终结符</param>
        private List<GrammarStructure> FindTargetStuc(Nonterminal nt, Terminal t)
        {
            List<GrammarStructure> target = new List<GrammarStructure>();
            foreach (var stc in grammarProductions[nt])
            {
                var fi = GetFirstOfStructure()[stc];
                if (fi.Contains(t))
                {
                    target.Add(stc);
                }
            }
            return target.Count == 0 ? null : target;
        }
        //未完成, 目标获取通用LL分析程序
        public LLProc GenLLProc()
        {
            GetFirstOfStructure();
            GetFollow();
            LLTable tmp = new LLTable();
            foreach (var nt in nonterminals)
            {
                tmp.Add(nt, new Dictionary<Terminal, GrammarProduction>());
                foreach (var stc in grammarProductions[nt])
                {
                    var fi = firstSet[stc];
                    foreach (var ter in fi)
                    {
                        tmp[nt].Add(ter, new GrammarProduction(new GrammarStructure(new List<GrammarSymbol> { nt }), new HashSet<GrammarStructure> { stc }));
                    }
                    if (fi.Contains(Terminal.GetEmpty()))
                    {

                    }
                }
            }
            throw new System.NotImplementedException();
        }

    }
    /// <summary>
    /// 终结符流
    /// </summary>
    public class SymbolIter
    {
        private int index;
        private List<Terminal> symbols;

        public SymbolIter(List<Terminal> symbols)
        {
            this.symbols = symbols ?? throw new System.ArgumentNullException();
            index = 0;
        }
        /// <summary>
        /// 默认所有终结符长度只有一
        /// </summary>
        /// <param name="sentence"></param>
        public SymbolIter(string sentence)
        {
            if (sentence == null || sentence.Length == 0)
            {
                throw new System.ArgumentException();
            }
            index = 0;
            symbols = new List<Terminal>();
            foreach (var ch in sentence)
            {
                symbols.Add(new Terminal(ch));
            }
        }
        /// <summary>
        /// 获取并消耗流
        /// </summary>
        /// <returns></returns>
        public Terminal Next()
        {
            if (index == symbols.Count)
            {
                return null;
            }
            return symbols[index++];
        }
        /// <summary>
        /// 获取 但不消耗流 
        /// </summary>
        /// <returns></returns>
        public Terminal Get()
        {
            if (index >= symbols.Count)
            {
                return null;
            }

            return symbols[index];
        }
        /// <summary>
        /// 回退流
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public bool BackN(int n)
        {
            if (n > index)
            {
                return false;
            }

            index -= n;
            return true;
        }
        /// <summary>
        /// 判断流是否消耗完
        /// </summary>
        /// <returns></returns>
        public bool HasNext()
        {
            return index < symbols.Count;
        }
        /// <summary>
        /// 获取流剩余非终结符数
        /// </summary>
        /// <returns></returns>
        public int GetRest()
        {
            return symbols.Count - index;
        }

        public override string ToString()
        {
            string tmp = "";
            foreach (var sm in symbols)
            {
                tmp += sm;
            }
            return tmp;
        }
    }
    /// <summary>
    /// 通用LL分析程序，内部包含分析表和分析时所用的栈
    /// </summary>
    public class LLProc
    {
        private LLTable llTable;
        public LLProc(LLTable lLTable)
        {
            llTable = llTable;
        }

        public override string ToString()
        {
            return Tmp.PrintTable(llTable);
        }
    }
    //暂时没想好去处
    class Tmp
    {
        public static string PrintTable<C, R, V>(Dictionary<C, Dictionary<R, V>> table)
        {
            string tmp = "";
            foreach (C outK in table.Keys)
            {
                tmp += outK + ":{ ";
                foreach (R innerK in table[outK].Keys)
                {
                    tmp += innerK + ": " + table[outK][innerK] + ",";
                }
                tmp += " }";
            }
            return tmp;
        }
    }
}
