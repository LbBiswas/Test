using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoRestart : MonoBehaviour {

	public YoutubePlayer ytPlayer;

	// Use this for initialization
	void Start () {
		Debug.Log ("video start");
	}

	void Awake () {
		Debug.Log ("video awake");
	}

	void OnEnable () {
		Debug.Log ("video OnEnable");

		ytPlayer.Play (ytPlayer.youtubeUrl);
//		ytPlayer.RetryPlayYoutubeVideo ();
	}

	void OnDisable() {
		Debug.Log ("video OnDisable");


//		Destroy (ytPlayer);

//		ytPlayer.
//		ytPlayer.RetryPlayYoutubeVideo ();
//		ytPlayer.Stop ();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
