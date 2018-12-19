using ErrorCore;
using System.Collections.Generic;

namespace CLK.util
{
    public class RG : CFG
    {
        public RG(List<GrammarProduction> grammarProductions, Nonterminal startNonterminalSymbol = null) :
            base(grammarProductions, startNonterminalSymbol)
        {
            if (grammarType == GrammarType.ContextFree)
            {
                throw new IllegalGrammarException("文法不符合上下文无关文法定义");
            }
        }

        public DFA ToDfa()
        {
            throw new System.NotImplementedException("未实现文法向dfa转换");
        }
    }
    /// <summary>
    /// DFA，支持识别单个单词或通过迭代识别某个单词返回状态序列
    /// </summary>
    public class DFA
    {
        private readonly Dictionary<Nonterminal, Dictionary<Terminal, HashSet<Nonterminal>>> dfa;
        private readonly Nonterminal startState;
        private readonly HashSet<Nonterminal> endStates;
        private readonly HashSet<Nonterminal> nonterminals;
        private readonly HashSet<Terminal> terminals;

        private Nonterminal currentState;
        private string targetWord;

        public DFA(Dictionary<Nonterminal, Dictionary<Terminal, HashSet<Nonterminal>>> dfa, Nonterminal startState)
        {
            this.dfa = dfa;
            this.startState = startState;
        }
        public bool JudgeWord(string word)
        {
            throw new System.NotImplementedException();
        }
    }
}
