using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.mediation.impl;
using app;

public class ButtonMediator : Mediator {

	[Inject]
	public ButtonView view { get; set; }

	[Inject]
	public UiSignal uiSignal { get; set;}

	[Inject]
	public SystemResponseSignal systemReponseSignal {get; set;}

	public void uiSignalListener(string key, string type, Dictionary<string, object> data)
	{
		uiSignal.Dispatch (key, type, data);
	}

	public void systemResponseSignalListener(string key, Dictionary<string, object> data) {

		switch(key)
		{
		case SystemResponseEvents.Start:
			if(view.announcePresence)
			{
				uiSignal.Dispatch(view.buttonName, UiEvents.Presence, new Dictionary<string, object>{{"AttachedObject", view.attachedObject}});
			}

			break;
		}
	}

	// Use this for initialization
	override public void OnRegister(){

		//Debug.Log ("OnRegister(toolTip)");

		view.init ();


		view.uiSignal.AddListener (uiSignalListener);
		systemReponseSignal.AddListener(systemResponseSignalListener);



	}

	override public void OnRemove()
	{
		if(view != null)
			view.uiSignal.RemoveListener (uiSignalListener);
		
		if(systemReponseSignal != null)
			systemReponseSignal.RemoveListener(systemResponseSignalListener);
	}

}
