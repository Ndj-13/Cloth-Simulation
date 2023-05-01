using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Source.P1
{
    public class Fixer : MonoBehaviour
    {
        private Bounds _bounds;
        //private bool _isInside;
        private Vector3 _pos;
        private float _distance;

        public bool Move;
        private bool _mouseDown;

        public bool MovingFixer() { return _mouseDown; }
        public Vector3 FixerPosition() { return transform.position; }

        public void Start()
        {
            _pos = transform.position;
        }

        public void OnMouseDown()
        {
            if(Move)
            {
                _distance = Vector3.Distance(transform.position, Camera.main.transform.position);
                _mouseDown = true;
                _pos = transform.position;
            }
            
        }

        public void OnMouseUp()
        {
            _mouseDown = false;
        }

        private void Update()
        {

            if (_mouseDown)
            {
                Debug.Log("Arrastrar tela");
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Vector3 rayPoint = ray.GetPoint(_distance);
                transform.position = rayPoint;
            }
        }

        public bool IsInside(Vector3 nodePos)
        {
            _bounds = GetComponent<Collider>().bounds;
            bool _isInside = _bounds.Contains(nodePos);
            return _isInside;
        }

        public bool Collides(Vector3 nodePos)
        {
            if(nodePos.y < transform.position.y)
            {
                return false;
            }
            else
            {
                RaycastHit hit;
                if (Physics.Linecast(nodePos, transform.position, out hit))
                {
                    //Debug.DrawLine(nodePos, hit.point, Color.cyan);
                    if (hit.distance <= 0.1f)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            
        }
    }
}
