using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.mediation.impl;
using app;

public class AppMediator : Mediator {

	[Inject]
	public AppView view { get; set; }

	[Inject]
	public UiSignal uiSignal { get; set;}

	[Inject]
	public SystemResponseSignal systemReponseSignal {get; set;}

	[Inject]
	public SystemRequestSignal systemRequestSignal {get; set;}

	[Inject]
	public Utils utils{ get; set; }

	public void systemResponseSignalListener(string key, Dictionary<string, object> data){
		this.OnSystemResponseSignalListener (key, data);
	}

	public void uiSignalListener(string key, string type, Dictionary<string, object> data) {
		this.OnUiSignalListener (key, type, data);

	}

	// Use this for initialization
	override public void OnRegister(){


		view.Init ();

		systemReponseSignal.AddListener(systemResponseSignalListener);
		uiSignal.AddListener (uiSignalListener);

		this.OnMediatorRegister ();
	}

	override public void OnRemove()
	{
		this.OnMediatorRemove ();

		if(systemReponseSignal != null)
			systemReponseSignal.RemoveListener(systemResponseSignalListener);

		if (uiSignal != null)
			uiSignal.RemoveListener (uiSignalListener);


	}

	protected virtual void OnSystemResponseSignalListener(string key, Dictionary<string, object> data)
	{

	}

	protected virtual void OnUiSignalListener(string key, string type, Dictionary<string, object> data) {


	}

	protected virtual void OnMediatorRegister()
	{


	}

	protected virtual void OnMediatorRemove()
	{


	}

}
