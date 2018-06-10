using UnityEngine;
using System.Collections;

public class AvatarDebris : MonoBehaviour {
	
	float timer;
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
		timer += Time.deltaTime;
		if (timer > 3.0f) {
			Destroy(gameObject);
		}
	}
}
