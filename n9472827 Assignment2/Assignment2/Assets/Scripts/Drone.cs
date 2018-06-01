using UnityEngine;
using System.Collections;

public class Drone : Enemy {

	//Drone Behaviour Variables
	public GameObject motherShip;
	public Vector3 scoutPosition;

	private float scoutTimer;
	private float detectTimer;
	private float scoutTime = 10.0f;
	private float detectTime = 5.0f;
	public float detectionRadius = 400.0f;
	private int newResourceVal;
	public GameObject newResourceObject;

	// shooting variables
	private float fireTimer = 5.0f;
	public GameObject alienLaser;
	private Vector3 shootingOffset = new Vector3(0,0,-10);

	private float attackOrFlee;
	private float hunger = 0f;
	private float hungerTimer = 0.1f;

	//Drone FSM Enumerator
	public enum DroneBehaviours{
		Idle,
		Scouting,
		Foraging,
		EliteForaging,
		Attacking,
		Fleeing
	}
	public DroneBehaviours droneBehaviour;

	public float healingFactor = 0.5f;
	private float maxHealth;
	private float healingTime = 0.5f;

	// The resources collected by a drone travelling back to the ship
	public int cargo = 0;

	//Boid Steering/Flocking Variables
	public float separationDistance = 25.0f;
	public float cohesionDistance = 50.0f;
	public float separationStrength = 250.0f;
	public float cohesionStrength = 25.0f;
	private Vector3 cohesionPos = new Vector3(0f, 0f, 0f);
	private int boidIndex = 0;

    GameManager gameManager;

    Rigidbody rb;

    //Movement & Rotation Variables
    public float speed = 50.0f;
    private float rotationSpeed = 5.0f;
    private float adjRotSpeed;
    private Quaternion targetRotation;
    public GameObject target;
    public float targetRadius = 200f;

	private Vector3 tarVel;
	private Vector3 tarPrevPos;
	private Vector3 attackPos;
	private float distanceRatio = 0.05f;
	private Vector3 fleePos;

    // Use this for initialization
    void Start() {
		cargo = 0;

        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

        rb = GetComponent<Rigidbody>();

		motherShip = gameManager.alienMothership;
		scoutPosition = motherShip.transform.position;

		health = Random.Range((0.5f*health),(1.5f*health));
		maxHealth = health;
		speed = Random.Range((0.5f*speed),(1.5f*speed));
		healingFactor = Random.Range((0.5f*healingFactor),(5f*healingFactor));
		targetRadius = Random.Range ((0.8f * targetRadius),(1.5f * targetRadius));
		detectionRadius = Random.Range((0.7f*detectionRadius),(1.3f*detectionRadius)); 
    }


    // Update is called once per frame
    void Update() {

        //Acquire player if spawned in
		if (gameManager.gameStarted) {
			//Acquire player if spawned in
			target = gameManager.playerDreadnaught;
			// Heursitic function here
			attackOrFlee = (Friends()*health) + hunger*(((speed*targetRadius)/5000)+(health*healingFactor)/10);
			if (attackOrFlee >= 1000) {
				droneBehaviour = DroneBehaviours.Attacking;
			} else if (attackOrFlee < 1000) {
				if (motherShip != null) {
					droneBehaviour = DroneBehaviours.Fleeing;
				} else {
					droneBehaviour = DroneBehaviours.Attacking;
				}
			}
			//droneBehaviour = DroneBehaviours.Attacking;
		}

//        //Move towards valid targets
//        if(target)
//			MoveTowardsTarget(target.transform.position);

		//Boid cohesion/segregation
		BoidBehaviour();

		//Drone Behaviours - State Switching
		switch (droneBehaviour) {
		case DroneBehaviours.Scouting:
			Scouting();
			break;
		case DroneBehaviours.Foraging:
			Forage ();
			break;
		case DroneBehaviours.EliteForaging:
			EliteForage ();
			break;
		case DroneBehaviours.Attacking:
			Attacking ();
			break;
		case DroneBehaviours.Fleeing:
			Fleeing ();
			break;
		}

		RegenHealth ();

    }

	private void MoveTowardsTarget(Vector3 targetPos) {
        //Rotate and move towards target if out of range
        if (Vector3.Distance(targetPos, transform.position) > targetRadius) {

            //Lerp Towards target
            targetRotation = Quaternion.LookRotation(targetPos - transform.position);
            adjRotSpeed = Mathf.Min(rotationSpeed * Time.deltaTime, 1);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, adjRotSpeed);

            rb.AddRelativeForce(Vector3.forward * speed * 20 * Time.deltaTime);
        }
    }

	private void BoidBehaviour() {
		//Increment boid index reference
		boidIndex++;
		//Check if last boid in Enemy list
		if (boidIndex >= gameManager.enemyList.Length) {
			//Re-Compute the cohesionForce
			Vector3 cohesiveForce = (cohesionStrength / Vector3.Distance(cohesionPos, transform.position)) * (cohesionPos - transform.position);
			//Apply Force
			rb.AddForce(cohesiveForce);
			//Reset boidIndex
			boidIndex = 0;
			//Reset cohesion position
			cohesionPos.Set(0f, 0f, 0f);
		}
		//Currently analysed boid variables
		Vector3 pos = gameManager.enemyList[boidIndex].transform.position;
		Quaternion rot = gameManager.enemyList[boidIndex].transform.rotation;
		float dist = Vector3.Distance(transform.position, pos);
		//If not this boid
		if (dist > 0f) {
			//If within separation
			if (dist <= separationDistance) {
				//Compute scale of separation
				float scale = separationStrength / dist;
				//Apply force to ourselves
				rb.AddForce(scale * Vector3.Normalize(transform.position - pos));
			}
		//Otherwise if within cohesion distance of other boids
		} else if (dist < cohesionDistance && dist > separationDistance) { 
			//Calculate the current cohesionPos
			cohesionPos = cohesionPos + pos * (1f / (float)gameManager.enemyList.Length);
			//Rotate slightly towards current boid
			transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, 1f);            	
		}
	}

	//Drone FSM Behaviour - Scouting
	private void Scouting() {
		//If no new resource object found
		if (!newResourceObject) {
			//If close to scoutPosition, randomize new position to investigate within gamespace around mothership
			if (Vector3.Distance(transform.position, scoutPosition) < detectionRadius && Time.time > scoutTimer) { 
				//Generate new random position
				Vector3 position;
				position.x = motherShip.transform.position.x + Random.Range(-1500, 1500);
				position.y = motherShip.transform.position.y + Random.Range(-400, 400);
				position.z = motherShip.transform.position.z + Random.Range(-1500, 1500);

				scoutPosition = position;

				//Update scoutTimer
				scoutTimer = Time.time + scoutTime;

			} else {
				MoveTowardsTarget(scoutPosition);
				Debug.DrawLine(transform.position, scoutPosition, Color.yellow);
			}
			//Every few seconds, check for new resources
			if (Time.time > detectTimer) {
				newResourceObject = DetectNewResources();
				detectTimer = Time.time + detectTime;
			}
		}
		//Resource found, head back to Mothership
		else {
			target = motherShip;

			//In range of mothership, relay information and reset to drone again
			if (Vector3.Distance (transform.position, motherShip.transform.position) < targetRadius) {
				motherShip.GetComponent<Mothership> ().drones.Add (this.gameObject);
				motherShip.GetComponent<Mothership> ().scouts.Remove (this.gameObject);
				motherShip.GetComponent<Mothership> ().resourceObjects.Add (newResourceObject);
				newResourceVal = 0;
				newResourceObject = null;
				droneBehaviour = DroneBehaviours.Idle;
			} else {
				Debug.DrawLine(transform.position, target.transform.position, Color.green);
				MoveTowardsTarget (target.transform.position);
			}
		}
	}

	// Method used by elite foragers
	private void EliteForage() {
		// if no new objects - perform foraging duties
		if (!newResourceObject) {
			Forage ();
			// periodically check for better resources on the way
			if (Time.time > detectTimer) {
				newResourceObject = DetectNewResources();
				detectTimer = Time.time + detectTime;
			}
		// If a new resource is found and the drone does not have any current cargo
		} else {
			if (cargo == 0) {
				// Compare this new resource against the original and check if knowledge about it is already with mothership
				if ((newResourceObject.GetComponent<Asteroid> ().resource < target.GetComponent<Asteroid> ().resource) || (motherShip.GetComponent<Mothership>().resourceObjects.Contains(newResourceObject))) {
					newResourceObject = null;
				} else {
				// if new resource is better - become scout and report back
					droneBehaviour = DroneBehaviours.Scouting;
				}
			}
		}
	}

	// Method used by drones selected as foragers (and to an extent elite foragers)
	private void Forage() {
		// On begining journey to asteroid
		if (target == null) {
			target = motherShip;
		}
		// If the drone has not reached the asteroid yet. move towards it
		if (target != motherShip) {
			if (Vector3.Distance (transform.position, target.transform.position) > targetRadius) {
				MoveTowardsTarget (target.transform.position);
				Debug.DrawLine (transform.position, target.transform.position, Color.blue);
			// upon reaching the asteroid - take some of its resource as cargo
			} else {
				cargo = 10;
				target.GetComponent<Asteroid> ().resource -= 10;
				if (target.GetComponent<Asteroid> ().resource < 0) {
					cargo = cargo + target.GetComponent<Asteroid> ().resource;
					motherShip.GetComponent<Mothership> ().resourceObjects.Remove (target);
				}
				// Set the target as the mothership to return with gathered resources
				target = motherShip;
			}
		} else {
			if (Vector3.Distance (transform.position, target.transform.position) > targetRadius) {
				MoveTowardsTarget (target.transform.position);
				Debug.DrawLine (transform.position, target.transform.position, Color.green);
			} else {
			// A safety check to ensure the drones can only deposit positive values of cargo in case of errors
				if (cargo > 0) {
					motherShip.GetComponent<Mothership> ().resourcesCollected += cargo;
				}
				// reset all parameters and return to idle state
				cargo = 0;
				motherShip.GetComponent<Mothership>().drones.Add(this.gameObject);
				motherShip.GetComponent<Mothership>().foragers.Remove(this.gameObject);
				droneBehaviour = DroneBehaviours.Idle;
			}
		}
	}

	//Method used periodically by scouts/elite forages to check for new valid resources
	private GameObject DetectNewResources() {
		//Go through list of asteroids and ...
		for (int i = 0; i < gameManager.asteroids.Length; i++) {

			//... check if they are within detection distance
			if (Vector3.Distance (transform.position, gameManager.asteroids [i].transform.position) <= detectionRadius) {

				//Find the best one
				if (gameManager.asteroids [i].GetComponent<Asteroid> ().resource > newResourceVal) {
					newResourceObject = gameManager.asteroids [i];
				}
			}
		}
		//Double check to see if the Mothership already knows about it and return it if not
		if (motherShip.GetComponent<Mothership> ().resourceObjects.Contains (newResourceObject)) {
			return null;
		} else {
			return newResourceObject;
		}
	}

	//Drone FSM Behaviour - Attacking
	private void Attacking() {
		if (Time.time > hungerTimer && hunger > 0) {
			hungerTimer = hungerTimer + Time.time;
			hunger = hunger - 1;
		}
		//Calculate target's velocity (without using RB)
		tarVel = (target.transform.position - tarPrevPos) / Time.deltaTime;
		tarPrevPos = target.transform.position;
		//Calculate intercept attack position (p = t + r * d * v)
		attackPos = target.transform.position + distanceRatio * Vector3.Distance(transform.position, target.transform.position) * tarVel;
		attackPos.y = attackPos.y + 10;
		Debug.DrawLine(transform.position, attackPos, Color.red);
		// Not in range of intercept vector - move into position
		if(Vector3.Distance(transform.position, attackPos) > targetRadius)
			MoveTowardsTarget(attackPos);
		else {
			//Look at target - Lerp Towards target
			targetRotation = Quaternion.LookRotation(target.transform.position - transform.position);
			adjRotSpeed = Mathf.Min(rotationSpeed * Time.deltaTime, 1);
			transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, adjRotSpeed);
			//Fire Weapons at target
			//...
			if (Time.time > fireTimer) {
				Instantiate (alienLaser, (transform.position+shootingOffset), transform.rotation);
				fireTimer = Time.time + fireTimer;
			}
		}
	}

	// Drone FSM Behaviour - Fleeing
	private void Fleeing () {
		if (Time.time > hungerTimer) {
			hungerTimer = hungerTimer + Time.time;
			hunger = hunger + 1.0f;
		}
		//Calculate target's velocity (without using RB)
		tarVel = (target.transform.position - tarPrevPos) / Time.deltaTime;
		tarPrevPos = target.transform.position;
		//Calculate intercept position (p = t + r * d * v) and multiply it by -1 to get the flee vector
		fleePos = (target.transform.position + distanceRatio * Vector3.Distance(transform.position, target.transform.position) * tarVel)*-1.0f;
		if (Vector3.Distance (transform.position, target.transform.position) > (targetRadius * 5.0f)) {
			if (Vector3.Distance (transform.position, motherShip.transform.position) > (targetRadius)) {
				MoveTowardsTarget (motherShip.transform.position);
				Debug.DrawLine (transform.position, motherShip.transform.position, Color.green);
			}
		} else {
			MoveTowardsTarget (fleePos);
			Debug.DrawLine (transform.position, fleePos, Color.yellow);
		}
	}

	// check how many other drones in close range
	private int Friends() {
		int clusterStrength = 0;
		for (int i = 0; i < gameManager.enemyList.Length; i++) {
			if (Vector3.Distance (transform.position, gameManager.enemyList [i].transform.position) < targetRadius) {
				clusterStrength += 1;
			}
		}
		return clusterStrength;
	}
	// Heal any damage taken over time
	private void RegenHealth() {
		if (health < maxHealth && Time.time > healingTime) {
			health += healingFactor;
			healingTime = healingTime + Time.time;
		}
	}
}
