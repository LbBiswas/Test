using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.mediation.impl;
using app;

public class ProMediator : Mediator {

	[Inject]
	public ProView view { get; set; }

	[Inject]
	public UiSignal uiSignal { get; set;}

	[Inject]
	public SystemResponseSignal systemReponseSignal {get; set;}

	[Inject]
	public SystemRequestSignal systemRequestSignal {get; set;}

	[Inject]
	public Utils utils{ get; set; }

	public void systemResponseSignalListener(string key, Dictionary<string, object> data){


			//		Debug.Log ("proMedSysRes: " + key);

		switch (key) {
		case SystemResponseEvents.ProData:
			{

				ProSeriesStatus ProStatus = utils.getValueForKey<ProSeriesStatus> (data, "ProStatus");
//				bool isConToProBantam = utils.getValueForKey<bool> (data, "isBantamComp");

//				if (ProStatus == null) {
//					Debug.Log ("ERROR: ProStatus = null");
//				} else {
//					Debug.Log ("ProStatus != null " + ProStatus.isPro + ", " + ProStatus.isAppProEnabled);
//				}

				if (view.isAppProCompatible && !ProStatus.isAppProEnabled) {
					view.isActive.SetActive (false);
				} 
//				else if(view.isBantamSpecific){
//
//					if (isConToProBantam) {
//						view.isActive.SetActive (true);
//						view.isInteractable.interactable = true;
//					} else {
//						view.isActive.SetActive (false);
//						view.isInteractable.isOn = false;
//						view.isInteractable.interactable = false;
//
//					}
//				}

			}
			break;
		}
		


//		if (data != null && data.ContainsKey("Mask")) {
//			int mask = utils.getValueForKey<int> (data, "Mask");
//
//			if(view.debug)
//				Debug.Log (view.mask + ", " + mask);
//
//			if (view.mask != 0 && mask != null) {
//
//				if (!((view.mask & mask) > 0)) {
//					return;
//				}
//
//			}
//		}
//
//		switch (key) {
//		case SystemResponseEvents.SwitchUpdate:
//
//			if (view.isFlashButton) {
//				SwitchStatus status = utils.getValueForKey<SwitchStatus> (data, "SwitchStatus");
//				if (status != null) {
//					if (status.isFlashing && status.isOn) {
//						view.gameObject.GetComponent<ButtonView> ().setOn (true);
//					} else {
//						view.gameObject.GetComponent<ButtonView> ().setOn (false);
//					}
//				}
//			}
//
//
//			break;
//		case SystemResponseEvents.SetupIndicatorOn:
//			if (view.isFlash) {
//				if (view.isInverted) {
//					view.setupIndicator.SetActive (false);
//				} else {
//					view.setupIndicator.SetActive (true);
//				}
//			}
//			break;
//		case SystemResponseEvents.SetupIndicatorOff:
//			if (view.isFlash) {
//				if (view.isInverted) {
//					view.setupIndicator.SetActive (true);
//				} else {
//					view.setupIndicator.SetActive (false);
//				}
//			}
//			break;
//		case SystemResponseEvents.SetupOn:
//			if (!view.isFlash) {
//				if (view.isInverted) {
//					view.setupIndicator.SetActive (false);
//				} else {
//					view.setupIndicator.SetActive (true);
//				}
//			}
//			break;
//		case SystemResponseEvents.SetupOff:
//			if (!view.isFlash) {
//				if (view.isInverted) {
//					view.setupIndicator.SetActive (true);
//				} else {
//					view.setupIndicator.SetActive (false);
//				}
//			}
//			break;
//		}
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
