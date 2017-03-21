using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*
 * From Rubiks wiki (http://rubiks.wikia.com/wiki/Notation):
 * 
 *     F (Front): the side currently facing you
 *     B (Back): the side opposite the front
 *     U (Up): the side above or on top of the front side
 *     D (Down): the side opposite Up or on bottom
 *     L (Left): the side directly to the left of the front
 *     R (Right): the side directly to the right of the front
 *     
 *     When a prime symbol ['] follows a letter, it means to turn
 *         the face counter-clockwise a quarter-turn, while a letter
 *         without a prime symbol means to turn it a quarter-turn
 *         clockwise.
 * 
 * In our case, denote counter-clockwise rotations with a 2.
 */ 
public enum Rotation {
	F,  B,  U,  D,  L,  R,
	F2, B2, U2, D2, L2, R2
};

public class Cube : MonoBehaviour {

	Queue<Rotation> rotations = new Queue<Rotation>();

	internal CubePart[] cubeParts;
	internal CubePart[] middles;
	internal CubePart center;

	bool rotating = false;
	RunningRotation runningRotation;
	Vector3 middlePos;

	void Start () {
		Debug.Log ("Starting cube.");

		cubeParts = (CubePart[]) GameObject.FindObjectsOfType (typeof(CubePart));
		Debug.Log ("Found " + cubeParts.Length + " CubeParts.");

		middles = GameObject.FindGameObjectWithTag ("Middle").GetComponentsInChildren<CubePart> ();
		Debug.Log ("Found " + middles.Length + " middles.");

		center = GameObject.FindGameObjectWithTag ("Center").GetComponent<CubePart> ();
		Debug.Log ((center != null ? "Found" : "Did not find") + " center.");

		Main.Start ();
	}

	void Update () {
		// If we're already rotating, continue
		if (rotating) {
			continueRotation ();
		// Otherwise, if we can start a rotation, start it
		} else if (rotations.Count != 0) {
			startRotation ();
		}
	}

	internal Rotation reverseRotation(Rotation rotation) {
		switch (rotation) {
		case(Rotation.F):
			return Rotation.F2;
		case(Rotation.B):
			return Rotation.B2;
		case(Rotation.U):
			return Rotation.U2;
		case(Rotation.D):
			return Rotation.D2;
		case(Rotation.L):
			return Rotation.L2;
		case(Rotation.R):
			return Rotation.R2;
		case(Rotation.F2):
			return Rotation.F;
		case(Rotation.B2):
			return Rotation.B;
		case(Rotation.U2):
			return Rotation.U;
		case(Rotation.D2):
			return Rotation.D;
		case(Rotation.L2):
			return Rotation.L;
		case(Rotation.R2):
			return Rotation.R;
		default:
			throw new BadProgrammingException ("Somehow given a rotation outside of reverseRotation scope.");
		}
	}
		
	// Enqueue a rotation
	public void rotate (Rotation rotation) {
		Debug.Log ("Enqueue rotation: " + rotation);
		rotations.Enqueue (rotation);
	}

	// Enqueue array of rotations
	public void rotate (Rotation[] rotations, bool reverse = false) {
		if (reverse) {
			Array.Reverse (rotations);
		}

		Rotation r;
		foreach (Rotation rotation in rotations) {
			r = rotation;
			if (reverse) {
				r = reverseRotation (r);
			}
			rotate (r);
		}
	}

	// Enqueue a string of rotations
	public void rotate (String rotations, bool reverse = false) {
		List<Rotation> result = new List<Rotation> ();

		foreach (string rot_str in rotations.Split (' ')) {
			switch (rot_str) {
			case "F":
				result.Add (Rotation.F);
				break;
			case "B":
				result.Add (Rotation.B);
				break;
			case "U":
				result.Add (Rotation.U);
				break;
			case "D":
				result.Add (Rotation.D);
				break;
			case "L":
				result.Add (Rotation.L);
				break;
			case "R":
				result.Add (Rotation.R);
				break;
			case "F'":
			case "F2":
				result.Add (Rotation.F2);
				break;
			case "B'":
			case "B2":
				result.Add (Rotation.B2);
				break;
			case "U'":
			case "U2":
				result.Add (Rotation.U2);
				break;
			case "D'":
			case "D2":
				result.Add (Rotation.D2);
				break;
			case "L'":
			case "L2":
				result.Add (Rotation.L2);
				break;
			case "R'":
			case "R2":
				result.Add (Rotation.R2);
				break;
			}
		}

		rotate (result.ToArray (), reverse);
	}

	// Do a rotation
	void startRotation() {
		Rotation rotation = rotations.Dequeue ();
		Debug.Log ("Perform rotation: " + rotation);

		// Find side to rotate in relation to center cube
		CubePart[] toRotate = getCubePartsToRotate(rotation);
		Debug.Log ("Found " + toRotate.Length + " parts to rotate.");

		// Find middle cube to rotate around
		// TODO: Store these in a rotation to cubepart hashmap from the start
		CubePart middle = null;

		foreach (CubePart mp in middles) {
			middle = Array.Find (toRotate, cp => cp == mp);

			if (middle != null) {
				break;
			}
		}

		if (middle == null) {
			throw new BadProgrammingException ("Could not find a middle to rotate around.");
		}

		Vector3 rotateAround = getAxisToRotateAround (rotation, middle);

		float rotateSpeed = getRotateSpeed (rotation);

		rotating = true;
		runningRotation = new RunningRotation (rotation, toRotate, middle, rotateAround, rotateSpeed);
	}

	// Continue an existing rotation
	void continueRotation() {
		bool finished = false;

		// Degrees to rotate
		float degrees = runningRotation.rotateSpeed * Time.deltaTime;

		// If it over-rotates then correct rotation and finish.
		runningRotation.totalRotated += Math.Abs(degrees);
		if (runningRotation.totalRotated >= 90) {
			degrees = (runningRotation.totalRotated - 90) * (degrees < 0 ? -1 : 1);
			finished = true;
		}

		// Rotate each cube part around middle cube
		foreach (CubePart cp in runningRotation.parts) {
			cp.doRotation (runningRotation.middle.transform.position, runningRotation.rotateAround, degrees);
		}

		if (finished) {
			rotating = false;
			runningRotation = null;
		}
	}

	CubePart[] getCubePartsToRotate(Rotation rotation) {
		Vector3 center_pos = center.transform.position;
		Predicate<CubePart> requirement;

		switch (rotation) {
		// right side
		case Rotation.R2:
		case Rotation.R:
			requirement = (cp => cp.transform.position.x > center_pos.x + 1);
			break;
		// left side
		case Rotation.L2:
		case Rotation.L:
			requirement = (cp => cp.transform.position.x < center_pos.x - 1);
			break;
		
		// top
		case Rotation.U2:
		case Rotation.U:
			requirement = (cp => cp.transform.position.y > center_pos.y + 1);
			break;
		// bottom
		case Rotation.D2:
		case Rotation.D:
			requirement = (cp => cp.transform.position.y < center_pos.y - 1);
			break;
		
		// back
		case Rotation.B2:
		case Rotation.B:
			requirement = (cp => cp.transform.position.z > center_pos.z + 1);
			break;
		// front
		case Rotation.F2:
		case Rotation.F:
			requirement = (cp => cp.transform.position.z < center_pos.z - 1);
			break;
		default:
			throw new BadProgrammingException ("Unable to handle given rotation.");
		}

		return Array.FindAll(cubeParts, requirement);
	}

	Vector3 getAxisToRotateAround (Rotation rotation, CubePart middle) {
		switch (rotation) {
		case Rotation.R2:
		case Rotation.R:
			return Vector3.right;
		case Rotation.L2:
		case Rotation.L:
			return Vector3.left;
		case Rotation.U2:
		case Rotation.U:
			return Vector3.up;
		case Rotation.D2:
		case Rotation.D:
			return Vector3.down;
		case Rotation.B2:
		case Rotation.B:
			return Vector3.forward;
		case Rotation.F2:
		case Rotation.F:
			return Vector3.back;
		default:
			throw new BadProgrammingException ("Unable to handle given rotation.");
		}
	}

	float getRotateSpeed (Rotation rotation) {
		float rotateSpeed = 45;

		switch (rotation) {
		case Rotation.R:
		case Rotation.L:
		case Rotation.U:
		case Rotation.D:
		case Rotation.B:
		case Rotation.F:
			return rotateSpeed;
		case Rotation.R2:
		case Rotation.L2:
		case Rotation.U2:
		case Rotation.D2:
		case Rotation.B2:
		case Rotation.F2:
			return rotateSpeed * -1;
		default:
			throw new BadProgrammingException ("Unable to handle given rotation.");
		}
	}

	class RunningRotation {
		internal Rotation rotation;
		internal CubePart[] parts;
		internal CubePart middle;
		internal Vector3 rotateAround;
		internal float rotateSpeed;
		internal float totalRotated;

		internal RunningRotation(Rotation rotation, CubePart[] parts, CubePart middle, Vector3 rotateAround, float rotateSpeed) {
			this.rotation = rotation;
			this.parts = parts;
			this.middle = middle;
			this.rotateAround = rotateAround;
			this.rotateSpeed = rotateSpeed;
			this.totalRotated = 0f;
		}
	}
		
}

class BadProgrammingException: Exception {
	internal BadProgrammingException(string message): base(message) {
	}
}
