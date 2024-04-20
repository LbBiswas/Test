using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.mediation.impl;
using app;

public class ColorChangeMediator : Mediator {

	[Inject]
	public ColorChangeView view { get; set; }

	[Inject]
	public UiSignal uiSignal { get; set;}

	[Inject]
	public SystemResponseSignal systemReponseSignal {get; set;}

	[Inject]
	public SystemRequestSignal systemRequestSignal {get; set;}

	[Inject]
	public Utils utils{ get; set; }

	public void systemResponseSignalListener(string key, Dictionary<string, object> data){

		if(!view.isEnabled)
			return;

		switch (key) {
		case SystemResponseEvents.UpdateColor:
			Color color = utils.getValueForKey<Color> (data, "Color");

			if (view == null || color == null) {
				Debug.Log ("Null: " + gameObject.name);
			}
			if (color != null) {
				view.indicator.color = color;
			}
			break;

		}
	}


	public void uiSignalListener(string key, string type, Dictionary<string, object> data) {

	}

	// Use this for initialization
	override public void OnRegister(){


		view.init ();

		systemReponseSignal.AddListener(systemResponseSignalListener);
		uiSignal.AddListener (uiSignalListener);


	}

	override public void OnRemove()
	{

		if(systemReponseSignal != null)
			systemReponseSignal.RemoveListener(systemResponseSignalListener);

		if (uiSignal != null)
			uiSignal.RemoveListener (uiSignalListener);
	}

}
