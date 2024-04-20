using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

using UnityEngine.EventSystems;

using strange.extensions.dispatcher.eventdispatcher.api;
using strange.extensions.mediation.impl;
using strange.extensions.signal.impl;
using strange.extensions.context.api;

using TMPro;

using app;

public class ButtonView : View {
	public string toolTip;
	public float tipTimeSeconds = 2.5f;
	public string customName;
	public GameObject attachedObject; 
	public bool announcePresence = false;
	public bool isToggle = false;
	public Image indicator;
	public GameObject dim;
	public ScrollRect scrollRect;
	public bool isDebug = false;
	public bool isAndroidHide = false;


	public UiSignal uiSignal = new UiSignal();

	public System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch ();

	bool isHover = false;
	bool isOn = false;

	Vector3 lastMouse = Vector3.zero;

	private bool wasDraged = false;

	[HideInInspector]
	public string buttonName = "Unknown";

	public void mouseEnter(PointerEventData eventData){
		timer.Start ();
		isHover = true;
		lastMouse = Input.mousePosition;
	}

	public void mouseExit(PointerEventData eventData){
		timer.Reset ();
		if (toolTip != null && toolTip.Length > 0) {
			uiSignal.Dispatch ("ToolTip", null, new Dictionary<string, object> {
				{ "AttachedObject", attachedObject },
				{
					"Message",
					toolTip
				},
				 {
					"IsVisable",
					false
				},
				 {
					"PointerEventData",
					eventData
				}
			});
		}
		isHover = false;
	}

	public void mouseDown(PointerEventData eventData){
		timer.Reset ();
		timer.Start ();
		uiSignal.Dispatch(buttonName, UiEvents.MouseDown, new Dictionary<string, object>{{"AttachedObject", attachedObject}, {"PointerEventData", eventData}});
		if (indicator != null) {
			indicator.gameObject.SetActive (true);
		}

		if (dim != null) {
			dim.SetActive(false);
		}

	}

	public void mouseUp(PointerEventData eventData){
		timer.Reset ();
		timer.Start ();
		uiSignal.Dispatch(buttonName, UiEvents.MouseUp, new Dictionary<string, object>{{"AttachedObject", attachedObject}, {"PointerEventData", eventData}});
		if (indicator != null) {
			indicator.gameObject.SetActive (false);
		}

		if (dim != null) {
			dim.SetActive(true);
		}

	}

	public void beginDrag(PointerEventData eventData){
		timer.Reset ();
		timer.Start ();
		uiSignal.Dispatch(buttonName, UiEvents.BeginDrag, new Dictionary<string, object>{{"AttachedObject", attachedObject}, {"PointerEventData", eventData}});

		if (isDebug) {
			Debug.Log ("beginDrag: " + gameObject.name);
		}

		if (scrollRect != null) {
			scrollRect.OnBeginDrag (eventData);
			wasDraged = true;
		}
	}

	public void endDrag(PointerEventData eventData)
	{
		timer.Reset ();
		timer.Start ();
		uiSignal.Dispatch(buttonName, UiEvents.EndDrag,  new Dictionary<string, object>{{"AttachedObject", attachedObject}, {"PointerEventData", eventData}});

		if (isDebug) {
			Debug.Log ("endDrag " + gameObject.name);
		}

		if (scrollRect != null) {
			scrollRect.OnEndDrag (eventData);
		}
	}

	public void drag(PointerEventData eventData)
	{
		timer.Reset ();
		timer.Start ();
		uiSignal.Dispatch(buttonName, UiEvents.Drag, new Dictionary<string, object>{{"AttachedObject", attachedObject}, {"PointerEventData", eventData}});

		if (isDebug) {
			Debug.Log ("drag " + gameObject.name);
		}

		if (scrollRect != null) {
			scrollRect.OnDrag (eventData);
		}
	}

	public void setOn(bool _isOn){
		isOn = _isOn;

		if (isOn) {
			if (indicator != null) {
				indicator.gameObject.SetActive (true);
			}

			if (dim != null) {
				dim.SetActive(false);
			}

		} else {
			if (indicator != null) {
				indicator.gameObject.SetActive (false);
			}

			if (dim != null) {
				dim.SetActive(true);
			}
		}
	}

	public void click(PointerEventData eventData)
	{
		timer.Reset ();
		timer.Start ();

		if (isDebug) {
			Debug.Log ("click " + gameObject.name);
		}

		if (wasDraged) {
			wasDraged = false;
			return;
		}

		if (isToggle) {

			isOn = !isOn;

			if (isOn) {
				if (indicator != null) {
					indicator.gameObject.SetActive (true);
				}

				if (dim != null) {
					dim.SetActive(false);
				}

				uiSignal.Dispatch(buttonName + " On", UiEvents.Click, new Dictionary<string, object>{{"AttachedObject", attachedObject}, {"PointerEventData", eventData}});
			} else {
				if (indicator != null) {
					indicator.gameObject.SetActive (false);
				}

				if (dim != null) {
					dim.SetActive(true);
				}
				uiSignal.Dispatch(buttonName + " Off", UiEvents.Click, new Dictionary<string, object>{{"AttachedObject", attachedObject}, {"PointerEventData", eventData}});
			}

		} else {
			uiSignal.Dispatch(buttonName, UiEvents.Click, new Dictionary<string, object>{{"AttachedObject", attachedObject}, {"PointerEventData", eventData}});
		}

	}

	public void setName(string name)
	{
		buttonName = name;
		customName = name;
	}


	private void findScrollRect(GameObject currentObject){

		ScrollRect scrollRectTemp = currentObject.GetComponent<ScrollRect> ();

		if (isDebug) {
			Debug.Log ("findScrollRect: Looking - " + currentObject.name);
		}

		if (scrollRectTemp != null) {
			
			scrollRect = scrollRectTemp;

			if (isDebug) {
				Debug.Log ("findScrollRect: Found!");
			}

		} else {

			if (currentObject.transform.parent != null) {

				if (isDebug) {
					Debug.Log ("findScrollRect: Checking parent... ");
				}

				findScrollRect (currentObject.transform.parent.gameObject);
			} else {
				if (isDebug) {
					Debug.Log ("findScrollRect: Not Found!");
				}
			}
		}
	}

#if UNITY_ANDROID && !UNITY_EDITOR_OSX
	public void OnEnable()
	{
		if (isAndroidHide)
		{
			gameObject.SetActive(false);
		}
	}
#endif

	public void init()
	{
		if (customName == null || customName.Length == 0)
			buttonName = gameObject.name;
		else
			buttonName = customName;

		timer.Reset ();

		EventTrigger trigger = gameObject.GetComponent<EventTrigger> ();

		if (trigger == null) {
			trigger = gameObject.AddComponent<EventTrigger> ();
		}

		EventTrigger.Entry pointerEnter = new EventTrigger.Entry ();
		pointerEnter.eventID = EventTriggerType.PointerEnter;
		pointerEnter.callback.AddListener ((eventData) => {
			mouseEnter ((PointerEventData)eventData);
		});
		trigger.triggers.Add (pointerEnter);

		EventTrigger.Entry pointerExit = new EventTrigger.Entry ();
		pointerExit.eventID = EventTriggerType.PointerExit;
		pointerExit.callback.AddListener ((eventData) => {
			mouseExit ((PointerEventData)eventData);
		});
		trigger.triggers.Add (pointerExit);

		EventTrigger.Entry pointerDown = new EventTrigger.Entry ();
		pointerDown.eventID = EventTriggerType.PointerDown;
		pointerDown.callback.AddListener ((eventData) => {
			mouseDown ((PointerEventData)eventData);
		});
		trigger.triggers.Add (pointerDown);

		EventTrigger.Entry pointerUp = new EventTrigger.Entry ();
		pointerUp.eventID = EventTriggerType.PointerUp;
		pointerUp.callback.AddListener ((eventData) => {
			mouseUp ((PointerEventData)eventData);
		});
		trigger.triggers.Add (pointerUp);

		EventTrigger.Entry beginDragEvent = new EventTrigger.Entry ();
		beginDragEvent.eventID = EventTriggerType.BeginDrag;
		beginDragEvent.callback.AddListener ((eventData) => {
			beginDrag ((PointerEventData)eventData);
		});
		trigger.triggers.Add (beginDragEvent);

		EventTrigger.Entry endDragEvent = new EventTrigger.Entry ();
		endDragEvent.eventID = EventTriggerType.EndDrag;
		endDragEvent.callback.AddListener ((eventData) => {
			endDrag ((PointerEventData)eventData);
		});
		trigger.triggers.Add (endDragEvent);

		EventTrigger.Entry dragEvent = new EventTrigger.Entry ();
		dragEvent.eventID = EventTriggerType.Drag;
		dragEvent.callback.AddListener ((eventData) => {
			drag ((PointerEventData)eventData);
		});
		trigger.triggers.Add (dragEvent);

		EventTrigger.Entry clickEvent = new EventTrigger.Entry ();
		clickEvent.eventID = EventTriggerType.PointerClick;
		clickEvent.callback.AddListener ((eventData) => {
			click ((PointerEventData)eventData);
		});
		trigger.triggers.Add (clickEvent);

		if (scrollRect == null) {
			findScrollRect (this.gameObject);
		}

	}

	private string _text;

	public string text{
		get{ return _text;}
		set{
			_text = value;

			TextMeshProUGUI[] texts = gameObject.GetComponentsInChildren<TextMeshProUGUI> (true);

			for (int i = 0; i < texts.Length; i++) {
				texts [i].text = _text;
			}
		}
	}

	// Update is called once per frame
	void Update () {

		if(toolTip != null && toolTip.Length > 0)
		{
			if (timer.ElapsedMilliseconds >= tipTimeSeconds * 1000) {
				uiSignal.Dispatch("ToolTip", null, new Dictionary<string, object>{{"Message", toolTip},{"IsVisable", true}});
			}

			if (isHover) {
				if (lastMouse.x != Input.mousePosition.x || lastMouse.y != Input.mousePosition.y) {
					timer.Reset ();
					timer.Start ();
					uiSignal.Dispatch("ToolTip", null, new Dictionary<string, object>{{"Message", toolTip},{"IsVisable", false}});

					lastMouse = Input.mousePosition;
				}
			}
		}
	}
}
