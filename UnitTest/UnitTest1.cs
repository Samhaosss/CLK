using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using CLK.LexicalCore;
using System.Collections.Generic;

namespace UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        
        // 测试c#的Skip生成的迭代器
        [TestMethod]
        public void TestMethod1()
        {
            string test = "aabb汉语ccd英语d";
            char[] charArray = test.ToCharArray();
            Assert.AreEqual(test.Length, charArray.Length);
            Assert.AreEqual('汉', charArray[4]);
            foreach(var ch in charArray.Skip(2).Take(4).ToArray())
            {
                System.Diagnostics.Trace.WriteLine($"{ch}");
            }
            //Console.WriteLine($"{charArray[4]}");
        }
        [TestMethod]
        public void TestTakenReader()
        {
            TakenReader takenReader = new TakenReader(@"C:\Users\sam\source\repos\CLK\LexicalCore\log4net.config");
            List<string> takens = new List<string>();
            while (takenReader.hasNext()) {
                var ch = takenReader.next();
                if(ch.Equals(' '))
                {
                    takens.Add(takenReader.GetWord().ToString());
                }
            }
            foreach(var tak in takens)
            {
                System.Diagnostics.Trace.WriteLine($"{tak}");
            }


        }
    }
}
