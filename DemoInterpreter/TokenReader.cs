using System;
using System.Linq;
/*
 * 实现了简单了流式读取
 * **/
namespace CLK.Interpreter
{
    namespace DemoLexer
    {
        /*
         *  实现词法分析需要预读，这里为预读实现类的接口
         *  无论内部怎么变，预读类实现最终将暴露用于读取单个字符、判断流是否读取结束、
         *  获取或Pass连续的单词、流长度的接口。
         *  GetWord与Pass的布尔参数用于控制是否保留当前获取的单词的最后一个字符在流中以及最后一个字符是否返回，
         *  因为词法分析时调用Getword通常是在识别了一个不属于当前单词的字符，方法的默认行为支持这一点。
         * **/
        public interface ITokenReader
        {
            bool HasNext();
            char? Next();
            void Pass(bool finish = false);
            string GetWord(bool finish = false);
            long GetStreamLength();
        }
        /*
         * 当前支持从文件、内存创建
         * **/
        public class TokenReaderFactory
        {
            public static ITokenReader GetFromFile(String fileName)
            {
                // 如果由于种种原因无法读取文件内容，由这里抛出异常
                char[] data = System.IO.File.ReadAllText(fileName).ToArray();
                return new TokenReader(data);
            }
            public static ITokenReader GetFromByteStream(char[] stream)
            {
                return new TokenReader(stream);
            }
        }
        /*
         * TakenReader:
         *      不采用两端缓存，直接将文件读入大数组
         *  @TEST PASSED
         * */
        internal class TokenReader : ITokenReader
        {
            private readonly long streamLength;
            private int startPos;
            private int endPos;//当前已流过字节的之后一个字节
            private readonly char[] buf;//流

            public long Length => streamLength;
            public TokenReader(char[] data)
            {
                buf = data;
                streamLength = buf.Length;
                startPos = endPos = 0;
            }
            //读取一个字符，如果达到末尾范围null
            public char? Next()
            {
                char? ch = null;
                if (streamLength > 0 && streamLength != endPos)
                {
                    ch = new char?(buf[endPos]);
                    endPos++;
                }
                return ch;
            }
            // 判断endpos是否达到了末尾
            public bool HasNext() { return streamLength > 0 && streamLength != endPos; }
            // 获取startpos与endpos之间的字符，通过设置finish选择是否在流中和返回串中保留最后一个字符
            public string GetWord(bool finish = false)
            {
                string word = null;
                char[] result = null;
                if (startPos != endPos)
                {
                    result = new char[endPos - startPos];
                    Array.Copy(buf, startPos, result, 0, endPos - startPos);
                    startPos = endPos;
                    word = new string(result);
                    // 如果finish为false则去除最后一个字符
                    if (!finish)
                    {
                        word = word.Remove(result.Length - 1);
                        --startPos;
                    }
                }
                return word;
            }
            public void Pass(bool finish = false)
            {
                startPos = (finish) ? endPos : endPos - 1;
            }

            public long GetStreamLength()
            {
                return streamLength;
            }
        }
    }

}
