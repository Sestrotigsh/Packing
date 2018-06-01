using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class Mothership : MonoBehaviour {

	//Resource Harvesting Variables
	public List<GameObject> drones = new List<GameObject>();
	public List<GameObject> scouts = new List<GameObject>();
	public List<GameObject> eliteForagers = new List<GameObject>();
	public List<GameObject> foragers = new List<GameObject>();
	public int maxScouts = 4;
	public int maxForagers = 3;
	public int maxEliteForagers = 2;

    public GameObject enemy;
    public int numberOfEnemies = 20;

    public GameObject spawnLocation;

	// The time between checks / sending out scouts
	public float scoutCheckTimer = 5.0f;

	// The value to determine which drone is best
	private float scoutHeuristic;
	private int bestDrone;

	public List<GameObject> resourceObjects = new List<GameObject>();

	private float forageTimer;
	private float forageTime = 10.0f;

	public int resourcesCollected = 0;
	public int gameLimit = 200;

    // initialise the boids
    void Start() {

        for (int i = 0; i < numberOfEnemies; i++) {

            Vector3 spawnPosition = spawnLocation.transform.position;

            spawnPosition.x = spawnPosition.x + Random.Range(-50, 50);
            spawnPosition.y = spawnPosition.y + Random.Range(-50, 50);
            spawnPosition.z = spawnPosition.z + Random.Range(-50, 50);

			GameObject thisEnemy = Instantiate(enemy, spawnPosition, spawnLocation.transform.rotation) as GameObject;
			drones.Add(thisEnemy); 


        }
    }

    // Update is called once per frame
    void Update() {
		// Periodically check if the the scout list is full and if not assign a scout
		if (Time.time > scoutCheckTimer && scouts.Count < maxScouts) {
			// Use a heuristic function that weights speed and detection radius equally
			scoutHeuristic = 0;
			bestDrone = 0;
			for (int i = 0; i < drones.Count-1; i++) {
				// Error check for when player destroys drones
				if (drones [i] != null) {
					if (((0.5 * drones [i].GetComponent<Drone> ().speed) + (0.5 * drones [i].GetComponent<Drone> ().detectionRadius)) > scoutHeuristic) {
						scoutHeuristic = ((0.5f * drones [i].GetComponent<Drone> ().speed) + (0.5f * drones [i].GetComponent<Drone> ().detectionRadius));
						bestDrone = i;
					}
				}
			}
			scouts.Add(drones[bestDrone]);
			drones.Remove(drones[bestDrone]);
			scouts[scouts.Count - 1].GetComponent<Drone>().droneBehaviour = Drone.DroneBehaviours.Scouting;
			scoutCheckTimer = Time.time+scoutCheckTimer;
		}

		//(Re)Determine best resource objects periodically
		if (resourceObjects.Count > 0 && Time.time > forageTimer) {

			//Sort resource objects delegated by their resource amount in decreasing order
			resourceObjects.Sort(delegate (GameObject a, GameObject b) {
				return (b.GetComponent<Asteroid>().resource).CompareTo(a.GetComponent<Asteroid>().resource);
			});

			if (eliteForagers.Count <= maxEliteForagers) {
				// Use a heuristic function that priorities speed but also considers the detection radius
				for (int i = 0; i < drones.Count-1; i++) {
					scoutHeuristic = 0;
					bestDrone = 0;
					if (((0.8*drones[i].GetComponent<Drone>().speed) + (0.2*drones[i].GetComponent<Drone>().detectionRadius)) > scoutHeuristic) {
						scoutHeuristic = ((0.8f * drones [i].GetComponent<Drone> ().speed) + (0.2f * drones [i].GetComponent<Drone> ().detectionRadius));
						bestDrone = i;
					}
				}
				eliteForagers.Add (drones [bestDrone]);
				drones.Remove (drones [bestDrone]);
				eliteForagers [eliteForagers.Count - 1].GetComponent<Drone> ().droneBehaviour = Drone.DroneBehaviours.EliteForaging;
				///////////////////////////////////////////////////////////////// CHOOSE ASTEROID BY HEURISTIC

				eliteForagers [eliteForagers.Count - 1].GetComponent<Drone> ().target = resourceObjects[(eliteForagers.Count-1)%(resourceObjects.Count)];
				// resourceObjects.RemoveAt (0);
			// If all elite foragers occupied  and foragers free - send a forager
			} else if (foragers.Count <= maxForagers) {
				// Use a heuristic function that only considers speed
				for (int i = 0; i < drones.Count-1; i++) {
					scoutHeuristic = 0;
					bestDrone = 0;
					if ((drones[i].GetComponent<Drone>().speed) > scoutHeuristic) {
						scoutHeuristic = ((0.8f * drones [i].GetComponent<Drone> ().speed) + (0.2f * drones [i].GetComponent<Drone> ().detectionRadius));
						bestDrone = i;
					}
				}
				foragers.Add (drones [bestDrone]);
				drones.Remove (drones [bestDrone]);
				foragers [foragers.Count - 1].GetComponent<Drone> ().droneBehaviour = Drone.DroneBehaviours.Foraging;

				// The asteroid to forage from is chosen as the highest valued asteroid not currently being foraged. if the resource list is smaller than (2 elite + 3 foragers) then cycle to the highest value.
				foragers [foragers.Count - 1].GetComponent<Drone> ().target = resourceObjects[((eliteForagers.Count-1)+(foragers.Count-1))%(resourceObjects.Count)];
				// resourceObjects.RemoveAt (0);
			}
			forageTimer = Time.time + forageTime;
		}

		// If the mothership collects enough resources - enable the endgame state
		if (resourcesCollected > gameLimit) {
			SceneManager.LoadScene ("EndGame");
		}
    }
}

