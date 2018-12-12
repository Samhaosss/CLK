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
            return errorHandler ?? (errorHandler = new SampleInterpreterError());
        }
        private List<string> errorTable;

        private SampleInterpreterError()
        {
            errorTable = new List<string>();
        }
        public void AddError(string msg)
        {
            errorTable.Add(msg);
        }
        public void ReportError()
        {
            errorTable.ForEach(x => System.Console.Error.WriteLine(x));
        }
    }
    /// <summary>
    ///  构建文法是文法单元以非英文字符开头时抛出的异常
    /// </summary>
    public class IllegalChException : System.Exception
    {
        public IllegalChException(string msg) : base(msg) { }
        public IllegalChException() : base() { }
        public IllegalChException(string message, System.Exception inner) : base(message, inner) { }
        public IllegalChException(System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    /// <summary>
    /// 构建文法时检测到文法不合法时抛出的异常
    /// </summary>
    public class IllegalGrammarException : System.Exception
    {
        public IllegalGrammarException(string msg) : base(msg) { }
        public IllegalGrammarException() : base() { }
        public IllegalGrammarException(string message, System.Exception inner) : base(message, inner) { }
        public IllegalGrammarException(System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
