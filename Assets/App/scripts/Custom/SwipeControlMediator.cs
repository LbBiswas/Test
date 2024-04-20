using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.mediation.impl;
using app;

public class SwipeControlMediator : Mediator {

	[Inject]
	public SwipeControlView view { get; set; }

	[Inject]
	public UiSignal uiSignal { get; set;}

	[Inject]
	public SystemResponseSignal systemReponseSignal {get; set;}

	[Inject]
	public SystemRequestSignal systemRequestSignal {get; set;}

	[Inject]
	public Utils utils{ get; set; }

	private float startDragTime;
	private float stopDragTime;

	private Vector2 stopPosition;
	private Vector2 startPosition;

	private int index = 0;

	private Vector2 initialPosition;

	public void systemResponseSignalListener(string key, Dictionary<string, object> data)
	{

	}

	IEnumerator AnimatePanel()
	{
		float diffTime = Time.realtimeSinceStartup - stopDragTime;
		//Debug.Log ("diffTime " + diffTime);
		float totalTime = 0.3f;
		while(diffTime < totalTime)
		{
			view.swipePanel.localPosition = Vector2.Lerp (startPosition, stopPosition, diffTime/totalTime);

			yield return new WaitForSeconds(0.015f);
			diffTime = Time.realtimeSinceStartup - stopDragTime;
			//Debug.Log ("diffTime " + diffTime);
		}

		Debug.Log (stopPosition);
		view.swipePanel.localPosition = stopPosition;

		systemRequestSignal.Dispatch(SystemRequestEvents.UpdateSourceId, new Dictionary<string, object>{{"Id", index}, {"Type", UiEvents.EndDrag}});

	}

	private bool isInit = false;

	//PointerEventData
	//AttachedObject
	public void uiSignalListener(string key, string type, Dictionary<string, object> data) {

		//Debug.Log ("Key: " + key);

		if (!key.Contains ("Button") && !key.Contains ("Background")) {
			return;
		}

		switch (type) {
		case UiEvents.Drag:
			{
				PointerEventData stopEventData = utils.getValueForKey<PointerEventData> (data, "PointerEventData");

				if (stopEventData != null) {
					Vector2 delta = stopEventData.pressPosition - stopEventData.position;
					if (Mathf.Abs(delta.x) > 90) {	
						view.swipePanel.localPosition = new Vector2 ((index * -990) + initialPosition.x - delta.x, initialPosition.y);
					}
				}
			}
				break;
		case UiEvents.BeginDrag:

			if(!isInit)
			{
				isInit = true;
				initialPosition = view.swipePanel.localPosition;
			}
			startDragTime = Time.realtimeSinceStartup;

			systemRequestSignal.Dispatch(SystemRequestEvents.UpdateSourceId, new Dictionary<string, object>{{"Id", index}, {"Type", UiEvents.BeginDrag}});

			break;
		case UiEvents.EndDrag:
			{
				Debug.Log ("End Drag...");

				PointerEventData stopEventData = utils.getValueForKey<PointerEventData> (data, "PointerEventData");

				stopDragTime = Time.realtimeSinceStartup;

				if (stopDragTime > startDragTime && stopEventData != null) {
					Vector2 delta = stopEventData.pressPosition - stopEventData.position;

					//Debug.Log ("delta.x: " + delta.x);

					if (Mathf.Abs (delta.x) > 90) {

						//int lastIndex = index;

						if (delta.x > 0) {
							Debug.Log ("Swipe Left...");
							index++;
							if (index > 3)
								index = 3;
						} else {
							Debug.Log ("Swipe Right...");
							index--;
							if (index < 0)
								index = 0;
						}

						startPosition = new Vector2 (view.swipePanel.localPosition.x, initialPosition.y);
						Debug.Log (startPosition);
						stopPosition = new Vector2 ((index * -990) + initialPosition.x, initialPosition.y);
						Debug.Log (stopPosition);

						StopCoroutine ("AnimatePanel");
						stopDragTime = Time.realtimeSinceStartup;
						StartCoroutine ("AnimatePanel");

						systemRequestSignal.Dispatch (SystemRequestEvents.UpdateSourceId, new Dictionary<string, object>{ { "Id", index } });
					} else {
						
						startPosition = new Vector2 (view.swipePanel.localPosition.x, initialPosition.y);
						Debug.Log (startPosition);
						stopPosition = new Vector2 ((index * -990) + initialPosition.x, initialPosition.y);
						Debug.Log (stopPosition);

						StopCoroutine ("AnimatePanel");
						stopDragTime = Time.realtimeSinceStartup;
						StartCoroutine ("AnimatePanel");

					}

				}
			}

			break;
		}

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
