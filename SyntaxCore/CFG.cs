using ErrorCore;
using SyntaxCore;
using System;
using System.Collections.Generic;

namespace CLK.util
{

    using FirstSet = KeySet<GrammarStructure, Terminals>;
    using FollowSet = KeySet<Nonterminals, Terminals>;
    /*
     *  上下文无关文法相关的算法实现在此类内部
     * **/
    /// <summary>
    ///  上下文无关文法
    /// </summary>
    public class CFG : CSG
    {
        public CFG(List<GrammarProduction> grammarProductions, Nonterminals startNonterminalSymbol = null) :
            base(grammarProductions, startNonterminalSymbol)
        {
            if (grammarType == GrammarType.ContextSensitive)
            {
                throw new IllegalGrammarException("文法不符合上下文无关文法定义");
            }
        }
        /*
         * 下面需要实现上下文无关文法的所有相关算法 如LL分析表生成、LR\SLR分析表
         * **/
        public FirstSet First(GrammarStructure structure)
        {
            throw new NotImplementedException("First集算法未完成");
        }
        public FollowSet Follow(Nonterminals nonterminals)
        {
            throw new NotImplementedException("First集算法未完成");
        }
    }
}
