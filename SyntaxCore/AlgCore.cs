using CLK.util;
using System;
using System.Collections.Generic;
namespace SyntaxCore
{



    /*
     * 此类描述 {key:{ value1,value2, ... }} 用于first集 follow集
     * FirstSet  => KeySet<GrammarStructure,Terminals>
     * FollowSet => KeySet<Nonterminals,Terminals>
     * **/

    using FirstSet = KeySet<GrammarStructure, Terminals>;
    using FollowSet = KeySet<Nonterminals, Terminals>;
    public class KeySet<T, V>
    {
        private T key;
        private HashSet<V> value;
        public T Key { get => key; set => key = value; }
        public HashSet<V> Value { get => value; set => this.value = value; }
    }
    /*
     *  待完成的数据结构:
     *      预测分析表
     *      项目
     *      有效项目集
     *      项目集规范组
     *      DFA
     *      SLR分析表
     *      LR1分析表
     * **/
    /*
     *  下面尝试实现一个 通用的表结构的接口
     *  将表的行视为主键 表中的每个项为一个tuple =><列值,表格值>
     *  一个主键将对应一个tuple的set, 包含tuple的set被实现为每个主键的items
     *  首先将实现Items、table接口,每个具体表都实现此接口
     * **/

    // 下面是未完成代码
    public interface IItems<ColType, VType>
    {
        void Add(Tuple<ColType, VType> item);
        void Delete(Tuple<ColType, VType> item);
    }
    public interface ITable<RowType, ColType, VType>
    {
        void AddLine(RowType key, ColType value);
    }

    public class PredictionTableItem
    {
        private HashSet<Tuple<Terminals, GrammarProduction>> item;
        public HashSet<Tuple<Terminals, GrammarProduction>> Item { get => item; set => item = value; }

        public PredictionTableItem() { item = new HashSet<Tuple<Terminals, GrammarProduction>>(); }
        public void Add(Tuple<Terminals, GrammarProduction> tuple) { item.Add(tuple); }
        public void Delete(Tuple<Terminals, GrammarProduction> tuple) { item.Remove(tuple); }
    }
    public class PredictionAnalysisTable
    {
        private Dictionary<Nonterminals, PredictionTableItem> table;

        public PredictionAnalysisTable()
        {
            table = new Dictionary<Nonterminals, PredictionTableItem>();
        }
        // 增加一整行到table
        public void Add(Nonterminals key, PredictionTableItem value)
        {
            PredictionTableItem item;
            if (!table.TryGetValue(key, out item))
            {
                table.Add(key, value);
            }
        }
    }

    public class AlgCore
    {
        public static FirstSet First(Grammar grammar, GrammarStructure structure)
        {
            throw new NotImplementedException("First集算法未完成");
        }
        public static FollowSet Follow(Grammar grammar, Nonterminals nonterminals)
        {
            throw new NotImplementedException("First集算法未完成");
        }
    }
}
