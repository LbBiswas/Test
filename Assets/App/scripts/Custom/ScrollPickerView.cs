using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using strange.extensions.dispatcher.eventdispatcher.api;
using strange.extensions.mediation.impl;
using strange.extensions.signal.impl;
using strange.extensions.context.api;
using app;

public class ScrollPickerView : View, IPointerClickHandler, IPointerUpHandler, IPointerDownHandler {

	public UiSignal uiSignal = new UiSignal();

	public string customName;
	public GameObject attachedObject; 

	[HideInInspector]
	public string buttonName = "Unknown";

	public void OnPointerUp(PointerEventData eventData) {
		
	}

	public void OnPointerDown(PointerEventData eventData) {
		
	}

	public void OnPointerClick (PointerEventData eventData) {
		
		uiSignal.Dispatch(buttonName, UiEvents.Click, new Dictionary<string, object>{{"AttachedObject", attachedObject}, {"PointerEventData", eventData}});
	}

	public void init(){
		
		if (customName == null || customName.Length == 0)
			buttonName = gameObject.name;
		else
			buttonName = customName;

		if (attachedObject == null) {
			attachedObject = gameObject;
		}
	}
}
