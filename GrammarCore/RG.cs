using ErrorCore;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace CLK.GrammarCore
{
    /// <summary>
    ///  正规文法
    /// </summary>
    public class RG : CFG
    {
        private DFA dfa;

        public RG(List<GrammarProduction> grammarProductions, Nonterminal startNonterminalSymbol = null) :
            base(grammarProductions, startNonterminalSymbol)
        {
            if (grammarType == GrammarType.ContextFree)
            {
                throw new IllegalGrammarException("文法不符合正则文法定义");
            }
        }
        /// <summary>
        /// 文法是否为有线性文法
        /// </summary>
        /// <returns></returns>
        public bool IsRightLinearGrammar()
        {
            //TODO:完成线性文法判断
            throw new System.NotImplementedException();
        }
        /// <summary>
        /// 去除传递符号、空转移、归一为左递归
        /// </summary>
        public RG Normalize()
        {
            //TODO:左线性到右线性转换、传递符号消除、空产生式消除
            throw new System.NotImplementedException();
        }


        /// <summary>
        /// 有线性文法到DFA转换
        /// </summary>
        public DFA ToDFA()
        {
            //TODO: 思考 这个方法放在这里是否合理
            if (this.dfa != null)
            {
                return this.dfa;
            }
            // 一个终结符可以对应多个转移 因此使用hashSet
            var dfa = new Dictionary<Nonterminal, Dictionary<Terminal, HashSet<Nonterminal>>>();
            Nonterminal end = GenEndState();//生成一个终态,
            dfa.Add(end, new Dictionary<Terminal, HashSet<Nonterminal>>());
            // 遍历每个产生式 如果 为A => a 则将 在dfa中加入 (A, {a:end}) 如果为 A=>aB 加入 (A,{a:B})
            // 下面的做法确保只有一个终态
            foreach (var k in grammarProductions.Keys)
            {
                dfa.Add(k, new Dictionary<Terminal, HashSet<Nonterminal>>());
                foreach (var stc in grammarProductions[k])
                {
                    var tmp = stc[0]; //如果转换失败 说明不是右线性文法，说明之前的判断存在问题
                    Nonterminal target = null;
                    // A=>a （a可以为空）或 A =>B,如果为空 则产生一条到达终态的空转移
                    // 如果为非终结符 则产生从当前状态到该状态的空转移
                    if (stc.Length() == 1)
                    {
                        if (tmp.GetSymbolType() == SymbolType.Nonterminal)
                        {
                            target = (Nonterminal)tmp;
                            tmp = Terminal.Empty;
                        }
                        else
                        {
                            target = end;
                        }
                    }
                    // TODO: 这里需要确保已被转换为右线性 否则转换错误
                    else
                    {
                        target = (Nonterminal)stc[1];
                    }
                    if (dfa[k].TryGetValue((Terminal)tmp, out HashSet<Nonterminal> set))
                    {
                        set.Add(target);
                    }
                    else
                    {
                        dfa[k].Add((Terminal)tmp, new HashSet<Nonterminal> { target });
                    }
                }
            }
            this.dfa = new DFA(dfa, startNonterminalSymbol, new HashSet<Nonterminal> { end });
            return this.dfa;
        }
        private Nonterminal GenEndState()
        {
            string name = "END";
            Nonterminal end = new Nonterminal("END");
            while (grammarProductions.ContainsKey(end))
            {
                name += "'";
                end = new Nonterminal(name);
            }
            return end;
        }
    }
    /// <summary>
    /// 通用DFA 
    /// </summary>
    public class DFA
    {
        /*
         * 目前的设计是这里仅仅是 DFA数学定义的体现 并不作为串的识别
         * **/
        private readonly Dictionary<Nonterminal, Dictionary<Terminal, HashSet<Nonterminal>>> dfa;
        private readonly Nonterminal startState;
        private readonly HashSet<Nonterminal> endStates;
        private HashSet<Nonterminal> states;
        private HashSet<Terminal> terminals;

        /// <summary>
        /// 默认定义方式，通过转移函数，开始状态，终结状态定义
        /// </summary>
        public DFA(Dictionary<Nonterminal, Dictionary<Terminal, HashSet<Nonterminal>>> dfa, Nonterminal startState, HashSet<Nonterminal> endStates)
        {
            CheckPara(dfa, startState, endStates);
            this.dfa = dfa; //这里直接引用了传递的参数 因此需要确保外部不再修改, 否则内部的不可变性被破坏
            this.startState = startState;
            this.endStates = new HashSet<Nonterminal>(endStates);
            GenSymbol();
        }
        private void CheckPara(Dictionary<Nonterminal, Dictionary<Terminal, HashSet<Nonterminal>>> dfa, Nonterminal startState, HashSet<Nonterminal> endStates)
        {
            // dfa endstate不能为空 startstate、endstate必须在 dfa中 
            // TODO: 处理无用状态
            if (dfa.Count == 0 || endStates.Count == 0)
            {
                throw new System.ArgumentException("用于构建dfa的参数不可长度不可为0");
            }
            if (!dfa.Keys.Contains(startState) || !endStates.IsSubsetOf(dfa.Keys))
            {
                throw new System.ArgumentException("用于构建dfa的开始状态或终结状态必须是DFA的状态集的成员");
            }
        }
        private void GenSymbol()
        {
            states = new HashSet<Nonterminal>();
            terminals = new HashSet<Terminal>();
            foreach (var kv in dfa)
            {
                states.Add(kv.Key);
                foreach (var ter in kv.Value.Keys)
                {
                    terminals.Add(ter);
                }
            }
            System.Console.WriteLine($"TERMINAL:{terminals.Count},State:{states.Count}");
        }
        public RG ToRegularGrammar()
        {
            //TODO: 完成DFA to RG
            throw new System.NotImplementedException();
        }
        /// <summary>
        /// 获取转移函数值
        /// </summary>
        /// <param name="state">状态</param>
        /// <param name="terminal">终结符</param>
        /// <returns>转移状态</returns>
        public HashSet<Nonterminal> Move(Nonterminal state, Terminal terminal)
        {
            return dfa[state][terminal];
        }
        public DFA Minimize()
        {
            //TODO: 完成DFA极小化
            throw new System.NotImplementedException();
        }
        /// <summary>
        /// 表格形式打印
        /// </summary>
        public void Print()
        {
            foreach (var kv in dfa)
            {
                System.Console.Write($"{kv.Key} => ");
                foreach (var inner in kv.Value)
                {
                    System.Console.Write($"{inner.Key} : {inner.Value.Count}");
                }
                System.Console.Write("\n");
            }
            var table = Tmp.DicToTable(dfa, terminals, "DFA", "State");
            table.Print();
        }
    }
    // 暂时没想到去处
    class Tmp
    {
        public static string HashToString<T>(HashSet<T> data)
        {
            string tmp = "";
            foreach (var da in data)
            {
                tmp += da.ToString() + " ";
            }
            return tmp;
        }
        public static DataTable DicToTable<R, C, V>(Dictionary<R, Dictionary<C, HashSet<V>>> dic, HashSet<C> colName, string tableName, string firstCol = "")
        {
            DataTable table = new DataTable(tableName);
            table.Columns.Add(firstCol, typeof(string));
            foreach (var cname in colName)
            {
                table.Columns.Add(cname.ToString(), typeof(string));
            }
            foreach (var kv in dic)
            {
                DataRow row = table.NewRow();
                foreach (var v in kv.Value)
                {
                    row[v.Key.ToString()] = HashToString(v.Value);
                }
                table.Rows.Add(row);
            }
            return table;
        }
    }
}
