using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Data.Common;

namespace Assets.Source.P1
{
    public class Node
    {
        private Vector3 _pos;
        private Vector3 _vel;
        private Vector3 _force;

        private float _nodeForce;
        private float _mass;
        public bool isFixed;

        private float _damping;

        public Node(Vector3 pos, float mass, float damping)
        {
            _pos = pos;
            _mass = mass;

            _vel = Vector3.zero;
            isFixed = false;

            //Amortiguamiento en nodos: frena movimiento absoluto
            _damping = damping * _mass;
        }

        //Getters y setters
        public Vector3 GetPos() { return _pos; }
        public Vector3 GetVel() { return _vel; }
        public Vector3 GetForce() { return _force; }

        public void SetPos(Vector3 pos) { _pos = pos; }
        public void SetVel(Vector3 vel) { _vel = vel; }
        public void SetForce(Vector3 force) { _force = force; }

        //public float GetMass() { return this.mass; }
        public bool IsFixed() { return this.isFixed; }
        public void FixNode() { this.isFixed = true; }

        public void ComputeForces(Vector3 externalForces)
        {
            _force += _mass * externalForces;
            _force -= _damping * _vel;
        }

        public void WindForce(Vector3 speed, float friction)
        {
            //Para el viento:
            _force += friction * (Vector3.Dot(_pos.normalized, (speed - _vel))) * _pos.normalized;
        }
    }
}
