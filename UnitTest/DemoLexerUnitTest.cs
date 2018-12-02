using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
/*
 * 公用测试单元
 * **/
namespace UnitTest
{
    [TestClass]
    public class DemoLexerUnitTest
    {

        [TestMethod]
        public void ReadKeywords()
        {
            var path = Environment.CurrentDirectory + @"\" + "keywords";
            var spliter = new char[3] { ' ', '\r', '\n', };
            var keywordsList = System.IO.File.ReadAllText(path).
                           Split(spliter, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim(spliter))
                           .ToList();
            keywordsList.ForEach(x => Console.WriteLine(x + ";"));
        }

        // 测试c#的Skip生成的迭代器
        [TestMethod]
        public void TestMethod1()
        {
            string test = "aabb汉语ccd英语d";
            char[] charArray = test.ToCharArray();
            Assert.AreEqual(test.Length, charArray.Length);
            Assert.AreEqual('汉', charArray[4]);
            foreach (var ch in charArray.Skip(2).Take(4).ToArray())
            {
                System.Diagnostics.Trace.WriteLine($"{ch}");
            }
        }
        [TestMethod]
        public void TestInRange()
        {

            Assert.IsTrue(IsInRange('A', 'A', 'Z'));
            Assert.IsTrue(IsInRange('F', 'A', 'Z'));
            Assert.IsTrue(IsInRange('Z', 'A', 'Z'));
            Assert.IsTrue(IsInRange('0', '0', '9'));
        }
        private bool IsInRange(char tar, char start, char end)
        {
            return (tar >= start) && (tar <= end);
        }

    }
}
