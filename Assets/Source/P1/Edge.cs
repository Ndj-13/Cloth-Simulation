using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Source.P1
{
    public class Edge
    {
        private int _nodeA;
        private int _nodeB;
        private int _otherNode;

        public Edge(int nodeA, int nodeB, int otherNode)
        {
            //A y B siempre ordenados (A < B)
            if(nodeA < nodeB)
            {
                _nodeA = nodeA;
                _nodeB = nodeB;
            }
            else
            {
                _nodeA = nodeB;
                _nodeB = nodeA;
            }
            
            _otherNode = otherNode;
        }

        public int GetNodeA() { return _nodeA; }
        public int GetNodeB() { return _nodeB; }
        public int GetOtherNode() { return _otherNode; }
    }
}
