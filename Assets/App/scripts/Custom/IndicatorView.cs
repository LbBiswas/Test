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

public class IndicatorView : View {

	public int id;
	public RectTransform indicator;
	public TextMeshProUGUI label;
	public GameObject enableObject;

	public void init()
	{

	}

}