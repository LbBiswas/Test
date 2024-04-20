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
using Kakera;

public class GlobalView : View {

		[Inject]
		public SystemRequestSignal systemRequestSignal {get; set;}

	public Signal voicePhraseChange  = new Signal();


	public Signal textChange = new Signal ();


	public Signal useLegendChange = new Signal();

	//[Header("Switches")]
	//[Space(5)]

	public GameObject switchSetupPanel;
	public TextMeshProUGUI[] sourceTitles;
	public GameObject flashStrobeButton;
	public GameObject offRoadButton;
	public Toggle isDimmable;
	public Toggle isMomentary;
	public Toggle canStrobe;
	public Toggle canFlash;
	public Slider red;
	public Slider green;
	public Slider blue;
	public InputField label1;
	public InputField label2;
	public InputField label3;
	public Image brightIcon;
	public Toggle isLegend;
	public Image backLight;

	public GameObject sourceSetupPanel;
	public InputField sourceLabel;
	public Image sourceBackground;
	public Image sourceTempBackground;
	public Slider sourceRed;
	public Slider sourceGreen;
	public Slider sourceBlue;
	public Toggle isCelcius;

	public Toggle isBantamLowPower;
	public GameObject LowPowerPanel;

	public Slider[] dimmers;
	public TextMeshProUGUI[] percents;

	public Text passkey;

	public SwitchView setupSwitchView;

	public GameObject deviceSelectonPanel;
	public GameObject deviceSelectonItem;
	public GameObject iconSelectionPanel;
	public TMP_FontAsset deviceListFont;

	public GameObject switchHDSetupPanel;
	public GameObject sourceSESetupPanel;
	public GameObject rcpmSetupPanel;

	public GameObject otaDeviceSelectPanel;
	public GameObject otaUpgradePanel;

	public Text otaUpgradeMessage;
	public Text otaUpgradeHeader;
    public GameObject otaCancelButton;

    public GameObject otaButtonText1;
	public GameObject otaButtonText2;

	public GameObject otaOkButtonText1;
	public GameObject otaOkButtonText2;

	public GameObject bleErrPanel;
	public Text bleErrMessage;

	public GameObject supportKeyObject;
	public InputField supportKeyInput;

	public Text versionNum;
	public Text debugText;

	public Text connDevMessage;
	public Text connDevName;

	public GameObject[] swPos;

	// pro

	public GameObject[] proLogo;

	public GameObject proSeriesButton;

	public GameObject proPurchasePanel;
	public GameObject proOptionsPanel;
	public GameObject proNamePanel;
	public GameObject proVideoPanel;
	 
	public GameObject proOptionsButton;

	public GameObject stdSwitchOptions;
	public GameObject stdFlashTog;
	public GameObject proStrobeOptions;
	public Text proStrobeReadout;
	public Slider StrobeOnSlider;
	public Slider StrobeOffSlider;

	public GameObject proSwitchOptions;
	public Toggle isAutoOn;
	public Toggle isIgnCtrl;
	public Toggle isLockout;
	public InputField switchTimer;
	public Text proOptionsNote;
	public Toggle isInputLatching;
	public InputField currentLimit;
	public Toggle isCurrentRestart;
	public Toggle isInputEnabled;
	public Toggle isInputLockout;
	public Toggle isInputLockInvert;

	public GameObject EnterPasskeyPanel;

	public GameObject[] sources;

	[HideInInspector]
	public RecordingCanvasView recordingCanvas;

	public void init()
	{
		recordingCanvas = gameObject.GetComponent<RecordingCanvasView> ();
	}

	public void updateText(){
		
		textChange.Dispatch ();
	}

	public void updateLegend(){
		useLegendChange.Dispatch ();
	}

	[SerializeField]
	public Unimgpicker imagePicker;




}