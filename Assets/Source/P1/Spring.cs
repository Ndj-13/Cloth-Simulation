using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.UIElements;

namespace Assets.Source.P1
{
    public class Spring //cada muelle de un triangluo
    {
        private Node _nodeA, _nodeB;
        int indexMuelle;

        private float _initialLength;
        private float _actualLength;

        private float _springForce;
        private float _stiffness;
        private float _damping;

        public Spring(Node nodeA, Node nodeB, float stiffness, float damping)
        {
            _nodeA = nodeA;
            _nodeB = nodeB;
            _stiffness = stiffness;

            _actualLength = (nodeA.GetPos() - nodeB.GetPos()).magnitude;
            _initialLength = (nodeA.GetPos() - nodeB.GetPos()).magnitude;

            //_springForce = -stiffness * 
            _damping = damping * _stiffness;
        }

        //Getters
        public Node GetNodeA() { return _nodeA; }
        public Node GetNodeB() { return _nodeB; }

        public float GetLength() { return _actualLength; }

        public void UpdateLength()
        {
            _actualLength = (_nodeA.GetPos() - _nodeB.GetPos()).magnitude;    
        }

        public void ComputeForces()
        {
            Vector3 u = _nodeA.GetPos() - _nodeB.GetPos();
            u.Normalize();
            Vector3 v = _nodeA.GetVel() - _nodeB.GetVel();

            float forcemag = -_stiffness * (_actualLength - _initialLength) - _damping * Vector3.Dot(u, v);

            Vector3 force = forcemag * u;

            _nodeA.SetForce(_nodeA.GetForce() + force);
            _nodeB.SetForce(_nodeB.GetForce() - force);
        }

        

    }
}
