using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.mediation.impl;
using app;

public class IndicatorMediator : Mediator {

	[Inject]
	public IndicatorView view { get; set; }

	[Inject]
	public UiSignal uiSignal { get; set;}

	[Inject]
	public SystemResponseSignal systemReponseSignal {get; set;}

	[Inject]
	public SystemRequestSignal systemRequestSignal {get; set;}

	[Inject]
	public Utils utils{ get; set; }


	private int myId = -1;

	public void systemResponseSignalListener(string key, Dictionary<string, object> data){

		if (key == SystemResponseEvents.OutputCurrent && view.enableObject.activeSelf) {

			OutputCurrent status = utils.getValueForKey<OutputCurrent> (data, "OutputCurrent");

			if (myId == status.id) {

				float scale = status.value * (1.0f / 0.75f);

				if (scale > 1.0f)
					scale = 1.0f;

				view.indicator.localScale = new Vector3 (1, scale);

				if (view.label != null) {
					int value = Mathf.RoundToInt (scale * 30.0f);
					view.label.text = value + "";
				}
			}
		}

		if (key == SystemResponseEvents.CurrentIndicatorOn) {
			view.enableObject.SetActive (true);
		}

		if (key == SystemResponseEvents.CurrentIndicatorOff) {
			view.enableObject.SetActive (false);
		}
	}

	public void uiSignalListener(string key, string type, Dictionary<string, object> data) {


	}

	// Use this for initialization
	override public void OnRegister(){


		view.init ();

		int? i = utils.getIntFromString (view.gameObject.transform.parent.gameObject.name);
		int? j = utils.getIntFromString (view.gameObject.transform.parent.gameObject.transform.parent.gameObject.transform.parent.gameObject.name);

		if (i != null && j != null) {
			myId = (int)((j * 8) + i);

		}

		view.id = myId;

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
