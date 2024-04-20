using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;


using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.mediation.impl;
using app;

public class ProNameChangeMediator : Mediator {

	[Inject]
	public ProNameChangeView view { get; set; }

	[Inject]
	public UiSignal uiSignal { get; set;}

	[Inject]
	public SystemResponseSignal systemReponseSignal {get; set;}

	[Inject]
	public SystemRequestSignal systemRequestSignal {get; set;}

	[Inject]
	public Utils utils{ get; set; }


//	private bool isPro = false;
//
//	public void setPro(bool newPro)
//	{
//		isPro = newPro;
//	}

	private string lastRawString = null;
	private string lastDevUuid = null;

	private void setStrings(string newUuid, string newRaw)
	{
		lastDevUuid = newUuid;
		lastRawString = newRaw;
	}


	public void systemResponseSignalListener(string key, Dictionary<string, object> data){

		//		Debug.Log ("proMedSysRes: " + key);

		switch (key) {
		case SystemResponseEvents.ProData:

			//			SwitchStatus[] switches = utils.getValueForKey<SwitchStatus[]> (data, "switches");

//			ProSeriesStatus ProStatus = utils.getValueForKey<ProSeriesStatus> (data, "ProStatus");
			//			ProSwitchStatus[] ProSwitches = utils.getValueForKey<ProSwitchStatus[]> (data, "ProSwitches");
//			string ConnMessage = utils.getValueForKey<string> (data, "ConnMess");
//			int Compat = utils.getValueForKey<int> (data, "Compatibility");

			string rawName = utils.getValueForKey<string> (data, "RawDevName");
			string niceName = utils.getValueForKey<string> (data, "NiceDevName");
			string devUuid = utils.getValueForKey<string> (data, "DeviceUuid");



//			if (devUuid != null) {
				setStrings (devUuid, rawName);
//			}

			view.proCurrentName.text = "Current Device Name: " + niceName;

			break;
		}
	}

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {

	}

	public void uiSignalListener(string key, string type, Dictionary<string, object> data) {

		//		Debug.Log ("optMed: " + key + ", " + type);

		if (type == UiEvents.Click) {

			switch (key) {

			case "Pro Name Cancel Button":

				break;
			case "Pro Name Default Button":

//				view.proNewName.text = getDefaultString (lastRawString);
				view.proNewName.text = AppStartCommand.getDeviceString (lastRawString, null);

				break;
			case "Pro Name Apply Button":

				string newName = view.proNewName.text;

				systemRequestSignal.Dispatch (SystemRequestEvents.UpdateProName, new Dictionary<string, object> {
					{ "DevUuid", lastDevUuid },
					{ "RawName", lastRawString },
					{ "NewName", newName },

				});

				break;
			case "Change Dev Name Button":
				{
					view.proNewName.text = string.Empty;
				}
				break;

			}
		}

	}

	// Use this for initialization
	override public void OnRegister(){

		view.init ();

		systemReponseSignal.AddListener(systemResponseSignalListener);
		uiSignal.AddListener (uiSignalListener);



		//		for (int i = 0; i < view.sources.Length; i++) {
		//			view.sources[i].color = new Color (1.0f, 0.0f, 0.0f, 0.0f);
		//		}


	}

	override public void OnRemove()
	{

		if(systemReponseSignal != null)
			systemReponseSignal.RemoveListener(systemResponseSignalListener);

		if (uiSignal != null)
			uiSignal.RemoveListener (uiSignalListener);
	}
}
