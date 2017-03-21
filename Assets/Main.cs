using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Main : MonoBehaviour {

	static Cube cube;

	// Use this for initialization
	public static void Start () {
		cube = (Cube) GameObject.FindObjectOfType (typeof(Cube));

		// Rotate cube one way, then back, using Singmaster Notation.
		cube.rotate("F B U D L R F' B' U' D' L' R'");
		cube.rotate("F B U D L R F' B' U' D' L' R'", reverse: true);
	}

}
