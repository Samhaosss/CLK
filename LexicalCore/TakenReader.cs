using System;
using System.IO;
using System.Linq;
/*
 * 实现了简单了流式读取
 * **/
namespace CLK.LexicalCore
{
    /*
     * TakenReader:
     *      不采用两端缓存，直接将文件读入大数组
     * */
    public class TakenReader
    {
        // private static ILog logger = LogManager.
        //   GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        //流式读取
        private readonly string fileName;
        private readonly long fileLength;
        private int startPos;
        private int endPos;//当前已流过字节的之后一个字节
        private readonly char[] buf;//流

        public string TargetFile => fileName;
        public long Length => fileLength;

        public TakenReader(string fileName)
        {
            this.fileName = fileName;
            startPos = 0;
            endPos = 0;
            // 可能存在权限问题，这里只考虑了文件不存在的异常处理。
            FileInfo fileInfo = new FileInfo(fileName);
            fileLength = fileInfo.Length;
            buf = (fileLength > 0) ? System.IO.File.ReadAllText(fileName).ToArray() : null;
        }
        public char? next()
        {
            // 如果求值顺序和预期不同，这里可能存在bug
            //(fileLength > 0 && fileLength != endPos) ? new char?(buf[endPos++]) : null;
            char? ch = null;
            // 应该使用buf.length而不是fileLength
            if (fileLength > 0 && buf.Length != endPos)
            {
                ch = new char?(buf[endPos]);
                endPos++;
            }
            return ch;
        }
        public bool hasNext() { return fileLength > 0 && buf.Length != endPos; }

        public string GetWord(bool finish = false)
        {
            string word = null;
            char[] result = null;
            if (fileLength > 0 && startPos != endPos)
            {
                result = new char[endPos - startPos];
                Array.Copy(buf, startPos, result, 0, endPos - startPos);
                word = (!finish && result.Length > 1) ? new string(result).Remove(result.Length - 1) :
                        new string(result);
                // 如果未结束，意味着当前字符还未使用，因此需要退格
                startPos = finish ? endPos : endPos - 1;
            }
            return word;
        }
        public void pass(bool finish = false)
        {
            startPos = (finish) ? endPos : endPos - 1;
        }

    }
}
