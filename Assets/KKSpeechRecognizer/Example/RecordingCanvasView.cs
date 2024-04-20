using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

using System;


using UnityEngine.EventSystems;

using strange.extensions.dispatcher.eventdispatcher.api;
using strange.extensions.mediation.impl;
using strange.extensions.signal.impl;
using strange.extensions.context.api;

using app;

using KKSpeech;

public class RecordingCanvasView : View {

	[Inject]
	public SystemResponseSignal systemResponseSignal { get; set;}

	public Text phraseText;

	private string _resultText;

	private string resultText{
		get{
			return _resultText;
		}

		set{
			phraseText.text = value;
			_resultText = value;
			systemResponseSignal.Dispatch(SystemResponseEvents.VoicePhrase, new Dictionary<string, object>{{SystemResponseEvents.VoicePhrase, _resultText}});

		}
	}

	float lastUpdateTime = 0;

	int subIndex = 0;

	string phrase = "";

	bool isStarted = false;

	bool isInit = false;

	IEnumerator DetectSilence(){

		if (isInit) {
			resultText = "One moment...";
			isInit = true;
			yield return new WaitForSeconds (2.5f);
		}

		if (SpeechRecognizer.IsRecording()) {
			resultText = "One moment...";
			SpeechRecognizer.StopIfRecording();
			yield return new WaitForSeconds (2.5f);
		}

		SpeechRecognizer.StartRecording(true);

		isStarted = true;

		phrase = "";
		resultText = "";
		subIndex = 0;
		lastUpdateTime = Time.realtimeSinceStartup;

		while (isStarted) {
			while (phrase.Length < 200 || Time.realtimeSinceStartup - lastUpdateTime < 3.0) {

				if (Time.realtimeSinceStartup - lastUpdateTime > 1.0) {
					if (phrase.Length > subIndex) {
						resultText = phrase.Substring (subIndex);
						subIndex = phrase.Length;
					}

				}

				yield return new WaitForSeconds (0.1f);
			}

			if (SpeechRecognizer.IsRecording()) {
				SpeechRecognizer.StopIfRecording();
			}

			if (!isStarted)
				yield break;


			yield return new WaitForSeconds (2.5f);
			lastUpdateTime = Time.realtimeSinceStartup;

			phrase = "";
			resultText = "";
			subIndex = 0;
			SpeechRecognizer.StartRecording(true);
		}

	}

	public void StopDetection(){
		isStarted = false;
		StopCoroutine ("DetectSilence");
		resultText = "";
		if (SpeechRecognizer.IsRecording ()) {
			SpeechRecognizer.StopIfRecording ();
		}
	}

	public void StartDetection() {
		isStarted = true;
		if (SpeechRecognizer.ExistsOnDevice()) {
			SpeechRecognizerListener listener = GameObject.FindObjectOfType<SpeechRecognizerListener>();
			listener.onAuthorizationStatusFetched.AddListener(OnAuthorizationStatusFetched);
			listener.onAvailabilityChanged.AddListener(OnAvailabilityChange);
			listener.onErrorDuringRecording.AddListener(OnError);
			listener.onErrorOnStartRecording.AddListener(OnError);
			listener.onFinalResults.AddListener(OnFinalResult);
			listener.onPartialResults.AddListener(OnPartialResult);
			listener.onEndOfSpeech.AddListener(OnEndOfSpeech);

			if (SpeechRecognizer.GetAuthorizationStatus() == KKSpeech.AuthorizationStatus.Authorized) {
				StopCoroutine ("DetectSilence");
				StartCoroutine ("DetectSilence");
			} else {
				SpeechRecognizer.RequestAccess();
			}

		} else {
			resultText = "Sorry, but this device doesn't support speech recognition";
		}



	}

	private void processResult(string result){
		lastUpdateTime = Time.realtimeSinceStartup;
		phrase = result;
	}

	public void OnFinalResult(string result) {
		if (Application.platform == RuntimePlatform.Android) {
			resultText = result;
		}
		processResult (result);
	}

	public void OnPartialResult(string result) {
		processResult (result);
	}

	public void OnAvailabilityChange(bool available) {
		if (!available) {
			resultText = "Speech Recognition not available";
		} else {
			resultText = "Say something :-)";
		
			StopCoroutine ("DetectSilence");

			if (isStarted) {
				StopCoroutine ("DetectSilence");
				StartCoroutine ("DetectSilence");
			}
		}
	}

	public void OnAuthorizationStatusFetched(AuthorizationStatus status) {
		switch (status) {
		case AuthorizationStatus.Authorized:

			StopCoroutine ("DetectSilence");

			if (isStarted) {
				StopCoroutine ("DetectSilence");
				StartCoroutine ("DetectSilence");
			}

			break;
		default:
			
			resultText = "Cannot use Speech Recognition, authorization status is " + status;
			break;
		}
	}

	public void OnEndOfSpeech() {

		isInit = true;
		StopCoroutine ("DetectSilence");
		StartCoroutine ("DetectSilence");
	}

	public void OnError(string error) {
		Debug.LogError(error);
		resultText = "Something went wrong... Try again! \n [" + error + "]";

		StopCoroutine ("DetectSilence");

		if (!error.ToString ().ToLower ().Contains ("quota")) {

			if (isStarted) {
				StartCoroutine ("DetectSilence");
			}

		} else {
			resultText = "Quota limit... try again later...";
		}
	}

	public void init(){

	}
}