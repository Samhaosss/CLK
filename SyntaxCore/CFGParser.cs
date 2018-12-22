using CLK.AnalysisDs;
using ErrorCore;
using System.Collections.Generic;
namespace CLK.GrammarCore.Parser
{
    /*
     * 考虑到随后可能需要更精细的控制 因此把各种分析方法抽象为有内部状态的各个分析器
     * **/
    /// <summary>
    /// 上下文无关文法解析器 包含递归预测分析、ll分析、lr分析
    /// </summary>
    public class ParseFactory
    {

    }
    /// <summary>
    /// 递归预测分析解析器
    /// </summary>
    public class RecursivePredictionParser
    {

    }
    /// <summary>
    /// 非递归预测分析解析器
    /// </summary>
    public class LLParser
    {
        private Stack<GrammarSymbol> stateStack;
        private CFG currentTarget;
        private PredictionAnalysisTable llTable;
        //下面属性与某一次识别过程相关 每次更新input 需要完全更新状态
        private SymbolStream input;
        private Terminal currentTerminal;
        private GrammarSymbol topSymbol;
        private bool finish;
        private bool success;

        public Stack<GrammarSymbol> StateStack { get => stateStack; }

        public LLParser(CFG targetGrammar)
        {
            if (!targetGrammar.IsSatisfyNonrecuPredictionAnalysis())
            {
                throw new IllegalGrammarException("期望进行非递归预测分析的文法不满足LL(1)定义");
            }
            currentTarget = targetGrammar;
            llTable = targetGrammar.GetPATable();
        }
        public void Feed(SymbolStream input)
        {
            if (stateStack != null)
            {
                stateStack.Clear();
            }
            this.input = input;
            stateStack.Push(Terminal.End);
            stateStack.Push(currentTarget.StartNonterminalSymbol);
            currentTerminal = input.Next();
            topSymbol = stateStack.Peek();
            finish = false;
            success = false;
        }
        public bool IsFinish()
        {
            return finish;
        }
        public bool IsSuccess() { return success; }
        public bool MoveOneStep()
        {
            if (input == null)
            {
                throw new System.FieldAccessException("单步执行前需要先向分析器投入输入");
            }
            if (finish)
            {
                return false;
            }

            if (currentTerminal.GetSymbolType() == SymbolType.Terminal)
            {
                if (topSymbol.Equals(currentTerminal))
                {
                    topSymbol = stateStack.Pop();
                    currentTerminal = input.Next();
                    if (currentTerminal.Equals(Terminal.End))
                    {
                        success = true;
                    }
                }
                else
                {
                    finish = true;
                    success = false;
                    return false;
                }
            }
            else
            {
                // TODO: 这里的getitem的实现可能无表项返回null而是抛异常，随后修复
                var stc = llTable.GetItem((Nonterminal)topSymbol, currentTerminal);
                if (stc != null)
                {
                    var rev = stc.Structure;
                    rev.Reverse();
                    stateStack.Pop();
                    foreach (var nt in rev)
                    {
                        stateStack.Push(nt);
                    }
                }
                else
                {
                    finish = true;
                    success = false;
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 识别给定的一个句子
        /// </summary>
        /// <param name="input">终结符流</param>
        /// <returns></returns>
        public bool Parse(SymbolStream input)
        {
            if (!input.HasNext())
            {
                return false;
            }
            Stack<GrammarSymbol> stateStack = new Stack<GrammarSymbol>();
            stateStack.Push(Terminal.End); // $入栈
            stateStack.Push(currentTarget.StartNonterminalSymbol); //开始符号入栈
            Terminal currentInput = input.Next();
            GrammarSymbol topSymbol = stateStack.Peek();
            do
            {
                if (topSymbol.GetSymbolType() == SymbolType.Terminal)
                {
                    if (topSymbol.Equals(currentInput))
                    {
                        topSymbol = stateStack.Pop();
                        currentInput = input.Next();
                    }
                    else
                    {
                        //throw new System.NotImplementedException("未完成错误处理");
                        return false;
                    }
                }
                else
                {
                    // TODO: 这里的getitem的实现可能无表项返回null而是抛异常，随后修复
                    var stc = llTable.GetItem((Nonterminal)topSymbol, currentInput);
                    if (stc != null)
                    {
                        var rev = stc.Structure;
                        rev.Reverse();
                        stateStack.Pop();
                        foreach (var nt in rev)
                        {
                            stateStack.Push(nt);
                        }
                    }
                    else
                    {
                        return false;
                        //throw new System.NotImplementedException("未完成错误处理");
                    }
                }

            } while (!topSymbol.Equals(Terminal.End));
            return true;
        }

    }

    /// <summary>
    /// 终结符流
    /// </summary>
    public class SymbolStream
    {
        private int index;
        private List<Terminal> symbols;

        public SymbolStream(List<Terminal> symbols)
        {
            this.symbols = symbols ?? throw new System.ArgumentNullException();
            index = 0;
        }
        /// <summary>
        /// 默认所有终结符长度只有一
        /// </summary>
        /// <param name="sentence"></param>
        public SymbolStream(string sentence)
        {
            if (sentence == null || sentence.Length == 0)
            {
                throw new System.ArgumentException();
            }
            index = 0;
            symbols = new List<Terminal>();
            foreach (var ch in sentence)
            {
                symbols.Add(new Terminal(ch));
            }
        }
        /// <summary>
        /// 获取并消耗流
        /// </summary>
        /// <returns></returns>
        public Terminal Next()
        {
            if (index == symbols.Count)
            {
                return null;
            }
            return symbols[index++];
        }
        /// <summary>
        /// 获取 但不消耗流 
        /// </summary>
        /// <returns></returns>
        public Terminal Get()
        {
            if (index >= symbols.Count)
            {
                return null;
            }

            return symbols[index];
        }
        /// <summary>
        /// 回退流
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public bool BackN(int n)
        {
            if (n > index)
            {
                return false;
            }

            index -= n;
            return true;
        }
        /// <summary>
        /// 判断流是否消耗完
        /// </summary>
        /// <returns></returns>
        public bool HasNext()
        {
            return index < symbols.Count;
        }
        /// <summary>
        /// 获取流剩余非终结符数
        /// </summary>
        /// <returns></returns>
        public int GetRest()
        {
            return symbols.Count - index;
        }

        public override string ToString()
        {
            string tmp = "";
            foreach (var sm in symbols)
            {
                tmp += sm;
            }
            return tmp;
        }
    }
}
