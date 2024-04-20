using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.mediation.impl;
using app;

public class SetupMediator : Mediator {

	[Inject]
	public SetupView view { get; set; }

	[Inject]
	public UiSignal uiSignal { get; set;}

	[Inject]
	public SystemResponseSignal systemReponseSignal {get; set;}

	[Inject]
	public SystemRequestSignal systemRequestSignal {get; set;}

	[Inject]
	public Utils utils{ get; set; }

	public void systemResponseSignalListener(string key, Dictionary<string, object> data){

		if (data != null && data.ContainsKey("Mask")) {
			int mask = utils.getValueForKey<int> (data, "Mask");

			if(view.debug)
				Debug.Log (view.mask + ", " + mask);

			if (view.mask != 0){// && mask != null) {	// replaced with ContainsKey
				
				if (!((view.mask & mask) > 0)) {
					return;
				}

			}
		}

		switch (key) {
		case SystemResponseEvents.SwitchUpdate:
			
			if (view.isFlashButton) {
				SwitchStatus status = utils.getValueForKey<SwitchStatus> (data, "SwitchStatus");
				if (status != null) {
					if (status.isFlashing && status.isOn) {
						view.gameObject.GetComponent<ButtonView> ().setOn (true);
					} else {
						view.gameObject.GetComponent<ButtonView> ().setOn (false);
					}
				}
			}
				

			break;
		case SystemResponseEvents.SetupIndicatorOn:
			if (view.isFlash) {
				if (view.isInverted) {
					view.setupIndicator.SetActive (false);
				} else {
					view.setupIndicator.SetActive (true);
				}
			}

			if (view.isFlashTouchable) {
				if (view.gameObject.GetComponent<Image> () != null) {
					Color col = view.gameObject.GetComponent<Image> ().color;
					if (view.isInverted) {
						col.a = 0;
					} else {
						col.a = 0.5f;
					}
					view.gameObject.GetComponent<Image> ().color = col;
				}
			}
			break;
		case SystemResponseEvents.SetupIndicatorOff:
			if (view.isFlash) {
				if (view.isInverted) {
					view.setupIndicator.SetActive (true);
				} else {
					view.setupIndicator.SetActive (false);
				}
			}

			if (view.isFlashTouchable) {
				if (view.gameObject.GetComponent<Image> () != null) {
					Color col = view.gameObject.GetComponent<Image> ().color;
					if (view.isInverted) {
						col.a = 0.5f;
					} else {
						col.a = 0;
					}
					view.gameObject.GetComponent<Image> ().color = col;
				}
			}
			break;
		case SystemResponseEvents.SetupOn:
			if (!view.isFlash) {
				if (view.isInverted) {
					view.setupIndicator.SetActive (false);
				} else if (!view.isDefaultOff){
					view.setupIndicator.SetActive (true);
				}
			}
			break;
		case SystemResponseEvents.SetupOff:
			if (!view.isFlash) {
				if (view.isInverted && !view.isDefaultOff) {
					view.setupIndicator.SetActive (true);
				} else {
					view.setupIndicator.SetActive (false);
				}
			}
			break;
		}
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
