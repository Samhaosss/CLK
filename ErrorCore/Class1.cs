using System.Collections.Generic;

namespace ErrorCore
{
    /*
     *  暂时还未想好整套错误处理流程,现在只是简单的记录下来，最终汇报到标准错误
     *  随后可能定义相应的错误类型
     * **/
    public class SampleInterpreterError
    {
        private List<string> errorTable;

        public SampleInterpreterError()
        {
            errorTable = new List<string>();
        }
        public void addError(string msg)
        {
            errorTable.Add(msg);
        }
        public void reportError()
        {
            foreach (var error in errorTable)
            {
                System.Console.Error.WriteLine(error);
            }
        }
    }
}
