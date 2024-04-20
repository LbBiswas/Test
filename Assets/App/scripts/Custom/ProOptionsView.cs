using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

using UnityEngine.EventSystems;

using strange.extensions.dispatcher.eventdispatcher.api;
using strange.extensions.mediation.impl;
using strange.extensions.signal.impl;
using strange.extensions.context.api;

using app;

//public class ProSwitchStatus {
//	public bool isOnStart = false;
//	public int swOnTimer = 0;
//}

public class ProSeriesStatus {
	public bool isAppProEnabled = false;
	public bool isPro = false;
	public bool isAutoSync = true;
	public bool isSyncFromApp = false;
	public bool isDisableDeepSleep = false;
	public bool deviceIsCompatible = false;
	public bool needsSync = false;
	public bool isInputLinking = false;
//	public ProSwitchStatus[32];
}


public static class ProInfo {
	// update "first" defines to latest non-pro version of firmware
	public const int FIRST_PRO_BANTAM 			= 0x0137;
	public const int FIRST_PRO_TOUCHSCREEN		= 0x0124;
	public const int FIRST_PRO_SWITCH_HD 		= 0x0128;

	public const int NOT_COMPATIBLE				= -1;
	public const int NOT_CONNECTED				= 0;
	public const int NEED_UPDATE				= 1;
	public const int IS_COMPATIBLE				= 2;
	public const int IS_CHECKING				= 3;

}

public class ProOptionsView : View {

	public Text proCompatibleMessage;
	public Toggle proDisableDeepSleep;
	public Text proConnectionMessage;
	public Toggle proEnableSync;
	public Toggle proSyncFromDev;
	public Toggle proSyncFromApp;
	public GameObject proSyncNowButton;
	public GameObject proResetSettings;
	public GameObject proDisableProButton;
	public GameObject proOkButton;
	public GameObject proDisableSyncButton;

	public GameObject proNameChangeButton;
	public GameObject proDisableNameButton;

	public Toggle proEnableInputLink;

	public void init(){

	}

}