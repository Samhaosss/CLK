using CLK.GrammarCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace CLK.AnalysisDs
{
    using LLTable = Dictionary<Nonterminal, Dictionary<Terminal, GrammarStructure>>;


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
    /// 预测分析表
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
    class AnalysisDs
    {
    }
}
