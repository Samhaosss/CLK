using ErrorCore;
using System.Collections.Generic;

namespace CLK.util
{
    public class RG : CFG
    {
        public RG(List<GrammarProduction> grammarProductions, Nonterminals startNonterminalSymbol = null) :
            base(grammarProductions, startNonterminalSymbol)
        {
            if (grammarType == GrammarType.ContextFree)
            {
                throw new IllegalGrammarException("文法不符合上下文无关文法定义");
            }
        }
    }
}
