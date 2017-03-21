using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubePart : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void doRotation(Vector3 point, Vector3 axis, float angle) {
		transform.RotateAround (point, axis, angle);
	}
}
