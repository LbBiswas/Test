using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.mediation.impl;
using app;

using System;

using TMPro;
using UnityEngine.Networking;

//#if PLATFORM_ANDROID
//using UnityEngine.Android;
//#endif

public static class MyListExtensions
{
	public static double Mean(this List<double> values)
	{
		return values.Count == 0 ? 0 : values.Mean(0, values.Count);
	}

	public static double Mean(this List<double> values, int start, int end)
	{
		double s = 0;

		for (int i = start; i < end; i++)
		{
			s += values[i];
		}

		return s / (end - start);
	}

	public static double Variance(this List<double> values)
	{
		return values.Variance(values.Mean(), 0, values.Count);
	}

	public static double Variance(this List<double> values, double mean)
	{
		return values.Variance(mean, 0, values.Count);
	}

	public static double Variance(this List<double> values, double mean, int start, int end)
	{
		double variance = 0;

		for (int i = start; i < end; i++)
		{
			variance += System.Math.Pow((values[i] - mean), 2);
		}

		int n = end - start;
		if (start > 0) n -= 1;

		return variance / (n);
	}

	public static double StandardDeviation(this List<double> values)
	{
		return values.Count == 0 ? 0 : values.StandardDeviation(0, values.Count);
	}

	public static double StandardDeviation(this List<double> values, int start, int end)
	{
		double mean = values.Mean(start, end);
		double variance = values.Variance(mean, start, end);

		return System.Math.Sqrt(variance);
	}
}


public static class TransformDeepChildExtension
{
	//Breadth-first search
	public static Transform FindDeepChild(this Transform aParent, string aName)
	{
		var result = aParent.Find(aName);
		if (result != null)
			return result;
		foreach(Transform child in aParent)
		{
			result = child.FindDeepChild(aName);
			if (result != null)
				return result;
		}
		return null;
	}


	/*
     //Depth-first search
	public static Transform FindDeepChild(this Transform aParent, string aName)
	{
		foreach(Transform child in aParent)
		{
			if(child.name == aName )
				return child;
			var result = child.FindDeepChild(aName);
			if (result != null)
				return result;
		}
		return null;
	}
	*/
}

public class GlobalMediator : Mediator {

	[Inject]
	public GlobalView view { get; set; }

	[Inject]
	public UiSignal uiSignal { get; set;}

	[Inject]
	public SystemResponseSignal systemReponseSignal {get; set;}

	[Inject]
	public SystemRequestSignal systemRequestSignal {get; set;}

	[Inject]
	public Utils utils{ get; set; }

	private int switchSetupId = -1;
	private int currentSourceId = 0;
	private bool isSetupOn = false;
	private bool isOffRoad = false;

	private Dictionary<string, object> lastData;

	private SwitchStatus lastSwitchStatus = null;

	private Color lastColor = Color.white;
	private Color newColor = Color.white;

	private bool isUpdateEnabled = false;

	private int debugCount = 0;

	private bool needsPosSave = true;
	private float swRow0, swRow1, swCol0, swCol1, swCol2, swCol3;

	private bool isPro = false;

	private string lastPermissionsString = "";
	private int lastPermissionsResponse = -1;
	private bool lastWasPermissions = false;
	private bool lastWasOtaInfoOnly = false;

	private void updateView(){
		if (lastSwitchStatus != null) {
			view.dimmers[lastSwitchStatus.id / 8].gameObject.SetActive (lastSwitchStatus.isDimmable);

			view.flashStrobeButton.gameObject.SetActive ((lastSwitchStatus.canFlash || lastSwitchStatus.canStrobe) && isOffRoad);

			if (lastSwitchStatus.canFlash) {
				view.flashStrobeButton.GetComponentInChildren<ButtonView>().text = "Flash";
			} else if (lastSwitchStatus.canStrobe) {
				view.flashStrobeButton.GetComponentInChildren<ButtonView>().text = "Strobe";
			}

			if (lastSwitchStatus.isDimmable) {
				view.dimmers [lastSwitchStatus.id / 8].value = lastSwitchStatus.value;
				view.percents[lastSwitchStatus.id / 8].text = ((int)(lastSwitchStatus.value * 100.0f)) + "%";
			}
		}


	}

	IEnumerator ChangeColor(){
		float stopDragTime = Time.realtimeSinceStartup;
		float diffTime = Time.realtimeSinceStartup - stopDragTime;
		//Debug.Log ("diffTime " + diffTime);
		float totalTime = 0.3f;

		Vector3 lastColorLerp = new Vector3 (lastColor.r, lastColor.g, lastColor.b);
		Vector3 newColorLerp = new Vector3 (newColor.r, newColor.g, newColor.b);

		while(diffTime < totalTime)
		{
			Vector3 tempColor = Vector3.Lerp (lastColorLerp, newColorLerp, diffTime/totalTime);

			view.sourceBackground.color = new Color (tempColor.x, tempColor.y, tempColor.z);

			yield return new WaitForSeconds(0.015f);
			diffTime = Time.realtimeSinceStartup - stopDragTime;
			//Debug.Log ("diffTime " + diffTime);
		}

		List<double> data = new List<double>{newColor.r * 100.0F, newColor.g * 100.0F, newColor.b * 100.0F };

		if (data.StandardDeviation () < 8) {
			systemReponseSignal.Dispatch(SystemResponseEvents.UpdateColor, new Dictionary<string, object>{{"Color", Color.red}});
		} else {
			systemReponseSignal.Dispatch(SystemResponseEvents.UpdateColor, new Dictionary<string, object>{{"Color", newColor}});
		}
			
		view.sourceBackground.color = newColor;

	}

	IEnumerator frameDelay(){
		isUpdateEnabled = false;
		yield return null;
		systemRequestSignal.Dispatch (SystemRequestEvents.SystemData, null);
		yield return null;
		isUpdateEnabled = true;
		yield break;
	}

	private Dictionary<string, string> deviceList;

	IEnumerator displayList(){

		RectTransform content = view.deviceSelectonPanel.transform.FindDeepChild ("Content").GetComponent<RectTransform>();

		foreach (Transform child in content.transform) {
			GameObject.Destroy(child.gameObject);
		}

		float offset = 0;

		foreach (KeyValuePair<string, string> kvp in deviceList) {

			GameObject newLineItem = Instantiate (view.deviceSelectonItem, content.transform);

			RectTransform newRectTransform = newLineItem.GetComponent<RectTransform> ();
			TextMeshProUGUI textObj = newLineItem.GetComponentInChildren<TextMeshProUGUI> ();
			DeviceItem deviceItem = newLineItem.GetComponent<DeviceItem> ();

			deviceItem.deviceInfo = new KeyValuePair<string, string> (kvp.Key, kvp.Value);

			textObj.font = view.deviceListFont;
			textObj.text = AppStartCommand.getDeviceString (kvp.Key, kvp.Value);


			yield return null;

			newRectTransform.localScale = new Vector3 (1.0f, 1.0f, 1.0f);
			newRectTransform.sizeDelta = new Vector2( content.rect.xMax - content.rect.xMin, 45f);
			newRectTransform.localPosition = new Vector3 (0f, -offset, 0f);
			Debug.Log (newRectTransform.localPosition);

			offset += 40.0f;




		}

	}

	private void turnOnProLogos(bool turnON)
	{
		return;
		//view.proLogo[0].SetActive (turnON);
		//view.proLogo[1].SetActive (turnON);
		//view.proLogo[2].SetActive (turnON);
		//view.proLogo[3].SetActive (turnON);
	}

	private int getStrobeVal(float val)
	{
//		int norm = (int)(val * val * 228f + val * 26f) + 1;
		int norm = (int)(val * val * 254 + 1);

		if (norm < 1) {
			norm = 1;
		}else if(norm > 255){
			norm = 255;
		}

		return norm;
	}

	private float setSliderPos(int val)
	{
//		float valf = (float)( Math.Sqrt((double)(val-1) * 228 + 169) - 13)/228; 
		float valf = (float) Math.Sqrt((double)(val-1) / 254); 

		if (valf < 0.0f) {
			valf = 0.0f;
		}else if(valf > 1.0f){
			valf = 1.0f;
		}

		return valf;
	}

	public void systemResponseSignalListener(string key, Dictionary<string, object> data){
		switch (key) {
		case SystemResponseEvents.DebugOff:
			view.debugText.text = "";
			break;

		case SystemResponseEvents.DebugOn:
			view.debugText.text = "Remote Debug Enabled...";
			break;

		case SystemResponseEvents.Passkey:
			uint passkey = utils.getValueForKey<uint> (data, SystemResponseEvents.Passkey);

			if (passkey != 0) {
				view.passkey.text = passkey.ToString ("D6");
			} else {
				view.passkey.text = "";
			}

			break;
		case SystemResponseEvents.VoicePhrase:

			string phrase = utils.getValueForKey<string> (data, SystemResponseEvents.VoicePhrase);

			if (phrase.Length > 10) {
				onVoicePhraseChange (phrase);
			}

			break;
		case SystemResponseEvents.SetupOn:
			isSetupOn = true;

				bool isRestoreText = utils.getValueForKey<bool>(data, "isRestoreText");

				if(isRestoreText)
                {
					view.otaButtonText1.GetComponent<TextMeshProUGUI>().text = "Restore";
					view.otaButtonText2.GetComponent<TextMeshProUGUI>().text = "Restore";

					//view.otaOkButtonText1.GetComponent<TextMeshProUGUI>().text = "Restore";
					//view.otaOkButtonText2.GetComponent<TextMeshProUGUI>().text = "Restore";
				}
				else
				{
					view.otaButtonText1.GetComponent<TextMeshProUGUI>().text = "Upgrade";
					view.otaButtonText2.GetComponent<TextMeshProUGUI>().text = "Upgrade";

					//view.otaOkButtonText1.GetComponent<TextMeshProUGUI>().text = "OK";
					//view.otaOkButtonText2.GetComponent<TextMeshProUGUI>().text = "OK";
				}

				turnOnProLogos (false);

			if (isPro) {
				view.versionNum.text = "v " + Application.version + " PRO";
			} else {
				view.versionNum.text = "v " + Application.version;
			}


			break;
		case SystemResponseEvents.SetupOff:
			isSetupOn = false;

			if (isPro) {
				turnOnProLogos (true);
			}

			updateView();

			break;
		case SystemResponseEvents.ShowSwitchSetup:
			switchSetupId = utils.getValueForKey<int> (data, "Id");

			SwitchMediator mediator = view.setupSwitchView.gameObject.transform.GetComponent<SwitchMediator> ();

			mediator.setId (switchSetupId);

			view.sourceSetupPanel.SetActive (false);
			view.switchSetupPanel.SetActive (true);

				enableProBantam(switchSetupId / 8);

			StartCoroutine ("frameDelay");

			break;
		case SystemResponseEvents.ShowSourceSetup:

			view.switchSetupPanel.SetActive (false);
			view.sourceSetupPanel.SetActive (true);
			systemRequestSignal.Dispatch (SystemRequestEvents.SystemData, null);

			break;
		case SystemResponseEvents.ShowProPanel:

			view.switchSetupPanel.SetActive (false);
			view.sourceSetupPanel.SetActive (false);

			if (isPro) {	// isGlobalProSeries
				view.proOptionsPanel.SetActive (true);
			} else {
				view.proPurchasePanel.SetActive (true);
			}

			systemRequestSignal.Dispatch (SystemRequestEvents.SystemData, null);

			break;
		case SystemResponseEvents.SourceIdUpdated:
			
			int tempId = utils.getValueForKey<int> (data, "Id");
			String type = utils.getValueForKey<string> (data, "Type");

			if (type != null && data.ContainsKey("Id")) {	// tempId != null) {

				if (type == UiEvents.BeginDrag) {
					for (int i = 0; i < view.sources.Length; i++) {
						CanvasGroup group = view.sources [i].transform.GetComponent<CanvasGroup> () as CanvasGroup;
						group.alpha = 1.0f;

					}
				}

				if (type == UiEvents.EndDrag) {
					for (int i = 0; i < view.sources.Length; i++) {
						CanvasGroup group = view.sources [i].transform.GetComponent<CanvasGroup> () as CanvasGroup;
						if (tempId == i) {
							group.alpha = 1.0f;
						} else {
							group.alpha = 0.0f;
						}

					}
				}
			}

			//if (tempId != null && lastData != null) {
			if (data.ContainsKey("Id") && lastData != null) {

				currentSourceId = tempId;

				Vector3[] sourceColors = utils.getValueForKey<Vector3[]> (lastData, "sourceColors");

				if (sourceColors != null && sourceColors.Length == 4) {
					lastColor = newColor;
					newColor = new Color (sourceColors [currentSourceId].x, sourceColors [currentSourceId].y, sourceColors [currentSourceId].z);

					StopCoroutine ("ChangeColor");
					StartCoroutine ("ChangeColor");
				}
					
			}

		

			break;

		case SystemResponseEvents.SwitchUpdate:

			Debug.Log ("Global SwitchUpdate");
			
			SwitchStatus tempStatus = utils.getValueForKey<SwitchStatus> (data, "SwitchStatus");

			if (tempStatus != null) {
				lastSwitchStatus = tempStatus;

				SwitchStatus[] switches = utils.getValueForKey<SwitchStatus[]> (lastData, "switches");

				switches [tempStatus.id] = tempStatus;

				lastData ["switches"] = switches;

//				Debug.Log (switches [tempStatus.id].isFlashing);
			}

			updateView ();

			break;

		case SystemResponseEvents.DeviceList:

			deviceList = utils.getValueForKey<Dictionary<string, string>> (data, SystemResponseEvents.DeviceList);

			if (deviceList != null && deviceList.Count > 0) {
				view.deviceSelectonPanel.SetActive (true);

				StartCoroutine ("displayList");

			}


			break;
		case SystemResponseEvents.SystemData:

			{
				SwitchStatus[] switches = utils.getValueForKey<SwitchStatus[]> (data, "switches");

				string[] sourceNames = utils.getValueForKey<string[]> (data, "sourceNames");
				Vector3[] sourceColors = utils.getValueForKey<Vector3[]> (data, "sourceColors");
				bool[] sourceTemps = utils.getValueForKey<bool[]> (data, "sourceTemps");

				lastData = data;

//				Debug.Log ("D2");

				if (switchSetupId > -1 && switches != null && switches.Length == 32) {


					SwitchStatus thisSwitch = switches [switchSetupId];

					view.isDimmable.isOn = thisSwitch.isDimmable;
					view.isMomentary.isOn = thisSwitch.isMomentary;
					view.canFlash.isOn = thisSwitch.canFlash;
					view.canStrobe.isOn = thisSwitch.canStrobe;

					view.red.value = thisSwitch.red;
					view.green.value = thisSwitch.green;
					view.blue.value = thisSwitch.blue;

					view.label1.text = thisSwitch.label1;
					view.label2.text = thisSwitch.label2;
					view.label3.text = thisSwitch.label3;
					//view.buttonLabel.text = thisSwitch.label;

//					Debug.Log ("Xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx ");
//					Debug.Log (thisSwitch.isLegend);

					view.isLegend.isOn = thisSwitch.isLegend;

					if (isPro) {

						view.StrobeOnSlider.value = setSliderPos (thisSwitch.proStrobeOn);
						view.StrobeOffSlider.value = setSliderPos (thisSwitch.proStrobeOff);

						float onVal = thisSwitch.proStrobeOn * 0.02f;
						float offVal = thisSwitch.proStrobeOff * 0.02f;

						view.proStrobeReadout.text = ">> ON/OFF " + onVal.ToString ("n2") + "s/" + offVal.ToString ("n2") + "s";

						view.isInputEnabled.isOn = thisSwitch.proIsInputEnabled;
						view.isInputLockout.isOn = thisSwitch.proIsInputLockout;
						view.isInputLockInvert.isOn = thisSwitch.proIsInputLockInvert;
					}
					//view.backLight.color = new Color (view.red.value, view.green.value, view.blue.value);

					if (isPro && view.isAutoOn.interactable) {


						view.isAutoOn.isOn = thisSwitch.proIsAutoOn;
						view.isIgnCtrl.isOn = thisSwitch.proIsIgnCtrl;
						view.isLockout.isOn = thisSwitch.proIsLockout;
						view.switchTimer.text = thisSwitch.proOnTimer.ToString ();

						view.isInputLatching.isOn = thisSwitch.proIsInputLatch;
						view.isCurrentRestart.isOn = thisSwitch.proIsCurrentRestart;
						view.currentLimit.text = thisSwitch.proCurrentLimit.ToString ();
					
					}

				}

				if (sourceNames != null && sourceNames.Length == 4) {

					for (int i = 0; i < 4; i++) {
						view.sourceTitles [i].text = sourceNames [i];
					}
				}

				view.sourceTempBackground.color = new Color (sourceColors [currentSourceId].x, sourceColors [currentSourceId].y, sourceColors [currentSourceId].z);
				view.sourceBackground.color = new Color (sourceColors [currentSourceId].x, sourceColors [currentSourceId].y, sourceColors [currentSourceId].z);
				view.sourceRed.value = sourceColors [currentSourceId].x;
				view.sourceGreen.value = sourceColors [currentSourceId].y;
				view.sourceBlue.value = sourceColors [currentSourceId].z;
				view.sourceLabel.text = sourceNames [currentSourceId];
				view.isCelcius.isOn = sourceTemps [currentSourceId];


			}
			break;
		case SystemResponseEvents.SelectOtaDevice:

			view.otaDeviceSelectPanel.SetActive (true);

			break; 
		case SystemResponseEvents.SelectOtaUpgrade:


			string UpgradeMessage = utils.getValueForKey<string> (data, "otaUpgradeMessage");
				//bool canOn = utils.getValueForKey<bool> (data, "CancelOn");
				//bool okQuit = utils.getValueForKey<bool> (data, "QuitOnOk");

				bool isRestore = false;

				if (data.ContainsKey("isRestore"))
				{
					isRestore = utils.getValueForKey<bool>(data, "isRestore");
				}

				if (data.ContainsKey("isInfoOnly")) {
					lastWasOtaInfoOnly = utils.getValueForKey<bool>(data, "isInfoOnly");
				} else {
					lastWasOtaInfoOnly = false;
				}

				view.otaCancelButton.SetActive(true);// canOn);

                view.otaUpgradeMessage.text = UpgradeMessage;

				if(isRestore)
				{
					view.otaUpgradeHeader.text = "WARNING";// Firmware Restore";

					view.otaOkButtonText1.GetComponent<TextMeshProUGUI>().text = "Restore";
					view.otaOkButtonText2.GetComponent<TextMeshProUGUI>().text = "Restore";
				}
				else
                {
					view.otaUpgradeHeader.text = "Firmware Upload"; //"Firmware Upgrade";

					view.otaOkButtonText1.GetComponent<TextMeshProUGUI>().text = "OK";
					view.otaOkButtonText2.GetComponent<TextMeshProUGUI>().text = "OK";
				}
			

			//if (okQuit) {
			//	view.otaUpgradeHeader.text = "Bluetooth Error";
			//}

			view.otaUpgradePanel.SetActive (true);

				lastWasPermissions = false;


				break;
			case SystemResponseEvents.ShowBleErrPanel:

				string errMessage = utils.getValueForKey<string>(data, "bleErrMessage");

				view.bleErrMessage.text = errMessage;

				view.bleErrPanel.SetActive(true);

				break;
			case SystemResponseEvents.DisplayPermissionMessage:

				string MessageString = utils.getValueForKey<string>(data, "MessageString");
				string myPerString = utils.getValueForKey<string>(data, "PermissionString");
				int myPerResp = utils.getValueForKey<int>(data, "PermissionResponse");

				

				if (!String.IsNullOrEmpty(MessageString))
				{
					Debug.Log("SystemResponseEvents.DisplayPermissionMessage: " + myPerString + ", " + myPerResp);

					lastPermissionsString = myPerString;
					lastPermissionsResponse = myPerResp;
					lastWasPermissions = true;

					view.otaUpgradeMessage.text = MessageString;

					view.otaUpgradeHeader.text = "APP PERMISSIONS";

					view.otaOkButtonText1.GetComponent<TextMeshProUGUI>().text = "OK";
					view.otaOkButtonText2.GetComponent<TextMeshProUGUI>().text = "OK";

					view.otaCancelButton.SetActive(false);

					view.otaUpgradePanel.SetActive(true);
				}
                else
                {
					Debug.Log("SystemResponseEvents.DisplayPermissionMessage null....");
				}

				break;
			case SystemResponseEvents.ProData:
			
//			SwitchStatus[] switches = utils.getValueForKey<SwitchStatus[]> (data, "switches");

			ProSeriesStatus ProStatus = utils.getValueForKey<ProSeriesStatus> (data, "ProStatus");
//			ProSwitchStatus[] proSwitches = utils.getValueForKey<ProSwitchStatus[]> (data, "ProSwitches");
			string ConnMessage = utils.getValueForKey<string> (data, "ConnMess");

			//bool isBantamComp = utils.getValueForKey<bool> (data, "isBantamComp");
			int isProComp = utils.getValueForKey<int> (data, "Compatibility");
				//byte connAddress = utils.getValueForKey<int>(data, "ConnAddress");

				isBantamCompatible = utils.getValueForKey<bool>(data, "isBantamComp");
				connectedDevAddress = utils.getValueForKey<byte>(data, "ConnAddress");


				int indexSt = ConnMessage.IndexOf (":") + 2;
			int indexEnd = ConnMessage.IndexOf ("\n");

			if (indexSt < 0 || indexEnd < 0 || !ConnMessage.StartsWith ("Connected to")) {
				view.connDevMessage.enabled = false;
				ConnMessage = "NO DEVICE CONNECTED";
			} else {
				view.connDevMessage.enabled = true;
				ConnMessage = ConnMessage.Substring (indexSt, indexEnd - indexSt);
			}

			view.connDevName.text = ConnMessage;


			if (ProStatus.isPro) {
//					view.proSwitchOptions.SetActive (true);

				if (view.proPurchasePanel.activeSelf) {

					view.proPurchasePanel.SetActive (false);
					view.proOptionsPanel.SetActive (true);
				}

				view.proOptionsButton.SetActive (true);
				view.proStrobeOptions.SetActive (true);
				view.stdFlashTog.SetActive (false);

				turnOnProLogos (true);

					//enableProBantam(-1);

					//				if (isBantamComp && isProComp == ProInfo.IS_COMPATIBLE) {
					////					view.isAutoOn.isOn = false;
					//					view.isAutoOn.interactable = true;
					//					view.isIgnCtrl.interactable = true;
					//					view.isLockout.interactable = true;

					//					view.switchTimer.interactable = true;

					//					view.proOptionsNote.text = "";


					//					view.isInputLatching.interactable = true;
					//					view.currentLimit.interactable = true;
					//					view.isCurrentRestart.interactable = true;

					//					systemRequestSignal.Dispatch (SystemRequestEvents.SystemData, null);

					//				} else {
					//					view.isAutoOn.isOn = false;
					//					view.isAutoOn.interactable = false;

					//					view.isIgnCtrl.isOn = false;
					//					view.isIgnCtrl.interactable = false;

					//					view.isLockout.isOn = false;
					//					view.isLockout.interactable = false;

					//					view.switchTimer.interactable = false;
					//					view.switchTimer.text = "";

					//					view.isInputLatching.isOn = false;
					//					view.isInputLatching.interactable = false;

					//					view.currentLimit.interactable = false;
					//					view.currentLimit.text = "";

					//					view.isCurrentRestart.isOn = false;
					//					view.isCurrentRestart.interactable = false;

					//					view.proOptionsNote.text = "Connect to a pro-compatible Bantam to enable pro features";
					//				}

				} else {
				
//					view.proSwitchOptions.SetActive (false);
				view.proOptionsButton.SetActive (false);
				view.proStrobeOptions.SetActive (false);
				view.stdFlashTog.SetActive (true);

				view.proSwitchOptions.SetActive (false);
				view.stdSwitchOptions.SetActive (true);

				turnOnProLogos (false);
			}




			isPro = ProStatus.isPro;

			Debug.Log ("Update pro Status, isPro: " + isPro);



			
			break;
		case SystemResponseEvents.SecurityData:

			bool isOn = utils.getValueForKey<bool>(data, "TurnOn");

				Debug.Log("global: SystemResponseEvents.SecurityData: " + isOn);


			if(data.ContainsKey("TurnOn"))
            {
					Debug.Log("global: SystemResponseEvents.SecurityData: " + isOn);
					view.EnterPasskeyPanel.SetActive(isOn);
			}
            else
            {
					Debug.Log("global: SystemResponseEvents.SecurityData: null");
					view.EnterPasskeyPanel.SetActive(false);
			}

			break;
		case SystemResponseEvents.ActivateIcon:
			
			int iconId = utils.getValueForKey<int> (data, "IconId");	
			int index = utils.getValueForKey<int> (data, "Index");	
			SwitchStatus[] switchesLoc = utils.getValueForKey<SwitchStatus[]> (lastData, "switches");

			Debug.Log ("SystemResponseEvents.ActivateIcon: iconId = " + iconId + ", index = " + index);

			string objName = "Image (" + iconId.ToString () + ")";

			Debug.Log ("Rec objName = " + objName);


			view.iconSelectionPanel.SetActive (true);

			GameObject imObj = GameObject.Find (objName);

			if (imObj != null) {
				Debug.Log ("found obj = " + imObj.name);
				switchesLoc [index].sprite = imObj.GetComponent<Image> ().sprite;
				switchesLoc [index].isLegend = true;
				switchesLoc [index].legendId = iconId;
			} else {
				Debug.Log ("no find obj");
			}

			view.iconSelectionPanel.SetActive (false);

//			switchesLoc [index].isDirty = true;	

			lastData ["switches"] = switchesLoc;

			systemRequestSignal.Dispatch (SystemRequestEvents.UpdateSystemData, lastData);


			break;
		case SystemResponseEvents.EnableSixSwitchGui:
			{

				if (needsPosSave) {

					swRow0 = view.swPos [0].GetComponent<RectTransform> ().localPosition.y;
					swRow1 = view.swPos [4].GetComponent<RectTransform> ().localPosition.y;

					swCol0 = view.swPos [0].GetComponent<RectTransform> ().localPosition.x;
					swCol1 = view.swPos [1].GetComponent<RectTransform> ().localPosition.x;
					swCol2 = view.swPos [2].GetComponent<RectTransform> ().localPosition.x;
					swCol3 = view.swPos [3].GetComponent<RectTransform> ().localPosition.x;

					needsPosSave = false;
				}
//			Vector2[] sw = new Vector2[8];

//			RectTransform sw5;

				RectTransform sw0 = view.swPos [0].GetComponent<RectTransform> ();
				RectTransform sw1 = view.swPos [1].GetComponent<RectTransform> ();
				RectTransform sw2 = view.swPos [2].GetComponent<RectTransform> ();
				RectTransform sw3 = view.swPos [3].GetComponent<RectTransform> ();
				RectTransform sw4 = view.swPos [4].GetComponent<RectTransform> ();
				RectTransform sw5 = view.swPos [5].GetComponent<RectTransform> ();

//			for (int i = 0; i < 7; i++) {
//			
//				sw [i] = view.swPos [i].GetComponent<RectTransform> ().position;
//			}

//			sw [3].position = new Vector2 (sw [0].position.x, sw [7].position.y);
//			sw [4] = new Vector2 (sw [1].x, sw [7].y);
//			sw4.position = new Vector2 (sw [1].x, sw [7].y);

//			sw3.position = new Vector2 (view.swPos [0].GetComponent<RectTransform> ().position.x, view.swPos [7].GetComponent<RectTransform> ().position.y);
//			sw4.position = new Vector2 (view.swPos [1].GetComponent<RectTransform> ().position.x, view.swPos [7].GetComponent<RectTransform> ().position.y);
//			sw5.position = new Vector2 (view.swPos [2].GetComponent<RectTransform> ().position.x, view.swPos [7].GetComponent<RectTransform> ().position.y);

//				float x1 = view.swPos [2].GetComponent<RectTransform> ().position.x;
//				float y1 = view.swPos [7].GetComponent<RectTransform> ().position.y;


//				sw0.position = new Vector2 ((swCol0 + swCol1)/2, swRow0);
//				sw1.position = new Vector2 ((swCol1 + swCol2)/2, swRow0);
//				sw2.position = new Vector2 ((swCol2 + swCol3)/2, swRow0);
//				sw3.position = new Vector2 ((swCol0 + swCol1)/2, swRow1);
//				sw4.position = new Vector2 ((swCol1 + swCol2)/2, swRow1);
//				sw5.position = new Vector2 ((swCol2 + swCol3)/2, swRow1);

				float diff = swCol1 - swCol0;

				sw0.localPosition = new Vector2 (swCol0 + diff / 3, swRow0);
				sw1.localPosition = new Vector2 ((swCol1 + swCol2)/2, swRow0);
				sw2.localPosition = new Vector2 (swCol3 - diff / 3, swRow0);
				sw3.localPosition = new Vector2 (swCol0 + diff / 3, swRow1);
				sw4.localPosition = new Vector2 ((swCol1 + swCol2)/2, swRow1);
				sw5.localPosition = new Vector2 (swCol3 - diff / 3, swRow1);

//				sw5.position = new Vector2 ((float)659.3 ,(float)(-296.0));

				view.swPos [6].SetActive (false);
				view.swPos [7].SetActive (false);
		
			break;
		}
		case SystemResponseEvents.DisableSixSwitchGui:
			{
//			RectTransform[] sw1 = new RectTransform[8];

				if (needsPosSave)
					break;
				
				RectTransform sw0 = view.swPos [0].GetComponent<RectTransform> ();
				RectTransform sw1 = view.swPos [1].GetComponent<RectTransform> ();
				RectTransform sw2 = view.swPos [2].GetComponent<RectTransform> ();
			RectTransform sw3 = view.swPos [3].GetComponent<RectTransform> ();
			RectTransform sw4 = view.swPos [4].GetComponent<RectTransform> ();
			RectTransform sw5 = view.swPos [5].GetComponent<RectTransform> ();

					//				sw3.position = new Vector2 (view.swPos [7].GetComponent<RectTransform> ().position.x, view.swPos [0].GetComponent<RectTransform> ().position.y);
					//				sw4.position = new Vector2 (view.swPos [0].GetComponent<RectTransform> ().position.x, view.swPos [7].GetComponent<RectTransform> ().position.y);
					//				sw5.position = new Vector2 (view.swPos [1].GetComponent<RectTransform> ().position.x, view.swPos [7].GetComponent<RectTransform> ().position.y);

					//sw0.localPosition = new Vector2(swCol0, swRow0);

				sw0.localPosition = new Vector2 (swCol0, swRow0);
				sw1.localPosition = new Vector2 (swCol1, swRow0);
				sw2.localPosition = new Vector2 (swCol2, swRow0);
				sw3.localPosition = new Vector2 (swCol3, swRow0);
				sw4.localPosition = new Vector2 (swCol0, swRow1);
				sw5.localPosition = new Vector2 (swCol1, swRow1);

			view.swPos [6].SetActive (true);
			view.swPos [7].SetActive (true);
//
//			for (int i = 0; i < 7; i++) {
//
//				sw1 [i] = view.swPos [i].GetComponent<RectTransform> ();
//			}

//			sw1 [3].position = new Vector2 (sw1 [7].position.x, sw1 [0].position.y);
//			sw1 [4].position = new Vector2 (sw1 [0].position.x, sw1 [7].position.y);
//			sw1 [5].position = new Vector2 (sw1 [1].position.x, sw1 [7].position.y);



			break;
			}
		case SystemResponseEvents.BantamLowPowerTog:
                //{

				
                if(data != null && data.ContainsKey("isBantamLowPower") && data.ContainsKey("isBantamLowPowerCompat"))
                {
                    bool isLp = utils.getValueForKey<bool>(data, "isBantamLowPower");
                    bool isLpComp = utils.getValueForKey<bool>(data, "isBantamLowPowerCompat");

                    view.isBantamLowPower.interactable = isLpComp;

                    view.isBantamLowPower.isOn = isLp;
                }
                else
                {
                    if (view.isBantamLowPower != null)
                    {
                        view.isBantamLowPower.interactable = false;
                    }

                }
					break;
				//}
    }
	}

	//		IEnumerator DisableIapButton(){
	//	
	//			yield return new WaitForEndOfFrame ();
	//	
	//		view.proPurchasePanel.SetActive (false);
	//		view.proOptionsPanel.SetActive (true);
	//		}

	private bool isBantamCompatible = false;
	private byte connectedDevAddress = 0;

	private void enableProBantam(int sourceAddress)
    {
		bool validMatch = false;

		Debug.Log("enableProBantam(): " + sourceAddress + "/" + connectedDevAddress + " - " + isBantamCompatible);

		if (sourceAddress >= 0 && sourceAddress < 4)//32)
		{
			if (connectedDevAddress > 0)
			{
				validMatch = ((0x01 << sourceAddress) & connectedDevAddress) > 0 ? true : false;
			}
		}
        else
        {
			validMatch = false;
		}
		
		if(isBantamCompatible && validMatch)
        {
			//view.isAutoOn.isOn = false;
			view.isAutoOn.interactable = true;
			view.isIgnCtrl.interactable = true;
			view.isLockout.interactable = true;

			view.switchTimer.interactable = true;

			int numOfAdd = 0;

			for(int i = 0; i < 4; i++)
            {
				numOfAdd += (connectedDevAddress & (0x01 << i)) > 0 ? 1 : 0;
			}

			if(numOfAdd == 1)
            {
				view.proOptionsNote.text = "";
			}
            else
            {
				view.proOptionsNote.text = "error";// "Note: (Please update Bantam) these features are only programable if connected Bantam is on the same address";
			}

			//if (connectedDevAddress)
			//{ 
			//	view.proOptionsNote.text = "";
   //         }

			view.isInputLatching.interactable = true;
			view.currentLimit.interactable = true;
			view.isCurrentRestart.interactable = true;

			//systemRequestSignal.Dispatch(SystemRequestEvents.SystemData, null);

		}
		else
		{
			view.isAutoOn.isOn = false;
			view.isAutoOn.interactable = false;

			view.isIgnCtrl.isOn = false;
			view.isIgnCtrl.interactable = false;

			view.isLockout.isOn = false;
			view.isLockout.interactable = false;

			view.switchTimer.interactable = false;
			view.switchTimer.text = "";

			view.isInputLatching.isOn = false;
			view.isInputLatching.interactable = false;

			view.currentLimit.interactable = false;
			view.currentLimit.text = "";

			view.isCurrentRestart.isOn = false;
			view.isCurrentRestart.interactable = false;

			if (isBantamCompatible)
			{
				if (connectedDevAddress == 0)
				{
					view.proOptionsNote.text = "Update BantamX to enable all features.";
				}
				else
				{
					//view.proOptionsNote.text = "Connect to a pro-compatible BantamX on this address to enable features";
					view.proOptionsNote.text = "To enable all features, you must be conencted to a Pro-Series compatible system (Bantam/BantamX) on this address.";
				}
			}
            else
            {
				//view.proOptionsNote.text = "Connect to a pro-compatible BantamX to enable pro features";
				view.proOptionsNote.text = "To enable all features, you must be conencted to a Pro-Series compatible system (Bantam/BantamX).";
			}
		}
	}

	private float lastSliderUpdate = 0.0f;

	//private bool wasAllOffHeld = false;

	IEnumerator AllOffInstance;
	IEnumerator AllOff(){

		SwitchStatus[] switches = utils.getValueForKey<SwitchStatus[]> (lastData, "switches");

//		Debug.Log ("D3");

		yield return new WaitForSeconds(1.0f);

//		if (switches != null && switches.Length == 32) {
//
//
//			offStartIndex = (currentSourceId * 8);
//			offStopIndex = (currentSourceId * 8) + 8;
//
//			if (SendAllOffInstance != null)
//				StopCoroutine (SendAllOffInstance);
//
//			SendAllOffInstance = SendAllOff ();
//			StartCoroutine (SendAllOffInstance);
//
////			StopCoroutine ("SendAllOff");
////			StartCoroutine("SendAllOff");
//		}
//
//		yield return new WaitForSeconds(1.0f);
//
		//wasAllOffHeld = true;

		if (switches != null && switches.Length == 32) {

			offStartIndex = 0;
			offStopIndex = 32;

			if (SendAllOffInstance != null)
				StopCoroutine (SendAllOffInstance);

			SendAllOffInstance = SendAllOff ();
			StartCoroutine (SendAllOffInstance);

//			StopCoroutine ("SendAllOff");
//			StartCoroutine("SendAllOff");
		}



		yield break;
	}

	private int offStartIndex = 0;
	private int offStopIndex = 0;

	IEnumerator SendAllOffInstance;
	IEnumerator SendAllOff(){

		Debug.Log ("SendAllOff()");
		SwitchStatus[] switches = utils.getValueForKey<SwitchStatus[]> (lastData, "switches");
//		Debug.Log ("D4");

		while (offStartIndex != offStopIndex && offStartIndex < offStopIndex) {
			yield return new WaitForSeconds(0.066f);
			Debug.Log ("SendAllOff() : " + offStartIndex + ", " + offStopIndex);
			switches [offStartIndex].isOn = false;

			systemRequestSignal.Dispatch(SystemRequestEvents.SwitchUpdate, new Dictionary<string, object>{{"SwitchStatus", switches[offStartIndex]}});


			offStartIndex++;
		}
		yield break;
	}

	//private bool isProSwOptions = false;

	public void uiSignalListener(string key, string type, Dictionary<string, object> data) {

		//Debug.Log ("gm: UI: Key = " + key + ", Type = " + type);

		switch (type) {
		case UiEvents.MouseDown:

			switch (key) {
			case "All Off":
				
				//wasAllOffHeld = false;

				if (AllOffInstance != null)
					StopCoroutine (AllOffInstance);

				AllOffInstance = AllOff ();
				StartCoroutine (AllOffInstance);

//				StopCoroutine ("AllOff");
//				StartCoroutine("AllOff");


				break;
			}

			break;
		case UiEvents.MouseUp:

			switch (key) {
			case "All Off":
				
				if (AllOffInstance != null)
					StopCoroutine (AllOffInstance);

//				StopCoroutine ("AllOff");

				break;
			}

			break;
		}

		if (type == UiEvents.Click) {

			if (key.StartsWith ("Is Celcius")) {
				debugCount++;

				if (debugCount >= 7) {
					systemRequestSignal.Dispatch (SystemRequestEvents.EnableDebugLog, null);
				}
			}

			if(key.StartsWith("Image")){
				GameObject imageObj = utils.getAttachedObject (data);

				if (imageObj != null) {
					Image image = imageObj.GetComponent<Image> ();

				

					if (image != null) {

						SwitchStatus[] switches = utils.getValueForKey<SwitchStatus[]> (lastData, "switches");
//						ProSwitchStatus[] proSwitches = utils.getValueForKey<ProSwitchStatus[]> (data, "ProSwitches");

						if (switchSetupId > -1 && switches != null && switches.Length == 32) {


							switches [switchSetupId].isDimmable = view.isDimmable.isOn;
							switches [switchSetupId].isMomentary = view.isMomentary.isOn;
							switches [switchSetupId].canFlash = view.canFlash.isOn;
							switches [switchSetupId].canStrobe = view.canStrobe.isOn;

							switches [switchSetupId].red = view.red.value;
							switches [switchSetupId].green = view.green.value;
							switches [switchSetupId].blue = view.blue.value;

							switches [switchSetupId].label1 = view.label1.text;
							switches [switchSetupId].label2 = view.label2.text;
							switches [switchSetupId].label3 = view.label3.text;
							switches [switchSetupId].isLegend = true;
							switches [switchSetupId].sprite = image.sprite;

							int legId;

//							if(Int32.TryParse(image.sprite.name .Substring (5, 3), out legId))

//							int stnum = imageObj.name.IndexOf ("(");
							int psLen = imageObj.name.IndexOf (")") - 7;

							Debug.Log ("par = " + imageObj.name + " / " + imageObj.name.Substring (7, psLen));

							if(Int32.TryParse(imageObj.name.Substring (7, psLen), out legId))
							{
								switches [switchSetupId].legendId = legId;
							}
							else
							{
								switches [switchSetupId].legendId = 255;
							}

							if (isPro) {

								switches [switchSetupId].proStrobeOn = getStrobeVal(view.StrobeOnSlider.value);
								switches [switchSetupId].proStrobeOff = getStrobeVal(view.StrobeOffSlider.value);

								switches [switchSetupId].proIsInputEnabled = view.isInputEnabled.isOn;
								switches [switchSetupId].proIsInputLockout = view.isInputLockout.isOn;
								switches [switchSetupId].proIsInputLockInvert = view.isInputLockInvert.isOn;

							}
							else
                            {
								if(switches[switchSetupId].canFlash)
                                {
									switches[switchSetupId].proStrobeOn = 10;
									switches[switchSetupId].proStrobeOff = 10;
								}
								else if (switches[switchSetupId].canStrobe)
								{
									switches[switchSetupId].proStrobeOn = 1;
									switches[switchSetupId].proStrobeOff = 4;
								}
								else
                                {
									switches[switchSetupId].proStrobeOn = 255;
									switches[switchSetupId].proStrobeOff = 0;
								}
							}

							if (isPro && view.isAutoOn.interactable) {

								switches [switchSetupId].proIsAutoOn = view.isAutoOn.isOn;
								switches [switchSetupId].proIsIgnCtrl = view.isIgnCtrl.isOn;
								switches [switchSetupId].proIsLockout = view.isLockout.isOn;

								switches [switchSetupId].proIsInputLatch = view.isInputLatching.isOn;
								switches [switchSetupId].proIsCurrentRestart = view.isCurrentRestart.isOn;

								if (!string.IsNullOrEmpty(view.switchTimer.text) && IsDigitsOnly (view.switchTimer.text)) {
									Int32 val = Int32.Parse (view.switchTimer.text);
									switches [switchSetupId].proOnTimer = (val > 1440 ? 1440 : val);
								} else {
									switches [switchSetupId].proOnTimer = 0;
								}

								if (!string.IsNullOrEmpty(view.currentLimit.text) && IsDigitsOnly (view.currentLimit.text)) {
									Int32 val = Int32.Parse (view.currentLimit.text);

									val = (val > 30 ? 30 : val);
									val = (val < 2 ? 2 : val);

									if (val % 2 != 0)
										val -= 1;

									switches [switchSetupId].proCurrentLimit = val;
								} else {
									switches [switchSetupId].proCurrentLimit = 30;
								}

							}

						}

//						if (switchSetupId > -1 && proSwitches != null && proSwitches.Length == 32) {
//							
//							if (view.switchTimer != null) {
//								proSwitches [switchSetupId].swOnTimer = Int32.Parse (view.switchTimer.text);
//							} else {
//								proSwitches [switchSetupId].swOnTimer = 0;
//							}
//
//							proSwitches [switchSetupId].isOnStart = view.isAutoOn.isOn;
//
//						}


						lastData["switches"] = switches;
//						lastData ["proSwitches"] = proSwitches;

						systemRequestSignal.Dispatch (SystemRequestEvents.UpdateSystemData, lastData);
						//systemRequestSignal.Dispatch (SystemRequestEvents.UpdateProCanData, lastData, switchSetupId);

					}
				}

				view.iconSelectionPanel.SetActive (false);


//				view.otaDeviceSelectPanel.SetActive (true);
			}

			if (key.StartsWith ("Ota Device Select")) {
				
				view.otaDeviceSelectPanel.SetActive (false);

				string otaButtonVal = key.Substring (18);

				systemRequestSignal.Dispatch (SystemRequestEvents.SelectOtaDevice, new Dictionary<string, object>{{"otaButton", otaButtonVal}});
//				systemRequestSignal.Dispatch (SystemRequestEvents.SelectOtaDevice, otaButtonVal);
			}

			switch (key) {
//			case "Ota Device Select Cancel":
//			case "Ota Device Select Bantam":
//			case "Ota Device Select Touchscreen":
//			case "Ota Device Select SwitchHD":
//
//				view.otaDeviceSelectPanel.SetActive (false);
//
//				break;
			case "Source SE":
				view.sourceSESetupPanel.SetActive (true);
				break;
			case "Source SE Setup Ok":
				view.sourceSESetupPanel.SetActive (false);
				break;
			case "Switch HD":
				view.switchHDSetupPanel.SetActive (true);
				break;
			case "Switch HD Setup Ok":
				view.switchHDSetupPanel.SetActive (false);
				break;
			case "Select Image":
					view.imagePicker.Show("Select Image", "unimgpicker");//, 256);
				break;
			case "Device Selection Ok":
				view.deviceSelectonPanel.SetActive (false);
				break;
			case "Icon Selection Ok":
				view.iconSelectionPanel.SetActive (false);
				break;
			case "Select Icon":
				view.iconSelectionPanel.SetActive (true);
				break;
			case "Device Item":
				view.deviceSelectonPanel.SetActive (false);

				GameObject deviceItemObj = utils.getAttachedObject (data);

				if (deviceItemObj != null) {
					DeviceItem deviceItem = deviceItemObj.GetComponent<DeviceItem>();

					if (deviceItem != null) {
						systemRequestSignal.Dispatch(SystemRequestEvents.SelectDevice, new Dictionary<string, object>{{SystemRequestEvents.SelectDevice, deviceItem}});
					}
				}

				break;
			case "Voice On":
				if (view.recordingCanvas != null) {

//#if PLATFORM_ANDROID

//						Debug.Log("try get voice permission");
//						if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
//						{
//							Permission.RequestUserPermission(Permission.Microphone);
//						}
//						Debug.Log("did get voice permission" + Permission.HasUserAuthorizedPermission(Permission.Microphone));
//#endif

						view.recordingCanvas.StartDetection ();

					//systemReponseSignal.Dispatch(SystemResponseEvents.VoicePhrase, new Dictionary<string, object>{{"VoicePhrase", "hey siri turn on switch 1 "}});
				}
				break;
			case "Voice Off":
				if (view.recordingCanvas != null) {
					view.recordingCanvas.StopDetection ();
				}
				break;
			case "Off Road On":
				{
					isOffRoad = true;


					SwitchStatus[] switches = utils.getValueForKey<SwitchStatus[]> (lastData, "switches");

					Debug.Log ("D6");

					foreach (SwitchStatus status in switches) {
						if (status != null && status.wasFlashing) {
							status.isFlashing = true;
							status.wasFlashing = false;
							systemRequestSignal.Dispatch (SystemRequestEvents.SwitchUpdate, new Dictionary<string, object>{ {
									"SwitchStatus",
									status
								} });
						}

					}

					if (lastSwitchStatus != null) {
						lastSwitchStatus.isFlashing = true;
					}

					updateView ();
				}
				break;
			case "Off Road Off":
				{
					isOffRoad = false;



					foreach (KeyValuePair<string, object> kvp in lastData) {
						Debug.Log (kvp.Key);
					}

					SwitchStatus[] switches = utils.getValueForKey<SwitchStatus[]> (lastData, "switches");
					Debug.Log ("D7");
					foreach (SwitchStatus status in switches) {

						Debug.Log(status.id + ": " + status.isFlashing);
						if (status != null && status.isFlashing) {
							status.wasFlashing = true;
							status.isFlashing = false;
							systemRequestSignal.Dispatch(SystemRequestEvents.SwitchUpdate, new Dictionary<string, object>{{"SwitchStatus", status}});
						}

					}

					if (lastSwitchStatus != null) {
						lastSwitchStatus.isFlashing = false;
					}

					updateView ();
				}
				break;
			case "Flash On":
				
				if (isOffRoad && lastSwitchStatus != null) {
					lastSwitchStatus.isFlashing = true;
					systemRequestSignal.Dispatch(SystemRequestEvents.SwitchUpdate, new Dictionary<string, object>{{"SwitchStatus", lastSwitchStatus}});
				}
				break;
			case "Flash Off":
				
				if (lastSwitchStatus != null) {
					lastSwitchStatus.isFlashing = false;
					lastSwitchStatus.wasFlashing = false;
					systemRequestSignal.Dispatch(SystemRequestEvents.SwitchUpdate, new Dictionary<string, object>{{"SwitchStatus", lastSwitchStatus}});
				}
				break;
			case "Setup Ok":
				{
					SwitchStatus[] switches = utils.getValueForKey<SwitchStatus[]> (lastData, "switches");
//					Debug.Log ("D8");

					debugCount = 0;

					if (switchSetupId > -1 && switches != null && switches.Length == 32) {
						

						switches [switchSetupId].isDimmable = view.isDimmable.isOn;
						switches [switchSetupId].isMomentary = view.isMomentary.isOn;
						switches [switchSetupId].canFlash = view.canFlash.isOn;
						switches [switchSetupId].canStrobe = view.canStrobe.isOn;

						switches [switchSetupId].red = view.red.value;
						switches [switchSetupId].green = view.green.value;
						switches [switchSetupId].blue = view.blue.value;

						switches [switchSetupId].label1 = view.label1.text;
						switches [switchSetupId].label2 = view.label2.text;
						switches [switchSetupId].label3 = view.label3.text;
						switches [switchSetupId].isLegend = view.isLegend.isOn;

						switches [switchSetupId].isDirty = true;

						if (isPro) {

							switches [switchSetupId].proStrobeOn = getStrobeVal(view.StrobeOnSlider.value);
							switches [switchSetupId].proStrobeOff = getStrobeVal(view.StrobeOffSlider.value);

							switches [switchSetupId].proIsInputEnabled = view.isInputEnabled.isOn;
							switches [switchSetupId].proIsInputLockout = view.isInputLockout.isOn;
							switches [switchSetupId].proIsInputLockInvert = view.isInputLockInvert.isOn;

						}
						else
						{
							if (switches[switchSetupId].canFlash)
							{
								switches[switchSetupId].proStrobeOn = 10;
								switches[switchSetupId].proStrobeOff = 10;
                            }
							else if (switches[switchSetupId].canStrobe)
							{
								switches[switchSetupId].proStrobeOn = 1;
								switches[switchSetupId].proStrobeOff = 4;
							}
							else
							{
								switches[switchSetupId].proStrobeOn = 255;
								switches[switchSetupId].proStrobeOff = 0;
							}
						}


							if (isPro && view.isAutoOn.interactable) {

							switches [switchSetupId].proIsAutoOn = view.isAutoOn.isOn;
							switches [switchSetupId].proIsIgnCtrl = view.isIgnCtrl.isOn;
							switches [switchSetupId].proIsLockout = view.isLockout.isOn;

							switches [switchSetupId].proIsInputLatch = view.isInputLatching.isOn;
							switches [switchSetupId].proIsCurrentRestart = view.isCurrentRestart.isOn;

							if (!string.IsNullOrEmpty(view.switchTimer.text) && IsDigitsOnly (view.switchTimer.text)) {
								Int32 val = Int32.Parse (view.switchTimer.text);
								switches [switchSetupId].proOnTimer = (val > 1440 ? 1440 : val);
							} else {
								switches [switchSetupId].proOnTimer = 0;
							}

							if (!string.IsNullOrEmpty(view.currentLimit.text) && IsDigitsOnly (view.currentLimit.text)) {
								Int32 val = Int32.Parse (view.currentLimit.text);

								val = (val > 30 ? 30 : val);
								val = (val < 2 ? 2 : val);

								if (val % 2 != 0)
									val -= 1;

								switches [switchSetupId].proCurrentLimit = val;
							} else {
								switches [switchSetupId].proCurrentLimit = 30;
							}

						}

						systemRequestSignal.Dispatch (SystemRequestEvents.SendTsPackets, new Dictionary<string, object>{ {
								SystemRequestEvents.SendTsPackets,
								switches [switchSetupId]
							} });

						//systemRequestSignal.Dispatch (SystemRequestEvents.SwitchUpdate, new Dictionary<string, object>{ 
						//	{"SwitchStatus", switches [switchSetupId]} 
						//});

						systemRequestSignal.Dispatch(SystemRequestEvents.UpdatedSwitchSettings, new Dictionary<string, object>{
							{"SwitchStatus", switches [switchSetupId]}
						});

						}

					lastData ["switches"] = switches;

					systemRequestSignal.Dispatch (SystemRequestEvents.UpdateSystemData, lastData);


					systemRequestSignal.Dispatch (SystemRequestEvents.Save, null);


					switchSetupId = -1;
					view.switchSetupPanel.SetActive (false);

				}
				break;
			case "Source Setup Ok":

					string[] sourceNames = utils.getValueForKey<string[]> (lastData, "sourceNames");
					Vector3[] sourceColors = utils.getValueForKey<Vector3[]> (lastData, "sourceColors");
					bool[] sourceTemps = utils.getValueForKey<bool[]> (lastData, "sourceTemps");
					
					sourceNames [currentSourceId] = view.sourceLabel.text;
					sourceColors [currentSourceId] = new Vector3 (view.sourceRed.value, view.sourceGreen.value, view.sourceBlue.value);
					sourceTemps [currentSourceId] = view.isCelcius.isOn;

					
					lastData ["sourceNames"] = sourceNames;
					lastData ["sourceColors"] = sourceColors;
					lastData ["sourceTemps"] = sourceTemps;


                    if (view.isBantamLowPower.interactable)
                    {
                        bool isBantamLowPower = view.isBantamLowPower.isOn;
                        systemRequestSignal.Dispatch(SystemRequestEvents.BantamLowPowerTog, new Dictionary<string, object> { { "isBantamLowPower", isBantamLowPower } });
                    }


                    systemRequestSignal.Dispatch (SystemRequestEvents.UpdateSystemData, lastData);
					systemRequestSignal.Dispatch (SystemRequestEvents.Save, null);
					view.sourceSetupPanel.SetActive (false);
				break;
			case "Scan":
				view.sourceSetupPanel.SetActive (false);
				break;
			case "Background":
				if (isSetupOn) {
					systemRequestSignal.Dispatch (SystemRequestEvents.ShowSourceSetup, null);
				}
				break;
			case "ProSeries":
				if (isSetupOn) {
					systemRequestSignal.Dispatch (SystemRequestEvents.ShowProPanel, null);
				}
				break;
			case "Help":
				if (isSetupOn) {
					systemRequestSignal.Dispatch (SystemRequestEvents.ShowHelp, null);
					Application.OpenURL("https://www.youtube.com/channel/UCB71_qCAr7fLCjBmmynkkIg");
				}
				break;
			case "Ota Upgrade Panel Cancel":
			case "Ota Upgrade Cancel":
				view.otaUpgradePanel.SetActive (false);
					lastWasPermissions = false;
					break;
			case"Ota Upgrade OK":
				view.otaUpgradePanel.SetActive (false);

					if (lastWasPermissions)
					{
						Debug.Log("send SystemRequestEvents.DisplayPermissionMessage: " + lastPermissionsString + ", " + lastPermissionsResponse);

						systemRequestSignal.Dispatch(SystemRequestEvents.DisplayPermissionMessage, new Dictionary<string, object> {
								{
								"PermissionString",
								lastPermissionsString
							},
								{
								"PermissionResponse",
								lastPermissionsResponse
							}
						});

						lastPermissionsString = "";
						lastPermissionsResponse = -1;
						lastWasPermissions = false;
					}
					else
					{
						//						if (view.otaUpgradeHeader.text.StartsWith("Bluetooth Error"))
						//						{
						//#if UNITY_EDITOR
						//							UnityEditor.EditorApplication.isPlaying = false;
						//#else
						//							Application.Quit();
						//#endif
						//						}

						//if (view.otaCancelButton.activeSelf.Equals(true))

						if (lastWasOtaInfoOnly) {
						} else { 
							systemRequestSignal.Dispatch(SystemRequestEvents.SelectOtaUpgrade, null);
						}
					}
				break;
				case "BleErr Panel Cancel":
				case "BleErr Cancel":
					view.bleErrPanel.SetActive(false);
					break;
				case "BleErr OK":
					view.bleErrPanel.SetActive(false);

#if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
#else
						Application.Quit();
#endif
                        //systemRequestSignal.Dispatch(SystemRequestEvents.SelectOtaUpgrade, null);

					break;
				case "Pro Series Button":
				{
//				ProSeriesMediator mediator = view.setupSwitchView.gameObject.transform.GetComponent<ProSeriesMediator> ();
					view.sourceSetupPanel.SetActive (false);

					if (isPro) {	// isGlobalProSeries

						view.proOptionsPanel.SetActive (true);


					} else {

						view.proPurchasePanel.SetActive (true);


					}
				}
				break;
			case "Pro Panel Cancel":
			case "Pro Cancel Button":
			case "Pro OK Button":
				{
//					Debug.Log ("globMed");

					view.proPurchasePanel.SetActive (false);
					view.proOptionsPanel.SetActive (false);

//					if (isPro) {
//						view.proOptionsPanel.SetActive (false);
//					} else {
//						view.proPurchasePanel.SetActive (false);
//						view.proOptionsPanel.SetActive (false);
//					}
				}
				break;
			case "Pro Video Panel Cancel":
			case "Pro Video Back Button":
				{
					view.proVideoPanel.SetActive (false);
					view.proPurchasePanel.SetActive (true);

//					view.proVideoPanel.gameObject.SetActive (false);
				}
				break;
			case "Pro Video Button":
				{

//					view.proVideoPanel
					view.proVideoPanel.SetActive (true);
					view.proPurchasePanel.SetActive (false);
				}
				break;
			case "Pro Restore Button":
				{
					PurchaseHandler.Instance.RestorePurchases ();
				}
				break;
			case "Pro Purchase Button":
				{
					PurchaseHandler.Instance.BuyProSeries ();
					
//					view.proPurchasePanel.SetActive (false);
//					view.proOptionsPanel.SetActive (true);
//
//					systemRequestSignal.Dispatch(SystemRequestEvents.SelectPurchasePro, new Dictionary<string, object>{{"isUnPurchase", false}});

					// launch purchase dialog...
					// isGlobalProSeries = true // + save to flash
				}
				break;
			case "Pro Unpurchase Button":
				{
					view.proOptionsPanel.SetActive (false);
					view.proPurchasePanel.SetActive (true);

					systemRequestSignal.Dispatch(SystemRequestEvents.SelectPurchasePro, new Dictionary<string, object>{{"isUnPurchase", true}});

					// isGlobalProSeries = false // + save to flash
				}
				break;
			case "Change Dev Name Button":
				{
					view.proOptionsPanel.SetActive (false);
					view.proNamePanel.SetActive (true);

				}
				break;
			case "Pro Name Panel Cancel":
			case "Pro Name Cancel Button":
			case "Pro Name Apply Button":
				{
					view.proOptionsPanel.SetActive (true);
					view.proNamePanel.SetActive (false);
				}
				break;
			case "Passkey Panel Cancel":
			case "Passkey Cancel Button":
				{
					view.EnterPasskeyPanel.SetActive(false);
				}
				break;
				case "Pro Settings Button On":
				{
					//isProSwOptions = true;

					view.proSwitchOptions.SetActive (true);
					view.stdSwitchOptions.SetActive (false);

				}
				break;
			case "Pro Settings Button Off":
				{
					//isProSwOptions = false;

					view.proSwitchOptions.SetActive (false);
					view.stdSwitchOptions.SetActive (true);

				}
				break;
			case "Is Bantam Low Power":

					if(view.isBantamLowPower.isOn)
                    {
						view.LowPowerPanel.SetActive(true);
					}
				break;
			case "Low Power Panel Cancel":
			case "Low Power Cancel":
				view.LowPowerPanel.SetActive(false);
				view.isBantamLowPower.isOn = false;
				break;
			case "Low Power Ok":
				view.LowPowerPanel.SetActive(false);
				break;

				default:
				{
					Debug.Log ("Unknown UI sig: " + key);
				}
				break;
			}

		}

		if (type == UiEvents.Drag || type == UiEvents.EndDrag) {
			if(key == "Slider"){

				if (lastSwitchStatus != null) {
					float currentTime = Time.realtimeSinceStartup;
					if (currentTime - lastSliderUpdate > 0.1f) {
						lastSliderUpdate = currentTime;

						GameObject sliderObj = utils.getAttachedObject (data);
						if (sliderObj != null) {
							Slider slider = sliderObj.GetComponent<Slider> ();

							lastSwitchStatus.value = slider.value;

							view.percents[lastSwitchStatus.id / 8].text = ((int)(lastSwitchStatus.value * 100.0f)) + "%";

							if (!lastSwitchStatus.isMomentary) {
								lastSwitchStatus.isOn = true;
							}
							systemRequestSignal.Dispatch(SystemRequestEvents.SwitchUpdate, new Dictionary<string, object>{{"SwitchStatus", lastSwitchStatus}});
						}

					}
				}

			}else if (key == "Switch Red Slider" || key == "Switch Green Slider" || key == "Switch Blue Slider") {
				view.backLight.transform.parent.GetComponent<Image>().color = new Color (view.red.value, view.green.value, view.blue.value);

			}else if (key == "Source Red Slider" || key == "Source Green Slider" || key == "Source Blue Slider") {
				view.sourceTempBackground.color = new Color (view.sourceRed.value, view.sourceGreen.value, view.sourceBlue.value);

				List<double> data2 = new List<double>{view.sourceTempBackground.color.r * 100.0F, view.sourceTempBackground.color.g * 100.0F, view.sourceTempBackground.color.b * 100.0F };

				if (data2.StandardDeviation () < 8) {
					systemReponseSignal.Dispatch(SystemResponseEvents.UpdateColor, new Dictionary<string, object>{{"Color", Color.red}});
				} else {
					systemReponseSignal.Dispatch(SystemResponseEvents.UpdateColor, new Dictionary<string, object>{{"Color", view.sourceTempBackground.color }});
				}


			}else if (key == "Strobe On Slider" || key == "Strobe Off Slider") {

				float onVal = getStrobeVal(view.StrobeOnSlider.value) * 0.02f;
				float offVal = getStrobeVal(view.StrobeOffSlider.value) * 0.02f;

				view.proStrobeReadout.text = ">> ON/OFF " + onVal.ToString("n2") + "s/" + offVal.ToString("n2") + "s";


				/// update stuff

				SwitchStatus[] switches = utils.getValueForKey<SwitchStatus[]> (lastData, "switches");
				//					Debug.Log ("D8");

				if (switchSetupId > -1 && switches != null && switches.Length == 32 && view.canStrobe.isOn && switches [switchSetupId].isOn) {


					switches [switchSetupId].isDimmable = false;
					switches [switchSetupId].isMomentary = false;
					switches [switchSetupId].canFlash = false;
					switches [switchSetupId].canStrobe = view.canStrobe.isOn;

//					switches [switchSetupId].red = view.red.value;
//					switches [switchSetupId].green = view.green.value;
//					switches [switchSetupId].blue = view.blue.value;
//
//					switches [switchSetupId].label1 = view.label1.text;
//					switches [switchSetupId].label2 = view.label2.text;
//					switches [switchSetupId].label3 = view.label3.text;
//					switches [switchSetupId].isLegend = view.isLegend.isOn;
//
//					switches [switchSetupId].isDirty = true;
//
//					if (isPro) {
//
					switches [switchSetupId].proStrobeOn = getStrobeVal(view.StrobeOnSlider.value);
					switches [switchSetupId].proStrobeOff = getStrobeVal(view.StrobeOffSlider.value);
//					}
//
//					if (isPro && view.isAutoOn.interactable) {
//
//						switches [switchSetupId].proIsAutoOn = view.isAutoOn.isOn;
//						switches [switchSetupId].proIsIgnCtrl = view.isIgnCtrl.isOn;
//
//						if (IsDigitsOnly (view.switchTimer.text)) {
//							Int32 val = Int32.Parse (view.switchTimer.text);
//							switches [switchSetupId].proOnTimer = (val > 1440 ? 1440 : val);
//						}
//
//					}

//					systemRequestSignal.Dispatch (SystemRequestEvents.SendTsPackets, new Dictionary<string, object>{ {
//							SystemRequestEvents.SendTsPackets,
//							switches [switchSetupId]
//						} });


					systemRequestSignal.Dispatch (SystemRequestEvents.UpdateStrobe, new Dictionary<string, object>{
						{"UpdateStrobe", switches [switchSetupId]}
					});


				}

//				lastData ["switches"] = switches;
//
//				systemRequestSignal.Dispatch (SystemRequestEvents.UpdateSystemData, lastData);

//				systemRequestSignal.Dispatch (SystemRequestEvents.Save, null);


//				switchSetupId = -1;
//				view.switchSetupPanel.SetActive (false);


			}
		}
	}

	private char[] splitChars = new char[]{ ' ', '-', '\n' };

	private string[] commandTokens = {"ok", "okay", "google", "hey", "siri", "syria", "turn", "on", "off", "dim", " ", "the"};

	private static string[] ones = {
		"zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", 
		"ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen",
	};

	private static string[] tens = { "zero", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

	private static string[] thous = { "hundred", "thousand", "million", "billion", "trillion", "quadrillion" };

	public static string ToWords(decimal number)
	{
		if (number < 0)
			return "negative " + ToWords(System.Math.Abs(number));

		int intPortion = (int)number;
		int decPortion = (int)((number - intPortion) * (decimal) 100);

		return string.Format("{0} dollars and {1} cents", ToWords(intPortion), ToWords(decPortion));
	}

	private static string ToWords(int number, string appendScale = "")
	{
		string numString = "";
		if (number < 100)
		{
			if (number < 20)
				numString = ones[number];
			else
			{
				numString = tens[number / 10];
				if ((number % 10) > 0)
					numString += "-" + ones[number % 10];
			}
		}
		else
		{
			int pow = 0;
			string powStr = "";

			if (number < 1000) // number is between 100 and 1000
			{
				pow = 100;
				powStr = thous[0];
			}
			else // find the scale of the number
			{
				int log = (int)System.Math.Log(number, 1000);
				pow = (int)System.Math.Pow(1000, log);
				powStr = thous[log];
			}

			numString = string.Format("{0} {1}", ToWords(number / pow, powStr), ToWords(number % pow)).Trim();
		}

		return string.Format("{0} {1}", numString, appendScale).Trim();
	}

	private List<string> stripTokens(List<string> tokens){
		
		for (int i = 0; i < commandTokens.Length; i++) {
			if(tokens.Contains(commandTokens[i]))
			{
				tokens.Remove (commandTokens[i]);
			}
		}

		return tokens;
	}

	private bool IsDigitsOnly(string str)
	{
		foreach (char c in str)
		{
			if (c < '0' || c > '9')
				return false;
		}

		return true;
	}

	private List<string> tokensToWords(List<string>tokens){
		List<string> newTokens = new List<string> ();

		for (int j = 0; j < tokens.Count; j++) {
			if (IsDigitsOnly (tokens [j])) {

				int number = 0;

				try{
					number = Convert.ToInt32(tokens [j]);

					string wordString = ToWords(number);
					string[] numberStringTokens = wordString.Split (splitChars);

					for (int k = 0; k < numberStringTokens.Length; k++) {
						newTokens.Add (numberStringTokens [k].Trim());
					}
				}catch(System.Exception) {// e){
					newTokens.Add (tokens [j].Trim());
				}





			} else {

				if (tokens [j].Trim () == "to" || tokens [j].Trim () == "too") {
					newTokens.Add ("two");
				}else if(tokens [j].Trim () == "for"){
					newTokens.Add ("four");
				}else{
					newTokens.Add (tokens [j].Trim());
				}
			}
		}

		return newTokens;

	}

	private void processVoiceData(int type, string phrase, List<string>tokens){
		tokens = stripTokens (tokens);

		tokens = tokensToWords (tokens);

		SwitchStatus[] switches = utils.getValueForKey<SwitchStatus[]> (lastData, "switches");

		List<string>[] switchTokens = new List<string>[switches.Length];

		List<KeyValuePair<int, int>> distanceList = new List<KeyValuePair<int, int>> ();

		for (int i = 0; i < switches.Length; i++) {
			string switchLabel = "";

			//switches [i].label1 + " " + switches [i].label2 + " " + switches [i].label3;

			if (switches [i].label1 != null && switches [i].label1.Length > 0){
				switchLabel += switches [i].label1.Trim();
			}

			if (switches [i].label2 != null && switches [i].label2.Length > 0) {

				if (switchLabel.Length > 0) {
					switchLabel += " " + switches [i].label2.Trim();
				} else {
					switchLabel += switches [i].label2.Trim();
				}
			}

			if (switches [i].label3 != null && switches [i].label3.Length > 0) {

				if (switchLabel.Length > 0) {
					switchLabel += " " + switches [i].label3.Trim();
				} else {
					switchLabel += switches [i].label3.Trim();
				}
			}



			string[] switchTokensPre = switchLabel.ToLower ().Split (splitChars);
			switchTokens[i] = tokensToWords(new List<string>(switchTokensPre));

			string debugTokens = "";

			foreach (string t in tokens) {
				debugTokens += ":" + t + ":";
			}

			string debugSwitchTokens = "";

			foreach (string t in switchTokens[i]) {
				debugSwitchTokens += ":" + t + ":";
			}

			//Debug.Log (debugTokens);
			//Debug.Log (debugSwitchTokens);
			int dist = Levenshtein.EditDistance (tokens, switchTokens[i]);
			//Debug.Log (dist);
			//Debug.Log (Levenshtein.EditDistance ("switch one", "switch one"));

			KeyValuePair<int, int> kvp = new KeyValuePair<int, int> (dist, i);

			distanceList.Add(kvp);
		}
			
		distanceList.Sort (delegate(KeyValuePair<int, int> c1, KeyValuePair<int, int> c2) { return c1.Key.CompareTo(c2.Key); });

		/*
		foreach( KeyValuePair<int, int> kvp in distanceList)
		{
			Debug.Log(String.Format("Key = {0}, Value = {1}, Label = {2}", kvp.Key, kvp.Value, switches[kvp.Value].label));
		}
		*/

		if (distanceList [0].Key == 0) {
			
			Debug.Log ("Found Match...");

			switch (type) {
			case 0:
				switches [distanceList [0].Value].isOn = false;
				break;
			case 1:
				switches [distanceList [0].Value].isOn = true;
				break;
			case 2:

				break;
			}

			systemRequestSignal.Dispatch(SystemRequestEvents.SwitchUpdate, new Dictionary<string, object>{{"SwitchStatus", switches [distanceList [0].Value]}});

		}
	}

	public void onVoicePhraseChange(string voicePhrase){
		SwitchStatus[] switches = utils.getValueForKey<SwitchStatus[]> (lastData, "switches");

		if (switches != null && switches.Length == 32) {
			voicePhrase = voicePhrase.ToLower ();

			string[] tokens = voicePhrase.Trim().Split (splitChars);

			List<string> tokenList = new List<string> (tokens);

			if(tokenList.Contains("siri") || tokenList.Contains("syria") || tokenList.Contains("google"))
			{
				if (voicePhrase.Contains ("turn on")) {
					processVoiceData (1, voicePhrase, tokenList);
				} else if (voicePhrase.Contains ("turn off")) {
					processVoiceData (0, voicePhrase, tokenList);
				} else if(voicePhrase.Contains("%")){
					processVoiceData (2, voicePhrase, tokenList);
				}
			}
		}
	}

	public void onTextChange(){

		if (!isUpdateEnabled)
			return;

		SwitchStatus[] switches = utils.getValueForKey<SwitchStatus[]> (lastData, "switches");

		if (switchSetupId > -1 && switches != null && switches.Length == 32) {

			// check input char is compatible with touchscreen text

			for (int i = 0; i < view.label1.text.Length; i++) {
				if (view.label1.text [i] < 0x20 || view.label1.text [i] > 0x7F) {// || view.label1.text [i] == 'a') {
					view.label1.text = view.label1.text.Remove (i, 1);
				}
			}

			for (int i = 0; i < view.label2.text.Length; i++) {
				if (view.label2.text [i] < 0x20 || view.label2.text [i] > 0x7F) {// || view.label2.text [i] == 'b') {
					view.label2.text = view.label2.text.Remove (i, 1);
				}
			}

			for (int i = 0; i < view.label3.text.Length; i++) {
				if (view.label3.text [i] < 0x20 || view.label3.text [i] > 0x7F) {// || view.label3.text [i] == 'c') {
					view.label3.text = view.label3.text.Remove (i, 1);
				}
			}



			switches [switchSetupId].label1 = view.label1.text;
			switches [switchSetupId].label2 = view.label2.text;
			switches [switchSetupId].label3 = view.label3.text;

			Debug.Log ("onTextChange()");

//			systemRequestSignal.Dispatch(SystemRequestEvents.SendTsPackets, new Dictionary<string, object>{{SystemRequestEvents.SendTsPackets, switches [switchSetupId]}});
//			switches [switchSetupId].updateTs = 2;

//			updateTsTextPacket (switchSetupId);
		}

		lastData["switches"] = switches;

		systemRequestSignal.Dispatch (SystemRequestEvents.UpdateSystemData, lastData);
	}

	public void onLegendChange(){

		if (!isUpdateEnabled)
			return;

		SwitchStatus[] switches = utils.getValueForKey<SwitchStatus[]> (lastData, "switches");

		if (switchSetupId > -1 && switches != null && switches.Length == 32) {


			switches [switchSetupId].isLegend = view.isLegend.isOn;

//			sendIconSelectPacket (switchSetupId);

//			systemRequestSignal.Dispatch(SystemRequestEvents.SendTsPackets, new Dictionary<string, object>{{SystemRequestEvents.SendTsPackets, switches [switchSetupId]}});

//			switches [switchSetupId].updateTs = 3;
		}

		lastData["switches"] = switches;

		systemRequestSignal.Dispatch (SystemRequestEvents.UpdateSystemData, lastData);

	}



	private bool isInit = false;
	// Use this for initialization
	override public void OnRegister(){

		view.init ();

		systemReponseSignal.AddListener(systemResponseSignalListener);
		uiSignal.AddListener (uiSignalListener);

		view.textChange.AddListener(onTextChange);

		view.useLegendChange.AddListener (onLegendChange);

		view.imagePicker.Completed += (string path) =>
		{
			StartCoroutine(LoadImage(path));
		};

		isInit = true;

		CanvasScaler canvasScaler = view.gameObject.GetComponent<CanvasScaler> ();

		Resolution resolution = Screen.currentResolution;

		if ((float)Camera.main.pixelWidth / (float)Camera.main.pixelHeight > 1.8f) {
			canvasScaler.matchWidthOrHeight = 0.746f;

			foreach (GameObject source in view.sources) {
				RectTransform t = source.GetComponent<RectTransform> ();
				Vector3 newPos = new Vector3 (t.localPosition.x + 100, t.localPosition.y, t.localPosition.z);
				t.localPosition = newPos;

			}
		}



	}

	override public void OnRemove()
	{

		if(systemReponseSignal != null)
			systemReponseSignal.RemoveListener(systemResponseSignalListener);

		if (uiSignal != null)
			uiSignal.RemoveListener (uiSignalListener);

		if (view.textChange != null)
			view.textChange.RemoveListener (onTextChange);

		if (view.useLegendChange != null)
			view.useLegendChange.RemoveListener (onLegendChange);
	}


	private IEnumerator LoadImage(string path)
	{
		//if (!www.isNetworkError && !www.isHttpError)
		//{
		//	Debug.LogError ("Failed to load texture url1:" + url + " - " + www.error + ", " + www.isDone);
		//}

		var url = "file://" + path;
		var unityWebRequestTexture = UnityWebRequestTexture.GetTexture(url);
		yield return unityWebRequestTexture.SendWebRequest();

		var texture = ((DownloadHandlerTexture)unityWebRequestTexture.downloadHandler).texture;
		if (texture == null)
		{
			Debug.LogError("Failed to load texture url:" + url);
		}
		
		else {
			Debug.LogError ("Do something with the texture url:" + url);

			//var texture = DownloadHandlerTexture.GetContent(www);

			SwitchStatus[] switches = utils.getValueForKey<SwitchStatus[]> (lastData, "switches");

			if (switchSetupId > -1 && switches != null && switches.Length == 32) {

				switches[switchSetupId].legendId = 255;
				switches [switchSetupId].isLegend = true;
				switches [switchSetupId].sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));


			}

			lastData["switches"] = switches;

			systemRequestSignal.Dispatch (SystemRequestEvents.UpdateSystemData, lastData);
		}


	}

	void OnApplicationPause(bool pauseStatus)
	{
		if(isInit)
		{
			systemRequestSignal.Dispatch (SystemRequestEvents.IsPaused, new Dictionary<string, object>{ {
					SystemRequestEvents.IsPaused,
					pauseStatus
				} });
		}
	}
}
