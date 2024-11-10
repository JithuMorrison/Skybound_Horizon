using UnityEngine;
using System.Collections;

public class Birds : MonoBehaviour {
	public lb_BirdController birdControl;
	Ray ray;
	RaycastHit[] hits;

	void Start(){
		birdControl = GameObject.Find ("_livingBirdsController").GetComponent<lb_BirdController>();
		SpawnSomeBirds();
	}

	void Update () {
		if(Input.GetMouseButtonDown(0)){
			hits = Physics.RaycastAll (ray);
			foreach(RaycastHit hit in hits){
				if (hit.collider.tag == "lb_bird"){
					hit.transform.SendMessage ("KillBirdWithForce",ray.direction*500);
					break;
				}
			}
		}
	}

	IEnumerator SpawnSomeBirds(){
		yield return 2;
		birdControl.SendMessage ("SpawnAmount",10);
	}
}
