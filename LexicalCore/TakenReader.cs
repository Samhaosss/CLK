using System.IO;
using System.Linq;

namespace CLK.LexicalCore
{
    /*
     * TakenReader:
     *      不采用两端缓存，直接将文件读入大数组
     *      Bug:读取的第一个字符为:?
     *      对比发现，vs给每个文件头加一个'?' .......
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
                if (fileLength > 0)
                {
                    byte[] tmp = new byte[fileLength];
                    using (var fileHandler = fileInfo.OpenRead())
                    {
                        var le = fileHandler.Read(tmp, 0, tmp.Length);
                        buf = System.Text.Encoding.UTF8.GetString(tmp).ToCharArray();
                    }
                }
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
        public System.Collections.Generic.IEnumerable<char> GetWord()
        {
            System.Collections.Generic.IEnumerable<char> result = null;
            if (fileLength > 0 && startPos != endPos)
            {
                result = buf.Skip(startPos)
                                 .Take(endPos - startPos);
                // TODO: 根据词法分析的需求 这里可能需要修改
                startPos = endPos;
            }
            return result;
        }

    }
}
