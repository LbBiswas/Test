using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
//using UnityEngine.Purchasing;


using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.mediation.impl;
using app;

public class ProPurchaseMediator : Mediator {

	[Inject]
	public ProPurchaseView view { get; set; }

	[Inject]
	public UiSignal uiSignal { get; set;}

	[Inject]
	public SystemResponseSignal systemReponseSignal {get; set;}

	[Inject]
	public SystemRequestSignal systemRequestSignal {get; set;}

	[Inject]
	public Utils utils{ get; set; }


//	private bool isPro = false;

//	public void setPro(bool newPro)
//	{
//		isPro = newPro;
//	}



	public void systemResponseSignalListener(string key, Dictionary<string, object> data){

		//		Debug.Log ("proMedSysRes: " + key);

		switch (key) {
		case SystemResponseEvents.ProData:

			//			SwitchStatus[] switches = utils.getValueForKey<SwitchStatus[]> (data, "switches");

			ProSeriesStatus ProStatus = utils.getValueForKey<ProSeriesStatus> (data, "ProStatus");
			string ConnMessage = utils.getValueForKey<string> (data, "ConnMess");
			int Compat = utils.getValueForKey<int> (data, "Compatibility");



//			setPro (ProStatus.isPro);

			Debug.Log ("SystemResponseEvents.ProData: systemRequestSignal - " + systemRequestSignal);

			//			if(isPro)

			view.proConnectionMessage.text = ConnMessage;

			Color col;

//			view.proCompatibleMessage.text = "";

						switch(Compat){
						case ProInfo.NOT_CONNECTED:
							
							view.proCompatibleMessage.text = "-";
			
							col = new Vector4 (1, 1, 1, 1);
							view.proCompatibleMessage.color = col;
							break;
						case ProInfo.NEED_UPDATE:
							view.proCompatibleMessage.text = "Upgrade Firmware";
			
							col = new Vector4 (1, 0.5f, 0, 1);
							view.proCompatibleMessage.color = col;
							break;
						case ProInfo.IS_COMPATIBLE:
							view.proCompatibleMessage.text = "Device Pro-Compatible";
			
							col = new Vector4 (0, 1, 0, 1);
							view.proCompatibleMessage.color = col;
							break;
						case ProInfo.NOT_COMPATIBLE:
							view.proCompatibleMessage.text = "Device NOT Pro-Compatible";
			
							col = new Vector4 (1, 0, 0, 1);
							view.proCompatibleMessage.color = col;
							break;
						default:
							view.proCompatibleMessage.text = "";
							break;
						}

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

//		if (type == UiEvents.Click) {
//
//			switch (key) {
//
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
