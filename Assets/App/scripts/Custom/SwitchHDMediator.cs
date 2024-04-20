using UnityEngine;
//using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.mediation.impl;
using app;

public class SwitchHDMediator : Mediator {

	[Inject]
	public SwitchHDView view { get; set; }

	[Inject]
	public UiSignal uiSignal { get; set;}

	[Inject]
	public SystemResponseSignal systemReponseSignal {get; set;}

	[Inject]
	public SystemRequestSignal systemRequestSignal {get; set;}

	[Inject]
	public Utils utils{ get; set; }

	public void systemResponseSignalListener(string key, Dictionary<string, object> data){
		switch (key) {
		case SystemResponseEvents.SystemData:

			float[] switchHDColors = utils.getValueForKey<float[]> (data, "switchHDColors");
			int switchHDSource = utils.getValueForKey<int> (data, "switchHDSource");
			int switchHDTimer = utils.getValueForKey<int> (data, "switchHDTimer");

			bool isSwitchHDWake = true; 

			if (data.ContainsKey("switchHDWake"))
			{
				isSwitchHDWake = utils.getValueForKey<bool>(data, "switchHDWake");
			}

			view.redSlider.value = switchHDColors [0];
			view.greenSlider.value = switchHDColors [1];
			view.blueSlider.value = switchHDColors [2];
			view.indicatorSlider.value = switchHDColors [3];

			view.timer.text = switchHDTimer.ToString ();

                view.isWakeFromeIgn.isOn = isSwitchHDWake;


                for (int i = 0; i < view.sources.Length; i++) {
//				Debug.Log ("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%");
//				Debug.Log ("SwitchHD color: " + i + ", " + switchHDColors [i]);
				view.sources[i].color = new Color (1.0f, 0.0f, 0.0f, 0.0f);
			}
	

			view.sources[switchHDSource].color = new Color (1.0f, 0.0f, 0.0f, 1.0f);


			break;
		}
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void uiSignalListener(string key, string type, Dictionary<string, object> data) {

		//Debug.Log("SwitchHDMediator: Key = " + key + ", Type = " + type);


		if (type == UiEvents.MouseDown)
		{
			if (key.Contains("Switch HD Source "))// && type == UiEvents.MouseDown)
			{

				for (int i = 0; i < view.sources.Length; i++)
				{
					view.sources[i].color = new Color(1.0f, 0.0f, 0.0f, 0.0f);
				}

				switch (key)
				{
					case "Switch HD Source 1":
						view.sources[0].color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
						break;
					case "Switch HD Source 2":
						view.sources[1].color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
						break;
					case "Switch HD Source 3":
						view.sources[2].color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
						break;
					case "Switch HD Source 4":
						view.sources[3].color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
						break;
				}
			}
		}
		else if (type == UiEvents.Click)
		{
			switch (key)
			{
				case "Switch HD Setup Ok":

					bool isWake = view.isWakeFromeIgn.isOn;
					//int switchHDTimer = Int32.Parse(view.timer.text);

					//if(!string.IsNullOrEmpty(view.timer.text) && IsDigitsOnly(view.timer.text)) {
					//	Int32 val = Int32.Parse(view.timer.text);
					//	switchHDTimer = val;
					//} else
					//{
					//	switchHDTimer = 0;
					//}

					systemRequestSignal.Dispatch(SystemRequestEvents.SwitchHdSettings, new Dictionary<string, object> {
						{ "switchHDWake", isWake },
						//{ "switchHDTimer", switchHDTimer },
					});

					break;
			}
		}


	}

	// Use this for initialization
	override public void OnRegister(){


		view.init ();

		systemReponseSignal.AddListener(systemResponseSignalListener);
		uiSignal.AddListener (uiSignalListener);

		for (int i = 0; i < view.sources.Length; i++) {
			view.sources[i].color = new Color (1.0f, 0.0f, 0.0f, 0.0f);
		}


	}

	override public void OnRemove()
	{

		if(systemReponseSignal != null)
			systemReponseSignal.RemoveListener(systemResponseSignalListener);

		if (uiSignal != null)
			uiSignal.RemoveListener (uiSignalListener);
	}
}
