using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.mediation.impl;
using app;


public class ScrollPickerMediator : Mediator {

	[Inject]
	public ScrollPickerView view { get; set; }

	[Inject]
	public UiSignal uiSignal { get; set;}

	public void uiSignalListener(string key, string type, Dictionary<string, object> data)
	{
		uiSignal.Dispatch (key, type, data);
	}

	// Use this for initialization
	override public void OnRegister(){

		//Debug.Log ("OnRegister(toolTip)");

		view.init ();


		view.uiSignal.AddListener (uiSignalListener);



	}

	override public void OnRemove()
	{
		if(view != null)
			view.uiSignal.RemoveListener (uiSignalListener);

	}
}
