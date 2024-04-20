using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

using UnityEngine.EventSystems;

using strange.extensions.dispatcher.eventdispatcher.api;
using strange.extensions.mediation.impl;
using strange.extensions.signal.impl;
using strange.extensions.context.api;

using app;

public class SwitchView : View {

	public int id;
	public GameObject backLight;
	public GameObject brightText1;
	public GameObject brightText2;
	public GameObject brightText3;
	public GameObject dimText1;
	public GameObject dimText2;
	public GameObject dimText3;
	public GameObject brightIcon;
	public GameObject dimIcon;
	public GameObject dimIconText;
	public GameObject brightIconText;
	public bool isMomentary;

	public void init()
	{

	}

}