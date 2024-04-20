using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragCorrector : MonoBehaviour {

	public int baseTH = 6;
	public int basePPI = 210;
	public int dragTH = 0;

	// Use this for initialization
	void Start () {
		dragTH = baseTH * (int)Screen.dpi / basePPI;

		EventSystem eventSystem = GetComponent<EventSystem>();

		if (eventSystem) {
			eventSystem.pixelDragThreshold = dragTH;
			Debug.Log ("EventSystem: pixelDragThreshold = " + dragTH);
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
