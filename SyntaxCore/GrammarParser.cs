﻿using CLK.AnalysisDs;
using ErrorCore;
using System.Collections.Generic;
using System.Linq;
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
    /// 所有解析器解析过程可能的状态, 未初始化、未完成、失败、成功
    /// </summary>
    public enum ParserState { Uninitialezed, Unfinished, Failed, Succeed }
    /*
     * 因为作图不便，下面描述Parser的状态转移
     * 创建后处于未初始化状态 调用Init 使分析器进入未完成识别状态
     * 进入未完成识别状态后，可调用Walk、Run进行分析 最终进入 Failed或Succeed状态
     * 处于 Unfinished或Succeed状态下 调用Reboot使分析器重新进入最初的Unfinished状态 从而重新识别当前输入流
     * 处于任意状态下调用Init会使分析器重新进入unfinished状态，分析新的输入流
     * **/
    public interface IParser
    {
        ParserState Init(SymbolStream inputStream); //初始化
        ParserState Reboot();   //
        ParserState Walk(); //单步识别
        ParserState Run();  //从当前起 连续识别到最后
        ParserState GetState(); //获取状态
        void PrintState(); // 打印当前分析状态到终端
        ATL GetParseResult();   //获取分析树  
        string GetFailedReason();   //获取失败原因
    }

    /// <summary>
    /// 非递归预测分析解析器
    /// </summary>
    public class LLParser : IParser
    {
        //TODO:BUGS HERE
        // 分析器的静态数据： 分析表、文法、结束节点
        private CFG currentTarget;  //需要获取开始符号
        private PredictionAnalysisTable llTable;
        private Node endNode;
        //分析器的动态数据： 栈、输入流、栈顶符号、当前处理符号、状态、atl
        private Stack<Node> stateStack;
        private Node root;
        private ATL atl;    //分析树
        private SymbolStream input;
        private Terminal currentTerminal;
        private GrammarSymbol topSymbol;
        private ParserState state;
        /// <summary>
        /// 为适用于非递归预测分析的上下文无关文法新建分析器，创建的分析器处于未初始化状态
        /// </summary>
        /// <param name="targetGrammar">上下文无关文法</param>
        /// <exception cref="IllegalGrammarException">输入文法不符合非递归预测分析要求</exception>
        public LLParser(CFG targetGrammar)
        {
            if (!targetGrammar.IsSatisfyNonrecuPredictionAnalysis())
            {
                throw new IllegalGrammarException("期望进行非递归预测分析的文法不满足LL(1)定义");
            }
            // 初始化静态数据
            currentTarget = targetGrammar;
            llTable = targetGrammar.GetPATable();
            state = ParserState.Uninitialezed;
            endNode = new Node(Terminal.End, null);
            stateStack = new Stack<Node>();
        }

        /// <summary>
        /// 无论处于哪种状态，使用Init会重新置分析器到unfinished状态
        /// </summary>
        /// <param name="inputStream">终结符号流</param>
        /// <returns></returns>
        public ParserState Init(SymbolStream inputStream)
        {
            state = ParserState.Unfinished;
            input = inputStream;
            Clear();
            return state;
        }
        private void Clear()
        {
            stateStack.Clear();
            root = new Node(currentTarget.StartNonterminalSymbol, null);
            stateStack.Push(endNode);
            stateStack.Push(root);
            currentTerminal = input.Next();
            topSymbol = stateStack.Peek().Data;
        }
        /// <summary>
        /// 重置流，重置分析器至初始未完成状态
        /// </summary>
        public ParserState Reboot()
        {
            if (state == ParserState.Succeed || state == ParserState.Unfinished)
            {
                input.Reset();
                Clear();
                state = ParserState.Unfinished;
            }
            return state;
        }
        /// <summary>
        /// 单步识别，需要分析器处于unfinished状态
        /// </summary>
        /// <returns>识别后分析器状态</returns>
        public ParserState Walk()
        {
            if (state != ParserState.Unfinished)
            {
                return state;
            }
            if (currentTerminal == null)
            {
                return state = ParserState.Failed;
            }
            //如果栈顶符号未终结符
            if (topSymbol.GetSymbolType() == SymbolType.Terminal)
            {
                if (topSymbol.Equals(currentTerminal))
                {
                    if (topSymbol.Equals(endNode.Data))
                    {
                        state = ParserState.Succeed;
                        atl = new ATL(root);
                    }
                    else
                    {
                        currentTerminal = input.Next();
                        stateStack.Pop();
                        topSymbol = stateStack.Peek().Data;
                    }
                }
                else
                {
                    state = ParserState.Failed;
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
                    var top = stateStack.Pop();
                    var newNode = AddToSubNode(top, rev);
                    //处理空产生式
                    if (!(newNode.Count == 1 && rev[0].Equals(Terminal.Empty)))
                    {
                        foreach (var nt in newNode)
                        {
                            stateStack.Push(nt);
                        }
                    }
                    topSymbol = stateStack.Peek().Data;
                }
                else
                {
                    state = ParserState.Failed;
                }
            }
            return state;
        }

        public ParserState Run()
        {
            if (state != ParserState.Unfinished)
            {
                return state;
            }

            do
            {
                Walk();
            } while (state == ParserState.Unfinished);
            return state;
        }

        public ATL GetParseResult()
        {
            return state == ParserState.Succeed ? atl : null;
        }

        public ParserState GetState()
        {
            return state;
        }
        private List<Node> AddToSubNode(Node node, List<GrammarSymbol> sub)
        {
            List<Node> result = new List<Node>();
            foreach (var gs in sub)
            {
                Node tmp = new Node(gs, node);
                node.AddSubNode(tmp);
                result.Add(tmp);
            }
            return result;
        }

        public string GetFailedReason()
        {
            throw new System.NotImplementedException("未实现非递归预测分析的错误处理");
        }

        public void PrintState()
        {
            int len = stateStack.Max(x => x.Data.ToString().Count());
            int stackW = len + 15;
            string sp = "-".PadRight(stackW + 2, '-');
            System.Console.WriteLine("State Stack:     ");
            foreach (var tmp in stateStack)
            {
                System.Console.WriteLine(sp);
                string str = "|" + tmp.ToString().PadRight(stackW, ' ') + "|";
                System.Console.WriteLine(str);
            }
            System.Console.WriteLine(sp);
        }
    }

    public class LRParser : IParser
    {
        public string GetFailedReason()
        {
            throw new System.NotImplementedException();
        }

        public ATL GetParseResult()
        {
            throw new System.NotImplementedException();
        }

        public ParserState GetState()
        {
            throw new System.NotImplementedException();
        }

        public ParserState Init(SymbolStream inputStream)
        {
            throw new System.NotImplementedException();
        }

        public void PrintState()
        {
            throw new System.NotImplementedException();
        }

        public ParserState Reboot()
        {
            throw new System.NotImplementedException();
        }

        public ParserState Run()
        {
            throw new System.NotImplementedException();
        }

        public ParserState Walk()
        {
            throw new System.NotImplementedException();
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
        /// 重置流
        /// </summary>
        public void Reset()
        {
            index = 0;
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
