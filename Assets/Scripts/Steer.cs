using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Steer : MonoBehaviour {

	public GameObject waypoint;

	public float speed = 5f;
	public float turnSpeed = 45f; // degrees/second

	// how close we need to be to the waypoint to consider that we've arrived there
	public float arrivalTolerance = 1f;

	// controls over what algorithm to use
	public bool smoothTurning = false;

	List<GameObject> collisionThreats = new List<GameObject>();

	#region 2D position and angle of rotation
	Vector2 Vector3toVector2(Vector3 v) {
		return new Vector2(v.x, v.z);
	}

	Vector2 position {
		get {
			return Vector3toVector2(gameObject.transform.position);
		}

		set {
			transform.position = new Vector3(value.x, 0f, value.y);
		}
	}

	float CleanDegrees(float r) {
		// convert to range [0,360)
		while(r < 0 || r >= 360) {
			if(r<0) r+=360;
			if(r>=360) r-= 360;
		}
		return r;
	}

	float rotation {
		get {
			// return transform.rotation.eulerAngles.y;
			return CleanDegrees (- (transform.rotation.eulerAngles.y - 90));
		}

		set {
			Quaternion newRotation = Quaternion.identity;
			// newRotation.eulerAngles = new Vector3(0f, value, 0f);
			newRotation.eulerAngles = new Vector3(0f, - value + 90, 0f);
			transform.rotation = newRotation;
		}
	}

	Vector2 targetPosition {
		get {
			return new Vector2(waypoint.transform.position.x, waypoint.transform.position.z);
		}
	}
	#endregion

	#region direction and target rotation
	// Return's vehicle's current direction as a vector
	Vector2 direction {
		get {
			return new Vector2(Mathf.Cos(Mathf.Deg2Rad * rotation), Mathf.Sin(Mathf.Deg2Rad * rotation));
		}
	}

	// Returns direction towards next waypoint as a normalized vector
	Vector2 directionOfNextWaypoint {
		get {
			Vector2 d = targetPosition - position;
			d.Normalize();
			return d;
		}
	}

	// computes vector at 90 degrees to given vector - a good vector to follow if trying to avoid v
	Vector2 AvoidanceVector(Vector2 v) {
		return new Vector2(-v.y, v.x);
	}

	Vector2 targetDirection {
		get {
			if(collisionThreats.Count == 0) {
				return directionOfNextWaypoint;
			} else {
				Vector2 dir = directionOfNextWaypoint;
				foreach(GameObject threat in collisionThreats) {
					dir += AvoidanceVector(Vector3toVector2(threat.transform.forward));
				}
				dir /= 1 + collisionThreats.Count;
				return dir;
			}
		}
	}

	// Returns the rotation (in degrees) which will orient vehicle toward next waypoint
	float targetRotation {
		get {
			float r = Mathf.Rad2Deg * Mathf.Acos(targetDirection.x);
			if(targetDirection.y < 0) r*=-1;
			return CleanDegrees(r);
		}
	}
	#endregion

	#region collision detection
	void OnTriggerEnter(Collider other) {
		Debug.Log(name + " collided with " + other.name);
		if(other.CompareTag("obstacle")) {
			if(!collisionThreats.Contains(other.gameObject)) {
				collisionThreats.Add (other.gameObject);
			}
		}
	}
	
	void OnTriggerExit(Collider other) {
		collisionThreats.Remove(other.gameObject);
	}
	#endregion

	bool moving {
		get {
			return waypoint != null;
		}
	}

	// Returns true if we have arrived at the next waypoint
	bool Arrived() {
		return Vector2.Distance(position, targetPosition) <= arrivalTolerance;
	}

	void TurnToWaypoint() {
		if(moving) {
			if(!smoothTurning) {
				rotation = targetRotation;
			} else {
				float requiredTurn = CleanDegrees(targetRotation - rotation);
				if(requiredTurn>180) requiredTurn -= 360;
				float maxTurn = turnSpeed * Time.deltaTime;
				if(Mathf.Abs(requiredTurn) <= maxTurn) {
					rotation = targetRotation;
				} else {
					rotation += Mathf.Sign(requiredTurn) * maxTurn;
				}
			}
		}
	}

	void AdvanceWaypointIfArrived() {
		if(Arrived()) {
			Debug.Log ("Arrived at " + waypoint.name);
			waypoint = waypoint.GetComponent<WaypointNext>().nextWaypoint;
		}
	}

	void SteerAI() {
		AdvanceWaypointIfArrived();
		TurnToWaypoint();

		position += direction * speed * Time.deltaTime;
	}

	// Update is called once per frame
	void Update () {
		if(moving) SteerAI ();
	}
}
