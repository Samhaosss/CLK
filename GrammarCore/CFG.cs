﻿using CLK.AnalysisDs;
using CLK.GrammarCore.Parser;
using ErrorCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
namespace CLK.GrammarCore
{
    using CFGrammarDS = Dictionary<Nonterminal, HashSet<GrammarStructure>>;
    using FirstSet = SampleDictionary<GrammarStructure>;
    using FirstSetNT = SampleDictionary<Nonterminal>;
    using FollowSet = SampleDictionary<Nonterminal>;
    using LLTable = Dictionary<Nonterminal, Dictionary<Terminal, GrammarStructure>>;
    using LRTable = SparseTable<int, GrammarSymbol, LRAction>;
    /*
     *  这个类仅仅包含文法相关的算法 不包含任何分析程序
     *  具体而言包含如下算法:
     *      消除无用符号              =》 待完成，非常简单
     *      判断是否满足递归预测分析
     *      判断是否满足非递归预测分析
     *      左递归判断   
     *      通用左递归消除
     *      文法非终结符first集
     *      文法单元first集          =》 感觉有bug 需要修复
     *      follow集
     *      预测分析表获取 
     *      lr1 分析表获取
     *      slr 分析表获取
     *  这里还缺很多文法变换算法 如消除空产生式、文法规约、消除单产生式
     * **/
    /// <summary>
    ///  上下文无关文法
    /// </summary>
    public class CFG : CSG
    {
        /*
         * TODO: 添加去除无用符号、空产生式、单产生式算法
         * 这里依然返回新的文法 不改变原文法，因为继承的缺陷。
         * 如果修改原文法，在子类的修改必须也在父类中体现。否则如果进行向上转型，则会出现文法不一致
         * 但要保证父类子类一致 又会有一堆麻烦的操作，与其这样不如直接返回一个新的文法 依然保证文法的不变性
         * **/
        protected FirstSetNT first;   //每个非终结符的first集
        protected FirstSet firstSet; //每个文法单元的first集
        protected FollowSet follow; //每个非终结符的follow集
        protected PredictionAnalysisTable predictionAnalysisTable; //ll分析表
        protected ItemsSet itemsSet;  // 有效项目集 
        protected LRTable lrTable; //LR分析表
        protected new Dictionary<Nonterminal, HashSet<GrammarStructure>> grammarProductions;  // 为方便操作这里故意隐藏了父类实现
        /// <summary>
        /// 从产生式构造上下文无关文法，需要满足文法定义
        /// </summary>
        /// <param name="grammarProductions">产生式列表</param>
        /// <param name="startNonterminalSymbol">开始符号，如果为null则选取第一个产生式左部文法符号</param>
        public CFG(List<GrammarProduction> grammarProductions, Nonterminal startNonterminalSymbol = null) :
            base(grammarProductions, startNonterminalSymbol)
        {
            // 父类已经对文法实际类型做出了判断，这里需要确认
            if (grammarType == GrammarType.ContextSensitive || grammarType == GrammarType.ZeroType)
            {
                throw new IllegalGrammarException("文法不符合上下文无关文法定义");
            }
            // 覆盖父类的内部结构 左部仅仅包含非终结符 这样方便后面操作
            this.grammarProductions = new Dictionary<Nonterminal, HashSet<GrammarStructure>>();
            foreach (var ke in base.grammarProductions.Keys)
            {
                var nt = ke.GetFirstNt();
                Debug.Assert(nt != null);
                Debug.Assert(ke.Length() == 1);
                this.grammarProductions.Add(nt, base.grammarProductions[ke]);
            }
        }

        //下面是一堆算法实现
        /****************************************************************************************************************/
        /// <summary>
        /// 文法是否规范化：不包含空产生式、无用符号、单产生式
        /// </summary>
        /// <returns></returns>
        public bool IsNormalized()
        {

            throw new System.NotImplementedException("为实现文法规范化判断");
        }


        /// <summary>
        /// 获取文法的拓广文法
        /// </summary>
        /// <returns></returns>
        public CFG GetExtendGrammar()
        {
            var ls = ToProductionList(grammarProductions);
            Nonterminal newStart = GetNewStart(startNonterminalSymbol);
            GrammarProduction gp = new GrammarProduction(new GrammarStructure(newStart),
                new HashSet<GrammarStructure> { new GrammarStructure(startNonterminalSymbol) });
            ls.Add(gp);
            return new CFG(ls, newStart);
        }
        private Nonterminal GetNewStart(Nonterminal old)
        {
            string newName = old.Value + "'";
            while (newName.Equals(old.Value) || nonterminals.Any(nt => nt.Value.Equals(newName)))
            {
                newName += "'";
            }
            return new Nonterminal(newName);
        }

        /// <summary>
        /// 获取某左部文法符号所有右部产生式
        /// </summary>
        /// <param name="left"></param>
        /// <returns></returns>
        public HashSet<GrammarStructure> GetStructures(Nonterminal left)
        {
            return grammarProductions[left];
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
        protected HashSet<Nonterminal> FindReachable(Nonterminal target)
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
        protected HashSet<Nonterminal> FindDirectReachable(Nonterminal target)
        {
            var stcs = grammarProductions[target];
            HashSet<Nonterminal> result = new HashSet<Nonterminal>();
            foreach (var stc in stcs)
            {
                var nt = stc.FirstNTOrNull();
                if (nt != null)
                {
                    result.Add(nt);
                }
            }
            // 这里如果为空不返回null而是空hashSet
            return result;
        }

        /// <summary>
        /// 通用消除文法直接、间接左递归算法, 不会改变原文法，返回新的被消除左递归的文法
        /// </summary>
        public CFG EliminateRecursive()
        {
            // TODO:需要思考是返回一个新文法还是直接修改原文法
            // 如果修改了内部状态 可能导致用户已经获得的分析表、first集等失效, 因此决定修改为产生新的文法

            if (!IsLeftRecursive())
            {
                return this;
            }
            //将非终结符排序 从前至后，对
            var orderedNT = nonterminals.ToList();
            var len = orderedNT.Count;
            // 首先保留当前数据
            CFGrammarDS oldProdutions = DeepCloneDS();

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
            // 创建新的文法
            CFG ng = new CFG(ToProductionList(grammarProductions), startNonterminalSymbol);
            grammarProductions = oldProdutions;
            return ng;
        }
        /// <summary>
        /// 将cfg中文法的表示转换为父类中的表示
        /// </summary>
        private List<GrammarProduction> ToProductionList(CFGrammarDS ds)
        {
            List<GrammarProduction> nl = new List<GrammarProduction>();
            foreach (var kv in ds)
            {
                nl.Add(new GrammarProduction(new GrammarStructure(kv.Key), kv.Value));
            }
            return nl;
        }
        private CFGrammarDS DeepCloneDS()
        {
            CFGrammarDS newCfg = new CFGrammarDS();
            foreach (var kv in grammarProductions)
            {
                newCfg.Add(kv.Key, new HashSet<GrammarStructure>());
                foreach (var st in kv.Value)
                {
                    newCfg[kv.Key].Add(new GrammarStructure(st.Structure));
                }
            }
            return newCfg;
        }
        /// <summary>
        /// 去除文法符号产生式的直接左递归,仅供内部作为中间过程使用
        /// </summary>
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
            string termi = "Terminals:{ ";
            foreach (var ter in terminals)
            {
                termi += ter + ",";
            }

            termi += "}";
            string nt = "Nonterminals:{ ";
            foreach (var n in nonterminals)
            {
                nt += n + ",";
            }

            nt += "}";
            string grammarStr = $"Grammar:\n{tmp}Type:{grammarType}\nTerminals{termi}\nNonterminals{nt}";
            return grammarStr;
        }

        /// <summary>
        /// 获取文法所有右部文法单元的First集
        /// </summary>
        public FirstSet GetFirstSetOfStructure()
        {
            if (firstSet != null)
            {
                return firstSet;
            }
            _firstSet = new Dictionary<GrammarStructure, HashSet<Terminal>>();
            foreach (var rightSet in grammarProductions.Values)
            {
                foreach (var right in rightSet)
                {
                    if (_firstSet.TryGetValue(right, out HashSet<Terminal> tmp))
                    {
                        tmp.UnionWith(CalFirstOfStructure(right));
                    }
                    else
                    {
                        _firstSet.Add(right, CalFirstOfStructure(right));
                    }
                }
            }
            firstSet = new FirstSet(_firstSet);
            return firstSet;
        }
        /// <summary>
        /// 获取文法每个非终结符号First集
        /// </summary>
        /// <returns></returns>
        public FirstSetNT GetFirstSetOfNonterminals()
        {
            if (first != null)
            {
                return first;
            }
            else
            {
                _first = new Dictionary<Nonterminal, HashSet<Terminal>>();
                CalFirst();
                first = new FirstSetNT(_first);
                return first;
            }
        }
        private bool change = false;
        private Dictionary<GrammarStructure, HashSet<Terminal>> _firstSet;
        private Dictionary<Nonterminal, HashSet<Terminal>> _first;
        private void CalFirst()
        {
            do
            {
                change = false;
                foreach (var nt in nonterminals)
                {
                    if (!_first.ContainsKey(nt))
                    {
                        _first.Add(nt, new HashSet<Terminal>());
                    }
                    DoCal(nt);
                }
            } while (change);
        }
        private void DoCal(Nonterminal nt)
        {
            int startCount = _first[nt].Count;
            var right = grammarProductions[nt];
            foreach (var stc in right)
            {
                // 这里同时加入了空
                if (stc[0].GetSymbolType() == SymbolType.Terminal)
                {
                    _first[nt].Add((Terminal)stc[0]);
                }
            }
            foreach (var stc in right)
            {
                int index = 0;
                if (stc[0].GetSymbolType() != SymbolType.Terminal)
                {
                    foreach (GrammarSymbol sym in stc)
                    {
                        if (sym.GetSymbolType() == SymbolType.Nonterminal)
                        {
                            if (!_first.ContainsKey((Nonterminal)sym))
                            {
                                _first.Add((Nonterminal)sym, new HashSet<Terminal>());
                                DoCal((Nonterminal)sym);
                            }
                            HashSet<Terminal> ntFirst = new HashSet<Terminal>(_first[(Nonterminal)sym]);
                            if (ntFirst.Contains(Terminal.Empty))
                            {
                                ntFirst.Remove(Terminal.Empty);
                                _first[nt].UnionWith(ntFirst);
                                //如果最后非终结符还能推出空 则证明当前文法符号可推出空
                                if (index == stc.Length() - 1)
                                {
                                    _first[nt].Add(Terminal.Empty);
                                }
                            }
                            else
                            {
                                _first[nt].UnionWith(ntFirst);
                                break;
                            }

                        }
                        else { _first[nt].Add((Terminal)sym); break; }
                        index++;
                    }
                }
            }
            if (_first[nt].Count != startCount) { change = true; }
        }
        /// <summary>
        /// 计算可由文法推导的句型的First集
        /// </summary>
        /// <param name="structure">文法可推出的语法单元</param>
        /// <exception cref="IllegalStructureException">非法文法单元</exception>
        public HashSet<Terminal> CalFirstOfStructure(GrammarStructure structure)
        {
            Debug.Assert(structure != null);
            //TODO 这里是否需要添加对输入文法单元得检查？
            if (!structure.Nonterminals.IsSubsetOf(nonterminals))
            {
                throw new IllegalStructureException($"用于计算first集得文法单元必需由当前文法得终结符与非终结符构成:{structure}");
            }

            var firstSt = new HashSet<Terminal>();
            int index = 0;
            foreach (GrammarSymbol sym in structure)
            {
                if (sym.GetSymbolType() == SymbolType.Terminal)
                {
                    firstSt.Add((Terminal)sym);
                    break;
                }
                else
                {
                    // 这里调用First() 而不是直接过去first
                    var tmp = new HashSet<Terminal>(GetFirstSetOfNonterminals()[(Nonterminal)sym]);
                    if (tmp.Contains(Terminal.Empty))
                    {
                        tmp.Remove(Terminal.Empty);
                        firstSt.UnionWith(tmp);
                        if (index == structure.Length() - 1)
                        {
                            firstSt.Add(Terminal.Empty);
                        }
                    }
                    else
                    {
                        firstSt.UnionWith(tmp);
                        break;
                    }
                }
                index++;
            }
            return firstSt;
        }

        // 如果两个算法使用同一个bool 可能会线程不安全
        private bool change2 = false;
        private Dictionary<Nonterminal, HashSet<Terminal>> _follow;//存储中间值
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
            _follow = new Dictionary<Nonterminal, HashSet<Terminal>>
            {
                { startNonterminalSymbol, new HashSet<Terminal> { Terminal.End } }
            };
            do
            {
                change2 = false;
                CalFollow();
            } while (change2);
            follow = new FollowSet(_follow);
            return follow;
        }
        private void CalFollow()
        {
            //对于每一个非终结符得产生式进行遍历 计算每个非终结符的follow
            foreach (var pro in grammarProductions)
            {
                foreach (var strc in pro.Value)
                {
                    // 因为没找到迭代的写法 所以这里很丑
                    for (int i = 0; i < strc.Length(); i++)
                    {
                        if (strc[i].GetSymbolType() == SymbolType.Nonterminal)
                        {
                            Nonterminal current = (Nonterminal)strc[i];
                            if (!_follow.ContainsKey(current))
                            {
                                _follow.Add(current, new HashSet<Terminal>());
                            }

                            int startCount = _follow[current].Count;
                            //如果为最后一个非终结符 则将左部文法符号的follow加入
                            if (i == strc.Length() - 1)
                            {
                                if (!_follow.ContainsKey(pro.Key))
                                {
                                    _follow.Add(pro.Key, new HashSet<Terminal>());
                                }
                                _follow[current].UnionWith(_follow[pro.Key]);
                            }
                            else
                            {

                                var tmp = strc.GetRange(i + 1, strc.Length() - i - 1);
                                var tf = CalFirstOfStructure(tmp);
                                if (tf.Contains(Terminal.GetEmpty()))
                                {
                                    tf.Remove(Terminal.Empty);
                                    _follow[current].UnionWith(tf);
                                    if (!_follow.ContainsKey(pro.Key))
                                    {
                                        _follow.Add(pro.Key, new HashSet<Terminal>());
                                    }
                                    _follow[current].UnionWith(_follow[pro.Key]);
                                }
                                else
                                {
                                    _follow[current].UnionWith(tf);
                                }
                            }
                            if (startCount != _follow[current].Count) { change2 = true; }
                        }
                    }
                }
            }
        }
        protected new void GenSymbols()
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
            if (terminals.Contains(Terminal.Empty))
            {
                terminals.Remove(Terminal.Empty);
            }

            nonterminals.TrimExcess();
            terminals.TrimExcess();
        }
        /// <summary>
        /// 判断文法是否满足 递归调用预测分析程序的要求
        /// </summary>
        /// <returns></returns>
        public bool IsSatisfyRecPredictionAnalysis()
        {
            if (IsLeftRecursive())
            {
                return false;
            }
            // 对每个非终结符的右部文法单元 判断每个文法单元的first集两两之间是否由交集
            GetFirstSetOfStructure();
            foreach (var nt in nonterminals)
            {
                var stcs = grammarProductions[nt].ToList();
                for (int i = 0; i < stcs.Count - 1; i++)
                {
                    var current = stcs[i];
                    for (int j = i + 1; j < stcs.Count; j++)
                    {
                        if (firstSet[current].Any(X => stcs[j].Contains(X)))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        //递归下降 使用的变量，其他地方绝对不使用
        private SymbolStream internalSymBols = null;
        private Terminal currentTeminal = null;
        /// <summary>
        /// 通用递归下降分析 要求文法的那个非终结符的右部文法单元之间first无交集,否则返回false
        /// </summary>
        /// <returns></returns>
        public bool RecursiveAnalyze(SymbolStream symbolIter)
        {
            if (!IsSatisfyRecPredictionAnalysis())
            {
                return false;
            }
            internalSymBols = symbolIter ?? throw new System.ArgumentNullException();
            currentTeminal = internalSymBols.Next();
            return DoRecs(startNonterminalSymbol);
        }
        // 递归判断，目前还未加入分析树生成，因为还未实现树结构,还未加入处理回溯的逻辑，目前想到的处理回溯的方式会导致搜索爆炸
        private bool DoRecs(Nonterminal nt)
        {
            var stcs = FindTargetStuc(nt, currentTeminal); // 获取first集包含currentTerminal的stucture
            // 每次尤其只能有一个文法文法被选来匹配
            if (stcs == null || stcs.Count != 1)
            {
                return false;
            }
            var stc = stcs[0];
            foreach (GrammarSymbol sym in stc)
            {
                // 每一步都需要确保 流还未消耗结束
                if (currentTeminal == null)
                {
                    return false;
                }
                if (sym.GetSymbolType() == SymbolType.Terminal)
                {
                    if (sym.Equals(currentTeminal))
                    {
                        currentTeminal = internalSymBols.Next();
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (!DoRecs((Nonterminal)sym))
                    {
                        return false;
                    }
                }
            }
            // 如果是开始符号被匹配 但流还未消耗结束 则识别失败
            if (nt.Equals(startNonterminalSymbol) && internalSymBols.HasNext())
            {
                return false;
            }
            return true;
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
                var fi = GetFirstSetOfStructure()[stc];
                if (fi.Contains(t))
                {
                    target.Add(stc);
                }
            }
            return target.Count == 0 ? null : target;
        }
        //未完成, 目标获取通用LL分析程序
        /// <summary>
        /// 获取文法预测分析表
        /// </summary>
        /// <returns></returns>
        public PredictionAnalysisTable GetPATable()
        {
            if (predictionAnalysisTable != null)
            {
                return predictionAnalysisTable;
            }
            if (!IsSatisfyNonrecuPredictionAnalysis())
            {
                throw new IllegalGrammarException("当前文法不使用于非递归预测分析");
            }
            GetFirstSetOfStructure();
            GetFollow();

            LLTable llDs = new LLTable();
            HashSet<Terminal> terminalsWithEnd = new HashSet<Terminal>(terminals)
            {
                Terminal.End
            };
            Debug.Assert(!terminals.Contains(Terminal.Empty));
            foreach (var kv in grammarProductions)
            {
                llDs.Add(kv.Key, new Dictionary<Terminal, GrammarStructure>());
                // 对每个产生式的每个右部文法单元 检查每个终结符是否在当前文法单元的first集 如果不在且first包含空则
                // 检查当前非终结符的follow集
                foreach (var stc in kv.Value)
                {
                    // 下面这个写法很显然需要修改 但现在挺稳定 所以先保留
                    foreach (var ter in terminalsWithEnd)
                    {
                        if (ter.Equals(Terminal.GetEmpty())) { continue; }
                        if (firstSet[stc].Contains(ter))
                        {
                            if (llDs[kv.Key].ContainsKey(ter))
                            {
                                throw new IllegalGrammarException("尝试为不满足ll(1)文法要求的文法创建ll分析表");
                            }
                            else
                            {
                                llDs[kv.Key].Add(ter, stc);
                            }
                        }
                        else if (firstSet[stc].Contains(Terminal.Empty) && follow[kv.Key].Contains(ter))
                        {
                            llDs[kv.Key].Add(ter, stc);
                        }
                    }
                }
            }
            predictionAnalysisTable = new PredictionAnalysisTable(llDs, this);
            return predictionAnalysisTable;
        }

        /// <summary>
        /// 判断文法是否适合使用非递归预测分析
        /// </summary>
        /// <returns>返回值 当文法满足LL文法定义</returns>
        public bool IsSatisfyNonrecuPredictionAnalysis()
        {
            GetFirstSetOfStructure();
            GetFollow();
            //满足递归预测分析 则满足了first集之间的交集为空
            if (!IsSatisfyRecPredictionAnalysis())
            {
                return false;
            }
            //接下来判断 若某非终结符的候选式可推出空 则判断first与follow是否有交集
            // TODO: 完成完整判断
            foreach (var kv in grammarProductions)
            {
                foreach (var stc in kv.Value)
                {
                    if (firstSet[stc].Contains(Terminal.GetEmpty()))
                    {
                        // 判断除当前元素外 其他任何一个元素的first集若与当前非终结符的follow交集不为空 则不满足ll文法要求
                        if (kv.Value.Where(x => !x.Equals(stc)).Any(x => firstSet[x].Intersect(follow[kv.Key]).Count() != 0))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// 获取文法的项目集规范族
        /// </summary>
        /// <returns></returns>
        public ItemsSet GetItemsSet()
        {
            if (itemsSet != null)
            {
                return itemsSet;
            }
            // TODO BUG!!!!!!!!!!!!!
            VaildItemSet root = new VaildItemSet(this); //创建空串的有效项目集
            // TODO:这里需要确保 root一定为项目集的第一个元素
            List<VaildItemSet> result = new List<VaildItemSet>();
            List<VaildItemSet> newVIS = new List<VaildItemSet> { root };
            HashSet<GrammarSymbol> all = new HashSet<GrammarSymbol>();
            all.UnionWith(nonterminals); all.UnionWith(terminals);
            Debug.Assert(!terminals.Contains(Terminal.Empty));
            do
            {
                var tmp = new List<VaildItemSet>();
                foreach (var vi in newVIS)
                {
                    result.Add(vi);
                    foreach (var sym in all)
                    {
                        var newVI = vi.Go(sym);
                        if (newVI != null && !result.Contains(newVI) && !newVIS.Contains(newVI))
                        {
                            tmp.Add(newVI);
                        }
                    }
                }
                newVIS = tmp;
            } while (newVIS.Count != 0);
            itemsSet = new ItemsSet(result, root);
            return itemsSet;
        }

        /// <summary>
        /// 获取文法的LR1分析表
        /// </summary>
        /// <returns></returns>
        public LRTable GetLRTable()
        {
            if (lrTable != null)
            {
                return lrTable;
            }

            lrTable = GetItemsSet().ToLRTable();
            return lrTable;
        }
    }

}
