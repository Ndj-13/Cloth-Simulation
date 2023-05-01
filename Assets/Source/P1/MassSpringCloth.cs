using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using static UnityEditor.PlayerSettings;
using UnityEngine.UI;
using System.Linq;
using UnityEditor.Experimental.GraphView;

/// <summary>
/// Basic physics manager capable of simulating a given ISimulable
/// implementation using diverse integration methods: explicit,
/// implicit, Verlet and semi-implicit.
/// </summary>
namespace Assets.Source.P1
{
    public class MassSpringCloth : MonoBehaviour
    {
        /// <summary>
        /// Default constructor. Zero all. 
        /// </summary>
        public MassSpringCloth()
        {
            this.Paused = true;
            this.TimeStep = 0.01f;
            this.Gravity = new Vector3(0.0f, -9.81f, 0.0f);
            this.IntegrationMethod = Integration.Symplectic;
        }

        /// <summary>
        /// Integration method.
        /// </summary>
        public enum Integration
        {
            Explicit = 0,
            Symplectic = 1,
        };

        #region InEditorVariables

        public bool Paused;
        public float TimeStep;
        public Vector3 Gravity;
        public Integration IntegrationMethod;

        //Public variables
        public float NodeMass;

        public float ExtensionSpringsStiffness; //rigidez muelles de traccion
        public float CompressionSpringsStiffness; //rigidez muelles de flexion (menor q la de traccion)

        public float Damping;

        public bool Wind;
        public float WindFriction;
        public Vector3 _windSpeed = new Vector3(20f, 0.0f, 0.0f);

        //public float nodeForce;
        //public float springForce;

        #endregion

        #region OtherVariables
        #endregion

        //Malla
        private Mesh _mesh;
        private Collider _collider;
        private Vector3[] _verticesMesh;
        private int[] _trianglesMesh;

        //Nodos
        private List<Node> _nodes = new List<Node>();

        //Muelles
        private List<Spring> _springs = new List<Spring>();
        private List<Edge> _edges = new List<Edge>();
        private List<Edge> _extension = new List<Edge>(); //traccion
        private List<Spring> _compression = new List<Spring>(); //flexion

        //Fixer
        public Fixer fixer;
        private List<Vector3> _fixedNodes = new List<Vector3>();

        private float speedNode = 0.5f;

        #region MonoBehaviour

        public void Start()
        {

            /*--------
             * ACCESO A LA MALLA: VERTICES Y ARISTAS
             * -------*/
            _mesh = GetComponent<MeshFilter>().mesh;
            _collider = GetComponent<Collider>();
            _verticesMesh = _mesh.vertices;

            CrearNodos();
            //CrearAristas();

            /*--------
             * FIJAR NODOS: BOUNDING BOX BOUNDS
             * -------*/
            //fixer = gameObject.GetComponent<Fixer>();
            foreach (Node node in _nodes)
            {

                if (fixer.IsInside(node.GetPos()))
                {
                    node.FixNode();
                    _fixedNodes.Add(node.GetPos() - fixer.FixerPosition());
                }

                
            }

            /*------------
             * MUELES DE TRACCION Y FLEXION
             * -----------*/
            /*Definir estructura de datos Edge (vertexA, vertexB, vertexOther)
             * vertexA < vertexB --> ordenados
             * Datos se pueden ordenar de nuevo de acuerdo con vertexA y vertexB para detectar aristas duplicadas
             */
            _trianglesMesh = _mesh.triangles;
            for (int i= 0; i <_trianglesMesh.Length; i+=3)
            {
                //Por cada triangulo: 3 datos : (A, B, C), (B, C, A), (A, C, B)
                _edges.Add(new Edge(_trianglesMesh[i], _trianglesMesh[i + 1], _trianglesMesh[i + 2]));
                _edges.Add(new Edge(_trianglesMesh[i + 1], _trianglesMesh[i + 2], _trianglesMesh[i]));
                _edges.Add(new Edge(_trianglesMesh[i], _trianglesMesh[i + 2], _trianglesMesh[i + 1]));
            }

            //Ordenar vertexA y vertexB
            _edges.Sort(CompareVertex);

            /*Arista duplicada (p.ej., B-C en la figura) = muelle de tracción
             * Arista duplicada = muelle de flexión para los vértices opuestos A-D*/
            int idx = 1;
            foreach (Edge edge in _edges)
            {
                _extension.Add(edge);
                if(idx < _edges.Count)
                {
                    //Debug.Log($"Triangle1: ({edge.GetNodeA()}, {edge.GetNodeB()}), Triangle2: ({_edges[idx].GetNodeA()}, {_edges[idx].GetNodeB()})");
                    if (edge.GetNodeA() == _edges[idx].GetNodeA() && edge.GetNodeB() == _edges[idx].GetNodeB())
                    {
                        //Diagonal donde hay duplicado (por lo q flexor es diago contraria --> la diago q forman los otros vertices)
                        _compression.Add(new Spring(_nodes[edge.GetOtherNode()], _nodes[_edges[idx].GetOtherNode()], CompressionSpringsStiffness, Damping));
                        _extension.Remove(edge);
                    }
                }
                idx++;    
            }
            Debug.Log("Nº muelles de traccion: " + _extension.Count);
            Debug.Log("Nº muelles de flexion: " + _compression.Count);
            /*
            Debug.Log("MUELLE TRACCION");
            foreach (Edge p in _pullSprings)
            {
                Debug.Log($"Triangle: ({p.GetNodeA()}, {p.GetNodeB()}, {p.GetOtherNode()})");
            }*/

            /*
            Debug.Log("MUELLE FLEXOR");
            foreach (Spring f in _flexSprings)
            {
                Debug.Log($"Muelle: ({f.GetNodeA().GetPos()}, {f.GetNodeB().GetPos()})");
            }*/

            //Creamos aristas a partir de los muelles de traccion para evitar aristas duplicadas
            CrearAristas();

        }

        public void Update()
        {
            _mesh = GetComponent<MeshFilter>().mesh;
            _collider = GetComponent<Collider>();

            Vector3[] vertices = new Vector3[_mesh.vertexCount];

            int i = 0;
            foreach (Node node in _nodes)
            {
                vertices[i] = transform.InverseTransformPoint(node.GetPos());
                i++;
            }

            /*-----------------
             * Pintar malla
             * ----------------*/
            foreach (Edge pull in _extension)
            {
                Debug.DrawLine(_nodes[pull.GetNodeA()].GetPos(), _nodes[pull.GetNodeB()].GetPos(), Color.blue);
                //Debug.Log($"Spring: ({spring.GetNodeA().GetPos()}, {spring.GetNodeB().GetPos()})");
            }

            foreach (Spring flex in _compression)
            {
                Debug.DrawLine(flex.GetNodeA().GetPos(), flex.GetNodeB().GetPos(), Color.red);
            }

            _mesh.vertices = vertices;

            //_mesh.RecalculateNormals();

        }

        public void FixedUpdate()
        {
            if (this.Paused)
                return; // Not simulating

            // Select integration method
            switch (this.IntegrationMethod)
            {
                case Integration.Explicit: this.stepExplicit(); break;
                case Integration.Symplectic: this.stepSymplectic(); break;
                default:
                    throw new System.Exception("[ERROR] Should never happen!");
            }

            /*----------------
             * Comprobar colisiones
             * ---------------*/
            foreach (Node node in _nodes)
            {
                if (fixer.CompareTag("Sphere"))
                {
                    if (fixer.Collides(node.GetPos()))
                    {
                        node.FixNode();
                        _fixedNodes.Add(node.GetPos() - fixer.FixerPosition());
                    }
                    else
                    {
                        Debug.DrawLine(node.GetPos(), fixer.FixerPosition(), Color.cyan);
                    }
                }
                else
                {
                    if (fixer.IsInside(node.GetPos()))
                    {
                        node.FixNode();
                        _fixedNodes.Add(node.GetPos() - fixer.FixerPosition());
                    }

                }
                
                
                
            }

            //Comprobar pos Fixer
            if (fixer.MovingFixer())
            {
                Transform newPosition = fixer.GetComponent<Transform>();

                int idx = 0;
                foreach (Node node in _nodes)
                {
                    if (node.IsFixed())
                    {
                        node.SetPos(newPosition.position + _fixedNodes[idx]);
                        //_fixedNodes[idx] = node.GetPos();
                        idx++;
                    }
                }
            }
                
        }

        #endregion

        /// <summary>
        /// Performs a simulation step in 1D using Explicit integration.
        /// </summary>
        private void stepExplicit()
        {
            foreach (Node node in _nodes)
            {
                node.SetForce(Vector3.zero);
                node.ComputeForces(transform.GetComponent<MassSpringCloth>().Gravity);

            }

            foreach (Spring spring in _springs)
            {
                spring.ComputeForces();
            }

            foreach (Node node in _nodes)
            {
                if (!node.IsFixed())
                {
                    node.SetVel(node.GetVel() + TimeStep / NodeMass * node.GetForce());
                    node.SetPos(node.GetPos() + TimeStep * node.GetVel());
                }
            }

            foreach (Spring spring in _springs)
            {
                spring.UpdateLength();
            }
        }

        /// <summary>
        /// Performs a simulation step in 1D using Symplectic integration.
        /// </summary>
        private void stepSymplectic()
        {            
            foreach (Node node in _nodes)
            {
                node.SetForce(Vector3.zero);
                node.ComputeForces(Gravity);
                if(Wind)
                {
                    node.WindForce(_windSpeed, WindFriction);
                } 
            }
            
            foreach (Spring spring in _springs)
            {
                spring.ComputeForces();
            }
            
            foreach (Node node in _nodes)
            {
                if (!node.IsFixed())
                {
                    node.SetVel(node.GetVel() + node.GetForce() * TimeStep / NodeMass );
                    node.SetPos(node.GetPos() + node.GetVel() * TimeStep);
                }
            }
            

            foreach (Spring spring in _springs)
            {
                spring.UpdateLength();
            }

        }

        private void CrearNodos()
        {
            Vector3 globalPos;
            //nodes = new List<Node>(verticesMesh.Length);

            foreach (Vector3 v in _verticesMesh)
            {
                globalPos = transform.TransformPoint(v);
                Node node = new Node(globalPos, NodeMass, Damping);
                _nodes.Add(node);
            }
        }

        private void CrearAristas()
        {            
            foreach (Edge pull in _extension)
            {
                _springs.Add(new Spring(_nodes[pull.GetNodeA()], _nodes[pull.GetNodeB()], ExtensionSpringsStiffness, Damping));
                //Debug.Log($"Spring: ({spring.GetNodeA().GetPos()}, {spring.GetNodeB().GetPos()})");
            }

            foreach(Spring flex in _compression)
            {
                _springs.Add(flex);
            }

            //Debug.Log("Nº total muelles: " + _springs.Count);
        }

        private static int CompareVertex(Edge edgeA, Edge edgeB)
        {
            if(edgeA.GetNodeA()== edgeB.GetNodeA())
            {
                return edgeA.GetNodeB().CompareTo(edgeB.GetNodeB());
            }
            else
            {
                return edgeA.GetNodeA().CompareTo(edgeB.GetNodeA());
            }
        }


    }
}
