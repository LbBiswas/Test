using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.mediation.impl;
using app;

public class StatusMediator : Mediator {

	[Inject]
	public StatusView view { get; set; }

	[Inject]
	public UiSignal uiSignal { get; set;}

	[Inject]
	public SystemResponseSignal systemReponseSignal {get; set;}

	[Inject]
	public SystemRequestSignal systemRequestSignal {get; set;}

	[Inject]
	public Utils utils{ get; set; }


	private int myId = -1;
	private bool isCelcius = false;

	private string getTempString(float temp)
	{
		if(isCelcius)
		{
			return temp.ToString("F0") + " C";
		}
		else
		{
			return ((temp * (9.0f/5.0f)) + 32.0f).ToString("F0") + " F";
		}

	}

	public void systemResponseSignalListener(string key, Dictionary<string, object> data){

		if (key == SystemResponseEvents.SystemData && myId != -1)

		{

			bool[] sourceTemps = utils.getValueForKey<bool[]> (data, "sourceTemps");
			isCelcius = sourceTemps [myId];

		}

		if (key == SystemResponseEvents.StatusUpdate) {
			StatusUpdate update = utils.getValueForKey<StatusUpdate> (data, "StatusUpdate");

			//Debug.Log ("Status Update x...");

			//Debug.Log (myId);
			//Debug.Log (update);


			if (update != null && myId == (update.id & 0x03)) {

				//Debug.Log ("Status Update y...");
				if (view.isTemperature) {
					if(view.text != null)
						view.text.text = getTempString (update.temp);
				} else {
					float voltage = (((float)update.voltage * 5f / 255f) * 3f) + 0.7f;

					if ((update.id & 0xFC) == 0x80) {
						voltage = voltage * 2;
					}

					if(view.text != null)
						view.text.text = voltage.ToString("F1") + " V";

				}

			}
		}

	}

	public void uiSignalListener(string key, string type, Dictionary<string, object> data) {


	}

	// Use this for initialization
	override public void OnRegister(){


		view.init ();

		int? j = utils.getIntFromString (view.gameObject.transform.parent.gameObject.transform.parent.gameObject.transform.parent.gameObject.name);

		//Debug.Log ("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
		//Debug.Log (j);


		if (j != null) {
			myId = (int)j;

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
