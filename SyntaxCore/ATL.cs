using CLK.GrammarCore;
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

        public void AddSubNode(Node subNode) { subNodes.Add(subNode); }

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

        public ATL(Node root)
        {
            this.root = root;
        }
        /// <summary>
        /// 打印树到
        /// </summary>
        public void Print()
        {
            DoPrint(root);
        }
        private void DoPrint(Node node)
        {
            System.Console.Write($"{node}:[");
            if (node.SubNodes.Count == 0)
            {
                System.Console.WriteLine("]");
                return;
            }
            foreach (var sub in node.SubNodes)
            {
                System.Console.Write($"{sub},");
            }
            System.Console.WriteLine("]");
            foreach (var sub in node.SubNodes)
            {
                DoPrint(sub);
            }
        }
    }
}
