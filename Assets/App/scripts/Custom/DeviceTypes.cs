using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace app
{
	public static class sPODDeviceTypes{
		public const int Uninit = -1;
		public const int Bantam = 0;
		public const int Touchscreen = 1;
		public const int SwitchHD = 2;
		public const int SourceSE = 3;
		public const int RCPM = 4;
		public const int SwitchHDv2 = 5;
		public const int OTABootloader = 6;
		public const int SourceLT = 7;

	}

	public static class sPOD_BLE {
		
		public const string SPOD_SERVICE 		= "7E3AF9EC-8C0D-447B-8404-E99F6056685B";

		public const string COMM_CHAR 			= "B9064764-4C00-4C6F-A65B-EC02646FC6F4";
		public const string PASSKEY_CHAR 		= "1B9E52FB-F15C-45B7-8E28-73EE03267486";
		public const string OTA_CHAR 			= "DF8993A5-FE93-439E-A6E6-60978E741675";
		public const string PRO_CHAR 			= "024125C1-9BAF-43A4-BFD9-7722CCC9B8CB";
		public const string TOUCH_CHAR 			= "FB05C238-D6B9-4FA2-8027-BA1F0C884361";
		public const string SECURE_CHAR			= "A0BA57D9-7B58-4536-82E8-CCA1CBFD6CDE";

	}
}
