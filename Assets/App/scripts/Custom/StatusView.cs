using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

using UnityEngine.EventSystems;

using strange.extensions.dispatcher.eventdispatcher.api;
using strange.extensions.mediation.impl;
using strange.extensions.signal.impl;
using strange.extensions.context.api;

using app;
using TMPro;

public class StatusView : View {

	public TextMeshProUGUI text;
	public bool isTemperature;
	public int id;

	public void init()
	{

	}

}