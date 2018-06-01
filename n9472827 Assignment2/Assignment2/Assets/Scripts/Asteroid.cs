using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteroid : MonoBehaviour {

	public int resource = 50;
	private int maxResource;
	private float healingFactor = 10;

	// Use this for initialization
	void Start () {
		// randomly generate a resource value
		resource = Random.Range(10, 100);
		// Set an upper bound for resource regeneration
		maxResource = resource;
	}
	
	// Update is called once per frame
	void Update () {
		// if the asteroid has been harvested - slowly regenerate the resource - up to its initial value
		if (resource < maxResource) {
			if (Time.time > healingFactor) {
				healingFactor += Time.time;
				resource += 1;
			}
		}
	}
}
