using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

using UnityEngine.EventSystems;

using strange.extensions.dispatcher.eventdispatcher.api;
using strange.extensions.mediation.impl;
using strange.extensions.signal.impl;
using strange.extensions.context.api;

using app;



public class ProPurchaseView : View {

	public Text proCompatibleMessage;
	public GameObject proCancelButton;
	public GameObject proRestoreButton;
	public GameObject proPurchaseButton;

	public Text proConnectionMessage;

	public void init(){

	}

}