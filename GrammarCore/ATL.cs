using CLK.GrammarCore;
using System;
using System.Collections.Generic;
namespace CLK.AnalysisDs
{

    public class Node
    {
        private GrammarSymbol data;
        private List<Node> subNodes;
        private Node father;

        public Node(GrammarSymbol data, Node father)
        {
            this.data = data;
            this.father = father;
            subNodes = new List<Node>();
        }

        public List<Node> SubNodes { get => subNodes; }
        public GrammarSymbol Data { get => data; }
        public Node Father { get => father; }
        /// <summary>
        /// 添加子节点
        /// </summary>
        public void AddSubNode(Node subNode) { subNodes.Add(subNode); }
        /// <summary>
        /// 添加父节点，如果已存在则覆盖
        /// </summary>
        /// <param name="father"></param>
        public void AddFather(Node father)
        {
            this.father = father;
        }
        public override bool Equals(object obj)
        {
            var node = obj as Node;
            return node != null &&
                   EqualityComparer<GrammarSymbol>.Default.Equals(data, node.data);
        }

        public override int GetHashCode()
        {
            return 1768953197 + EqualityComparer<GrammarSymbol>.Default.GetHashCode(data);
        }

        public override string ToString()
        {
            return data.ToString();
        }

    }
    public class ATL
    {
        private Node root;
        /// <summary>
        /// 使用根节点创建树
        /// </summary>
        /// <param name="root"></param>
        public ATL(Node root)
        {
            this.root = root;
        }
        /// <summary>
        /// 打印树到
        /// </summary>
        public void Print()
        {
            DoPrint(root, "", true);
        }
        private void DoPrint(Node node, string indent, bool last)
        {
            Console.WriteLine(indent + "+-" + node.ToString());
            indent += last ? "   " : "|    ";
            for (int i = 0; i < node.SubNodes.Count; i++)
            {
                DoPrint(node.SubNodes[i], indent, i == node.SubNodes.Count - 1);
            }
        }
    }
}
