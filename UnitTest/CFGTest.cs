using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CLK.GrammarDS.Tests
{
    [TestClass()]
    public class AssemblyInfo
    {
        [TestMethod()]
        public void IsLeftRecursiveTest()
        {
            GrammarProduction production = new GrammarProduction("A", "     A a |  B b | C c");
            GrammarProduction production2 = new GrammarProduction("B", " b B | d D | A ");
            GrammarProduction production3 = new GrammarProduction("C", "  c  ");
            GrammarProduction production4 = new GrammarProduction("D", " d D |  a A | e B | d E");
            GrammarProduction production5 = new GrammarProduction("E", " a A |  a D | e ");
            SymbolIter symbolIter = new SymbolIter("aaaabbbbdddddaaaaaacc");
            CFG grammar = new CFG(new List<GrammarProduction> { production, production2, production3, production4, production5 });
            Console.WriteLine($"grammar:{grammar}");
            Assert.IsTrue(grammar.IsLeftRecursive());
            //消除左递归
            grammar.EliminateRecursive();
            Assert.IsFalse(grammar.IsLeftRecursive());
            Console.WriteLine($"grammar:{grammar}");
            //Assert.IsTrue(grammar.RecursiveAnalyze(symbolIter));

        }
    }
}
