using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

using UnityEngine.EventSystems;

using strange.extensions.dispatcher.eventdispatcher.api;
using strange.extensions.mediation.impl;
using strange.extensions.signal.impl;
using strange.extensions.context.api;

using app;



public class EnterPasskeyView : View {


	public GameObject passkeyCancelButton;
	public GameObject passkeyApplyButton;

	public Text currentName;
	public InputField passkeyField;

	public void init(){
		
	}

}