using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;


using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.mediation.impl;
using app;

public class ProOptionsMediator : Mediator {

	[Inject]
	public ProOptionsView view { get; set; }

	[Inject]
	public UiSignal uiSignal { get; set;}

	[Inject]
	public SystemResponseSignal systemReponseSignal {get; set;}

	[Inject]
	public SystemRequestSignal systemRequestSignal {get; set;}

	[Inject]
	public Utils utils{ get; set; }


//	private bool isPro = false;

	private bool isLastFromApp = false;

//	private ProSeriesStatus ProStatus;
//	private ProSwitchStatus[] ProSwitches;

//	public void setPro(bool newPro)
//	{
//		isPro = newPro;
//	}

	public void systemResponseSignalListener(string key, Dictionary<string, object> data){

//		Debug.Log ("proMedSysRes: " + key);

		switch (key) {
			case SystemResponseEvents.ProData:

				ProSeriesStatus ProStatus = utils.getValueForKey<ProSeriesStatus> (data, "ProStatus");
	//			ProSwitchStatus[] ProSwitches = utils.getValueForKey<ProSwitchStatus[]> (data, "ProSwitches");
				string ConnMessage = utils.getValueForKey<string> (data, "ConnMess");
	//			bool isDeepSleepComp = utils.getValueForKey<bool> (data, "isDeepSleepComp");
				int Compat = utils.getValueForKey<int> (data, "Compatibility");
				string rawName = utils.getValueForKey<string> (data, "RawDevName");
				bool isBantamComp = utils.getValueForKey<bool>(data, "isBantamComp");

				//			setPro (ProStatus.isPro);

				view.proConnectionMessage.text = ConnMessage;

				view.proEnableSync.isOn = ProStatus.isAutoSync;
				view.proSyncFromDev.isOn = !ProStatus.isSyncFromApp;
				view.proSyncFromApp.isOn = ProStatus.isSyncFromApp;

				isLastFromApp = ProStatus.isSyncFromApp;

	//			view.proDisableDeepSleep.isOn = false;
	//			view.proDisableDeepSleep.GetComponent

				Color col;

				if (Compat == ProInfo.IS_COMPATIBLE) {//isDeepSleepComp) { // is connected to pro device
					view.proDisableDeepSleep.isOn = ProStatus.isDisableDeepSleep;
					view.proDisableDeepSleep.interactable = true;


					view.proDisableSyncButton.SetActive (false);

					view.proCompatibleMessage.text = "PRO";

					col = new Vector4 (0, 1, 0, 1);
					view.proCompatibleMessage.color = col;

					if (isBantamComp)
					{
						view.proEnableInputLink.isOn = ProStatus.isInputLinking;
						view.proEnableInputLink.interactable = true;
					}
					else
					{
						view.proEnableInputLink.isOn = false;
						view.proEnableInputLink.interactable = false;
					}

				} else {
					view.proDisableDeepSleep.isOn = false;
					view.proDisableDeepSleep.interactable = false;

					view.proEnableInputLink.isOn = false;
					view.proEnableInputLink.interactable = false;

					view.proDisableSyncButton.SetActive (true);

					view.proCompatibleMessage.text = "non-pro";

					col = new Vector4 (1, 0, 0, 1);
					view.proCompatibleMessage.color = col;
				}

				if (Compat == ProInfo.NOT_CONNECTED) {
					view.proCompatibleMessage.text = "";
					view.proDisableNameButton.SetActive (true);
				} else {

					if (rawName.StartsWith ("OTA")) {
						view.proDisableNameButton.SetActive (true);
					} else {
						view.proDisableNameButton.SetActive (false);
					}

					
				}




				break;
		}
	}

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {

		if (isLastFromApp) {

			if (!view.proSyncFromApp.isOn) {
				isLastFromApp = false;
				view.proSyncFromDev.isOn = true;
			} else if (view.proSyncFromDev.isOn) {
				view.proSyncFromApp.isOn = false;
				isLastFromApp = false;
			}

		} else {

			if (view.proSyncFromApp.isOn) {
				isLastFromApp = true;
				view.proSyncFromDev.isOn = false;
			} else if (!view.proSyncFromDev.isOn) {
				view.proSyncFromApp.isOn = true;
				isLastFromApp = true;
			}

		}

	}

	public void uiSignalListener(string key, string type, Dictionary<string, object> data) {

//		Debug.Log ("optMed: " + key + ", " + type);

		if (type == UiEvents.Click) {

			switch (key) {

			case "Pro Panel Cancel":
			case "Pro OK Button":

				systemRequestSignal.Dispatch(SystemRequestEvents.UpdateProSettings, new Dictionary<string, object>{
					{"EnableSync", view.proEnableSync.isOn},
					{"SyncFromApp", view.proSyncFromApp.isOn},
					{"DisableDeepSleep", view.proDisableDeepSleep.isOn},
					{"EnableInputLink", view.proEnableInputLink.isOn},
					{"SyncNow", false}
				});

//				Debug.Log ("optMed ok");
				break;
			case "Sync Now Button":

				systemRequestSignal.Dispatch(SystemRequestEvents.UpdateProSettings, new Dictionary<string, object>{
					{"EnableSync", view.proEnableSync.isOn},
					{"SyncFromApp", view.proSyncFromApp.isOn},
					{"DisableDeepSleep", view.proDisableDeepSleep.isOn},
					{"EnableInputLink", view.proEnableInputLink.isOn},
					{"SyncNow", true}

				});

				break;
			case "Reset App Settings Button":

//				Debug.Log ("optMed reset");

				systemRequestSignal.Dispatch(SystemRequestEvents.ResetAppSettings, null);

				break;
//			case "Change Dev Name Button":
//
//
//
//			break;
			}
		}



//		if (key.Contains ("Switch HD Source ")  && type == UiEvents.MouseDown) {
//
//			for (int i = 0; i < view.sources.Length; i++) {
//				view.sources[i].color = new Color (1.0f, 0.0f, 0.0f, 0.0f);
//			}
//
//			switch (key) {
//			case "Switch HD Source 1":
//				view.sources[0].color = new Color (1.0f, 0.0f, 0.0f, 1.0f);
//				break;
//			case "Switch HD Source 2":
//				view.sources[1].color = new Color (1.0f, 0.0f, 0.0f, 1.0f);
//				break;
//			case "Switch HD Source 3":
//				view.sources[2].color = new Color (1.0f, 0.0f, 0.0f, 1.0f);
//				break;
//			case "Switch HD Source 4":
//				view.sources[3].color = new Color (1.0f, 0.0f, 0.0f, 1.0f);
//				break;
//			}
//		}


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
