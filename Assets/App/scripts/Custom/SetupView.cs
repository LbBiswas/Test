using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

using UnityEngine.EventSystems;

using strange.extensions.dispatcher.eventdispatcher.api;
using strange.extensions.mediation.impl;
using strange.extensions.signal.impl;
using strange.extensions.context.api;

using app;

public class SetupView : View {

	public GameObject setupIndicator;
	public bool isFlash = true;
	public bool isFlashTouchable = false;
	public bool isInverted = false;
	public int mask = 0;
	public bool debug = false;
	public bool isFlashButton = false;
	public bool isDefaultOff = false;


	public void init()
	{

	}

}