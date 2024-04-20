using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;


using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.mediation.impl;
using app;

public class SwitchMediator : Mediator {

	[Inject]
	public SwitchView view { get; set; }

	[Inject]
	public UiSignal uiSignal { get; set;}

	[Inject]
	public SystemResponseSignal systemReponseSignal {get; set;}

	[Inject]
	public SystemRequestSignal systemRequestSignal {get; set;}

	[Inject]
	public Utils utils {get; set;}


	private int myId = -1;

	private string buttonName = "";

	private bool stopEvents = false;

	private bool isSetup = false;
	private bool isLink = false;

	private SwitchStatus thisSwitch = new SwitchStatus ();

	float lastInteractonTime = 0;

	public void setId(int newId)
	{
		myId = newId;
	}

	//private bool needsPrint = true;

	private void updateView(bool isOn)
	{
		if (isOn) {

			if (thisSwitch.isLegend) {

				view.dimText1.SetActive (false);
				view.dimText2.SetActive (false);
				view.dimText3.SetActive (false);

				view.brightText1.SetActive (false);
				view.brightText2.SetActive (false);
				view.brightText3.SetActive (false);

				view.dimIcon.SetActive (false);
				view.brightIcon.SetActive (true);

				view.dimIconText.SetActive (false);
				view.brightIconText.SetActive(true);

			} else {
				view.dimText1.SetActive (false);
				view.dimText2.SetActive (false);
				view.dimText3.SetActive (false);

				view.brightText1.SetActive (true);
				view.brightText2.SetActive (true);
				view.brightText3.SetActive (true);

				view.dimIcon.SetActive (false);
				view.brightIcon.SetActive (false);

				view.dimIconText.SetActive (false);
				view.brightIconText.SetActive(false);
			}


			view.backLight.SetActive (true);


		} else {

			if (thisSwitch.isLegend) {

				view.dimText1.SetActive (false);
				view.dimText2.SetActive (false);
				view.dimText3.SetActive (false);

				view.brightText1.SetActive (false);
				view.brightText2.SetActive (false);
				view.brightText3.SetActive (false);

				view.dimIcon.SetActive (true);
				view.brightIcon.SetActive (false);

				view.dimIconText.SetActive (true);
				view.brightIconText.SetActive(false);

			} else {
				view.dimText1.SetActive (true);
				view.dimText2.SetActive (true);
				view.dimText3.SetActive (true);

				view.brightText1.SetActive (false);
				view.brightText2.SetActive (false);
				view.brightText3.SetActive (false);

				view.dimIcon.SetActive (false);
				view.brightIcon.SetActive (false);

				view.dimIconText.SetActive (false);
				view.brightIconText.SetActive(false);
			}


			view.backLight.SetActive (false);

		}
	}

	public void systemResponseSignalListener(string key, Dictionary<string, object> data){

		switch (key) {
		case SystemResponseEvents.OutputStatus:
			OutputStatus status = utils.getValueForKey<OutputStatus> (data, "OutputStatus");

			float currentTime = Time.realtimeSinceStartup;

			if (status != null && status.id == myId  && (currentTime - lastInteractonTime) > 2.0f) {
				updateView (status.status > 0 ? true : false);
			}

			break;
		case SystemResponseEvents.SetupOff:
			isSetup = false;
			break;

		case SystemResponseEvents.SetupOn:
			isSetup = true;
			break;
		case SystemResponseEvents.LinkOn:
			isLink = true;
			break;

		case SystemResponseEvents.LinkOff:
			isLink = false;
			break;
		case SystemResponseEvents.SystemData:


			SwitchStatus[] switches = utils.getValueForKey<SwitchStatus[]> (data, "switches");

			bool isTsStyleText = true;

			if (myId >= 0 && switches != null && switches.Length == 32) {

				thisSwitch = switches [myId];
				thisSwitch.id = myId;


//				view.brightIconText.GetComponent<TextMeshProUGUI> ().text = thisSwitch.label2;
//				view.dimIconText.GetComponent<TextMeshProUGUI> ().text = thisSwitch.label2;
					
				if (isTsStyleText && thisSwitch.label1 != string.Empty) {
					view.brightIconText.GetComponent<TextMeshProUGUI> ().text = thisSwitch.label1;
					view.dimIconText.GetComponent<TextMeshProUGUI> ().text = thisSwitch.label1;
				} else {
					view.brightIconText.GetComponent<TextMeshProUGUI> ().text = thisSwitch.label2;
					view.dimIconText.GetComponent<TextMeshProUGUI> ().text = thisSwitch.label2;
				}

				view.dimText1.GetComponent<TextMeshProUGUI> ().text = thisSwitch.label1;
				view.dimText2.GetComponent<TextMeshProUGUI> ().text = thisSwitch.label2;
				view.dimText3.GetComponent<TextMeshProUGUI> ().text = thisSwitch.label3;

				view.brightText1.GetComponent<TextMeshProUGUI> ().text = thisSwitch.label1;
				view.brightText2.GetComponent<TextMeshProUGUI> ().text = thisSwitch.label2;
				view.brightText3.GetComponent<TextMeshProUGUI> ().text = thisSwitch.label3;



				if (isTsStyleText)
				{

					float yMin1 = 104.8f;
					float yMin2 = 71.8f;
					float yMin3 = 38.9f;
					float yMin1_5 = 88.3f;
					float yMin2_5 = 55.4f;

					float yMax1 = -32.3f;
					float yMax2 = -67.2f;
					float yMax3 = -100.2f;
					float yMax1_5 = -49.8f;
					float yMax2_5 = -83.7f;

//					float xMin = 31.0f;
//					float xMax = -38.4f;

					RectTransform td1 = view.dimText1.GetComponent<RectTransform> ();
					RectTransform td2 = view.dimText2.GetComponent<RectTransform> ();
					RectTransform td3 = view.dimText3.GetComponent<RectTransform> ();

					RectTransform tb1 = view.brightText1.GetComponent<RectTransform> ();
					RectTransform tb2 = view.brightText2.GetComponent<RectTransform> ();
					RectTransform tb3 = view.brightText3.GetComponent<RectTransform> ();

					if (thisSwitch.label3 == string.Empty && thisSwitch.label1 != string.Empty) {

						if (thisSwitch.label2 == string.Empty) {
							
							td1.offsetMin = new Vector2 (td1.offsetMin.x, yMin2);
							td1.offsetMax = new Vector2 (td1.offsetMax.x, yMax2);

						} else {

							td1.offsetMin = new Vector2 (td1.offsetMin.x, yMin1_5);
							td1.offsetMax = new Vector2 (td1.offsetMax.x, yMax1_5);

							td2.offsetMin = new Vector2 (td1.offsetMin.x, yMin2_5);
							td2.offsetMax = new Vector2 (td1.offsetMax.x, yMax2_5);
						}

					} else {

						td1.offsetMin = new Vector2 (td1.offsetMin.x, yMin1);
						td1.offsetMax = new Vector2 (td1.offsetMax.x, yMax1);

						td2.offsetMin = new Vector2 (td1.offsetMin.x, yMin2);
						td2.offsetMax = new Vector2 (td1.offsetMax.x, yMax2);

						td3.offsetMin = new Vector2 (td1.offsetMin.x, yMin3);
						td3.offsetMax = new Vector2 (td1.offsetMax.x, yMax3);
					}


					tb1.offsetMin = new Vector2 (tb1.offsetMin.x, td1.offsetMin.y);
					tb1.offsetMax = new Vector2 (tb1.offsetMax.x, td1.offsetMax.y);

					tb2.offsetMin = new Vector2 (tb2.offsetMin.x, td2.offsetMin.y);
					tb2.offsetMax = new Vector2 (tb2.offsetMax.x, td2.offsetMax.y);

					tb3.offsetMin = new Vector2 (tb2.offsetMin.x, td3.offsetMin.y);
					tb3.offsetMax = new Vector2 (tb2.offsetMax.x, td3.offsetMax.y);
				}



				view.brightIcon.GetComponent<Image> ().sprite = thisSwitch.sprite;
				view.dimIcon.GetComponent<Image> ().sprite = thisSwitch.sprite;

				//view.backLight.GetComponent<Image> ().color = new Color (thisSwitch.red, thisSwitch.green, thisSwitch.blue);

				view.gameObject.transform.GetComponent<Image>().color = new Color (thisSwitch.red, thisSwitch.green, thisSwitch.blue);

				updateView (thisSwitch.isOn);

			}
			break;
		case SystemResponseEvents.SwitchUpdate:
			SwitchStatus tempSwitch = utils.getValueForKey<SwitchStatus> (data, "SwitchStatus");

			if (tempSwitch != null) {
				if (tempSwitch.id == myId) {
					updateView (tempSwitch.isOn);
				}
			}


			break;
		}

	}
	bool didHandleInMouseDown = false;
	IEnumerator StartMouseDown()
	{
		yield return new WaitForSeconds (0.1f);

		if(stopEvents)
			yield break;


		if (isSetup && !isLink) {
			didHandleInMouseDown = true;
			systemRequestSignal.Dispatch (SystemRequestEvents.ShowSwitchSetup, new Dictionary<string, object>{{"Id", myId}});
		} else {
			

			if (thisSwitch.isMomentary) {
				thisSwitch.isOn = true;
			} else {
				thisSwitch.isOn = !thisSwitch.isOn;
				didHandleInMouseDown = true;
			}

			systemRequestSignal.Dispatch (SystemRequestEvents.SwitchUpdate, new Dictionary<string, object>{ {
					"SwitchStatus",
					thisSwitch
				} });




		}

	}

	public void uiSignalListener(string key, string type, Dictionary<string, object> data) {

		if (buttonName == key) {

			lastInteractonTime = Time.realtimeSinceStartup;

			switch (type) {
			case UiEvents.BeginDrag:
				stopEvents = true;
				break;
			case UiEvents.MouseDown:
				stopEvents = false;
				didHandleInMouseDown = false;
				StopCoroutine ("StartMouseDown");
				StartCoroutine ("StartMouseDown");
				break;
			
			case UiEvents.MouseUp:
				StopCoroutine ("StartMouseDown");
					bool needsSend = false;
				if (!didHandleInMouseDown) {
					if (isSetup && !isLink) {
						systemRequestSignal.Dispatch (SystemRequestEvents.ShowSwitchSetup, new Dictionary<string, object>{ { "Id", myId } });
					} else {
						thisSwitch.isOn = !thisSwitch.isOn;
						needsSend = true;
					}

				}

				if (thisSwitch.isMomentary && thisSwitch.isOn) {
					thisSwitch.isOn = false;
					needsSend = true;
				}

				if (needsSend)
				{
					systemRequestSignal.Dispatch(SystemRequestEvents.SwitchUpdate, new Dictionary<string, object>{ {
						"SwitchStatus",
						thisSwitch
					} });
				}

				break;
			}
		
		}

	}

	// Use this for initialization
	override public void OnRegister(){


		view.init ();


		int? i = utils.getIntFromString (view.gameObject.transform.parent.gameObject.name);
		int? j = utils.getIntFromString (view.gameObject.transform.parent.gameObject.transform.parent.gameObject.transform.parent.gameObject.name);

		if (i != null && j != null) {
			myId = (int)((j * 8) + i);

		}

		view.id = myId;
		thisSwitch.id = myId;
		buttonName = "Button " + myId;

		ButtonView buttonView = gameObject.GetComponent<ButtonView> ();

		if (buttonView != null) {
			buttonView.setName (buttonName);
		}

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
