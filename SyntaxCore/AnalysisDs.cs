using CLK.GrammarCore;
using System.Collections.Generic;
using System.Data;

namespace CLK.AnalysisDs
{
    using LLTable = Dictionary<Nonterminal, Dictionary<Terminal, GrammarStructure>>;
    //尽管内部结构为dic 但应该封装一层
    // 防止用户随意修改，逻辑也更清楚
    public class FirstSet
    {

    }
    public class FollowSet
    {

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
