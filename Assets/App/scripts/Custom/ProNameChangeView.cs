using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

using UnityEngine.EventSystems;

using strange.extensions.dispatcher.eventdispatcher.api;
using strange.extensions.mediation.impl;
using strange.extensions.signal.impl;
using strange.extensions.context.api;

using app;



public class ProNameChangeView : View {


	public GameObject proCancelButton;
	public GameObject proDefaultButton;
	public GameObject proApplyButton;

	public Text proCurrentName;
	public InputField proNewName;

	public void init(){

	}

}