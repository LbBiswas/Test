using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;


using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.mediation.impl;
using app;

public class EnterPasskeyMediator : Mediator {

	[Inject]
	public EnterPasskeyView view { get; set; }

	[Inject]
	public UiSignal uiSignal { get; set;}

	[Inject]
	public SystemResponseSignal systemReponseSignal {get; set;}

	[Inject]
	public SystemRequestSignal systemRequestSignal {get; set;}

	[Inject]
	public Utils utils{ get; set; }


	//private string lastDevName = null;
	private string lastDevId = null;
	//private string lastDevUuid = null;

	//private void setStrings(string newUuid, string newRaw)
	//{
	//	lastDevUuid = newUuid;
	//	lastDevName = newRaw;
	//}

	private void fillPasskey(uint pass)
    {
		if (pass != 0)
		{
			view.passkeyField.text = pass.ToString("D6");
		}
        else
        {
            view.passkeyField.text = "";
        }
    }

	private bool didGetName = false;
	private bool needsPasskeyInit = true;

	public void systemResponseSignalListener(string key, Dictionary<string, object> data){

        //Debug.Log("secMedSysRes: " + key);

        switch (key) {
			//case SystemResponseEvents.ProData:

			//	string rawName = utils.getValueForKey<string> (data, "RawDevName");

			//	view.currentName.text = "Enter Passkey for:\n" + rawName;

			//	break;
			case SystemResponseEvents.SecurityData:

				bool isOn = utils.getValueForKey<bool>(data, "TurnOn");
				string DevId = utils.getValueForKey<string>(data, "DevId");
				string DevName = utils.getValueForKey<string>(data, "DevName");
				uint myPasskey = utils.getValueForKey<uint>(data, "Passkey");

				Debug.Log("secMedSysRes: SystemResponseEvents.SecurityData: " + DevId + ", " + DevName + ", " + myPasskey);

				if(isOn)
				{
					if (data.ContainsKey("DevId")){
						lastDevId = DevId;
					}
					
					if (data.ContainsKey("DevName")) {
						didGetName = true;
						view.currentName.text = "Enter Passkey for:\n" + DevName;
					}

					if (data.ContainsKey("Passkey") && !view.passkeyField.isFocused && !needsPasskeyInit) {
						needsPasskeyInit = false;
						fillPasskey(myPasskey);
					}
				}

				break;
            case SystemResponseEvents.Passkey:

				uint myPasskey2 = utils.getValueForKey<uint>(data, SystemResponseEvents.Passkey);


				if(view.passkeyField.isFocused)
                {
					needsPasskeyInit = false;
				}
				else if (needsPasskeyInit)
				{
					//Debug.Log("secMedSysRes: SystemResponseEvents.Passkey " + myPasskey2);

					needsPasskeyInit = false;
					fillPasskey(myPasskey2);
				}

                break;
		}
	}

	// Use this for initialization
	void Start()
	{
		if (!didGetName)
		{
			view.currentName.text = "Enter Passkey";
		}
	}

    private void OnEnable()
    {
		needsPasskeyInit = true;
		fillPasskey(0);
	}

    public void uiSignalListener(string key, string type, Dictionary<string, object> data) {

		//		Debug.Log ("optMed: " + key + ", " + type);

		if (type == UiEvents.Click) {

			switch (key) {

			case "Passkey Cancel Button":

				break;
			case "Passkey Apply Button":

					//string newName = view.proNewName.text;

					uint enteredPasskey = 0;

					uint.TryParse(view.passkeyField.text, out enteredPasskey);

                    Debug.Log("passkeyApply: " + lastDevId + ", " + enteredPasskey);

                    systemRequestSignal.Dispatch(SystemRequestEvents.EnteredPasskey, new Dictionary<string, object> {
						{ "DevId", lastDevId },
						{ "enteredPasskey", enteredPasskey }
					});

                    break;
			//case "Change Dev Name Button":
			//	{
			//		//view.proNewName.text = string.Empty;
			//	}
			//	break;

			}
		}

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
