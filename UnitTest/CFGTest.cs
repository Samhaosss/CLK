using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CLK.GrammarCore.Tests
{
    [TestClass()]
    public class AssemblyInfo
    {
        [TestMethod()]
        public void IsLeftRecursiveTest()
        {

            SymbolStream symbolIter = new SymbolStream("aaaabbbbdddddaaaaaacc");
            CFG grammar = null;
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
