using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node_v0 : MonoBehaviour {

    public Vector3 pos;
    public Vector3 vel;
    public Vector3 force;

    public float mass;
    public bool isFixed;

    // Use this for initialization
    private void Awake()
    {
        pos = transform.position;
        vel = Vector3.zero;
        //Instantiate(node);
    }

    void Start () {
	}
	
	// Update is called once per frame
	void Update () {
        transform.position = pos;
	}

    public void ComputeForces()
    {
        //force += mass * transform.parent.GetComponent<PhysicsManager>().Gravity;
    }
}
