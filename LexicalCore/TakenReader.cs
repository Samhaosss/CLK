using System;
using System.IO;
using System.Linq;

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
        private readonly string fileName;
        private readonly long fileLength;
        private int startPos;
        private int endPos;
        private readonly char[] buf;

        public string TargetFile => fileName;
        public long Length => fileLength;

        public TakenReader(string fileName)
        {
            this.fileName = fileName;
            startPos = 0;
            endPos = 0;
            // 可能存在权限问题，这里只考虑了文件不存在的异常处理。
            if (File.Exists(fileName))
            {
                FileInfo fileInfo = new FileInfo(fileName);
                fileLength = fileInfo.Length;
                System.Console.WriteLine($"File length:{fileLength}");
                buf = (fileLength > 0) ? System.IO.File.ReadAllText(fileName).ToArray() : null;

                /*
                 if (fileLength > 0)
                 {
                     byte[] tmp = new byte[fileLength];
                     using (var fileHandler = fileInfo.OpenRead())
                     {
                         var le = fileHandler.Read(tmp, 0, tmp.Length);
                         buf = System.Text.Encoding.UTF8.GetString(tmp).ToCharArray();
                     }
                 }*/
            }
            else
            {
                throw new FileNotFoundException();
            }
        }
        public char? next()
        {
            // 如果求值顺序和预期不同，这里可能存在bug
            char? ch = null;
            if (fileLength > 0 && buf.Length != endPos)
            {
                ch = new char?(buf[endPos++]);
            }
            return ch;
        }
        public bool hasNext() { return fileLength > 0 && buf.Length != endPos; }

        // 使用迭代器避免拷贝
        public string GetWord(bool finish = false)
        {
            string word = null;
            char[] result = null;
            if (fileLength > 0 && startPos != endPos)
            {
                result = new char[endPos - startPos];
                Array.Copy(buf, startPos, result, 0, endPos - startPos);
                word = new string(result);
                //result = new string(buf.Skip(startPos)
                //                .Take(endPos - startPos).ToArray());
                if (!finish && result.Length > 1)
                {
                    word = word.Remove(result.Length - 1);
                }
                // 如果未结束，意味着当前字符还未使用，因此需要退格
                startPos = finish ? endPos : endPos - 1;
            }
            return word;
        }
        public void pass()
        {
            startPos = endPos - 1;
        }

    }
}
