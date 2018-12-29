using CLK.GrammarCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
namespace CLK.AnalysisDs
{
    using LLTable = Dictionary<Nonterminal, Dictionary<Terminal, GrammarStructure>>;
    using LRTable = SparseTable<int, GrammarSymbol, LRAction>;

    /// <summary>
    /// 非终结符和文法单元first集、非终结符follow集的结构,只读
    /// </summary>
    /// <typeparam name="T">只能是Nonterminal或GrammarStructure</typeparam>
    public class SampleDictionary<T>
    {
        private Dictionary<T, HashSet<Terminal>> keySet;
        public SampleDictionary(Dictionary<T, HashSet<Terminal>> keySet)
        {
            if (!(typeof(T).Equals(typeof(Nonterminal)) || typeof(T).Equals(typeof(GrammarStructure))))
            {
                throw new Exception("类型参数错误");
            }
            this.keySet = keySet;
        }

        public override bool Equals(object obj)
        {
            Dictionary<T, HashSet<Terminal>> tmp = (Dictionary<T, HashSet<Terminal>>)obj;
            foreach (var key in keySet.Keys)
            {
                if (tmp.TryGetValue(key, out HashSet<Terminal> values))
                {
                    if (!values.SequenceEqual(keySet[key]))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public HashSet<Terminal> Get(T key)
        {
            if (!keySet.ContainsKey(key))
            {
                throw new KeyNotFoundException($"当前输入<{key}>不在key集合中");
            }

            return keySet[key];
        }

        public override int GetHashCode()
        {
            return -25021980 + EqualityComparer<Dictionary<T, HashSet<Terminal>>>.Default.GetHashCode(keySet);
        }

        public override string ToString()
        {
            string tmp = "";
            foreach (var key in keySet.Keys)
            {
                tmp += key + "=> {";
                foreach (var value in keySet[key])
                {
                    tmp += value + ", ";
                }
                tmp += "}\n";
            }
            return tmp.Remove(tmp.LastIndexOf('\n'));
        }
        public HashSet<Terminal> this[T index]
        {
            get { return keySet[index]; }
        }
    }

    /// <summary>
    /// 用于表示各类table的泛型类 如LL分析表、SLR、LR1分析表
    /// </summary>
    /// <typeparam name="R">行类型</typeparam>
    /// <typeparam name="C">列类型</typeparam>
    /// <typeparam name="V">表项类型</typeparam>
    public class SparseTable<R, C, V>
    {
        private Dictionary<R, Dictionary<C, V>> table;
        private List<C> cols;
        private DataTable interExp;
        /// <summary>
        /// 通过映射表构建table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="cols"></param>
        public SparseTable(Dictionary<R, Dictionary<C, V>> table, List<C> cols)
        {
            // TODO:这里为了方便 直接引用了客户传递的dic 但这会破坏不变性
            this.table = table;
            this.cols = cols;
        }
        public V GetItem(R row, C col)
        {
            return table[row][col];
        }
        public void Print()
        {
            if (interExp == null)
            {
                interExp = new DataTable();
                interExp.Columns.Add(" ", typeof(string));
                foreach (var t in cols)
                {
                    interExp.Columns.Add(t.ToString(), typeof(string));
                }
                foreach (var kv in table)
                {
                    DataRow dataRow = interExp.NewRow();
                    dataRow[" "] = kv.Key.ToString();
                    foreach (var item in kv.Value)
                    {
                        dataRow[item.Key.ToString()] = item.Value.ToString();
                    }
                    interExp.Rows.Add(dataRow);
                }
            }
            interExp.Print();
        }
    }
    /// <summary>
    /// 预测分析表 这部分应该被去掉 但因为实现的比较早，没法简单的去掉 所以保留
    /// </summary>
    public class PredictionAnalysisTable
    {

        private Dictionary<Nonterminal, Dictionary<Terminal, GrammarStructure>> table;
        private DataTable interExp; //目前仅仅用于好看的打印
        private CFG fatherGrammar;
        public PredictionAnalysisTable(LLTable table, CFG fatherGrammar)
        {
            this.table = table;
            this.fatherGrammar = fatherGrammar;
        }
        /// <summary>
        /// 获取表项 如果不存在则返回null
        /// </summary>
        /// <param name="row">行索引</param>
        /// <param name="col">列索引</param>
        /// <returns>Structure</returns>
        public GrammarStructure GetItem(Nonterminal row, Terminal col)
        {
            return table[row][col];
        }
        public Dictionary<Terminal, GrammarStructure> GetLine(Nonterminal row)
        {
            return table[row];
        }

        public void Print()
        {

            if (interExp == null)
            {
                interExp = new DataTable("LLTable");
                interExp.Columns.Add("Nonterminals", typeof(Nonterminal));
                foreach (var t in fatherGrammar.Terminals)
                {
                    if (!t.Equals(Terminal.GetEmpty()))
                    {
                        interExp.Columns.Add(t.ToString(), typeof(GrammarStructure));
                    }
                }
                interExp.Columns.Add(Terminal.End.ToString(), typeof(GrammarStructure));
                foreach (var kv in table)
                {
                    DataRow dataRow = interExp.NewRow();
                    dataRow["Nonterminals"] = kv.Key;
                    foreach (var item in kv.Value)
                    {
                        dataRow[item.Key.Value] = item.Value;
                    }
                    interExp.Rows.Add(dataRow);
                }
            }
            interExp.Print();
        }
        /// <summary>
        /// 返回表格形式
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (interExp == null)
            {
                interExp = new DataTable("LLTable");
                interExp.Columns.Add("Nonterminals", typeof(Nonterminal));
                foreach (var t in fatherGrammar.Terminals)
                {
                    interExp.Columns.Add(t.ToString(), typeof(GrammarStructure));
                }
                interExp.Columns.Add(Terminal.End.ToString(), typeof(GrammarStructure));
                foreach (var kv in table)
                {
                    DataRow dataRow = interExp.NewRow();
                    dataRow["Nonterminals"] = kv.Key;
                    foreach (var item in kv.Value)
                    {
                        dataRow[item.Key.Value] = item.Value;
                    }
                }
            }
            return interExp.ToString();
        }
    }

    public enum LRItemState { Reduction, Shift, ReductionExpect };
    /// <summary>
    ///  描述LR(1)项目，将被用为SLR,LR分析,具有不变性
    /// </summary>
    public class LRItem
    {
        private Nonterminal left;
        private GrammarStructure right; //以上两项构成产生式
        private int progress; // 表示产生式被识别的程度 值为 0~len， 取0，代表期待输入，取len 表示已经识别
        private readonly int len;
        private Terminal lookahead; //LR1的向前看符号 将来可能考虑使用列表来支持LRK项目 
        public Terminal Lookahead { get => lookahead; }
        public Nonterminal Left { get => left; }
        public GrammarStructure Right { get => right; }

        /// <summary>
        /// 创建LR0项目 目前使用
        /// </summary>
        public LRItem(Nonterminal left, GrammarStructure right)
        {
            if (left == null || right == null)
            {
                throw new System.ArgumentNullException();
            }

            this.left = left;
            this.right = right;
            progress = 0;
            len = right.Length();
            lookahead = null;
        }
        public LRItem(Nonterminal left, GrammarStructure right, Terminal lookahead) : this(left, right)
        {
            this.lookahead = lookahead;
        }
        public LRItem(LRItem another)
        {
            left = another.left;
            right = another.right;
            progress = another.progress;
            len = another.len;
            lookahead = another.lookahead;
        }
        /// <summary>
        /// 返回向前移动一个后的新项目
        /// </summary>
        /// <returns>移动成功返回新项目，否则返回null</returns>
        public LRItem Move(GrammarSymbol symbol)
        {
            if (progress > len)
            {
                throw new System.InvalidOperationException("已被识别的项目不可继续move");
            }
            else if (progress == len) //若果相等 意味着为归约项目
            {
                return null;
            }
            if (!right[progress].Equals(symbol))
            {
                return null;
            }

            var tmp = new LRItem(this);
            tmp.progress++;
            return tmp;
        }
        //一个LR项目可能是 移进项目、待约项目、归约项目、接受项目;
        /// <summary>
        /// 获取当前项目状态：移进项目、待约项目、归约项目
        /// </summary>
        /// <returns></returns>
        public LRItemState GetState()
        {
            LRItemState tmp;
            if (progress == len)
            {
                tmp = LRItemState.Reduction;
            }
            else
            {
                tmp = right[progress].GetSymbolType() == SymbolType.Terminal ? LRItemState.Shift : LRItemState.ReductionExpect;
            }
            return tmp;
        }
        /// <summary>
        /// 获取当前LR项目的待识别文法符号
        /// </summary>
        /// <returns></returns>
        public GrammarSymbol GetCurrent()
        {
            if (progress > len)
            {
                throw new System.InvalidOperationException();
            }

            return right[progress];
        }
        /// <summary>
        /// 获取除当前待识别文法符号的剩余符号构成的文法单元,如果未规约状态，则返回null
        /// </summary>
        /// <returns></returns>
        public GrammarStructure GetRest()
        {
            // TODO:加一些检查
            if (progress < len - 1)
            {
                return right.GetRange(progress + 1, len - (progress + 1));
            }
            else if (progress == len - 1)
            {
                return null;
            }
            else
            {
                throw new System.InvalidOperationException("算法错误，当前项目已经为无效状态");
            }
        }

        public override bool Equals(object obj)
        {
            var item = obj as LRItem;
            return item != null &&
                   EqualityComparer<Nonterminal>.Default.Equals(left, item.left) &&
                   EqualityComparer<GrammarStructure>.Default.Equals(right, item.right) &&
                   progress == item.progress &&
                   len == item.len &&
                   EqualityComparer<Terminal>.Default.Equals(lookahead, item.lookahead);
        }

        public override int GetHashCode()
        {
            var hashCode = -580352494;
            hashCode = hashCode * -1521134295 + EqualityComparer<Nonterminal>.Default.GetHashCode(left);
            hashCode = hashCode * -1521134295 + EqualityComparer<GrammarStructure>.Default.GetHashCode(right);
            hashCode = hashCode * -1521134295 + progress.GetHashCode();
            hashCode = hashCode * -1521134295 + len.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Terminal>.Default.GetHashCode(lookahead);
            return hashCode;
        }

        public override string ToString()
        {
            string tmp = "";
            tmp += left + "=>";
            var tmp2 = right.ToString();
            string item = progress == len ? tmp2 + " * " : tmp2.Insert(progress, " * ");
            tmp += item;
            tmp += ", " + lookahead;
            return tmp;
        }
    }
    /// <summary>
    /// 活前缀有效项目集
    /// </summary>
    public class VaildItemSet
    {
        private GrammarStructure prefix; //活前缀
        private HashSet<LRItem> items;  //有效项目集
        // 用于链接各个有效项目集 从而形成自动机
        private Dictionary<GrammarSymbol, VaildItemSet> son;
        private CFG grammar; //指向文法
        // TODO: 这里使用了可能不同原文法的文法
        public Dictionary<GrammarSymbol, VaildItemSet> Son { get => son; }
        public GrammarStructure Prefix { get => prefix; }
        public HashSet<LRItem> Items { get => items; }
        public CFG Grammar { get => grammar; }



        /// <summary>
        /// 从文法的拓广文法构建空的有效项目集
        /// </summary>
        /// <param name="grammar"></param>
        public VaildItemSet(CFG grammar)
        {
            var tmp = grammar.GetStructures(grammar.StartNonterminalSymbol);
            // 接受一个上下文无关文法 构建空字符的有效项目集
            this.grammar = tmp.Count != 1 ? grammar.GetExtendGrammar() : grammar;
            var starts = this.grammar.GetStructures(this.grammar.StartNonterminalSymbol);
            Debug.Assert(starts.Count == 1); // 拓广文法开始符号仅有一个右部产生式
            // S' => . S , $    
            LRItem item = new LRItem(this.grammar.StartNonterminalSymbol, starts.First(), Terminal.End);
            items = Closure(new HashSet<LRItem> { item });
            prefix = new GrammarStructure(Terminal.Empty);
            son = new Dictionary<GrammarSymbol, VaildItemSet>();
        }
        /// <summary>
        /// 创建 某活前缀的有效项目集
        /// </summary>
        /// <param name="prefix">活前缀</param>
        /// <param name="items">有效项目集</param>
        /// <param name="grammar">关联文法</param>
        public VaildItemSet(GrammarStructure prefix, HashSet<LRItem> items, CFG grammar)
        {
            this.prefix = prefix;
            this.items = items;
            this.grammar = grammar;
            son = new Dictionary<GrammarSymbol, VaildItemSet>();
        }
        /// <summary>
        /// 从当前活前缀的有效项目集计算GO
        /// </summary>
        /// <param name="symbol">文法符号</param>
        /// <returns>若转移成功返回新活前缀的有效项目集，否则返回Null</returns>
        public VaildItemSet Go(GrammarSymbol symbol)
        {
            // 求每一个LR项目的后继 随后闭包运算
            var newItems = new HashSet<LRItem>();
            foreach (var item in items)
            {
                var tmp = item.Move(symbol);
                if (tmp != null)
                {
                    newItems.Add(tmp);
                }
            }
            if (newItems.Count == 0)
            {
                return null;
            }

            var resultItems = Closure(newItems);
            var result = new VaildItemSet(prefix.Append(symbol), resultItems, grammar);
            // 加入当前节点的子节点
            if (!son.ContainsKey(symbol))
            {
                son.Add(symbol, result);
            }
            return result;
        }
        /// <summary>
        /// 计算输入项目集的闭包
        /// </summary>
        public HashSet<LRItem> Closure(HashSet<LRItem> lRItems)
        {
            HashSet<LRItem> result = new HashSet<LRItem>();
            HashSet<LRItem> newItems = new HashSet<LRItem>(lRItems);
            do
            {

                var tmp = new HashSet<LRItem>();
                foreach (var item in newItems)
                {
                    Debug.Assert(!result.Contains(item));
                    result.Add(item);
                    if (item.GetState() == LRItemState.ReductionExpect)
                    {
                        var nt = (Nonterminal)item.GetCurrent();
                        GrammarStructure rest = item.GetRest();// 获取当前非终结符之后的所有串 用于求first集
                        if (rest == null) // 处理 a.A 这样的情况
                        {
                            rest = new GrammarStructure(item.Lookahead);
                        }
                        else
                        {
                            rest = rest.Append(item.Lookahead);
                        }

                        var first = grammar.CalFirstOfStructure(rest);
                        var structs = grammar.GetStructures(nt);
                        foreach (var stcs in structs)
                        {
                            foreach (Terminal ter in first)
                            {
                                var newItem = new LRItem(nt, stcs, ter); // 计算后
                                if (!result.Contains(newItem) && !newItems.Contains(newItem)) { tmp.Add(newItem); }
                            }
                        }
                    }
                }
                newItems = tmp;
            } while (newItems.Count != 0);
            return result;
        }

        public override bool Equals(object obj)
        {
            // 只要项目集合相同则为等价
            var set = obj as VaildItemSet;
            return set != null &&
                   items.SequenceEqual(set.items);
        }
        //TODO:这里的hashcode计算可能不符合要求
        public override int GetHashCode()
        {
            var hashCode = -518340674;
            hashCode = hashCode * -1521134295 + EqualityComparer<HashSet<LRItem>>.Default.GetHashCode(items);
            return hashCode;
        }

        public override string ToString()
        {
            string tmp = "prefix:<" + prefix.ToString() + ">, itemsSet:{ ";
            foreach (var item in items)
            {
                tmp += "[ ";
                tmp += item.ToString() + " ";
                tmp += "],";
            }
            tmp += " }";

            return tmp;
        }
    }
    /// <summary>
    /// 项目集规范族
    /// </summary>
    public class ItemsSet
    {
        private List<VaildItemSet> itemSetClass;
        private VaildItemSet root;
        private LRTable tableBuf; // 缓存table

        public ItemsSet(List<VaildItemSet> itemSetClass, VaildItemSet root)
        {
            this.itemSetClass = itemSetClass;
            this.root = root;
        }
        /// <summary>
        /// 打印项目集规范族构成的DFA
        /// </summary>
        public void PrintDFA()
        {
            // TODO:BUGS ! 在生成邮箱项目集的同时构建的DFA做法存在问题
            DoPrint(root);
        }
        void DoPrint(VaildItemSet node)
        {
            Console.Write($"{node.Prefix}: [ ");
            foreach (var sub in node.Son)
            {
                Console.Write($"{sub.Key}=>{sub.Value.Prefix},");
            }
            Console.Write("]\n");
            foreach (var sub in node.Son)
            {
                DoPrint(sub.Value);
            }
        }

        /// <summary>
        /// 将项目集规范组转为LR1分析表
        /// </summary>
        /// <returns></returns>
        public LRTable ToLRTable()
        {
            if (tableBuf != null)
            {
                return tableBuf;
            }

            Dictionary<int, Dictionary<GrammarSymbol, LRAction>> table = new Dictionary<int, Dictionary<GrammarSymbol, LRAction>>();
            // 这里很难看 c#似乎没有更好的foreach with index方法了
            foreach (var itemSet in itemSetClass.Select((x, i) => new { Value = x, Index = i }))
            {
                int state = itemSet.Index;
                if (!table.ContainsKey(state))
                {
                    table.Add(state, new Dictionary<GrammarSymbol, LRAction>());
                }

                foreach (var item in itemSet.Value.Items)
                {
                    switch (item.GetState())
                    {
                        case LRItemState.Reduction:
                            GrammarStructure reduceTarget = item.Right;
                            Nonterminal result = item.Left;
                            Terminal lookahead = item.Lookahead;
                            LRActionType action = result.Equals(itemSet.Value.Grammar.StartNonterminalSymbol) ? LRActionType.Acc : LRActionType.Reduce;
                            object actionItem = action == LRActionType.Acc ? null : new ReduceAction(result, reduceTarget.Length());
                            if (action == LRActionType.Acc)
                            {
                                Debug.Assert(lookahead.Equals(Terminal.End));
                            }

                            table[state].Add(lookahead, new LRAction(action, actionItem));
                            break;
                        //无论是
                        case LRItemState.ReductionExpect:
                            GrammarSymbol _input = item.GetCurrent();
                            int _nextState = itemSetClass.IndexOf(itemSet.Value.Son[_input]);
                            if (!table[state].ContainsKey(_input))
                            {
                                table[state].Add(_input, new LRAction(LRActionType.Move, _nextState));
                            }

                            break;
                        case LRItemState.Shift:
                            GrammarSymbol input = item.GetCurrent();
                            int nextState = itemSetClass.IndexOf(itemSet.Value.Son[input]); //获取下一有效项目集的代表状态
                            Debug.Assert(nextState >= 0);
                            if (!table[state].ContainsKey(input))
                            {
                                table[state].Add(input, new LRAction(LRActionType.Shift, new ShiftAction(nextState)));
                            }

                            break;
                    };
                }
            }
            List<GrammarSymbol> cols = root.Grammar.GetAllSymbols();
            cols.Insert(0, Terminal.End);
            cols.Remove(root.Grammar.StartNonterminalSymbol);
            tableBuf = new LRTable(table, cols);
            return tableBuf;

        }

        public override string ToString()
        {
            string tmp = "项目集规范族:\n";
            foreach (var vi in itemSetClass)
            {
                tmp += vi + "\n";
            }
            return tmp;
        }
    }
    public enum LRActionType { Shift, Reduce, Acc, Move }
    // 并不需要实际关联一个产生式 只需要知道归约的结果和长度即可
    public class ReduceAction
    {
        private Nonterminal reduceResult;
        private int reduceLength;

        public ReduceAction(Nonterminal reduceResult, int reduceLength)
        {
            this.reduceResult = reduceResult;
            this.reduceLength = reduceLength;
        }

        public Nonterminal ReduceResult { get => reduceResult; }
        public int ReduceLength { get => reduceLength; }

        public override string ToString()
        {
            return "Reduce";
        }
    }
    public class ShiftAction
    {
        private int nextState;

        public ShiftAction(int nextState)
        {
            this.nextState = nextState;
        }

        public int NextState { get => nextState; }

        public override string ToString()
        {
            return "Shift" + nextState;
        }
    }
    public class LRAction
    {
        private LRActionType actionType;
        private object action;  //动作类型 关联的操作对象 如：规约动作关联的状态转移;

        public LRAction(LRActionType actionType, object action)
        {
            this.actionType = actionType;
            //下面的传唤用于检测关联动作是否合法 目的并不是类型转换
            switch (actionType)
            {
                case LRActionType.Acc:
                    this.action = null; break;
                case LRActionType.Move:
                    this.action = (int)action;  //状态用int值表示
                    break;
                case LRActionType.Reduce:
                    this.action = (ReduceAction)action;
                    break;
                case LRActionType.Shift:
                    this.action = (ShiftAction)action;
                    break;
            }
        }

        public object Action { get => action; }
        public LRActionType ActionType { get => actionType; }

        public override string ToString()
        {
            string tmp = "";
            if (action == null)
            {
                tmp = "ACC";
            }
            else
            {
                tmp = action.ToString();
            }

            return tmp;
        }
    }

}
