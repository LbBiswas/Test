using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.mediation.impl;
using app;

using System;



public class RecordingCanvasMediator : Mediator {

	[Inject]
	public RecordingCanvasView view { get; set; }

	[Inject]
	public UiSignal uiSignal { get; set;}

	[Inject]
	public SystemResponseSignal systemReponseSignal {get; set;}

	[Inject]
	public SystemRequestSignal systemRequestSignal {get; set;}

	[Inject]
	public Utils utils{ get; set; }



	public void systemResponseSignalListener(string key, Dictionary<string, object> data){
		

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
