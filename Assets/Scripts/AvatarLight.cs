using UnityEngine;
using System.Collections;

public class AvatarLight : MonoBehaviour {
	
	public Transform avatar;
	
	Transform transformRef;
	
	// Use this for initialization
	void Start () {
		transformRef = transform;
	}
	
	// Update is called once per frame
	void Update () {
		if (avatar) {
			Vector3 position = transformRef.position;
			position.x = avatar.position.x;
			position.y = avatar.position.y + 3.0f;
			transformRef.position = position;
		}
	}
}
