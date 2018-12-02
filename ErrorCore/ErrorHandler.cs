using System.Collections.Generic;
/*
 * 这里可以作为各个阶段错误处理的执行程序集
 * **/
namespace ErrorCore
{
    /*
     *  暂时还未想好整套错误处理流程,现在只是简单的记录下来，最终汇报到标准错误
     *  随后可能定义相应的错误类型
     * **/
    public class SampleInterpreterError
    {
        private static SampleInterpreterError errorHandler;
        public static SampleInterpreterError GetSampleInterpreterError()
        {
            return (errorHandler == null) ? errorHandler = new SampleInterpreterError() : errorHandler;
        }
        private List<string> errorTable;

        private SampleInterpreterError()
        {
            errorTable = new List<string>();
        }
        public void addError(string msg)
        {
            errorTable.Add(msg);
        }
        public void reportError()
        {
            errorTable.ForEach(x => System.Console.Error.WriteLine(x));
        }
    }
}
