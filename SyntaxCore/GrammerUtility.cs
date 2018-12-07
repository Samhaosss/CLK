using System.Collections.Generic;
using System.Diagnostics;
/*
*  这一部分目前完成基本非终结符、终结符、文法数据结构
*  并未完成所有需要的方法
*  文法创建：首先创建各个组成的structure,每个structure由终结符、非终结符组合而成
*    new Grammar( new List<GrammarProduction>( new GrammarProduction() -->左部, new List<GrammarProduction>{...} -->右部))
*  目前未提供更为简单的创建文法的方式，随后可以考虑通过模板生成
*  不过内部的文法表示由文法的文法分析程序递归构建，需要手动构建文法的部分并不多
* **/
namespace CLK.util
{

    // 终结符 非终结符的枚举类型
    public enum SymbolType { Terminals, Nonterminals };
    // 终结符和非终结符的接口 通过getType获取类型 而不是运行时识别
    public interface IGrammarSymbol
    {
        SymbolType GetType();
    }
    // 终结符
    // 这里并没有采用泛型，考虑一下后感觉应该把所有的终结符的值视为串
    // 目前看来这个设计没有问题 

    public class Terminals : IGrammarSymbol
    {
        public static string EmptyValue = "";
        private string value;

        public string Value { get => value; set => this.value = value; }

        SymbolType IGrammarSymbol.GetType()
        {
            return SymbolType.Terminals;
        }

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
    }
    // 对于非终结符，并没有实际的值，仅仅使用name标识
    // 将来可能拓展此类，加入属性，从而支持语法制导
    public class Nonterminals : IGrammarSymbol
    {
        private string name;
        public Nonterminals(string name)
        {
            this.name = name;
        }

        public string Name { get => name; set => name = value; }

        SymbolType IGrammarSymbol.GetType()
        {
            return SymbolType.Nonterminals;
        }

        public override string ToString()
        {
            return name;
        }
    }
    // 文法单元，如在文法:A => aA|abA中， A、aA、abA都是GrammarStructure
    // 这个单元作为文法的组成部分，支持创建 0、1、2、3各种文法，文法类型判断很容易
    // 此结构将作为产生式的组成 如文法： A[GrammarStruct] => a[GrammarStucture]A
    // 将来可以拓展此类以支持SLR、LR项目
    public class GrammarStructure
    {
        // 核心数据
        private List<IGrammarSymbol> structure;
        // 这里只是为了下面方便，其实并不需要在Stucture中存储这两个hashset
        private HashSet<Terminals> terminals;
        private HashSet<Nonterminals> nonterminals;
        public GrammarStructure(List<IGrammarSymbol> structure)
        {
            Debug.Assert(structure.Count != 0);
            this.structure = structure;
            Terminals = new HashSet<Terminals>();
            nonterminals = new HashSet<Nonterminals>();
            // 将符号分类入终结 非终结 hashset
            foreach (var syn in Structure)
            {
                if (syn.GetType() == SymbolType.Terminals)
                {
                    Terminals.Add((Terminals)syn);
                }
                else
                {
                    nonterminals.Add((Nonterminals)syn);
                }
            }
        }
        public bool IsStartWithNonterminals()
        {
            return structure[0].GetType() == SymbolType.Nonterminals;
        }

        public List<IGrammarSymbol> Structure { get => structure; set => structure = value; }
        public HashSet<Nonterminals> Nonterminals { get => nonterminals; set => nonterminals = value; }
        public HashSet<Terminals> Terminals { get => terminals; set => terminals = value; }

        public override string ToString()
        {
            string tmp = "";
            foreach (var syb in Structure)
            {
                tmp += (" " + syb.ToString());
            }
            return tmp;
        }
    }
    // 一个文法产生式 由左部文法单元和多个右部文法单元组成
    // 可以是任意类型文法的文法产生式
    public class GrammarProduction
    {
        private GrammarStructure leftStructure;
        private List<GrammarStructure> rightStructures;
        private HashSet<Terminals> terminals;
        private HashSet<Nonterminals> nonterminals;
        public GrammarProduction(GrammarStructure leftStructure, List<GrammarStructure> rightStructures)
        {
            this.leftStructure = leftStructure;
            this.rightStructures = rightStructures;
            terminals = new HashSet<Terminals>();
            nonterminals = new HashSet<Nonterminals>();
            terminals.UnionWith(leftStructure.Terminals);
            nonterminals.UnionWith(leftStructure.Nonterminals);
            foreach (var stu in rightStructures)
            {
                terminals.UnionWith(stu.Terminals);
                nonterminals.UnionWith(stu.Nonterminals);
            }
        }

        public GrammarStructure LeftStructure { get => leftStructure; set => leftStructure = value; }
        public List<GrammarStructure> RightStructures { get => rightStructures; set => rightStructures = value; }
        public HashSet<Terminals> Terminals { get => terminals; set => terminals = value; }
        public HashSet<Nonterminals> Nonterminals { get => nonterminals; set => nonterminals = value; }

        public bool IsLeftRecursive()
        {
            throw new System.NotImplementedException("产生式左递归判断未完成");
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

    // 文法：由多个文法产生式构成和开始符号构成
    // 可以直接获取文法的终结符号集 和非终结符
    // 目前想到的还需要加入的操作：
    //          文法类型判断
    //          是否左递归
    //          左递归、左公因子自动消除
    //          文法正确性
    // 
    public enum GrammarType { ZeroType, ContextSensitive, ContextFree, Regular }
    public class Grammar
    {
        // 所有产生式
        private List<GrammarProduction> grammarProductions;
        private Nonterminals startNonterminalSymbol;//开始符号
        private HashSet<Nonterminals> nonterminals;
        private HashSet<Terminals> terminals;
        private GrammarType grammarType;
        public Nonterminals StartNonterminalSymbol { get => startNonterminalSymbol; set => startNonterminalSymbol = value; }
        public List<GrammarProduction> GrammarProductions { get => grammarProductions; set => grammarProductions = value; }
        public HashSet<Nonterminals> Nonterminals { get => nonterminals; set => nonterminals = value; }
        public HashSet<Terminals> Terminals { get => terminals; set => terminals = value; }
        public GrammarType GrammarType { get => grammarType; set => grammarType = value; }

        public Grammar(List<GrammarProduction> grammarProductions, Nonterminals startNonterminalSymbol)
        {
            this.grammarProductions = grammarProductions;
            this.startNonterminalSymbol = startNonterminalSymbol;
            nonterminals = new HashSet<Nonterminals>();
            terminals = new HashSet<Terminals>();
            InitSet();
            // 如果要运行 需要注释掉这里 GType还未实现 
            grammarType = GType();
            //这里应该加入文法验证 如果验证不通过抛出异常
        }
        private bool GrammarValidate()
        {
            throw new System.NotImplementedException("未完成文法有效性验证");
        }
        public bool IsLeftRecursive()
        {
            throw new System.NotImplementedException("未完成左递归判断");
        }
        public void EliminateRecursiveAndCommonItem()
        {
            throw new System.NotImplementedException("未完成左递归左公因子消除");
        }
        private GrammarType GType()
        {
            throw new System.NotImplementedException("未完成文法类别判断");
        }

        private void InitSet()
        {
            nonterminals = new HashSet<Nonterminals>();
            terminals = new HashSet<Terminals>();
            foreach (var production in grammarProductions)
            {
                nonterminals.UnionWith(production.Nonterminals);
                terminals.UnionWith(production.Terminals);
            }
        }

        public Grammar(List<GrammarProduction> grammarProductions)
        {
            this.grammarProductions = grammarProductions;
            foreach (var symbol in grammarProductions[0].LeftStructure.Structure)
            {
                if (symbol.GetType() == SymbolType.Nonterminals)
                {
                    startNonterminalSymbol = (Nonterminals)symbol;
                }
            }
            InitSet();
        }
        public override string ToString()
        {
            var tmp = "";
            foreach (var gra in grammarProductions)
            {
                tmp += gra + "\n";
            }
            return tmp;
        }
    }

}
