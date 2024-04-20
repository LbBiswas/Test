using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections;
using System.Linq;

//#if PLATFORM_ANDROID
//using UnityEngine.Android;
//#endif

//using UnityEditor;
//using UnityEditor.Callbacks;


using strange.extensions.context.api;
using strange.extensions.command.impl;

using startechplus.ble;




namespace app
{

    public class AppStartCommand : Command
    {

        [Inject]
        public IBluetoothLeService ble { get; set; }

        [Inject]
        public BluetoothLeEventSignal bleSignal { get; set; }

        [Inject]
        public SystemResponseSignal systemResponseSignal { get; set; }


        [Inject]
        public SystemRequestSignal systemRequestSignal { get; set; }

        [Inject]
        public UiSignal uiSignal { get; set; }

        [Inject]
        public IRoutineRunner routineRunner { get; set; }

        [Inject]
        public Utils utils { get; set; }

        private System.Diagnostics.Stopwatch packetTimer = new System.Diagnostics.Stopwatch();

        static public bool isAppProEnabledFlag = true;          // set false before releaseing pre-pro versions
        static public bool isAppProAndroidFlag = false;          // set true to enable "pro" mode on android
        static public bool isAppDefaultProFlag = false;          // set true to default app to pro, and skip "purchase"
        static public bool isEnableGlobalOTA = true;            // set true to allow "restore" anytime, not just when update needed
        static public bool isEnableCustomIcons = true;          // allows transmission of "images" to touchscreen instead of just default icons

        private String currentBleState = "Idle";
        private String nextBleState = "Idle";


        private int deviceId = sPODDeviceTypes.Uninit;//0;
        private String[] deviceNames = { "sPOD", "Star Tech" };//, "OTA Bootloader"};

        //		private String[] deviceService =  {"7E3AF9EC-8C0D-447B-8404-E99F6056685B", 
        //										"7E3AF9EC-8C0D-447B-8404-E99F6056685B", 
        //										"5C689EB4-FF2E-11E5-86AA-5E5517507C66", 
        //										"F25D5EA0-C3E4-4F7A-AFD4-3FB22E9634E5", 
        //										"F25D5EA0-C3E4-4F7A-AFD4-3FB22E9634E5", 
        //										"7E3AF9EC-8C0D-447B-8404-E99F6056685B", 
        //										"00060000-F8CE-11E4-ABF4-0002A5D5C51B", };

        private String[] deviceService =  {sPOD_BLE.SPOD_SERVICE, 						// BantamX
											sPOD_BLE.SPOD_SERVICE, 						// Touchscreen
											"5C689EB4-FF2E-11E5-86AA-5E5517507C66", 	// Switch HD V1
											"F25D5EA0-C3E4-4F7A-AFD4-3FB22E9634E5", 	// Source SE
											"F25D5EA0-C3E4-4F7A-AFD4-3FB22E9634E5", 	// RCPM
											sPOD_BLE.SPOD_SERVICE, 						// Switch HD V2
											"00060000-F8CE-11E4-ABF4-0002A5D5C51B",
                                            sPOD_BLE.SPOD_SERVICE, 						// SourceLT
		};  // bootloader "device"




        //		private String[,] deviceCharacteristic = new string[7,4]
        //		{{"B9064764-4C00-4C6F-A65B-EC02646FC6F4", "1B9E52FB-F15C-45B7-8E28-73EE03267486", "DF8993A5-FE93-439E-A6E6-60978E741675", ""}, 
        //			{"B9064764-4C00-4C6F-A65B-EC02646FC6F4", "1B9E52FB-F15C-45B7-8E28-73EE03267486", "DF8993A5-FE93-439E-A6E6-60978E741675", "FB05C238-D6B9-4FA2-8027-BA1F0C884361"}, 
        //			{"5C68A13E-FF2E-11E5-86AA-5E5517507C66", "", "", ""}, 
        //			{"3E9883BD-A699-4ECC-88B8-28DE32292DD8", "45A634DC-B675-4EC2-A1F9-8FDAFF8D17F5", "43ECE40F-412E-4F68-9062-3B7C4DED1580", ""},
        //			{"3E9883BD-A699-4ECC-88B8-28DE32292DD8", "45A634DC-B675-4EC2-A1F9-8FDAFF8D17F5", "43ECE40F-412E-4F68-9062-3B7C4DED1580", ""},
        //			{"B9064764-4C00-4C6F-A65B-EC02646FC6F4", "1B9E52FB-F15C-45B7-8E28-73EE03267486", "DF8993A5-FE93-439E-A6E6-60978E741675", ""},
        //			{"00060001-F8CE-11E4-ABF4-0002A5D5C51B", "", "", ""}};	// bootloader "device"

        private String[,] deviceCharacteristic = new string[8, 6]
        {   {sPOD_BLE.COMM_CHAR, sPOD_BLE.PASSKEY_CHAR, sPOD_BLE.OTA_CHAR, sPOD_BLE.PRO_CHAR, sPOD_BLE.SECURE_CHAR, ""},
            {sPOD_BLE.COMM_CHAR, sPOD_BLE.PASSKEY_CHAR, sPOD_BLE.OTA_CHAR, sPOD_BLE.PRO_CHAR, sPOD_BLE.SECURE_CHAR, sPOD_BLE.TOUCH_CHAR},
            {"5C68A13E-FF2E-11E5-86AA-5E5517507C66", "", "", "", "", ""},
            {"3E9883BD-A699-4ECC-88B8-28DE32292DD8", "45A634DC-B675-4EC2-A1F9-8FDAFF8D17F5", "43ECE40F-412E-4F68-9062-3B7C4DED1580", "", "", ""},
            {"3E9883BD-A699-4ECC-88B8-28DE32292DD8", "45A634DC-B675-4EC2-A1F9-8FDAFF8D17F5", "43ECE40F-412E-4F68-9062-3B7C4DED1580", "", "", ""},
            {sPOD_BLE.COMM_CHAR, sPOD_BLE.PASSKEY_CHAR, sPOD_BLE.OTA_CHAR, sPOD_BLE.PRO_CHAR, sPOD_BLE.SECURE_CHAR, ""},
            {"00060001-F8CE-11E4-ABF4-0002A5D5C51B", "", "", "", "", ""},
            {sPOD_BLE.COMM_CHAR, sPOD_BLE.PASSKEY_CHAR, sPOD_BLE.OTA_CHAR, sPOD_BLE.PRO_CHAR, sPOD_BLE.SECURE_CHAR, ""}
        };

        private int[] deviceCharacteristicCount = { 5, 6, 1, 3, 3, 5, 1, 5 };

        //private String proCharacteristic = sPOD_BLE.PRO_CHAR;
        //private String otaCharacteristic = sPOD_BLE.OTA_CHAR;
        //private String secCharacteristic = sPOD_BLE.SECURE_CHAR;
        //private String TouchScreenCharacterisitc = sPOD_BLE.TOUCH_CHAR;

        //private String passkeyCharacteristic = "1B9E52FB-F15C-45B7-8E28-73EE03267486";
        //private String touchscreenCharacteristic = "FB05C238-D6B9-4FA2-8027-BA1F0C884361";
        private String currentDevice = null;
        private String currentDeviceName = null;
        private String lastDeviceName = "null";
        private String lastDeviceId = "null";

        private bool foundAllDeviceCharacteristics = false;
        //		private bool foundPasskeyCharacteristic = false;
        //		private bool foundTouchscreenCharacteristic = false;
        private bool foundOtaCharacteristic = false;
        private bool foundProCharacteristic = false;
        private bool foundSecCharacteristic = false;

        private bool[] foundDeviceCharacteritics;

        private float[] switchHDColors = { 0.0f, 0.0f, 1.0f, 1.0f };
        private int switchHDTimer = 120;
        private int switchHDSource = 0;
        private int switchHDPin = 0;
        private bool switchHDWake = true;
        private int tempPin = -1;
        private bool switchHDUnpairRequest = false;
        private bool switchHDNeedsSaved = false;
        private int switchHDLinkIndex = 0;

        private int sourceSEPin = 0;
        private int sourceSEPinTemp = 0;
        private bool sourceSEPinRequest = false;

        private int currentSourceId = 0;

        private bool queueSent = true;

        private bool isSetup = false;

        private GameObject logo = null;

        private string byteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        private uint[] crc32_table = {
            0x00000000, 0x77073096, 0xee0e612c, 0x990951ba, 0x076dc419, 0x706af48f,
            0xe963a535, 0x9e6495a3, 0x0edb8832, 0x79dcb8a4, 0xe0d5e91e, 0x97d2d988,
            0x09b64c2b, 0x7eb17cbd, 0xe7b82d07, 0x90bf1d91, 0x1db71064, 0x6ab020f2,
            0xf3b97148, 0x84be41de, 0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7,
            0x136c9856, 0x646ba8c0, 0xfd62f97a, 0x8a65c9ec, 0x14015c4f, 0x63066cd9,
            0xfa0f3d63, 0x8d080df5, 0x3b6e20c8, 0x4c69105e, 0xd56041e4, 0xa2677172,
            0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b, 0x35b5a8fa, 0x42b2986c,
            0xdbbbc9d6, 0xacbcf940, 0x32d86ce3, 0x45df5c75, 0xdcd60dcf, 0xabd13d59,
            0x26d930ac, 0x51de003a, 0xc8d75180, 0xbfd06116, 0x21b4f4b5, 0x56b3c423,
            0xcfba9599, 0xb8bda50f, 0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924,
            0x2f6f7c87, 0x58684c11, 0xc1611dab, 0xb6662d3d, 0x76dc4190, 0x01db7106,
            0x98d220bc, 0xefd5102a, 0x71b18589, 0x06b6b51f, 0x9fbfe4a5, 0xe8b8d433,
            0x7807c9a2, 0x0f00f934, 0x9609a88e, 0xe10e9818, 0x7f6a0dbb, 0x086d3d2d,
            0x91646c97, 0xe6635c01, 0x6b6b51f4, 0x1c6c6162, 0x856530d8, 0xf262004e,
            0x6c0695ed, 0x1b01a57b, 0x8208f4c1, 0xf50fc457, 0x65b0d9c6, 0x12b7e950,
            0x8bbeb8ea, 0xfcb9887c, 0x62dd1ddf, 0x15da2d49, 0x8cd37cf3, 0xfbd44c65,
            0x4db26158, 0x3ab551ce, 0xa3bc0074, 0xd4bb30e2, 0x4adfa541, 0x3dd895d7,
            0xa4d1c46d, 0xd3d6f4fb, 0x4369e96a, 0x346ed9fc, 0xad678846, 0xda60b8d0,
            0x44042d73, 0x33031de5, 0xaa0a4c5f, 0xdd0d7cc9, 0x5005713c, 0x270241aa,
            0xbe0b1010, 0xc90c2086, 0x5768b525, 0x206f85b3, 0xb966d409, 0xce61e49f,
            0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4, 0x59b33d17, 0x2eb40d81,
            0xb7bd5c3b, 0xc0ba6cad, 0xedb88320, 0x9abfb3b6, 0x03b6e20c, 0x74b1d29a,
            0xead54739, 0x9dd277af, 0x04db2615, 0x73dc1683, 0xe3630b12, 0x94643b84,
            0x0d6d6a3e, 0x7a6a5aa8, 0xe40ecf0b, 0x9309ff9d, 0x0a00ae27, 0x7d079eb1,
            0xf00f9344, 0x8708a3d2, 0x1e01f268, 0x6906c2fe, 0xf762575d, 0x806567cb,
            0x196c3671, 0x6e6b06e7, 0xfed41b76, 0x89d32be0, 0x10da7a5a, 0x67dd4acc,
            0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5, 0xd6d6a3e8, 0xa1d1937e,
            0x38d8c2c4, 0x4fdff252, 0xd1bb67f1, 0xa6bc5767, 0x3fb506dd, 0x48b2364b,
            0xd80d2bda, 0xaf0a1b4c, 0x36034af6, 0x41047a60, 0xdf60efc3, 0xa867df55,
            0x316e8eef, 0x4669be79, 0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236,
            0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f, 0xc5ba3bbe, 0xb2bd0b28,
            0x2bb45a92, 0x5cb36a04, 0xc2d7ffa7, 0xb5d0cf31, 0x2cd99e8b, 0x5bdeae1d,
            0x9b64c2b0, 0xec63f226, 0x756aa39c, 0x026d930a, 0x9c0906a9, 0xeb0e363f,
            0x72076785, 0x05005713, 0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0x0cb61b38,
            0x92d28e9b, 0xe5d5be0d, 0x7cdcefb7, 0x0bdbdf21, 0x86d3d2d4, 0xf1d4e242,
            0x68ddb3f8, 0x1fda836e, 0x81be16cd, 0xf6b9265b, 0x6fb077e1, 0x18b74777,
            0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c, 0x8f659eff, 0xf862ae69,
            0x616bffd3, 0x166ccf45, 0xa00ae278, 0xd70dd2ee, 0x4e048354, 0x3903b3c2,
            0xa7672661, 0xd06016f7, 0x4969474d, 0x3e6e77db, 0xaed16a4a, 0xd9d65adc,
            0x40df0b66, 0x37d83bf0, 0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9,
            0xbdbdf21c, 0xcabac28a, 0x53b39330, 0x24b4a3a6, 0xbad03605, 0xcdd70693,
            0x54de5729, 0x23d967bf, 0xb3667a2e, 0xc4614ab8, 0x5d681b02, 0x2a6f2b94,
            0xb40bbe37, 0xc30c8ea1, 0x5a05df1b, 0x2d02ef8d
        };

        private uint crc32(uint crc, byte[] buff, int size)
        {
            int p = 0;

            crc = crc ^ ~0U;

            while (size-- > 0)
            {
                crc = crc32_table[(crc ^ buff[p]) & 0xFF] ^ (crc >> 8);
                p++;
            }

            return crc ^ ~0U;
        }

        int bleFoundDelimiter = 0;
        int bleBufferIndex = 0;
        int blePacketLength = 0;
        byte[] bleBuffer = new byte[512];

        private int getSwitchIndex(byte address)
        {
            int index = -1;

            switch (address)
            {
                case 0x08:
                    {//switch 1
                        index = 0;
                        break;
                    }
                case 0x10:
                    {//switch 2
                        index = 1;
                        break;
                    }
                case 0x20:
                    {//switch 3
                        index = 2;
                        break;
                    }
                case 0x40:
                    {//switch 4
                        index = 3;
                        break;
                    }
                case 0x80:
                    {//switch 5
                        index = 4;
                        break;
                    }
                case 0x01:
                    {//switch 6
                        index = 5;
                        break;
                    }
                case 0x02:
                    {//switch 7
                        index = 6;
                        break;
                    }
                case 0x04:
                    {//switch 8
                        index = 7;
                        break;

                    }
            }

            return index;
        }

        private byte getSwitchOneHot(int index)
        {
            byte address = 255;

            switch (index % 8)
            {
                case 0:
                    {//switch 1
                        address = 0x08;
                        break;
                    }
                case 1:
                    {//switch 2
                        address = 0x10;
                        break;
                    }
                case 2:
                    {//switch 3
                        address = 0x20;
                        break;
                    }
                case 3:
                    {//switch 4
                        address = 0x40;
                        break;
                    }
                case 4:
                    {//switch 5
                        address = 0x80;
                        break;
                    }
                case 5:
                    {//switch 6
                        address = 0x01;
                        break;
                    }
                case 6:
                    {//switch 7
                        address = 0x02;
                        break;
                    }
                case 7:
                    {//switch 8
                        address = 0x04;
                        break;
                    }
            }

            return address;
        }

        public int sizeOfMtu = 23;

        private bool passKeyWasShown = false;
        private bool didInitialSystemDataLoad = false;

        private bool needsSyncToApp = true;

        public void DetectLinkPacket(string characteristic, byte[] packetData)
        {

            //			Debug.Log ("char: " + characteristic);// + "," + deviceCharacteristic[deviceId, 2]);
            //			Debug.Log("data: " + byteArrayToString(packetData));

            //if (characteristic == deviceCharacteristic[deviceId, 0]){
            if (characteristic == sPOD_BLE.COMM_CHAR)
            {
                //Debug.Log ("BLE: Process packet..." + UnityEngine.Time.time);
                //Debug.Log ("Link packet: " + byteArrayToString(packetData) + ", " + packetData.Length);

                for (int i = 0; i < packetData.Length; i++)
                {
                    if (bleFoundDelimiter == 1)
                    {
                        if (blePacketLength == -1)
                        {
                            if (packetData[i] <= 32)
                            {
                                blePacketLength = packetData[i] + 2;
                                bleBuffer[bleBufferIndex] = packetData[i];
                                bleBufferIndex++;
                                //Debug.Log ("Found Packet Length... " + i + ", " + blePacketLength);
                            }
                            else
                            {
                                bleFoundDelimiter = 0;
                                //Debug.Log ("Error Packet Length... " + i + ", " + packetData[i]);
                            }
                        }
                        else
                        {
                            bleBuffer[bleBufferIndex] = packetData[i];
                            bleBufferIndex++;

                            if (bleBufferIndex == blePacketLength)
                            {

                                ProcessLinkPacket(blePacketLength);
                                bleFoundDelimiter = 0;
                            }
                        }
                    }
                    else
                    {
                        if (packetData[i] == 0x55)
                        {
                            bleBufferIndex = 0;
                            blePacketLength = -1;
                            bleFoundDelimiter = 1;
                            bleBuffer[bleBufferIndex] = packetData[i];
                            bleBufferIndex++;

                            //Debug.Log ("Found Delimiter... " + i);
                        }
                    }
                }
            }
            //else if (characteristic == deviceCharacteristic[deviceId, 1]){
            else if (characteristic == sPOD_BLE.PASSKEY_CHAR)
            {
                //Debug.Log ("passkey data: " + byteArrayToString(packetData) + ", " + packetData.Length);
                uint passkey = BitConverter.ToUInt32(packetData, 0);

                if (passkey > 0 && passkey <= 999999)
                {

                    if (deviceId == sPODDeviceTypes.SwitchHDv2 && (passkey == 001234 || passkey == 000000)) // unsecured mode, no need to display
                        return;

                    passKeyWasShown = true;
                    systemResponseSignal.Dispatch(SystemResponseEvents.Passkey, new Dictionary<string, object> { { SystemResponseEvents.Passkey, passkey } });
                }
            }
            //else if (characteristic == deviceCharacteristic[deviceId, 2]){
            else if (characteristic == sPOD_BLE.OTA_CHAR)
            {
                Debug.Log("OTA Packet: " + byteArrayToString(packetData) + ", " + packetData.Length);

                CurrentDevOTA.BoardId = (int)((packetData[10] << 8) | packetData[11]);
                CurrentDevOTA.BoardRev = (int)(packetData[13]);

                int appBoardId = (int)((packetData[2] << 8) | packetData[3]);
                int appBoardRev = (int)(packetData[5]);

                CurrentDevOTA.AppVer = (int)((packetData[8] << 8) | packetData[9]);
                CurrentDevOTA.StkVer = (int)((packetData[16] << 8) | packetData[17]);


                if (packetData.Length >= 20)
                {
                    sizeOfMtu = (int)((packetData[18] << 8) | packetData[19]);
                }

                CurrentDevOTA.RecDevOta = true;

                if ((packetData[1] & 0x04) != 0)
                {
                    CurrentDevOTA.isAddressRead = true;
                    CurrentDevOTA.addressRead = (byte)(0x01 << (packetData[1] & 0x03));
                }


                if (CurrentDevOTA.BoardId != appBoardId)
                {// || CurrentDevOTA.BoardRev != appBoardRev) {

                    Debug.Log("board mismatch: " + CurrentDevOTA.BoardId + "/" + appBoardId + ", " + CurrentDevOTA.BoardRev + "/" + appBoardRev);

                    CurrentDevOTA.AppVer = 0x0100;
                    //					CurrentDevOTA.StkVer = 0xFFFF;
                }

                routineRunner.StopCoroutine(getOtaDataInstance);

                checkDevVersion();

                sendProData();
            }
            //else if (characteristic == deviceCharacteristic[deviceId, 3]){	// pro packet
            else if (characteristic == sPOD_BLE.PRO_CHAR)
            {
                //Debug.Log ("Pro packet: " + byteArrayToString(packetData) + ", " + packetData.Length);

                ProcessProPacket(packetData);
            }
            else if (characteristic == sPOD_BLE.SECURE_CHAR)
            {
                //Debug.Log("security packet: " + byteArrayToString(packetData) + ", " + packetData.Length);

                ProcessSecurityPacket(packetData);
            }

        }

        private char[] textL1 = new char[10];
        private char[] textL2 = new char[10];
        private char[] textL3 = new char[10];
        private int lastIndex = 255;


        public void ProcessProPacket(byte[] packetData)
        {
            Debug.Log("ProcessProPacket: " + byteArrayToString(packetData));
            switch (deviceId)
            {
                case sPODDeviceTypes.Bantam:
                    {
                        if ((packetData[1] & 0xC0) != 0x00) // Device type == Bantam
                            return;

                        int opcode = packetData[1] & 0x3F;

                        if (opcode == 0)
                        {
                            if ((packetData[3] & 0x02) != 0)
                                ProStatus.isDisableDeepSleep = ((packetData[3] & 0x01) != 0 ? true : false);

                            if ((packetData[3] & 0x08) != 0)
                                ProStatus.isInputLinking = ((packetData[3] & 0x04) != 0 ? true : false);

                            savePro();
                        }
                        else if (opcode == 2 || opcode == 3)
                        {
                            int swIndex = packetData[3];

                            if (swIndex >= 0 && swIndex < 32)
                            {
                                switches[swIndex].proIsAutoOn = (packetData[4] & 0x10) > 0 ? true : false;
                                switches[swIndex].proIsIgnCtrl = (packetData[4] & 0x08) > 0 ? true : false;
                                switches[swIndex].proIsLockout = (packetData[4] & 0x04) > 0 ? true : false;
                                switches[swIndex].proIsInputLatch = (packetData[4] & 0x02) > 0 ? true : false;
                                switches[swIndex].proIsCurrentRestart = (packetData[4] & 0x01) > 0 ? true : false;

                                switches[swIndex].proOnTimer = packetData[5] << 8 | packetData[6];

                                switches[swIndex].proCurrentLimit = packetData[7];

                                if (opcode == 3)
                                {
                                    switches[swIndex].proIsInputEnabled = (packetData[8] & 0x04) != 0 ? true : false;
                                    switches[swIndex].proIsInputLockout = (packetData[8] & 0x02) != 0 ? true : false;
                                    switches[swIndex].proIsInputLockInvert = (packetData[8] & 0x01) != 0 ? true : false;

                                    if ((packetData[8] & 0x08) != 0)
                                    {
                                        links[swIndex] = (packetData[9] << 0) |
                                            (packetData[10] << 8) |
                                            (packetData[11] << 16) |
                                            (packetData[12] << 24);
                                    }
                                }
                                //packet.Add((byte)(links[swIndex] >> offset & 0xFF));

                                //if (packetData[0] > 8)
                                //{
                                //	int offset = swIndex & ~0x07;       // get the base 8 shift for different sources/addresses

                                //	int rxLinks = packetData[8] << offset;
                                //	int linkMask = ~(0xFF << offset);
                                //	int oldLink = links[swIndex];

                                //	links[swIndex] = (oldLink & linkMask) | rxLinks;

                                //	Debug.Log("update bantam Pro links: (" + swIndex + "/" + offset + ") orig: 0x" + oldLink.ToString("X") + ", new: 0x" + packetData[8].ToString("X") + ", mask: 0x" + linkMask.ToString("X") + ", upd: 0x" + links[swIndex].ToString("X"));
                                //}
                            }

                            //Debug.Log("ProcessProPacket.Bantam");

                            sendSystemData();
                            //save();


                            if (needsSyncToApp && (swIndex & 0x07) >= 7)
                            {       // if all 8 packets have come through
                                    //ProStatus.needsSync = false;
                                needsSyncToApp = false;
                                save();
                                sendTurnOnProPacket();
                            }

                        }
                        //else if ((packetData[1] & 0x3F) == 8)
                        //{
                        //	if (ProcessCommSwPacket())
                        //		ProStatus.needsSync = false;
                        //}
                    }
                    break;
                case sPODDeviceTypes.Touchscreen:
                    {
                        if ((packetData[1] & 0xC0) != 0x40) // Device type == Touchscreen
                            return;

                        if ((packetData[1] & 0x3F) == 0)
                        {
                            ProStatus.isDisableDeepSleep = (packetData[3] != 0 ? true : false);

                            savePro();

                        }
                        else if ((packetData[1] & 0x3F) == 2)//1)
                        {
                            //int index2 = packetData [2];
                            //int index = index2 / 2;
                            //bool isFirst = (index2 % 2) == 0 ? true : false;
                            int index3 = packetData[2];
                            int index = index3 / 3;
                            int typeIndex = index3 % 3;

                            bool needsParse = false;

                            switch (typeIndex)
                            {
                                case 0:
                                    {
                                        byte swSet = packetData[3];

                                        switches[index].isDimmable = (swSet & 0x01) > 0 ? true : false;
                                        switches[index].isMomentary = (swSet & 0x02) > 0 ? true : false;
                                        switches[index].canFlash = (swSet & 0x04) > 0 ? true : false;
                                        switches[index].canStrobe = (swSet & 0x08) > 0 ? true : false;

                                        int rxLink = 0;

                                        rxLink += packetData[5] << 0;
                                        rxLink += packetData[6] << 8;
                                        rxLink += packetData[7] << 16;
                                        rxLink += packetData[8] << 24;

                                        links[index] = rxLink;

                                        if (packetData[4] > 0)
                                        {
                                            int id = packetData[4] - 1;

                                            Debug.Log("Rec id = " + id);

                                            systemResponseSignal.Dispatch(SystemResponseEvents.ActivateIcon, new Dictionary<string, object>{
                                                    {"Index", index},
                                                    {"IconId", id},
                                                    {"Switches", switches},
                                                });

                                            Debug.Log("ProcessProPacket.Touchscreen1 ");
                                            sendSystemData();
                                        }
                                        else
                                        {
                                            switches[index].isLegend = false;
                                        }

                                        if (sizeOfMtu >= (39 + 3) && packetData.Length >= 39)
                                        {
                                            for (int i = 0; i < 10; i++)
                                            {
                                                textL1[i] = (char)packetData[9 + i];
                                                textL2[i] = (char)packetData[19 + i];
                                                textL3[i] = (char)packetData[29 + i];
                                            }

                                            needsParse = true;
                                        }

                                        sendSystemData();

                                        break;
                                    }
                                case 1:
                                    {
                                        if (sizeOfMtu >= (33 + 3) && packetData.Length >= 33)
                                        {
                                            for (int i = 0; i < 10; i++)
                                            {
                                                textL1[i] = (char)packetData[3 + i];
                                                textL2[i] = (char)packetData[13 + i];
                                                textL3[i] = (char)packetData[23 + i];
                                            }

                                            needsParse = true;
                                        }
                                        else
                                        {
                                            for (int i = 0; i < 10; i++)
                                            {
                                                textL1[i] = (char)packetData[3 + i];
                                            }

                                            for (int i = 0; i < 3; i++)
                                            {
                                                textL2[i] = (char)packetData[13 + i];
                                            }
                                        }
                                    }
                                    break;
                                case 2:
                                    {
                                        for (int i = 5; i < 10; i++)
                                        {
                                            textL2[i] = (char)packetData[3 + (i - 5)];
                                        }

                                        for (int i = 0; i < 10; i++)
                                        {
                                            textL3[i] = (char)packetData[8 + i];
                                        }

                                        needsParse = true;
                                    }
                                    break;
                                default:
                                    {
                                        Debug.Log("ProcessProPacket.TStiErr: " + typeIndex + "/" + index3);
                                        return;
                                    }
                            }


                            if (needsParse)
                            {

                                if (typeIndex == 0 || lastIndex == index)
                                {

                                    switches[index].label1 = textL1[0] == 0 ? string.Empty : new string(textL1);
                                    switches[index].label2 = textL2[0] == 0 ? string.Empty : new string(textL2);
                                    switches[index].label3 = textL3[0] == 0 ? string.Empty : new string(textL3);

                                    switches[index].isDirty = true;
                                    Debug.Log("ProcessProPacket.Touchscreen3");
                                    sendSystemData();

                                    if (index == 31)
                                    {
                                        needsSyncToApp = false;
                                        sendTurnOnProPacket();
                                        save();

                                    }
                                }

                                textL1 = new char[10];
                                textL2 = new char[10];
                                textL3 = new char[10];
                            }

                            lastIndex = index;
                        }

                    }
                    break;
                case sPODDeviceTypes.SwitchHDv2:
                    {
                        if ((packetData[1] & 0xC0) != 0x80) // Device type == Switch HD
                            return;

                        if ((packetData[1] & 0x3F) == 2)//packetData [2] == 2)// settings packet
                        {

                            currentSourceId = (int)packetData[3];
                            switchHDSource = (int)packetData[4];

                            switchHDColors[0] = getColorFloat(packetData[5]);       // red
                            switchHDColors[1] = getColorFloat(packetData[6]);       // green
                            switchHDColors[2] = getColorFloat(packetData[7]);       // blue
                            switchHDColors[3] = getColorFloat(packetData[8]);       // indicators

                            switchHDTimer = (int)packetData[9];

                            setSwitchTypeFromByte(packetData[10], switchHDSource);      // don't care... found in commSw
                            setSwitchDimmableFromByte(packetData[11], switchHDSource);

                            setSwitchStrobeOrFlashFromBytes(packetData[12], packetData[13], switchHDSource);
                            Debug.Log("ProcessProPacket.SwitchHDv2");

                            sendSystemData();
                            save();

                            ProStatus.isDisableDeepSleep = ((packetData[14] & 0x01) != 0 ? true : false);

                            if ((packetData[14] & 0x04) != 0)
                            {
                                switchHDWake = ((packetData[14] & 0x02) != 0 ? true : false);
                                Debug.Log("switchHDWake: " + switchHDWake);
                            }

                            savePro();

                        }
                        /*else if ((packetData[1] & 0x3F) == 3)//packetData [2] == 3)
						{

							int offset = (int)packetData [3];

							links [(offset * 8) + 0] = (int)packetData [4] >> (offset * 8);
							links [(offset * 8) + 1] = (int)packetData [5] >> (offset * 8);
							links [(offset * 8) + 2] = (int)packetData [6] >> (offset * 8);
							links [(offset * 8) + 3] = (int)packetData [7] >> (offset * 8);
							links [(offset * 8) + 4] = (int)packetData [8] >> (offset * 8);
							links [(offset * 8) + 5] = (int)packetData [9] >> (offset * 8);
							links [(offset * 8) + 6] = (int)packetData [10] >> (offset * 8);
							links [(offset * 8) + 7] = (int)packetData [11] >> (offset * 8);
								Debug.Log ("ProcessProPacket.SwitchHDv2b");
							sendSystemData ();
							save ();

							if (offset == 3) {
								needsSyncToApp = false;
								sendTurnOnProPacket ();
							}
						} // */
                        else if ((packetData[1] & 0x3F) == 8)
                        {
                            if (ProcessCommSwPacket())
                                ProStatus.needsSync = false;
                        }
                    }
                    break;
                default:
                    break;
            }

            bool ProcessCommSwPacket()
            {
                int index = packetData[3];

                int ver = packetData[4];
                if (ver == 0)
                {
                    int dim = packetData[5];
                    switches[index].value = (dim > 0 && dim < 254) ? dim : switches[index].value;

                    switches[index].isMomentary = (packetData[6] & 0x01) > 0 ? true : false;
                    switches[index].isDimmable = (packetData[6] & 0x02) > 0 ? true : false;
                    switches[index].canStrobe = (packetData[6] & 0x04) > 0 ? true : false;
                    switches[index].canFlash = (packetData[6] & 0x08) > 0 ? true : false;

                    if (packetData[7] != 0 && packetData[8] != 0)
                    {
                        switches[index].proStrobeOn = packetData[7];
                        switches[index].proStrobeOn = packetData[8];
                    }

                    links[index] = (packetData[9] << 0) |
                                    (packetData[10] << 8) |
                                    (packetData[11] << 16) |
                                    (packetData[12] << 24);
                }

                return (index >= 31);
            }
        }

        private bool didSecRead = false;
        private bool isSecDevUnsecured = false;
        private bool isSecAuthenticated = false;
        private bool isSecInPairing = false;

        public void ProcessSecurityPacket(byte[] packetData)
        {
            int packetLength = packetData[0];

            if (packetData.Length != packetLength)
            {
                Debug.Log("ProcessSecurityPacket length Error... " + packetLength + "/" + packetData.Length);
                return;
            }

            Debug.Log("ProcessSecurityPacket() " + byteArrayToString(packetData));

            uint calcCrc = crc32(0, packetData, packetLength - 4);

            uint tempCrc = 0;
            tempCrc = tempCrc | packetData[packetLength - 1];
            tempCrc = (tempCrc << 8) | packetData[packetLength - 2];
            tempCrc = (tempCrc << 8) | packetData[packetLength - 3];
            tempCrc = (tempCrc << 8) | packetData[packetLength - 4];

            if (tempCrc == calcCrc)
            {
                int typeIndex = packetData[1];

                switch (typeIndex)
                {
                    case 0:
                        {
                            didSecRead = true;

                            isSecDevUnsecured = packetData[2] > 0 ? true : false;
                            isSecAuthenticated = packetData[3] > 0 ? true : false;
                            isSecInPairing = packetData[4] > 0 ? true : false;

                            break;
                        }
                    default:
                        {
                            Debug.Log("ProcessSecurityPacket.Err: " + typeIndex);
                            return;
                        }
                }
            }
            else
            {
                Debug.Log("ProcessSecurityPacket crc Error...");
            }
        }

        public void DetectRCPMPacket(string characteristic, byte[] packetData)
        {

            Debug.Log("DetectRCPMPacket " + packetData.Length + ", " + byteArrayToString(packetData));

            for (int i = 0; i < 6; i++)
            {

                int mask = 1 << i;
                int state = (packetData[0] & mask) >> i;

                float currentTime = Time.realtimeSinceStartup;

                if (state != currentRCPMState[i] && (currentTime - lastSwitchPacketSent) > 1.0f)
                {


                    currentRCPMState[i] = state;

                    switches[i].isOn = state > 0 ? true : false;

                    OutputStatus output = new OutputStatus();
                    output.id = i;
                    output.value = state == 0 ? 0f : 1.0f;
                    output.status = state > 0 ? 1 : 0;

                    switches[output.id].isOn = output.status > 0 ? true : false;
                    //					switches [output.id].value = output.value;

                    systemResponseSignal.Dispatch(SystemResponseEvents.OutputStatus, new Dictionary<string, object>{ {
                            "OutputStatus",
                            output
                        } });


                }


            }

        }

        private float[] lastSwitchValue = { -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f };

        private float lastSwitchPacketSent = 0.0f;

        public void DetectSEPacket(string characteristic, byte[] packetData)
        {

            Debug.Log("DetectSEPacket " + packetData.Length + ", " + byteArrayToString(packetData));

            for (int j = 0; j < packetData.Length; j++)
            {
                for (int i = bleBuffer.Length - 1; i > 0; i--)
                {
                    bleBuffer[i] = bleBuffer[i - 1];
                }

                bleBuffer[0] = packetData[j];

                if ((bleBuffer[4] - 0x80) >= 0 && (bleBuffer[4] - 0x80) <= 3)
                {

                    int index = -1;

                    switch (bleBuffer[3])
                    {
                        case 0x08: //switch 1
                            index = 0;
                            break;
                        case 0x10: //switch 2
                            index = 1;
                            break;
                        case 0x20: //switch 3
                            index = 2;
                            break;
                        case 0x40: //switch 4
                            index = 3;
                            break;
                        case 0x80: //switch 5
                            index = 4;
                            break;
                        case 0x01: //switch 6
                            index = 5;
                            break;
                        case 0x02: //switch 7
                            index = 6;
                            break;
                        case 0x04: //switch 8
                            index = 7;
                            break;
                    }

                    float currentTime = Time.realtimeSinceStartup;

                    if (index > -1 && (currentTime - lastSwitchPacketSent) > 1.0f)
                    {


                        float currentValueTemp = ((float)bleBuffer[2] / 255.0f);

                        if (lastSwitchValue[index] == currentValueTemp)
                        {

                            OutputStatus output = new OutputStatus();
                            output.id = index;
                            output.value = currentValueTemp;
                            output.status = currentValueTemp > 0 ? 1 : 0;

                            switches[output.id].isOn = output.status > 0 ? true : false;
                            //							switches [output.id].value = output.value;

                            systemResponseSignal.Dispatch(SystemResponseEvents.OutputStatus, new Dictionary<string, object>{ {
                                    "OutputStatus",
                                    output
                                } });
                        }

                        lastSwitchValue[index] = currentValueTemp;


                    }
                }
                else if (bleBuffer[4] == 0xA0 && bleBuffer[1] == 0x56 && bleBuffer[0] == 0x54)
                {
                    byte voltage = bleBuffer[3];
                    int temp = (int)bleBuffer[2];

                    if (temp > 200)
                    {
                        temp = temp - 256;
                    }

                    StatusUpdate update = new StatusUpdate();
                    update.voltage = voltage;
                    update.temp = temp;
                    update.id = 0;

                    systemResponseSignal.Dispatch(SystemResponseEvents.StatusUpdate, new Dictionary<string, object> { { "StatusUpdate", update } });

                    update.id = 1;
                    systemResponseSignal.Dispatch(SystemResponseEvents.StatusUpdate, new Dictionary<string, object> { { "StatusUpdate", update } });

                    update.id = 2;
                    systemResponseSignal.Dispatch(SystemResponseEvents.StatusUpdate, new Dictionary<string, object> { { "StatusUpdate", update } });

                    update.id = 3;
                    systemResponseSignal.Dispatch(SystemResponseEvents.StatusUpdate, new Dictionary<string, object> { { "StatusUpdate", update } });
                }
            }
        }


        public void DetectHDPacket(string characteristic, byte[] packetData)
        {

            if (characteristic == deviceCharacteristic[deviceId, 0])
            {


                int i;

                for (i = 0; i < packetData.Length; i++)
                {
                    if (bleFoundDelimiter == 1)
                    {
                        if (blePacketLength == -1)
                        {
                            if (packetData[i] <= 32)
                            {
                                blePacketLength = packetData[i] + 2;
                                bleBuffer[bleBufferIndex] = packetData[i];
                                bleBufferIndex++;
                            }
                            else
                            {
                                bleFoundDelimiter = 0;
                            }
                        }
                        else
                        {
                            bleBuffer[bleBufferIndex] = packetData[i];
                            bleBufferIndex++;

                            if (bleBufferIndex == blePacketLength)
                            {
                                //Debug.Log("ProcessPacket...\n");
                                ProcessHDPacket(blePacketLength);
                                bleFoundDelimiter = 0;
                            }
                        }
                    }
                    else
                    {
                        if (packetData[i] == 0x55)
                        {
                            bleBufferIndex = 0;
                            blePacketLength = -1;
                            bleFoundDelimiter = 1;
                            bleBuffer[bleBufferIndex] = packetData[i];
                            bleBufferIndex++;
                        }
                    }
                }
            }
        }


        public void ProcessHDPacket(int packetLength)
        {


            uint calcCrc = crc32(0, bleBuffer, packetLength - 4);

            uint tempCrc = 0;
            tempCrc = tempCrc | bleBuffer[packetLength - 1];
            tempCrc = (tempCrc << 8) | bleBuffer[packetLength - 2];
            tempCrc = (tempCrc << 8) | bleBuffer[packetLength - 3];
            tempCrc = (tempCrc << 8) | bleBuffer[packetLength - 4];

            if (tempCrc == calcCrc)
            {

                if (bleBuffer[2] == 0)//CAN Packet
                {
                    if (((bleBuffer[3] & 0xF0) - 0x80) == 0 /*optionsModel.appSourceId*/)
                    {

                        int index = getSwitchIndex(bleBuffer[4]) + ((bleBuffer[3] & 0x0F) * 8);

                        //Debug.Log("---------------------------------------- " + activityTimer.ElapsedMilliseconds);


                        float currentTime = Time.realtimeSinceStartup;

                        if (index > -1 && (currentTime - lastSwitchPacketSent) > 1.0f)
                        {


                            float currentValueTemp = ((float)bleBuffer[5] / 255.0f);

                            //							Debug.Log ("Output State Update...");

                            OutputStatus output = new OutputStatus();
                            output.id = index;
                            output.value = currentValueTemp;
                            output.status = currentValueTemp > 0 ? 1 : 0;

                            switches[output.id].isOn = output.status > 0 ? true : false;
                            //							switches [output.id].value = output.value;

                            Debug.Log("HD packet rec: " + output.id + ", " + output.value + ", " + output.status);

                            systemResponseSignal.Dispatch(SystemResponseEvents.OutputStatus, new Dictionary<string, object>{ {
                                    "OutputStatus",
                                    output
                                } });

                        }
                    }
                    else if (bleBuffer[3] == 0xA0)
                    {
                        byte voltage = bleBuffer[4];
                        int temp = (int)bleBuffer[5];

                        if (temp > 200)
                        {
                            temp = temp - 256;
                        }

                        if ((bleBuffer[7] & 0xFC) == 0x80)
                        {
                            //voltage = voltage * 2;
                        }

                        //Debug.Log ("Status Update..." + voltage + ", " + temp);

                        StatusUpdate update = new StatusUpdate();
                        update.voltage = voltage;
                        update.temp = temp;
                        update.id = (int)bleBuffer[7];// & 0x0F;
                        systemResponseSignal.Dispatch(SystemResponseEvents.StatusUpdate, new Dictionary<string, object> { { "StatusUpdate", update } });
                    }

                }
                else if (bleBuffer[2] == 4)
                {
                    tempPin = bleBuffer[4];
                    tempPin = (tempPin << 8) | bleBuffer[3];

                    Debug.Log("Pairing..." + tempPin);
                }


            }
        }

        private const int CAN_SWITCH_PACKET = 0x80;
        private const int CAN_DEBUG_PACKET = 0xA0;
        private const int CAN_STATUS_PACKET = 0xB0;
        private const int CAN_PRO_PACKET = 0xC0;

        public void ProcessLinkPacket(int packetLength)
        {
            uint calcCrc = crc32(0, bleBuffer, packetLength - 4);

            uint tempCrc = 0;
            tempCrc = tempCrc | bleBuffer[packetLength - 1];
            tempCrc = (tempCrc << 8) | bleBuffer[packetLength - 2];
            tempCrc = (tempCrc << 8) | bleBuffer[packetLength - 3];
            tempCrc = (tempCrc << 8) | bleBuffer[packetLength - 4];

            if (tempCrc == calcCrc)
            {
                if (bleBuffer[2] == 0)
                {//CAN Packet
                    if (((bleBuffer[3] & 0xF0) - 0x80) == 0 /*optionsModel.appSourceId*/)
                    {   //switch packet

                        int index = getSwitchIndex(bleBuffer[4]) + ((bleBuffer[3] & 0x0F) * 8);

                        //Debug.Log("---------------------------------------- " + activityTimer.ElapsedMilliseconds);

                        float currentTime = Time.realtimeSinceStartup;

                        if (index > -1 && (currentTime - lastSwitchPacketSent) > 1.0f)
                        {

                            if (ProStatus.isPro)
                            {

                                int strobeOn = bleBuffer[6];
                                int strobeOff = bleBuffer[7];

                                if (strobeOn != 0 && strobeOff != 0 &&
                                    (switches[index].proStrobeOn != strobeOn || switches[index].proStrobeOff != strobeOff))
                                {

                                    switches[index].proStrobeOn = strobeOn;
                                    switches[index].proStrobeOff = strobeOff;
                                    Debug.Log("ProcessLinkPacket");
                                    sendSystemData();
                                }
                            }

                            float currentValueTemp = ((float)bleBuffer[5] / 255.0f);

                            //Debug.Log ("Output State Update...");

                            OutputStatus output = new OutputStatus();
                            output.id = index;
                            output.value = currentValueTemp;
                            output.status = currentValueTemp > 0 ? 1 : 0;

                            switches[output.id].isOn = output.status > 0 ? true : false;
                            //							switches [output.id].value = output.value;

                            systemResponseSignal.Dispatch(SystemResponseEvents.OutputStatus, new Dictionary<string, object>{ {
                                    "OutputStatus",
                                    output
                                } });

                        }
                    }
                    else if (bleBuffer[3] == 0xA0)
                    {       // system packet

                        byte voltage = bleBuffer[4];
                        int temp = (int)bleBuffer[5];

                        if ((bleBuffer[7] & 0xFC) == 0x80)
                        {
                            //							voltage = voltage * 2;
                        }

                        if (temp > 200)
                        {
                            temp = temp - 256;
                        }

                        //						Debug.Log ("Status Update..." + voltage + ", " + temp);

                        StatusUpdate update = new StatusUpdate();
                        update.voltage = voltage;
                        update.temp = temp;
                        update.id = (int)bleBuffer[7];// & 0x03;
                        systemResponseSignal.Dispatch(SystemResponseEvents.StatusUpdate, new Dictionary<string, object> { { "StatusUpdate", update } });

                        //onSystemUpdateSignal.Dispatch("Battery", voltage);
                        //onSystemUpdateSignal.Dispatch("Temp", temp);
                    }
                    else if (((bleBuffer[3] & 0xF0) - 0xB0) == 0 /*optionsModel.appSourceId*/)
                    {   // current packet
                        int index = getSwitchIndex(bleBuffer[4]) + ((bleBuffer[3] & 0x0F) * 8);

                        //Debug.Log("---------------------------------------- " + activityTimer.ElapsedMilliseconds);

                        systemResponseSignal.Dispatch(SystemResponseEvents.CurrentIndicatorOn, null);

                        if (index > -1 /*&& activityTimer.ElapsedMilliseconds > 1000*/)
                        {
                            //Debug.Log ("Output Current Update...");

                            // 0 to 40.5 amps 11 bits
                            float currentValueTemp = ((float)(((int)bleBuffer[6] << 8) | (int)bleBuffer[7]) / 2047.0f);

                            //Debug.Log (index + ", amps = " + currentValueTemp + ", " + (currentValueTemp * 40.5));

                            OutputCurrent output = new OutputCurrent();
                            output.id = index;
                            output.value = currentValueTemp;
                            output.status = bleBuffer[5];

                            systemResponseSignal.Dispatch(SystemResponseEvents.OutputCurrent, new Dictionary<string, object>{ {
                                    "OutputCurrent",
                                    output
                                } });

                        }
                    }
                    else if (((bleBuffer[3] & 0xF0) - 0xC0) == 0)
                    {   // pro can packet

                        int index = getSwitchIndex(bleBuffer[4]) + ((bleBuffer[3] & 0x0F) * 8);


                        if (index > -1 && index < 32)
                        {

                            // update input controls

                            switches[index].proIsInputEnabled = (bleBuffer[5] & 0x04) != 0 ? true : false;
                            switches[index].proIsInputLockout = (bleBuffer[5] & 0x02) != 0 ? true : false;
                            switches[index].proIsInputLockInvert = (bleBuffer[5] & 0x01) != 0 ? true : false;

                            //if((bleBuffer[5] & 0x80) != 0)
                            //{
                            //	ProStatus.isInputLinking = (bleBuffer[5] & 0x40) != 0 ? true : false;
                            //}

                            if ((bleBuffer[5] & 0x20) != 0)
                            {
                                int lh = (bleBuffer[6] << 8) | bleBuffer[7];
                                int tl = links[index];
                                int mask = 0x0000ffff;
                                int shift = ((bleBuffer[5] & 0xC0) == 0x40) ? 16 : 0;

                                links[index] = (tl & ~(mask << shift)) + (lh << shift);
                            }


                            //int mask = 0x01 << (index & 0x07);

                            //if ((bleBuffer[6] & mask) > 0 && (bleBuffer[5] & 0xC0) != 0x40)       //(bleBuffer[6] > 0)
                            //{
                            //                         int offset = index & ~0x07;       // get the base 8 shift for different sources/addresses

                            //                         int rxLinks = bleBuffer[6] << offset;
                            //                         int linkMask = ~(0xFF << offset);
                            //                         int oldLink = links[index];

                            //                         links[index] = (oldLink & linkMask) | rxLinks;

                            //  //                       Debug.Log("update bantam Pro links: (" + index + "/" + offset +
                            //		//") orig: 0x" + oldLink.ToString("X") +
                            //		//", new: 0x" + bleBuffer[6].ToString("X") +
                            //		//", mask: 0x" + linkMask.ToString("X") +
                            //		//", upd: 0x" + links[index].ToString("X"));
                            //                     }

                            sendSystemData();
                        }
                    }

                }
                else if (bleBuffer[2] == 2)
                {
                    if (deviceId == sPODDeviceTypes.Bantam)
                    {
                        bool isBantamLowPower = (bleBuffer[3] & 0x01) != 0;
                        //bool isBantamInputLinking = (bleBuffer[3] & 0x02) != 0;

                        systemResponseSignal.Dispatch(SystemResponseEvents.BantamLowPowerTog, new Dictionary<string, object> {
                            { "isBantamLowPowerCompat", true },
                            { "isBantamLowPower", isBantamLowPower },
                        });


                    }
                }
                else if (bleBuffer[2] == 4)
                {
                    /*
					tempPin = bleBuffer [4];
					tempPin = (tempPin << 8) | bleBuffer [3];

					Debug.Log ("Pairing..." + tempPin);
					*/
                }
            }
            else
            {
                Debug.Log("CRC Error...");
            }
        }

        public void bleDisconnectAsync()
        {
            Debug.Log("bleDisconnectAsync");
            nextBleState = "Disconnecting";
            //bleSignalHandler("Tick", null);

            if (disconnectDelayInstance != null)
            {
                routineRunner.StopCoroutine(disconnectDelayInstance);
            }
            disconnectDelayInstance = disconnectDelay();
            routineRunner.StartCoroutine(disconnectDelayInstance);
        }

        IEnumerator getPassKeyInstance;
        IEnumerator getPassKey()
        {
            float startTime = Time.realtimeSinceStartup;
            //systemResponseSignal.Dispatch(SystemResponseEvents.Passkey, new Dictionary<string, object>{{SystemResponseEvents.Passkey, (uint)0}});

            while (Time.realtimeSinceStartup - startTime < 10.0f)
            {
                yield return new WaitForSeconds(1.0f);

                //ble.WriteCharacteristic(currentDevice, deviceService, deviceCharacteristic, packet.ToArray(), packet.Count, true);

                if (isDevicePairable(deviceId))
                {
                    //ble.ReadCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 1]);
                    ble.ReadCharacteristic(currentDevice, deviceService[deviceId], sPOD_BLE.PASSKEY_CHAR);
                }
            }

            while (currentBleState == "Pairing")
            {
                yield return new WaitForSeconds(1.0f);
            }

            if (passKeyWasShown && !isDeviceUnsecured)
            {
                passKeyWasShown = false;

                bleDisconnectAsync();
                //ble.DisconnectFromPeripheral (currentDevice);
            }

            yield return new WaitForSeconds(30.0f);
            systemResponseSignal.Dispatch(SystemResponseEvents.Passkey, new Dictionary<string, object> { { SystemResponseEvents.Passkey, (uint)0 } });
            yield break;
        }

        IEnumerator scanTimeoutInstance;
        IEnumerator scanTimout()
        {

            float startTime = Time.realtimeSinceStartup;

            float timeout = 5.0f;

            //if (Application.platform == RuntimePlatform.Android && !string.IsNullOrEmpty(lastDeviceName) && !lastDeviceName.StartsWith("null"))
            if (Application.platform == RuntimePlatform.Android)
            {
                timeout = 10.0f;        // little longer for android to find freshly bonded devices
            }

            while (true)
            {
                yield return new WaitForSeconds(1.0f);
                if (Time.realtimeSinceStartup - startTime > timeout)
                {
                    if (deviceList.Count > 0)
                    {
                        bleSignalHandler(BluetoothLeEvents.ScanTimeout, null);
                        yield break;
                    }
                }
#if UNITY_ANDROID
                if (Time.realtimeSinceStartup - startTime > (timeout * 2))
                {
                    bleSignalHandler(BluetoothLeEvents.ScanTimeout, null);
                    yield break;
                }
#endif

            }

            //			yield break;
        }

        IEnumerator connectTimeoutInstance;
        IEnumerator connectTimout(float timeout)
        {

            float myTimeout;

            if (timeout <= 0)
            {
                myTimeout = 15.0f;
            }
            else
            {
                myTimeout = timeout;
            }

            yield return new WaitForSeconds(myTimeout);
            bleSignalHandler(BluetoothLeEvents.ConnectTimeout, null);

            yield break;
        }

        public DeviceOTA CurrentDevOTA = new DeviceOTA();
        //		public DeviceOTA AlternateDevOTA = new DeviceOTA();

        IEnumerator getOtaDataInstance;
        IEnumerator getOtaData()
        {

            Debug.Log("Start getOtaDataInstance");
            float startTime;// = Time.realtimeSinceStartup;

            yield return null;

            while (currentBleState == "Pairing")
            {
                yield return null;
            }

            if (currentBleState != "Connected")
            {
                yield break;
            }

            // --- connected ---

            startTime = Time.realtimeSinceStartup;

            if (!foundOtaCharacteristic)
            {

                CurrentDevOTA.NeedUpgrade = OTAInfo.NOT_COMPATIBLE;
                sendProData();
                yield break;
            }

            //ble.SubscribeToCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic [deviceId, 2], false);
            ble.SubscribeToCharacteristic(currentDevice, deviceService[deviceId], sPOD_BLE.OTA_CHAR, false);

            sendProData();

            while (Time.realtimeSinceStartup - startTime < 10.0f)
            {

                yield return new WaitForSeconds(1.0f);

                SendCheckOTAPacket();

                if (CurrentDevOTA.RecDevOta)
                {
                    Debug.Log("ota packet rec");
                    yield break;
                }

                if (currentDevice == null)
                {
                    yield break;
                }
            }

            Debug.Log("No ota packet recieved");



            CurrentDevOTA.NeedUpgrade = -1;
            CurrentDevOTA.RecDevOta = false;

            switch (deviceId)
            {
                case sPODDeviceTypes.Bantam:
                    CurrentDevOTA.BoardId = OTAInfo.BOARD_ID_BANTAM;
                    CurrentDevOTA.BoardRev = OTAInfo.BOARD_REV_BANTAM;
                    CurrentDevOTA.StkVer = 0xFFFF;
                    //				CurrentDevOTA.StkVer = 0x0100;
                    CurrentDevOTA.AppVer = 0x0100;
                    break;
                case sPODDeviceTypes.Touchscreen:
                    CurrentDevOTA.BoardId = OTAInfo.BOARD_ID_TOUCHSCREEN;
                    CurrentDevOTA.BoardRev = OTAInfo.BOARD_REV_LR_TOUCHSCREEN;
                    CurrentDevOTA.StkVer = 0xFFFF;
                    //				CurrentDevOTA.StkVer = 0x0100;
                    CurrentDevOTA.AppVer = 0x0100;
                    break;
                case sPODDeviceTypes.SwitchHDv2:
                    CurrentDevOTA.BoardId = OTAInfo.BOARD_ID_SWITCH_HD;
                    CurrentDevOTA.BoardRev = OTAInfo.BOARD_REV_SWITCH_HD;
                    CurrentDevOTA.StkVer = 0xFFFF;
                    //				CurrentDevOTA.StkVer = 0x0100;
                    CurrentDevOTA.AppVer = 0x0100;
                    break;
                case sPODDeviceTypes.SourceLT:
                    CurrentDevOTA.BoardId = OTAInfo.BOARD_ID_SOURCE_LT;
                    CurrentDevOTA.BoardRev = OTAInfo.BOARD_REV_SOURCE_LT;
                    CurrentDevOTA.StkVer = 0xFFFF;
                    CurrentDevOTA.AppVer = 0x0100;
                    break;
                default:
                    CurrentDevOTA.NeedUpgrade = 0;
                    break;
            }


            if (CurrentDevOTA.NeedUpgrade != 0)
            {
                checkDevVersion();

                if (CurrentDevOTA.NeedUpgrade < 0)
                {

                    Debug.Log("firmware incompatible for upgrade");

                }
            }

            sendProData();

            yield break;
        }

        IEnumerator otaTimeoutInstance;
        IEnumerator otaTimeout()
        {

            float startTime = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - startTime < 5.0f)
            {

                yield return new WaitForSeconds(1.0f);

            }

            Screen.sleepTimeout = SleepTimeout.SystemSetting;

            utils.showAlert("Uploading Firmware", "ERROR:\nDevice disconnected, double check that it is plugged in.", logo);

            yield break;

        }

        IEnumerator checkProDataInstance;
        IEnumerator checkProData()
        {

            if (!ProStatus.isAppProEnabled)
            {
                yield break;
            }

            //Debug.Log("Start checkProDataInstance")
            //float startTime = Time.realtimeSinceStartup;

            yield return null;

            while (currentBleState == "Pairing")
            {
                yield return null;
            }

            if (currentBleState != "Connected")
            {
                yield break;
            }

            // --- connected ---

            if (!foundProCharacteristic)
            {
                yield break;
            }


            //while (Time.realtimeSinceStartup - startTime < 1.0f) {	// make sure it's actually connecting and not just looking for passkey
            //	if (currentDevice == null) {
            //		yield break;
            //	}

            //	yield return new WaitForSeconds (0.1f);
            //}

            //ble.SubscribeToCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic [deviceId, 3], false);
            ble.SubscribeToCharacteristic(currentDevice, deviceService[deviceId], sPOD_BLE.PRO_CHAR, false);

            if (ProStatus.isPro)
            {       // send "turn on pro" command

                //				Debug.Log ("Turn on pro: " + deviceId);

                sendTurnOnProPacket();

                //				Debug.Log ("pro sent");

                yield return new WaitForSeconds(0.1f);

                //				Debug.Log ("pro sent wait");

                if (ProStatus.isAutoSync)
                {
                    Debug.Log("autosync: from app " + ProStatus.isSyncFromApp);
                    ProStatus.needsSync = true;
                    float timeWaitForLoad = 0.0f;

                    while (!didInitialSystemDataLoad)
                    {       // wait for saved settings to load to avoid overwriting
                        yield return new WaitForSeconds(0.1f);
                        timeWaitForLoad += 0.1f;

                        if (timeWaitForLoad > 10.0f)
                        {
                            Debug.Log("system data load timeout");
                            yield break;
                        }

                    }

                    while (!CurrentDevOTA.RecDevOta)
                    {       // wait for ota data to ensure compatability
                        yield return new WaitForSeconds(0.1f);
                        timeWaitForLoad += 0.1f;

                        if (timeWaitForLoad > 10.0f)
                        {
                            Debug.Log("ota data read timeout");
                            break;  // proceed without...
                        }

                    }

                    proSyncSettings();

                    if (needsSyncToApp)
                    {

                        float delayTime = 5.0f;

                        if (deviceId == sPODDeviceTypes.Bantam)
                        {
                            delayTime = 2.0f;
                        }

                        yield return new WaitForSeconds(delayTime);

                        if (needsSyncToApp)
                        {       // sync didn't come through, retry
                            proSyncSettings();
                            yield return new WaitForSeconds(delayTime);

                            if (needsSyncToApp)
                            {       // if still not sync, move on
                                needsSyncToApp = false;
                                sendTurnOnProPacket();
                            }
                        }
                    }

                }
                else
                {
                    needsSyncToApp = false;
                    sendTurnOnProPacket();      // turn on tempWrite
                }
            }

            yield break;
        }

        bool isDeviceSubscribed = false;
        bool isDeviceUnsecured = false;
        bool didPairingTimeout = false;

        IEnumerator checkSecDataInstance;
        IEnumerator checkSecData()
        {
            float startTime = Time.realtimeSinceStartup;

            Debug.Log("checkSecData(): start");

            yield return null;

            while (!foundSecCharacteristic)
            {
                yield return null;

                if (currentBleState != "Pairing")
                {
                    yield break;
                }

                if (Time.realtimeSinceStartup - startTime > 1.0f)
                {
                    //bleSignalHandler(BluetoothLeEvents.ConnectTimeout, null);   // no security char... move on
                    bleSignalHandler(BluetoothLeEvents.PairingTimeout, new Dictionary<string, object> {
                        { "timoutReason", "no security characteristic" },
                        { "PairingResult", "Timeout" }
                    });
                    yield break;
                }
            }

            Debug.Log("checkSecData(): SubscribeToCharacteristic");
            ble.SubscribeToCharacteristic(currentDevice, deviceService[deviceId], sPOD_BLE.SECURE_CHAR, false);

            didSecRead = false;
            startTime = Time.realtimeSinceStartup;

            while (!didSecRead)
            {
                if (currentBleState != "Pairing")
                {
                    Debug.Log("!didSecRead: currentBleState = " + currentBleState);
                    yield break;
                }

                if (Time.realtimeSinceStartup - startTime > 2.5f)
                {
                    bleSignalHandler(BluetoothLeEvents.PairingTimeout, new Dictionary<string, object> {
                        { "timoutReason", "no reponse to sendReadSecurityPacket()" },
                        { "PairingResult", "Timeout" }
                    });
                    yield break;
                }

                sendReadSecurityPacket();
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log("checkSecData(): didSecRead " + isSecAuthenticated + " " + isSecDevUnsecured + " " + isSecInPairing);

            if (isSecAuthenticated)     // only auth if standard pairing
            {
                bleSignalHandler(BluetoothLeEvents.PairingTimeout, new Dictionary<string, object> {
                    { "timoutReason", "Already authenticated" },
                    { "PairingResult", "PasskeyAccepted" }
                });
                yield break;
            }

            if (!isSecDevUnsecured)
            {
                bleSignalHandler(BluetoothLeEvents.PairingTimeout, new Dictionary<string, object> {
                    { "timoutReason", "Not Unsecured" },
                    { "PairingResult", "Secured" }
                });

                yield break;
            }

            isDeviceUnsecured = true;

            if (devicePasskeys.ContainsKey(currentDevice))
            {
                Debug.Log("ContainsKey for:" + currentDevice + " -> " + devicePasskeys[currentDevice]);

                SendSecurityPasskey(devicePasskeys[currentDevice]);

                didSecRead = false;
                startTime = Time.realtimeSinceStartup;

                while (!didSecRead)
                {
                    if (currentBleState != "Pairing")
                    {
                        Debug.Log("devicePasskeys: currentBleState = " + currentBleState);
                        yield break;
                    }

                    if (Time.realtimeSinceStartup - startTime > 1.0f)
                    {
                        bleSignalHandler(BluetoothLeEvents.PairingTimeout, new Dictionary<string, object> {
                            { "timoutReason", "no reponse to SendSecurityPasskey1()" + devicePasskeys[currentDevice]},
                            { "PairingResult", "Timeout" }
                        });
                        yield break;
                        //break;
                    }

                    //SendSecurityPasskey(devicePasskeys[currentDevice]);
                    yield return new WaitForSeconds(0.5f);
                }

                if (isSecAuthenticated)
                {
                    bleSignalHandler(BluetoothLeEvents.PairingTimeout, new Dictionary<string, object> {
                        { "timoutReason", "Saved key accepted" },
                        { "PairingResult", "PasskeyAccepted" }
                    });

                    yield break;
                }
                else
                {
                    Debug.Log("Saved passkey bad, removing...");
                    devicePasskeys.Remove(currentDevice);
                }
            }

            if (isSecInPairing)
            {
                string displayName = getDeviceString(currentDeviceName, currentDevice);

                didSecRead = false;

                utils.hideAlert();
                systemResponseSignal.Dispatch(SystemResponseEvents.SecurityData, new Dictionary<string, object> {
                    { "TurnOn", true },
                    { "DevId", currentDevice},
                    { "DevName", displayName}
                });

                startTime = Time.realtimeSinceStartup;

                while (!didSecRead)
                {
                    if (currentBleState != "Pairing")
                    {
                        Debug.Log("!didSecRead2: currentBleState = " + currentBleState);
                        yield break;
                    }

                    if (Time.realtimeSinceStartup - startTime > 30.0f)
                    {
                        bleSignalHandler(BluetoothLeEvents.PairingTimeout, new Dictionary<string, object> {
                            { "timoutReason", "no passkey entered/accepted" },
                            { "PairingResult", "Timeout" }
                        });

                        systemResponseSignal.Dispatch(SystemResponseEvents.SecurityData, new Dictionary<string, object> {
                            { "TurnOn", false },
                        });

                        yield break;
                    }

                    //sendReadSecurityPacket();
                    yield return new WaitForSeconds(0.5f);
                }

                if (isSecAuthenticated)
                {
                    bleSignalHandler(BluetoothLeEvents.PairingTimeout, new Dictionary<string, object> {
                        { "timoutReason", "entered passkey accepted" },
                        { "PairingResult", "PasskeyAccepted" }
                    });
                }


                //bool isOn = utils.getValueForKey<bool>(data, "TurnOn");
                //string DevId = utils.getValueForKey<string>(data, "DevId");
                //string DevName = utils.getValueForKey<string>(data, "DevName");
                //uint myPasskey = utils.getValueForKey<uint>(data, "Passkey");
            }
            else
            {
                utils.showAlert("Alert", "Place device in pairing mode before attempting to pair", logo);
                //bleSignalHandler(BluetoothLeEvents.DisconnectedPeripheral, null);

                yield return new WaitForSeconds(5.0f);

                if (currentBleState == "Pairing")
                {
                    bleDisconnectAsync();
                }

                yield break;
            }

            while (currentBleState == "Pairing")
            {
                yield return null;
            }

            yield break;
        }

        IEnumerator sendAllTsDataInstance;
        IEnumerator sendAllTsData()
        {


            for (int i = 0; i < 32; i++)
            {

                sendTsSwitchPacket(switches[i]);
                sendTsBlinkPacket(switches[i]);
                sendTsTextPacket(switches[i]);
                sendIconSelectPacket(switches[i]);

                yield return null;
            }


            yield break;
        }

        IEnumerator sendProBantamSwitchesInstance;
        IEnumerator sendProBantamSwitches()
        {

            if (!ProStatus.isAppProEnabled)
            {
                yield break;
            }

            if (deviceId != sPODDeviceTypes.Bantam)
            {
                yield break;
            }


            if (ProStatus.needsSync)
            {

                for (int i = 0; i < 32; i++)
                {

                    sendProSwitchPacket(switches[i]);

                    yield return null;
                }

                ProStatus.needsSync = false;

            }
            else
            {

                for (int i = 0; i < 8; i++)
                {

                    sendProSwitchPacket(switches[currentSourceId * 8 + i]);

                    yield return null;
                }
            }

            yield break;
        }

        IEnumerator sendProCanPacketsInstance;
        IEnumerator sendProCanPackets()
        {

            if (!ProStatus.isAppProEnabled)
            {
                yield break;
            }

            //if (!ProStatus.needsSync)
            //{
            //	yield return null;
            //}

            if (ProStatus.needsSync)
            {

                for (int i = 0; i < 32; i++)
                {

                    sendCanProSwitchPacket(switches[i]);

                    yield return null;
                }

                ProStatus.needsSync = false;
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    sendCanProSwitchPacket(switches[currentSourceId * 8 + i]);

                    yield return null;
                }
            }

            yield break;
        }

        IEnumerator sendCommSettingsPacketsInstance;
        IEnumerator sendCommSettingsPackets()
        {
            if (!ProStatus.isAppProEnabled)
            {
                yield break;
            }

            yield return null;

            if (ProStatus.needsSync)
            {
                for (int i = 0; i < 32; i++)
                {
                    sendCommSwitchSettingsPacket(switches[i]);

                    yield return null;
                }

                ProStatus.needsSync = false;

            }

            yield break;
        }

        IEnumerator disconnectDelayInstance;
        IEnumerator disconnectDelay()
        {
            ble.DisconnectFromPeripheral(currentDevice);

            yield return null;

            bleSignalHandler("Tick", null);

            yield return new WaitForSeconds(0.25f);

            bleSignalHandler("DisconnectTimout", null);

            //if (currentBleState == "Disconnecting")
            //{
            //	nextBleState = "Disconnected";

            //	bleSignalHandler("Tick", null);
            //	bleSignalHandler("Tick", null);
            //}
            yield break;
        }

        //public static byte connectedDeviceId = 0;

        //private float lastBleTime = Time.realtimeSinceStartup;

        public static int getDeviceId(string name)
        {
            int id = 0;

            int indexLink = name.IndexOf("#");
            int indexLight = name.IndexOf("Lite");
            int indexSe = name.IndexOf("SE");
            int indexRcpm = name.IndexOf("Star Tech");
            int indexOTA = name.IndexOf("OTA");

            if (indexLink > -1)
            {
                string[] split = name.Split('#');

                //Debug.Log (split [0]);
                //Debug.Log (split [1]);

                string deviceId = split[1].Substring(0, 2);
                //				string deviceAddress = split [1].Substring (2, 6); 

                switch (deviceId)
                {
                    case "00":
                        id = sPODDeviceTypes.Bantam;
                        break;
                    case "01":
                        id = sPODDeviceTypes.Touchscreen;
                        break;
                    case "02":
                        id = sPODDeviceTypes.SwitchHDv2;
                        break;
                    case "03":
                        id = sPODDeviceTypes.SourceLT;
                        break;
                    default:
                        id = sPODDeviceTypes.Bantam;
                        break;
                }

            }
            else if (indexLight > -1)
            {
                id = sPODDeviceTypes.SwitchHD;
            }
            else if (indexSe > -1)
            {
                id = sPODDeviceTypes.SourceSE;
            }
            else if (indexRcpm > -1)
            {
                id = sPODDeviceTypes.RCPM;
            }
            else if (indexOTA > -1)
            {
                id = sPODDeviceTypes.OTABootloader;
            }

            return id;


        }

        public static bool isDevicePairable(int devId)
        {
            if (devId == sPODDeviceTypes.Bantam || devId == sPODDeviceTypes.Touchscreen || devId == sPODDeviceTypes.SwitchHDv2 || devId == sPODDeviceTypes.SourceLT)
            {
                return true;
            }

            return false;
        }

        public static bool isDeviceProCapable(int devId)
        {
            if (devId == sPODDeviceTypes.Bantam || devId == sPODDeviceTypes.Touchscreen || devId == sPODDeviceTypes.SwitchHDv2)
            {
                return true;
            }

            return false;
        }

        public static Dictionary<string, string> friendlyNames = new Dictionary<string, string>();
        public static Dictionary<string, UInt32> devicePasskeys = new Dictionary<string, UInt32>();


        public static string getDeviceString(string name, string currDev)
        {
            string retString = name;


            if (isAppProEnabledFlag &&
                !name.StartsWith("OTA") &&
                currDev != null && !currDev.StartsWith("null") &&
                friendlyNames.ContainsKey(currDev))
            {
                return friendlyNames[currDev];
            }

            int indexLink = name.IndexOf("#");
            int indexLight = name.IndexOf("Lite");
            int indexSe = name.IndexOf("SE");
            int indexRcpm = name.IndexOf("Star Tech");
            int indexOta = name.IndexOf("OTA");

            if (indexLink > -1)
            {
                string[] split = name.Split('#');

                //Debug.Log (split [0]);
                //Debug.Log (split [1]);

                string deviceId = split[1].Substring(0, 2);
                string deviceAddress = split[1].Substring(2, 6);

                switch (deviceId)
                {
                    case "00":
                        retString = "BantamX #" + deviceAddress;
                        break;
                    case "01":
                        retString = "Touchscreen #" + deviceAddress;
                        break;
                    case "02":
                        retString = "Switch HD #" + deviceAddress;
                        break;
                    case "03":
                        retString = "SourceLT #" + deviceAddress;
                        break;
                    default:
                        retString = split[0] + "#" + deviceAddress;
                        break;
                }

            }
            else if (indexLight > -1)
            {
                retString = "Switch HD";
            }
            else if (indexSe > -1)
            {
                retString = "Source SE";
            }
            else if (indexRcpm > -1)
            {
                retString = "RCPM";
            }
            else if (indexOta > -1)
            {
                retString = "Bootloader";
            }


            return retString;
        }

        //		private float scanStartTime;

        private float lastConnectingTime = 0;

        private float startProHdSyncTime = 0;


        private Dictionary<string, string> deviceList = new Dictionary<string, string>();



        private bool deviceFound = false;
        private bool connectTimeError = false;

        private float lastBleTime = Time.realtimeSinceStartup;
        private float debugTimer = 0;

        public void HandleRemoteLog(string logString, string stackTrace, LogType type)
        {

            Bugfender.Log(logString + " :: " + stackTrace);

        }

        private bool remoteLogEnabled = false;

        private void enableDebugLog()
        {

            if (!remoteLogEnabled)
            {

                remoteLogEnabled = true;

                Debug.Log("Debug Logging Enabled...");

                Application.logMessageReceived += HandleRemoteLog;

                ble.EnableDebug(true);

                Debug.Log("Debug Logging Enabled...");

                systemResponseSignal.Dispatch(SystemResponseEvents.DebugOn, null);
            }
        }

        private void disableDebugLog()
        {

            if (remoteLogEnabled)
            {

                remoteLogEnabled = false;

                Debug.Log("Debug Logging Disabled...");

                System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
                int debugStop = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;

                PlayerPrefs.SetInt("DebugTimerStop", debugStop);

                ble.EnableDebug(false);

                Application.logMessageReceived -= HandleRemoteLog;

                Debug.Log("Debug Logging Disabled...");

                systemResponseSignal.Dispatch(SystemResponseEvents.DebugOff, null);
            }
        }

        private bool didBleStartup = false;

        private string lastAndroidErrorDevice = null;

        public void bleSignalHandler(string key, Dictionary<string, object> data)
        {

            float currentTime = Time.realtimeSinceStartup;
            float diffTime = currentTime - lastBleTime;

            if (debugTimer >= 0)
            {

                debugTimer -= diffTime;

                if (debugTimer <= 0)
                {
                    disableDebugLog();

                }
            }

            if (key != BluetoothLeEvents.DiscoveredPeripheral &&
                key != BluetoothLeEvents.DidUpdateAdvertisementData &&
                key != BluetoothLeEvents.DidUpdateRssi &&
                key != BluetoothLeEvents.DidUpdateValueForCharacteristic &&
                !(currentBleState == "Connected" && key == BluetoothLeEvents.DidUpdateValueForCharacteristic)
                )
            {
                Debug.Log("bleSignalHandler: (" + currentBleState + ") " + key + ", " + data);
            }

            lastBleTime = currentTime;

            switch (currentBleState)
            {
                case "Idle":
                    if (key == BluetoothLeEvents.StateUpdate)
                    {
                        String bleState = utils.getValueForKey<String>(data, "state");
                        bool loadState = utils.getValueForKey<bool>(data, "didInitialSystemDataLoad");

                        //Debug.Log("BluetoothLeEvents.StateUpdate: " + bleState + ", " + loadState);

                        if (bleState != null && bleState == BluetoothLeEvents.StatePoweredOn)
                        {

                            didBleStartup = true;

                            if (didInitialSystemDataLoad)
                            {
                                nextBleState = "Scanning";
                            }
                        }
                        else if (data.ContainsKey("didInitialSystemDataLoad") && loadState)
                        {
                            didInitialSystemDataLoad = true;

                            if (didBleStartup)
                            {
                                nextBleState = "Scanning";
                            }

                        }
                    }
                    else if (key == "DisconnectTimout")
                    {
                        currentDevice = null;
                        nextBleState = "Scanning";
                    }
                    break;
                case "Scanning":
                    if (key == BluetoothLeEvents.DiscoveredPeripheral)
                    {

                        //Debug.Log ("BluetoothLeEvents.DiscoveredPeripheral");

                        String name = utils.getValueForKey<String>(data, "name");
                        String identifier = utils.getValueForKey<String>(data, "identifier");



                        if (identifier != null && name != null)
                        {

                            if (name != "Unknown" && !deviceList.ContainsKey(name))
                            {
                                Debug.Log("BluetoothLeEvents.DiscoveredPeripheral: " + name);
                            }


                            //						Debug.Log ("Identifier: " + identifier);

                            if (name.StartsWith("OTA Bootloader"))
                            {
                                deviceFound = false;
                                nextBleState = "Connecting";
                                currentDevice = identifier;
                                currentDeviceName = name;
                                deviceId = getDeviceId(currentDeviceName);
                                foundDeviceCharacteritics = new bool[deviceCharacteristicCount[deviceId]];
                                break;
                            }
                            else if (lastDeviceName == "null")
                            {
                                foreach (String deviceName in deviceNames)
                                {
                                    if (name.StartsWith(deviceName) && !deviceList.ContainsKey(name))
                                    {
                                        deviceList.Add(name, identifier);
                                    }
                                }
                            }
                            else
                            {

                                //							if (name.StartsWith (lastDeviceName) || name.Equals("OTA Bootloader", StringComparison.InvariantCultureIgnoreCase)) {
                                if (name.StartsWith(lastDeviceName))
                                {
                                    deviceFound = false;
                                    nextBleState = "Connecting";
                                    currentDevice = identifier;
                                    currentDeviceName = name;
                                    deviceId = getDeviceId(currentDeviceName);
                                    foundDeviceCharacteritics = new bool[deviceCharacteristicCount[deviceId]];
                                    break;
                                }
                                else
                                {
                                    foreach (String deviceName in deviceNames)
                                    {
                                        if (name.StartsWith(deviceName) && !deviceList.ContainsKey(name))
                                        {
                                            deviceList.Add(name, identifier);
                                        }
                                    }
                                }
                            }

                        }

                    }
                    else if (key == BluetoothLeEvents.ScanTimeout)
                    {


                        //systemResponseSignal.Dispatch(SystemResponseEvents.DeviceList, new Dictionary<string, object>{{SystemResponseEvents.DeviceList, deviceList}});

                        if (deviceList.Count == 1)
                        {

                            string name = deviceList.First().Key;
                            string identifier = null;

                            deviceList.TryGetValue(name, out identifier);

                            if (name != null && identifier != null)
                            {
                                deviceFound = false;
                                nextBleState = "Connecting";
                                currentDevice = identifier;
                                currentDeviceName = name;
                                deviceId = getDeviceId(currentDeviceName);
                                foundDeviceCharacteritics = new bool[deviceCharacteristicCount[deviceId]];
                            }

                        }
                        else if (deviceList.Count == 0)
                        { // && Application.platform == RuntimePlatform.Android) {
                            currentBleState = "Idle";   // likely locked up, restart scanning
                        }
                        else
                        {

                            systemResponseSignal.Dispatch(SystemResponseEvents.DeviceList, new Dictionary<string, object> { { SystemResponseEvents.DeviceList, deviceList } });

                        }



                    }
                    break;
                case "Connecting":
                    if (key == BluetoothLeEvents.DiscoveredCharacteristic)
                    {

                        String identifier = utils.getValueForKey<String>(data, "identifier");
                        String service = utils.getValueForKey<String>(data, "service");
                        String characteristic = utils.getValueForKey<String>(data, "characteristic");

                        if (identifier != null && identifier == currentDevice && service != null && service == deviceService[deviceId] && characteristic != null)
                        {

                            //						Debug.Log ("Found Characteristic: " + characteristic + ", " + deviceCharacteristicCount[deviceId]);

                            foundAllDeviceCharacteristics = true;

                            for (int i = 0; i < deviceCharacteristicCount[deviceId]; i++)
                            {
                                //							Debug.Log ("Matching Characteristic: " + characteristic + ", " + deviceCharacteristic[deviceId, i]);
                                if (characteristic == deviceCharacteristic[deviceId, i])
                                {
                                    Debug.Log("Match Found!");
                                    foundDeviceCharacteritics[i] = true;
                                    if (characteristic == sPOD_BLE.OTA_CHAR)
                                    {
                                        Debug.Log("OTA Char");
                                        foundOtaCharacteristic = true;
                                    }
                                    if (characteristic == sPOD_BLE.PRO_CHAR)
                                    {
                                        Debug.Log("PRO Char");
                                        foundProCharacteristic = true;
                                    }
                                    if (characteristic == sPOD_BLE.SECURE_CHAR)
                                    {
                                        Debug.Log("security Char");
                                        foundSecCharacteristic = true;
                                    }
                                    if (characteristic == sPOD_BLE.PASSKEY_CHAR)
                                    {
                                        Debug.Log("passkey Char");

                                        if (isDevicePairable(deviceId) && Application.platform == RuntimePlatform.Android)
                                        {
                                            Debug.Log("try read passkey");
                                            ble.ReadCharacteristic(currentDevice, deviceService[deviceId], sPOD_BLE.PASSKEY_CHAR);
                                        }
                                    }
                                }
                            }

                            for (int i = 0; i < deviceCharacteristicCount[deviceId]; i++)
                            {
                                if (foundDeviceCharacteritics[i] == false &&
                                    deviceCharacteristic[deviceId, i] != sPOD_BLE.OTA_CHAR &&
                                    deviceCharacteristic[deviceId, i] != sPOD_BLE.PRO_CHAR &&
                                    deviceCharacteristic[deviceId, i] != sPOD_BLE.SECURE_CHAR)
                                {
                                    //if (foundDeviceCharacteritics[i] == false) { 
                                    Debug.Log("Still waiting for other characteristics...");
                                    foundAllDeviceCharacteristics = false;
                                    break;
                                }
                            }

                            if (!deviceFound && foundAllDeviceCharacteristics)
                            {
                                Debug.Log("Device Found!");
                                deviceFound = true;

                                //ble.SubscribeToCharacteristic (currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 0], true); 
                                //if both indications and notifications are enabled on the device then iOS defaults to notifications the bootloader is only checking for notifications

                                //ble.SubscribeToCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 0], false);
                                //false (notifications enabled NOT indications) for bootloader it is looking for notifications not

                                //ble.ReadCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 0]);
                                //on the bootloader read is disabled, so this blocks on android while it waits for the read to return which it never does...

                                if (deviceId == sPODDeviceTypes.OTABootloader)
                                {
                                    ble.SubscribeToCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 0], false);

                                }
                                else
                                {
                                    ble.ReadCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 0]);
                                    ble.SubscribeToCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 0], true);

                                }
                            }
                        }
                    }
                    else if (key == BluetoothLeEvents.DidUpdateNotificationStateForCharacteristic)
                    {
                        String identifier = utils.getValueForKey<String>(data, "identifier");
                        String service = utils.getValueForKey<String>(data, "service");
                        String characteristic = utils.getValueForKey<String>(data, "characteristic");

                        if (identifier != null && identifier == currentDevice &&
                                service != null && service == deviceService[deviceId] &&
                                characteristic != null && characteristic == deviceCharacteristic[deviceId, 0])
                        {

                            if (isDevicePairable(deviceId))
                            {
                                nextBleState = "Pairing";
                            }
                            else
                            {
                                nextBleState = "Connected";
                            }
                        }
                    }
                    else if (key == BluetoothLeEvents.DisconnectedPeripheral)
                    {
                        String identifier = utils.getValueForKey<String>(data, "identifier");

                        if (identifier != null && (identifier == currentDevice || identifier == "Unknown"))
                        {
                            nextBleState = "Scanning";
                        }
                    }
                    else if (key == BluetoothLeEvents.ConnectTimeout)
                    {

                        //					nextBleState = "Connected";
                        //					break;

                        Debug.Log("Connection attempt timeout");

                        //					if(deviceId == sPODDeviceTypes.SwitchHDv2 && 

                        if (Application.platform == RuntimePlatform.Android && !connectTimeError)
                        {// && deviceId != sPODDeviceTypes.OTABootloader) {

                            utils.hideAlert();

                            if (deviceId == sPODDeviceTypes.OTABootloader)
                            {

                                saveOta();

                                string bleErrMessage =
                                    "   Your Android mobile device is experiencing a bluetooth error" +
                                //								"\n   After the app closes, wait three minutes for the sPOD device to boot back into normal mode, then reopen the app and reattempt the upgrade process" + 
                                "\n   After the app closes, reopen the app before the sPOD device boots back into normal mode (about 2 minutes), and the app will automatically attempt to resume the upgrade process" +
                                "\n   Reboot your Android device if the error persists" +

                                "\n\nPress \"OK\" and the Bantam app will close";

                                systemResponseSignal.Dispatch(SystemResponseEvents.ShowBleErrPanel, new Dictionary<string, object> {
                                 {"bleErrMessage", bleErrMessage}
                            });

                            }
                            else if (currentDevice == lastAndroidErrorDevice)
                            {// try to reconnect once, rather than immediately doing nuclear option

                                string bleErrMessage =
                                "   Your Android mobile device is experiencing a bluetooth error" +
                                "\n   1. Close the app with \"CLOSE APP\" button, reopen the app, and attempt to connnect again" +
                                "\n   2. If the error still perisists, reboot your Android device" +
                                "\n   3. If you have recently done a major Android update, you may need to reset your network settings " +
                                        "from Settings -> General Mangement -> Reset" +
                                "\n   4. Try clearing the bluetooth cache by toggling airplane mode on for a few seconds" +

                                ((deviceId == sPODDeviceTypes.Bantam) ?

                                "\n   5. Certain Android devices (S10+, p8 lite) have issues with the bluetooth stack. " +
                                        "If your device consistently has problems pairing, consider using security mode 2 (see user manual)"

                                : "");// +

                                systemResponseSignal.Dispatch(SystemResponseEvents.ShowBleErrPanel, new Dictionary<string, object> {
                                 {"bleErrMessage", bleErrMessage}
                            });

                                //"\n\nPress \"Close App\" and the Bantam app will close";

                                //otaAndroidMessage = 
                                //	"   Your Android mobile device is experiencing a bluetooth error" +
                                //"\n   After the app closes, reopen the app and attempt to connnect again" +
                                //"\n   Reboot your Android device if the error persists" +

                                //"\n\n\n\nPress \"OK\" and the Bantam app will close";
                            }
                            else
                            {
                                lastAndroidErrorDevice = currentDevice;
                                bleDisconnectAsync();
                            }

                            //systemResponseSignal.Dispatch (SystemResponseEvents.SelectOtaUpgrade, new Dictionary<string, object> {
                            //	 {"otaUpgradeMessage", bleErrMessage},
                            //	 {"CancelOn", false},
                            //	 {"QuitOnOk", true}
                            //});



                            nextBleState = "Idle";

                        }
                        else
                        {

                            nextBleState = "Scanning";
                        }
                    }
                    else if (key == BluetoothLeEvents.DidUpdateValueForCharacteristic)
                    {
                        String identifier = utils.getValueForKey<String>(data, "identifier");
                        String characteristic = utils.getValueForKey<String>(data, "characteristic");
                        byte[] packetData = utils.getValueForKey<byte[]>(data, "data");

                        if (identifier != null && identifier == currentDevice && characteristic != null)
                        {
                            if (characteristic == sPOD_BLE.PASSKEY_CHAR || characteristic == sPOD_BLE.SECURE_CHAR)
                            {
                                DetectLinkPacket(characteristic, packetData);
                            }
                        }
                    }
                    break;
                case "Pairing":
                    if (key == BluetoothLeEvents.DiscoveredCharacteristic)
                    {
                        String identifier = utils.getValueForKey<String>(data, "identifier");
                        String service = utils.getValueForKey<String>(data, "service");
                        String characteristic = utils.getValueForKey<String>(data, "characteristic");

                        if (identifier != null && identifier == currentDevice &&
                                    service != null && service == deviceService[deviceId] &&
                                    characteristic != null)
                        {
                            if (characteristic == sPOD_BLE.OTA_CHAR)
                            {
                                Debug.Log("Found OTA Char");
                                foundDeviceCharacteritics[2] = true;
                                foundOtaCharacteristic = true;
                            }
                            else if (characteristic == sPOD_BLE.PRO_CHAR)
                            {
                                Debug.Log("Found PRO Char");
                                foundDeviceCharacteritics[3] = true;
                                foundProCharacteristic = true;
                            }
                            else if (characteristic == sPOD_BLE.SECURE_CHAR)
                            {
                                Debug.Log("Found security Char");
                                foundDeviceCharacteritics[4] = true;
                                foundSecCharacteristic = true;
                            }
                        }
                    }
                    if (key == BluetoothLeEvents.DidUpdateValueForCharacteristic)
                    {
                        String identifier = utils.getValueForKey<String>(data, "identifier");
                        String characteristic = utils.getValueForKey<String>(data, "characteristic");
                        byte[] packetData = utils.getValueForKey<byte[]>(data, "data");

                        if (identifier != null && identifier == currentDevice && characteristic != null)
                        {
                            if (characteristic == deviceCharacteristic[deviceId, 0])
                            {
                                if (didPairingTimeout)
                                {
                                    nextBleState = "Connected";
                                }

                                if (!isDeviceSubscribed)
                                {
                                    Debug.Log("isDeviceSubscribed");
                                    isDeviceSubscribed = true;

                                    if (deviceId == sPODDeviceTypes.Bantam)
                                    {
                                        systemResponseSignal.Dispatch(SystemResponseEvents.CurrentIndicatorOn, null);

                                        //if (characteristic == sPOD_BLE.COMM_CHAR && packetData[2] == 2)		// if setting packet
                                        //{
                                        //    DetectLinkPacket(characteristic, packetData);
                                        //}
                                    }
                                    else if (deviceId == sPODDeviceTypes.SourceLT && characteristic == sPOD_BLE.COMM_CHAR)
                                    {
                                        systemResponseSignal.Dispatch(SystemResponseEvents.EnableSixSwitchGui, null);
                                        systemResponseSignal.Dispatch(SystemResponseEvents.CurrentIndicatorOn, null);
                                    }
                                }
                            }

                            if (characteristic == sPOD_BLE.PASSKEY_CHAR || characteristic == sPOD_BLE.SECURE_CHAR)
                            {
                                DetectLinkPacket(characteristic, packetData);
                            }
                        }
                    }
                    else if (key == BluetoothLeEvents.DisconnectedPeripheral)
                    {
                        String identifier = utils.getValueForKey<String>(data, "identifier");

                        if (identifier != null && (identifier == currentDevice || identifier == "Unknown"))
                        {
                            nextBleState = "Scanning";
                        }

                        utils.hideAlert();
                    }
                    else if (key == BluetoothLeEvents.PairingTimeout)
                    {
                        String reason = utils.getValueForKey<String>(data, "timoutReason");
                        String result = utils.getValueForKey<String>(data, "PairingResult");

                        Debug.Log("Pairing Timeout: " + reason);

                        if (!didPairingTimeout)
                        {
                            didPairingTimeout = true;

                            routineRunner.StopCoroutine(connectTimeoutInstance);
                            connectTimeoutInstance = connectTimout(2.5f);
                            routineRunner.StartCoroutine(connectTimeoutInstance);
                        }

                        if (result == "PasskeyAccepted")
                        {
                            nextBleState = "Connected";

                            //if(!isDeviceSubscribed)
                            //                     {
                            //	ble.ReadCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 0]);
                            //	ble.SubscribeToCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 0], true);
                            //}
                        }
                        else
                        {
                            if (isDeviceSubscribed)
                            {
                                nextBleState = "Connected";
                            }
                            else
                            {
                                utils.showAlert("Please Wait", "Waiting for:\n\n" + getDeviceString(lastDeviceName, lastDeviceId), logo);
                            }
                        }


                        //nextBleState = "Connected";
                    }
                    else if (key == BluetoothLeEvents.ConnectTimeout)
                    {
                        bleDisconnectAsync();
                    }
                    break;
                case "Connected":

                    if (key == BluetoothLeEvents.DidWriteCharacteristic)
                    {
                        string characteristic = utils.getValueForKey<string>(data, "characteristic");

                        //						Debug.Log ("did write to: " + characteristic); 
                        sendQueue();
                        switch (deviceId)
                        {
                            case sPODDeviceTypes.SourceLT:
                                break;
                            case sPODDeviceTypes.Bantam:
                                if (ProStatus.isPro && ProStatus.needsSync && ProStatus.isSyncFromApp && characteristic == sPOD_BLE.PRO_CHAR)
                                {
                                    //								sendProSwitchPacket ();

                                    //								if(sendProBantamSwitchesInstance != null)
                                    //									routineRunner.StopCoroutine (sendProBantamSwitchesInstance);
                                    //
                                    //								sendProBantamSwitchesInstance = sendProBantamSwitches ();
                                    //								routineRunner.StartCoroutine (sendProBantamSwitchesInstance);

                                }
                                else
                                {
                                }
                                break;
                            case sPODDeviceTypes.Touchscreen:

                                if (ProStatus.isPro && ProStatus.needsSync && ProStatus.isSyncFromApp && characteristic == sPOD_BLE.PRO_CHAR)
                                {


                                    if (sendAllTsDataInstance != null)
                                        routineRunner.StopCoroutine(sendAllTsDataInstance);

                                    sendAllTsDataInstance = sendAllTsData();
                                    routineRunner.StartCoroutine(sendAllTsDataInstance);


                                    ProStatus.needsSync = false;

                                    //								sendSwitchHDSettingsPacket (true);
                                    //								ProStatus.needsSync = false;
                                }

                                if (characteristic == sPOD_BLE.TOUCH_CHAR)
                                {
                                    //								sendQueue ();
                                }
                                break;
                            case sPODDeviceTypes.SwitchHDv2:

                                if (ProStatus.isPro && ProStatus.needsSync && ProStatus.isSyncFromApp && characteristic == sPOD_BLE.PRO_CHAR)
                                {
                                    //Debug.Log ("start sync to hd with setting packet");
                                    //sendSwitchHDSettingsPacket (true);
                                    //ProStatus.needsSync = false;
                                }
                                else
                                {
                                    //Debug.Log ("normal hd");
                                    //sendSwitchHDLinks ();
                                }

                                break;
                            case sPODDeviceTypes.SwitchHD:
                                sendSwitchHDLinks();
                                break;
                            case sPODDeviceTypes.RCPM:
                            case sPODDeviceTypes.SourceSE:

                                if (characteristic == deviceCharacteristic[deviceId, 2] && sourceSEPinRequest)
                                {
                                    sourceSEPinRequest = false;
                                    sourceSEPin = sourceSEPinTemp;
                                    if (deviceId == sPODDeviceTypes.SourceSE)
                                    {
                                        utils.showAlert("Paired!", "The PIN has been updated... if control fails reset the PIN to 0000 in this App and on the Source SE.", logo);
                                    }
                                    else
                                    {
                                        utils.showAlert("Paired!", "The PIN has been updated... if control fails reset the PIN to 0000 in this App and on the RCPM.", logo);

                                    }
                                    Debug.Log("sourceSEPin " + sourceSEPin);
                                    save();
                                }

                                break;
                        }

                    }
                    else if (key == BluetoothLeEvents.DisconnectedPeripheral)
                    {
                        String identifier = utils.getValueForKey<String>(data, "identifier");

                        if (identifier != null && identifier == currentDevice)
                        {
                            nextBleState = "Scanning";
                        }
                    }
                    else if (key == BluetoothLeEvents.DidUpdateValueForCharacteristic)
                    {
                        String identifier = utils.getValueForKey<String>(data, "identifier");
                        String characteristic = utils.getValueForKey<String>(data, "characteristic");
                        byte[] packetData = utils.getValueForKey<byte[]>(data, "data");

                        if (!isLink && identifier != null && identifier == currentDevice && characteristic != null)
                        {

                            switch (deviceId)
                            {
                                case sPODDeviceTypes.SourceLT:
                                    if (characteristic == sPOD_BLE.COMM_CHAR)
                                    {
                                        systemResponseSignal.Dispatch(SystemResponseEvents.EnableSixSwitchGui, null);
                                        systemResponseSignal.Dispatch(SystemResponseEvents.CurrentIndicatorOn, null);
                                    }
                                    DetectLinkPacket(characteristic, packetData);
                                    break;
                                case sPODDeviceTypes.Bantam:
                                    systemResponseSignal.Dispatch(SystemResponseEvents.CurrentIndicatorOn, null);
                                    DetectLinkPacket(characteristic, packetData);
                                    break;
                                case sPODDeviceTypes.Touchscreen:
                                case sPODDeviceTypes.SwitchHDv2:
                                    DetectLinkPacket(characteristic, packetData);
                                    //								systemResponseSignal.Dispatch (SystemResponseEvents.CurrentIndicatorOn, null);		// only turn ON if current packet detected
                                    break;
                                case sPODDeviceTypes.SwitchHD:
                                    DetectHDPacket(characteristic, packetData);
                                    systemResponseSignal.Dispatch(SystemResponseEvents.CurrentIndicatorOff, null);
                                    break;
                                case sPODDeviceTypes.RCPM:
                                    DetectRCPMPacket(characteristic, packetData);
                                    systemResponseSignal.Dispatch(SystemResponseEvents.EnableSixSwitchGui, null);
                                    systemResponseSignal.Dispatch(SystemResponseEvents.CurrentIndicatorOff, null);
                                    break;
                                case sPODDeviceTypes.SourceSE:
                                    DetectSEPacket(characteristic, packetData);
                                    systemResponseSignal.Dispatch(SystemResponseEvents.CurrentIndicatorOff, null);
                                    break;
                                case sPODDeviceTypes.OTABootloader:
                                    HandleOTAPacket(characteristic, packetData);
                                    //								systemResponseSignal.Dispatch (SystemResponseEvents.CurrentIndicatorOff, null);
                                    break;
                            }

                        }
                    }

                    break;
                case "Disconnecting":

                    if (key == BluetoothLeEvents.DisconnectedPeripheral)
                    {
                        //String identifier = utils.getValueForKey<String>(data, "identifier");

                        //if (identifier != null && (identifier == currentDevice || identifier == "Unknown"))
                        //{
                        currentDevice = null;
                        nextBleState = "Scanning";
                        //}
                    }
                    else if (key == "DisconnectTimout")
                    {
                        currentDevice = null;
                        nextBleState = "Scanning";
                    }

                    break;
                case "Disconnected":
                    nextBleState = "Scanning";
                    break;
            }

            if (currentBleState != nextBleState)
            {

                Debug.Log("BLE Update State: " + currentBleState + ", " + nextBleState);

                switch (nextBleState)
                {
                    case "Idle":

                        break;
                    case "Scanning":
#if UNITY_ANDROID
                        string blePerString = "android.permission.ACCESS_FINE_LOCATION";

                        AndroidRuntimePermissions.Permission blePer = AndroidRuntimePermissions.CheckPermission(blePerString);

                        if (currentBleState == "Idle" && blePer != AndroidRuntimePermissions.Permission.Granted)
                        {
                            utils.hideAlert();

                            Debug.Log("scan ask for ble permission");

                            string myMessageString =
                                "   Android requires location permissions to use bluetooth" +
                                "\n   You will be unable to connect to any devices or use the primary controls of this app without accepting" +
                                "\n   After accepting permission, press \"Setup\"->\"Scan\" to find devices";// +

                            if (blePer == AndroidRuntimePermissions.Permission.ShouldAsk)
                            {
                                myMessageString += "\n\nPress \"OK\" and the Android will ask for \"Location\" permissions again";
                            }
                            else
                            {
                                myMessageString += "\n\nPress \"OK\" and the Android will open device settings to allow you to manually grant permissions";
                            }

                            systemResponseSignal.Dispatch(SystemResponseEvents.DisplayPermissionMessage, new Dictionary<string, object> {
                                                    {"PermissionString", blePerString},
                                                    {"PermissionResponse", (int)blePer},
                                                    {"MessageString", myMessageString}
                                            });

                            nextBleState = "Idle";
                            break;
                        }
#endif

                        if (currentDevice != null)
                        {
                            Debug.Log("Initial scanning disconnect...");
                            //ble.DisconnectFromPeripheral (currentDevice);

                            bleDisconnectAsync();

                            break;
                        }

                        passKeyWasShown = false;

                        //					scanStartTime = Time.realtimeSinceStartup;
                        deviceList.Clear();

                        routineRunner.StopCoroutine(connectTimeoutInstance);
                        routineRunner.StopCoroutine(scanTimeoutInstance);

                        routineRunner.StopCoroutine(checkSecDataInstance);
                        //					routineRunner.StopCoroutine (getPassKeyInstance);

                        currentDevice = null;
                        foundAllDeviceCharacteristics = false;
                        //					foundPasskeyCharacteristic = false;
                        //					foundTouchscreenCharacteristic = false;
                        foundOtaCharacteristic = false;
                        foundProCharacteristic = false;
                        foundSecCharacteristic = false;

                        isDeviceUnsecured = false;
                        isDeviceSubscribed = false;
                        didPairingTimeout = false;

                        CurrentDevOTA.isAddressRead = false;
                        CurrentDevOTA.addressRead = 0;

                        needsSyncToApp = true;

                        //didSubscribe = false;

                        sizeOfMtu = 23;

                        try
                        {
                            lastDeviceName = ES2.Load<String>("lastDevice.txt?tag=lastDevice");
                            lastDeviceId = ES2.Load<String>("lastDevice.txt?tag=lastDeviceId");
                        }
                        catch (Exception e)
                        {//Exception e) {
                            Debug.Log("Fail try load lastDeviceName in scanning: " + e);
                            lastDeviceName = "null";
                            lastDeviceId = "null";
                        }

                        if (!lastDeviceName.StartsWith("OTA Bootloader") && !CurrentDevOTA.Upgrading)
                        {
                            if (lastDeviceName != "null" && lastDeviceName.Length > 0)
                            {
                                utils.showAlert("Please Wait", "Scanning for:\n\n" + getDeviceString(lastDeviceName, lastDeviceId), logo);
                            }
                            else
                            {
                                utils.showAlert("Please Wait", "Scanning...", logo);
                            }
                        }

                        ble.ScanForPeripheralsWithServiceUUIDs(null);
                        scanTimeoutInstance = scanTimout();
                        routineRunner.StartCoroutine(scanTimeoutInstance);

                        systemResponseSignal.Dispatch(SystemResponseEvents.DisableSixSwitchGui, null);
                        systemResponseSignal.Dispatch(SystemResponseEvents.BantamLowPowerTog, null);        // disable toggle until receive packet

                        sendProData();
                        sendSystemData();

                        clearPacketQueue();

                        break;

                    case "Connecting":

                        ble.StopScanning();
                        routineRunner.StopCoroutine(scanTimeoutInstance);

                        connectTimeError = false;

                        if (lastConnectingTime != 0 &&
                                Time.realtimeSinceStartup - lastConnectingTime < 10.0f &&
                                deviceId != sPODDeviceTypes.OTABootloader &&
                                !(Application.platform != RuntimePlatform.Android && isDevicePairable(deviceId)))
                        {

                            lastConnectingTime = Time.realtimeSinceStartup;
                            connectTimeError = true;

                            utils.showAlert("Hmmm...", "It looks like you are having problems connecting.  If this is your first time connecting to the device make sure it is in pairing mode and try again by tapping 'Setup' then 'Scan'. (see the manual) ", logo);

                        }
                        else
                        {
                            lastConnectingTime = Time.realtimeSinceStartup;
                            //						if (true || deviceId != sPODDeviceTypes.OTABootloader) {	
                            utils.showAlert("Please Wait", "Connecting to:\n\n " + getDeviceString(currentDeviceName, currentDevice), logo);
                            //						}
                            ble.ConnectToPeripheral(currentDevice);
                        }



                        routineRunner.StopCoroutine(connectTimeoutInstance);

                        connectTimeoutInstance = connectTimout(15.0f);
                        routineRunner.StartCoroutine(connectTimeoutInstance);


                        break;
                    case "Pairing":

                        lastDeviceName = currentDeviceName;
                        lastDeviceId = currentDevice;

                        //utils.showAlert("Please Wait", "Pairing...", logo);
                        utils.showAlert("Please Wait", "Pairing with\n\n" + getDeviceString(lastDeviceName, lastDeviceId), logo);

                        ES2.Save(lastDeviceName, "lastDevice.txt?tag=lastDevice");
                        ES2.Save(lastDeviceId, "lastDevice.txt?tag=lastDeviceId");

                        routineRunner.StopCoroutine(connectTimeoutInstance);

                        routineRunner.StopCoroutine(scanTimeoutInstance);


                        routineRunner.StopCoroutine(getPassKeyInstance);
                        getPassKeyInstance = getPassKey();
                        routineRunner.StartCoroutine(getPassKeyInstance);


                        routineRunner.StopCoroutine(checkSecDataInstance);
                        checkSecDataInstance = checkSecData();
                        routineRunner.StartCoroutine(checkSecDataInstance);


                        CurrentDevOTA = new DeviceOTA();
                        saveOta();
                        CurrentDevOTA.NeedUpgrade = OTAInfo.NEED_READ;
                        CurrentDevOTA.DeviceName = currentDeviceName;

                        routineRunner.StopCoroutine(getOtaDataInstance);
                        getOtaDataInstance = getOtaData();
                        routineRunner.StartCoroutine(getOtaDataInstance);


                        routineRunner.StopCoroutine(checkProDataInstance);
                        checkProDataInstance = checkProData();
                        routineRunner.StartCoroutine(checkProDataInstance);


                        break;
                    case "Connected":

                        routineRunner.StopCoroutine(connectTimeoutInstance);
                        lastAndroidErrorDevice = null;
                        if (deviceId != sPODDeviceTypes.OTABootloader)
                        {

                            systemResponseSignal.Dispatch(SystemResponseEvents.CurrentIndicatorOff, null);

                            routineRunner.StopCoroutine(scanTimeoutInstance);
                            lastDeviceName = currentDeviceName;
                            lastDeviceId = currentDevice;

                            ES2.Save(lastDeviceName, "lastDevice.txt?tag=lastDevice");
                            ES2.Save(lastDeviceId, "lastDevice.txt?tag=lastDeviceId");

                            //routineRunner.StopCoroutine (getPassKeyInstance);
                            //getPassKeyInstance = getPassKey ();
                            //routineRunner.StartCoroutine (getPassKeyInstance);

                            utils.showAlert("Please Wait", "Connected...", logo);
                            utils.hideAlert();

                            //CurrentDevOTA = new DeviceOTA ();
                            //saveOta ();

                            //CurrentDevOTA.NeedUpgrade = OTAInfo.NEED_READ;
                            //CurrentDevOTA.DeviceName = currentDeviceName;

                            if (isDevicePairable(deviceId))
                            {

                                if (getPassKeyInstance != null)
                                    routineRunner.StopCoroutine(getPassKeyInstance);
                                systemResponseSignal.Dispatch(SystemResponseEvents.Passkey, new Dictionary<string, object> { { SystemResponseEvents.Passkey, (uint)0 } });

                                //						if ((deviceId == sPODDeviceTypes.Bantam || deviceId == sPODDeviceTypes.Touchscreen || deviceId == sPODDeviceTypes.SwitchHDv2 || deviceId == sPODDeviceTypes.SourceLT) // if compatible device
                                //							&& foundDeviceCharacteritics [2] == true) {	//  and if ota char has been found
                                //							ble.SubscribeToCharacteristic (currentDevice, deviceService [deviceId], deviceCharacteristic [deviceId, 2], false);

                                //routineRunner.StopCoroutine (getOtaDataInstance);
                                //getOtaDataInstance = getOtaData ();
                                //routineRunner.StartCoroutine (getOtaDataInstance);

                                //routineRunner.StopCoroutine (checkProDataInstance);
                                //checkProDataInstance = checkProData ();
                                //routineRunner.StartCoroutine (checkProDataInstance);


                                //routineRunner.StopCoroutine(getPassKeyInstance);
                                //systemResponseSignal.Dispatch(SystemResponseEvents.Passkey, new Dictionary<string, object> { { SystemResponseEvents.Passkey, (uint)0 } });

                                if (deviceId == sPODDeviceTypes.Bantam)
                                {
                                    sendDeepSleepBantamPacket(false, true);
                                }

                            }
                            else
                            {

                                routineRunner.StopCoroutine(getPassKeyInstance);
                                getPassKeyInstance = getPassKey();
                                routineRunner.StartCoroutine(getPassKeyInstance);

                                CurrentDevOTA = new DeviceOTA();
                                saveOta();

                                CurrentDevOTA.DeviceName = currentDeviceName;

                                CurrentDevOTA.NeedUpgrade = OTAInfo.NOT_COMPATIBLE;
                                sendProData();
                            }

                        }
                        else
                        {
                            routineRunner.StopCoroutine(scanTimeoutInstance);
                            //						routineRunner.StopCoroutine (getPassKeyInstance);

                            routineRunner.StopCoroutine(otaTimeoutInstance);
                            routineRunner.StopCoroutine(checkProDataInstance);

                            //ble.SubscribeToCharacteristic ( currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 0], true);

                            lastDeviceName = currentDeviceName;
                            lastDeviceId = currentDevice;

                            transmitOTAFile();
                        }
                        break;
                    case "Disconnecting":

                        //routineRunner.StopCoroutine(disconnectDelayInstance);
                        //disconnectDelayInstance = disconnectDelay();
                        //routineRunner.StartCoroutine(disconnectDelayInstance);


                        break;
                    case "Disconnected":

                        break;
                }

                currentBleState = nextBleState;

            }
        }



        //static bool sendingLinking = false; 

        //		Queue<List<byte>> packetQueue = new Queue<List<byte>>();

        //		Queue<Dictionary<String,List<byte>>> packetQueue = new Queue<Dictionary<String,List<byte>>>();
        Queue<Dictionary<string, object>> packetQueue = new Queue<Dictionary<string, object>>();

        public void clearPacketQueue()
        {
            packetQueue.Clear();
            queueSent = true;
        }

        public void addPacketToQueue(List<byte> packet, string characteristic)
        {

            //			packetQueue.Enqueue (packet);
            packetQueue.Enqueue(new Dictionary<string, object> {
                {"characteristic", characteristic},
                {"packet", packet}
            });

            if (queueSent)
            {
                sendQueue();
            }
        }

        public void sendQueue()
        {

            if (currentDevice == null)
            {
                clearPacketQueue();
                return;
            }

            if (packetQueue.Count > 0)
            {
                //				List<byte> packet = packetQueue.Dequeue ();

                Dictionary<string, object> queueData = packetQueue.Dequeue();

                string queuedCharacteristic = utils.getValueForKey<string>(queueData, "characteristic");
                List<byte> packet = utils.getValueForKey<List<byte>>(queueData, "packet");

                ble.WriteCharacteristic(currentDevice, deviceService[deviceId], queuedCharacteristic, packet.ToArray(), packet.Count, true);
            }

            if (packetQueue.Count == 0)
            {
                queueSent = true;
            }
            else
            {
                queueSent = false;
            }
        }

        public void sendReadSecurityPacket()
        {
            Debug.Log("Read security flags for: " + currentDeviceName);

            List<byte> packet = new List<byte>();

            packet.Add(7);   // length
            packet.Add(0x00);   // id
            packet.Add(0x00);

            uint crc = crc32(0, packet.ToArray(), packet.Count);
            packet.AddRange(BitConverter.GetBytes(crc));

            if (currentDevice != null)
            {
                Debug.Log("sendReadSecurityPacket: " + byteArrayToString(packet.ToArray()));

                addPacketToQueue(packet, sPOD_BLE.SECURE_CHAR);
            }
            else
            {
                Debug.Log("Error sendReadSecurityPacket Data: " + byteArrayToString(packet.ToArray()));
            }
        }

        public void sendSetSecurityUnsecured()
        {
            sendSetSecurityMode(true);
        }

        public void sendSetSecuritySecured()
        {
            sendSetSecurityMode(false);
        }

        public void sendSetSecurityMode(bool isSetUnsecured)
        {
            Debug.Log("Set Security Mode for: " + currentDeviceName + " to: " + (isSetUnsecured ? "Unsecured" : "Secured"));

            List<byte> packet = new List<byte>();

            byte newMode = (byte)(isSetUnsecured ? 0x01 : 0x00);

            packet.Add(7);   // length
            packet.Add(0x01);   // id
            packet.Add(newMode);

            uint crc = crc32(0, packet.ToArray(), packet.Count);
            packet.AddRange(BitConverter.GetBytes(crc));

            if (currentDevice != null)
            {
                Debug.Log("sendReadSecurityPacket: " + byteArrayToString(packet.ToArray()));

                addPacketToQueue(packet, sPOD_BLE.SECURE_CHAR);
            }
            else
            {
                Debug.Log("Error sendReadSecurityPacket Data: " + byteArrayToString(packet.ToArray()));
            }
        }

        public void SendSecurityPasskey(uint passkey)
        {
            Debug.Log("Write passkey to: " + currentDeviceName + " -> passkey: " + passkey);

            List<byte> packet = new List<byte>();


            packet.Add(10);   // length
            packet.Add(0x02);   // id

            packet.AddRange(BitConverter.GetBytes(passkey));

            uint crc = crc32(0, packet.ToArray(), packet.Count);
            packet.AddRange(BitConverter.GetBytes(crc));

            if (currentDevice != null)
            {
                Debug.Log("sendReadSecurityPacket: " + byteArrayToString(packet.ToArray()));

                addPacketToQueue(packet, sPOD_BLE.SECURE_CHAR);
            }
            else
            {
                Debug.Log("Error sendReadSecurityPacket Data: " + byteArrayToString(packet.ToArray()));
            }
        }

        public void sendTurnOnProPacket()
        {
            if (!ProStatus.isAppProEnabled)
            {
                return;
            }

            if (ProStatus.isPro == false)
            {
                return;
            }

            Debug.Log("Turn on pro mode for: " + currentDeviceName);

            List<byte> packet = new List<byte>();
            byte devId;
            byte isWritable;
            byte isTempWritable;
            byte needsRead;

            packet.Add(0x03);   // length

            switch (deviceId)
            {
                case sPODDeviceTypes.Bantam:
                    devId = 0;
                    break;
                case sPODDeviceTypes.Touchscreen:
                    devId = 1;
                    break;
                case sPODDeviceTypes.SwitchHDv2:
                    devId = 2;
                    break;
                //			case sPODDeviceTypes.SourceLT:
                //				devId = 3;
                //				break;
                default:
                    return;
            }

            needsRead = 0;
            isWritable = (byte)(ProStatus.isSyncFromApp ? 1 : 0);                               // allows settings to be changed until commanded otherwise, saved in flash
            isTempWritable = (byte)(ProStatus.isSyncFromApp || !needsSyncToApp ? 1 : 0);        // allows settings to be changed, but resets on device every ble connection

            packet.Add((byte)((devId << 6) | isTempWritable << 2 | isWritable << 1 | needsRead));   // r/w
            packet.Add(0x00);

            if (currentDevice != null)
            {
                Debug.Log("sendTurnOnProPacket: " + byteArrayToString(packet.ToArray()));

                //ble.WriteCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 3], packet.ToArray(), packet.Count, true);
                //ble.WriteCharacteristic(currentDevice, deviceService[deviceId], sPOD_BLE.PRO_CHAR, packet.ToArray(), packet.Count, true);
                addPacketToQueue(packet, sPOD_BLE.PRO_CHAR);
            }
            else
            {
                Debug.Log("Error Data: " + byteArrayToString(packet.ToArray()));
            }
        }

        public void sendDeepSleepBantamPacket(bool isLowPowerMode, bool isLpReq)//, bool isInputLink, bool isIReq)
        {

            if (deviceId != sPODDeviceTypes.Bantam)
            {
                return;
            }

            //Debug.Log("Update Deep Sleep for: " + currentDeviceName + ", isLowPowerMode: " + isLowPowerMode + ", isReq: " + isReq);


            List<byte> packet = new List<byte>();

            packet.Add(0x55); //delimiter
            packet.Add(6); //length
            packet.Add(0x02); //type

            //packet.Add(0);
            //packet.Add(0);

            int lpByte = (isLowPowerMode ? 0x01 : 0x00) | (isLpReq ? 0x02 : 0x00);

            //int lpByte = (isLowPowerMode ? 0x01 : 0x00) | (isLpReq ? 0x02 : 0x00) |
            //				(isInputLink ? 0x04 : 0x00) | (isIReq ? 0x08 : 0x00);

            packet.Add(Convert.ToByte(lpByte));


            uint crc = crc32(0, packet.ToArray(), packet.Count);
            packet.AddRange(BitConverter.GetBytes(crc));

            if (currentDevice != null)
            {
                Debug.Log("sendDeepSleepBantamPacket: " + byteArrayToString(packet.ToArray()));

                addPacketToQueue(packet, sPOD_BLE.COMM_CHAR);
            }
            else
            {
                Debug.Log("Error Data: " + byteArrayToString(packet.ToArray()));
            }
        }

        public void sendDeepSleepProPacket()
        {
            //			if (!isAppProEnabledFlag || (Application.platform != RuntimePlatform.IPhonePlayer && Application.platform != RuntimePlatform.OSXEditor)) {
            if (!ProStatus.isAppProEnabled)
            {
                Debug.Log("sendDeepSleepProPacket(): !ProStatus.isAppProEnabled");
                return;
            }

            if (ProStatus.isPro == false)
            {
                Debug.Log("sendDeepSleepProPacket(): !ProStatus.isPro");
                return;
            }

            //if (deviceId != sPODDeviceTypes.Bantam && deviceId != sPODDeviceTypes.Touchscreen && deviceId != sPODDeviceTypes.SwitchHDv2) {
            if (!isDeviceProCapable(deviceId))
            {
                Debug.Log("sendDeepSleepProPacket(): !isDeviceProCapable(deviceId)");
                return;
            }

            Debug.Log("Update Deep Sleep for: " + currentDeviceName + ", isDisabled: " + ProStatus.isDisableDeepSleep
                 + ", needsSync: " + ProStatus.needsSync + ", isSyncFromApp: " + ProStatus.isSyncFromApp);

            List<byte> packet = new List<byte>();
            byte devId;
            byte isWritable;
            byte isTempWritable;
            byte needsRead;

            packet.Add(0x03);   // length

            switch (deviceId)
            {
                case sPODDeviceTypes.Bantam:
                    devId = 0;
                    break;
                case sPODDeviceTypes.Touchscreen:
                    devId = 1;
                    break;
                case sPODDeviceTypes.SwitchHDv2:
                    devId = 2;
                    break;
                //case sPODDeviceTypes.SourceLT:
                //	devId = 3;
                //	break;
                default:
                    return;
            }


            needsRead = 0;
            isWritable = (byte)(ProStatus.isSyncFromApp ? 1 : 0);
            isTempWritable = 1;
            byte isSyncing = (byte)((ProStatus.needsSync && ProStatus.isSyncFromApp) ? 1 : 0);

            packet.Add((byte)((devId << 6) | isSyncing << 3 | isTempWritable << 2 | isWritable << 1 | needsRead)); // r/w

            byte isSleep = (byte)((0x02) | (ProStatus.isDisableDeepSleep ? 0x01 : 0));
            if (deviceId == sPODDeviceTypes.Bantam)
            {
                isSleep |= (byte)((0x08) | (ProStatus.isInputLinking ? 0x04 : 0));
            }
            packet.Add(isSleep);    // r/w

            if (currentDevice != null)
            {
                Debug.Log("sendDeepSleepProPacket: " + byteArrayToString(packet.ToArray()));

                //ble.WriteCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 3], packet.ToArray(), packet.Count, true);
                //ble.WriteCharacteristic(currentDevice, deviceService[deviceId], sPOD_BLE.PRO_CHAR, packet.ToArray(), packet.Count, true);
                addPacketToQueue(packet, sPOD_BLE.PRO_CHAR);
            }
            else
            {
                Debug.Log("Error Data: " + byteArrayToString(packet.ToArray()));
            }
        }

        //bool needsSwOptionsSendAll = false;
        //int sendSwIndex = 0;

        public void sendProSwitchPacket(SwitchStatus switchStatus)
        {
            //			if (!isAppProEnabledFlag || (Application.platform != RuntimePlatform.IPhonePlayer && Application.platform != RuntimePlatform.OSXEditor)) {
            if (!ProStatus.isAppProEnabled)
            {
                return;
            }

            if (ProStatus.isPro == false)
            {
                return;
            }

            if (deviceId != sPODDeviceTypes.Bantam)
            {   // || deviceId != sPODDeviceTypes.SourceLT) {
                return;
            }

            if (switchStatus == null || switchStatus.id < 0 || switchStatus.id > 31)
            {
                Debug.Log("sendProSwitchPacket() err: " + (switchStatus == null ? "switchStatus == null" : switchStatus.id.ToString()));
                return;
            }

            List<byte> packet = new List<byte>();

            packet.Add((byte)13);   // length	//20

            byte devId = 0;
            byte needsRead = 0;
            byte isWritable = (byte)(ProStatus.isSyncFromApp ? 1 : 0);
            byte isTempWritable = 1;

            packet.Add((byte)((devId << 6) | isTempWritable << 2 | isWritable << 1 | needsRead));   // r/w
            packet.Add(0x00);
            packet.Add((byte)switchStatus.id); //(address);

            //timerVal = switches [swIndex].proOnTimer;

            byte outputVal = (byte)(
                (switchStatus.proIsAutoOn ? 0x10 : 0x00) |
                (switchStatus.proIsIgnCtrl ? 0x08 : 0x00) |
                (switchStatus.proIsLockout ? 0x04 : 0x00) |
                (switchStatus.proIsInputLatch ? 0x02 : 0x00) |
                (switchStatus.proIsCurrentRestart ? 0x01 : 0x00)
                );
            //byte autoVal = (byte)(switchStatus.proIsAutoOn ? 0x10 : 0x00);
            //byte ignVal = (byte)(switchStatus.proIsIgnCtrl ? 0x08 : 0x00);
            //byte lockVal = (byte)(switchStatus.proIsLockout ? 0x04 : 0x00);
            //byte inputVal = (byte)(switchStatus.proIsInputLatch ? 0x02 : 0x00);
            //byte restartVal = (byte)(switchStatus.proIsCurrentRestart ? 0x01 : 0x00);

            packet.Add(outputVal);

            packet.Add((byte)((switchStatus.proOnTimer >> 8) & 0xFF));
            packet.Add((byte)(switchStatus.proOnTimer & 0xFF));

            packet.Add((byte)switchStatus.proCurrentLimit);

            byte inputVal = (byte)(
                //0x80 | (ProStatus.isInputLinking ? 0x40 : 0) |
                0x20 |
                (switchStatus.proIsInputEnabled ? 0x04 : 0) |
                (switchStatus.proIsInputLockout ? 0x02 : 0) |
                (switchStatus.proIsInputLockInvert ? 0x01 : 0)
                );

            packet.Add(inputVal);

            packet.Add((byte)((links[switchStatus.id] >> 0) & 0xFF));
            packet.Add((byte)((links[switchStatus.id] >> 8) & 0xFF));
            packet.Add((byte)((links[switchStatus.id] >> 16) & 0xFF));
            packet.Add((byte)((links[switchStatus.id] >> 24) & 0xFF));

            //int offset = swIndex & ~0x07;
            //packet.Add((byte)(links[swIndex] >> offset & 0xFF));

            //Debug.Log("sendProSwitchPacket(" + swIndex + ") " + links[swIndex] + ", " + offset);

            //			Debug.Log("");
            //			for (int i = 0; i < 32; i++) {
            //				Debug.Log(" " + switches [i].proOnTimer);
            //			}



            //			for (int i = 0; i < 8; i++) {
            //
            //				swIndex = address * 8 + i;
            //
            //				isUpdateSw = 0x80;
            //
            //				timerVal = switches [swIndex].proOnTimer;
            //				autoVal = (byte)(switches [swIndex].proIsAutoOn ? 0x08 : 0x00);
            //				ignVal = (byte)(switches [swIndex].proIsIgnCtrl ? 0x10 : 0x00);
            //				lockVal = (byte)(switches [swIndex].proIsLockout ? 0x20 : 0x00);
            //
            //				packet.Add ((byte)(isUpdateSw | ignVal | autoVal | lockVal | ((timerVal >> 8) & 0x07)));
            //				packet.Add ((byte)(timerVal & 0xFF));
            //			}

            //addPacketToQueue (packet, deviceCharacteristic[deviceId, 3]);
            addPacketToQueue(packet, sPOD_BLE.PRO_CHAR);


            //			if (sendSwIndex >= 3 && needsSwOptionsSendAll) {
            //				needsSwOptionsSendAll = false;
            //				sendSwIndex = 0;
            //				ProStatus.needsSync = false;
            //			}
        }

        public void sendCommSwitchSettingsPacket(SwitchStatus switchStatus)
        {
            //Debug.Log("try sendCommSwitchSettingsPacket(" + switchStatus.id + ")");

            if (switchStatus == null || switchStatus.id < 0 || switchStatus.id > 31)
            {
                Debug.Log("sendCommSwitchSettingsPacket() err: " + (switchStatus == null ? "switchStatus == null" : switchStatus.id.ToString()));
                return;
            }


            List<byte> packet = new List<byte>();


            packet.Add(0x55);   //delimiter
            packet.Add(15);     //length: data - 2header + 4crc
            packet.Add(0x08);   //type

            packet.Add((byte)switchStatus.id);        // 0-31

            packet.Add(0x00);               // for "versioning", increment to have old firmware ignore packets

            packet.Add((byte)switchStatus.value);

            bool isFlash = switchStatus.canFlash && !ProStatus.isPro;
            bool isStrobe = switchStatus.canStrobe || (switchStatus.canFlash && ProStatus.isPro);

            byte flags = (byte)(
                (switchStatus.isMomentary ? 0x01 : 0) |
                (switchStatus.isDimmable ? 0x02 : 0) |
                (isStrobe ? 0x04 : 0) |
                (isFlash ? 0x08 : 0)
                );

            packet.Add(flags);

            //int strobeOn = 0xff;
            //int strobeOff = 0;

            //strobeOn = switchStatus.proStrobeOn;
            //strobeOff = switchStatus.proStrobeOff;

            packet.Add((byte)switchStatus.proStrobeOn);
            packet.Add((byte)switchStatus.proStrobeOff);

            packet.Add((byte)((links[switchStatus.id] >> 0) & 0xFF));
            packet.Add((byte)((links[switchStatus.id] >> 8) & 0xFF));
            packet.Add((byte)((links[switchStatus.id] >> 16) & 0xFF));
            packet.Add((byte)((links[switchStatus.id] >> 24) & 0xFF));



            uint crc = crc32(0, packet.ToArray(), packet.Count);
            packet.AddRange(BitConverter.GetBytes(crc));

            if (currentDevice != null)
            {
                Debug.Log("sendCommSwitchSettingsPacket: " + byteArrayToString(packet.ToArray()));

                addPacketToQueue(packet, sPOD_BLE.COMM_CHAR);
            }
            else
            {
                Debug.Log("Error Data: " + byteArrayToString(packet.ToArray()));
            }


            if (switchStatus.id == 31)
                ProStatus.needsSync = false;
        }


        public void sendSwitchHDPinPacket()
        {

            List<byte> packet = new List<byte>();

            //delimiter, length, type, data, crc
            //1, 1, 1, 7, 4
            //length = type + data + crc
            //length = 12

            packet.Add(0x55); //delimiter
            packet.Add(7); //length
            packet.Add(4); //type

            packet.Add((byte)(switchHDPin & 0xFF));
            packet.Add((byte)((switchHDPin >> 8) & 0xFF));


            uint crc = crc32(0, packet.ToArray(), packet.Count);

            packet.AddRange(BitConverter.GetBytes(crc));

            if (currentDevice != null)
            {
                Debug.Log("Data: & dev/name " + byteArrayToString(packet.ToArray()));

                //if (sendingLinking) {
                //ble.WriteCharacteristic(currentDevice, deviceService, deviceCharacteristic, packet.ToArray(), packet.Count, false);
                //} else {
                //ble.WriteCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 0], packet.ToArray(), packet.Count, true);
                addPacketToQueue(packet, deviceCharacteristic[deviceId, 0]);
                //}


            }
            else
            {
                Debug.Log("Error Data: " + byteArrayToString(packet.ToArray()));
            }

        }


        #region SwitchHD Helpers

        private byte getSwitchTypeByte(int sourceAddr)
        {
            int temp = 0;

            for (int i = 0; i < 8; i++)
            {
                int index = (sourceAddr * 8) + i;

                if (switches[index].isMomentary)
                {
                    temp = (1 << i) | temp;
                }
            }

            return Convert.ToByte(temp);
        }

        private void setSwitchTypeFromByte(byte value, int sourceAddr)
        {
            for (int i = 0; i < 8; i++)
            {
                int index = (sourceAddr * 8) + i;

                if ((value & (1 << i)) != 0)
                {
                    switches[index].isMomentary = true;
                }
                else
                {
                    switches[index].isMomentary = false;
                }
            }

        }

        private byte getSwitchDimmableByte(int sourceAddr)
        {
            int temp = 0;

            for (int i = 0; i < 8; i++)
            {
                int index = (sourceAddr * 8) + i;
                if (switches[index].isDimmable)
                {
                    temp = (1 << i) | temp;
                }
            }

            return Convert.ToByte(temp); ;
        }

        private void setSwitchDimmableFromByte(byte value, int sourceAddr)
        {
            for (int i = 0; i < 8; i++)
            {
                int index = (sourceAddr * 8) + i;

                if ((value & (1 << i)) != 0)
                {
                    switches[index].isDimmable = true;
                }
                else
                {
                    switches[index].isDimmable = false;
                }
            }

        }

        private byte[] getSwitchStrobeOrFlashBytes(int sourceAddr)
        {
            int[] temp = new int[2];
            temp[0] = 0;
            temp[1] = 0;

            for (int i = 0; i < 8; i++)
            {
                int index = (sourceAddr * 8) + i;

                if (switches[index].canFlash || switches[index].canStrobe)
                {
                    temp[0] = (1 << i) | temp[0];

                    if (switches[index].canFlash)
                        temp[1] = (1 << i) | temp[1];
                }
            }

            byte[] bytes = new byte[2];
            bytes[0] = Convert.ToByte(temp[0]);
            bytes[1] = Convert.ToByte(temp[1]);
            return bytes;

        }

        private void setSwitchStrobeOrFlashFromBytes(byte val1, byte val2, int sourceAddr)
        {
            for (int i = 0; i < 8; i++)
            {
                int index = (sourceAddr * 8) + i;

                switches[index].canFlash = false;
                switches[index].canStrobe = false;

                if ((val1 & (1 << i)) != 0)
                {
                    if ((val2 & (1 << i)) != 0)
                    {
                        switches[index].canFlash = true;
                    }
                    else
                    {
                        switches[index].canStrobe = true;
                    }
                }
            }
        }
        #endregion

        //private byte[] getProStrobe

        public void sendSwitchHDSettingsPacket(bool needsSaved)
        {

            if (needsSaved && switchHDNeedsSaved)
                return;

            Debug.Log("Sending Save Packet...");

            if (needsSaved)
                switchHDNeedsSaved = true;

            //			linkIndex = 0;
            switchHDLinkIndex = 0;

            List<byte> packet = new List<byte>();


            packet.Add(0x55); //delimiter
            packet.Add(15 + 2); //length
            packet.Add(0x02); //type
            packet.Add(Convert.ToByte(currentSourceId));
            packet.Add(Convert.ToByte(switchHDSource));
            packet.Add(getColorByte(switchHDColors[0]));
            packet.Add(getColorByte(switchHDColors[1]));
            packet.Add(getColorByte(switchHDColors[2]));
            packet.Add(getColorByte(switchHDColors[3]));
            packet.Add(Convert.ToByte(switchHDTimer));
            packet.Add(getSwitchTypeByte(switchHDSource));
            packet.Add(getSwitchDimmableByte(switchHDSource));
            packet.AddRange(getSwitchStrobeOrFlashBytes(switchHDSource));

            packet.Add(Convert.ToByte(0x04 | (switchHDWake ? 0x02 : 0x00)));

            uint crc = crc32(0, packet.ToArray(), packet.Count);
            packet.AddRange(BitConverter.GetBytes(crc));

            if (currentDevice != null)
            {
                Debug.Log("sendSwitchHDSettingsPacket: " + byteArrayToString(packet.ToArray()));

                //ble.WriteCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 0], packet.ToArray(), packet.Count, true);
                addPacketToQueue(packet, deviceCharacteristic[deviceId, 0]);
            }
            else
            {
                Debug.Log("Error Data: " + byteArrayToString(packet.ToArray()));
            }
        }


        private void sendSwitchHDLinks()
        {
            if (switchHDNeedsSaved)
            {

                Debug.Log("Sending SwitchHD Link Packet...");

                List<byte> packet = new List<byte>();

                int offset = switchHDLinkIndex * 8;

                packet.Add(0x55); //delimiter
                packet.Add(14); //length
                packet.Add(0x03); //type
                packet.Add(Convert.ToByte(switchHDLinkIndex));

                packet.Add(Convert.ToByte(links[offset + 0] >> offset & 0xFF));
                packet.Add(Convert.ToByte(links[offset + 1] >> offset & 0xFF));
                packet.Add(Convert.ToByte(links[offset + 2] >> offset & 0xFF));
                packet.Add(Convert.ToByte(links[offset + 3] >> offset & 0xFF));
                packet.Add(Convert.ToByte(links[offset + 4] >> offset & 0xFF));
                packet.Add(Convert.ToByte(links[offset + 5] >> offset & 0xFF));
                packet.Add(Convert.ToByte(links[offset + 6] >> offset & 0xFF));
                packet.Add(Convert.ToByte(links[offset + 7] >> offset & 0xFF));

                uint crc = crc32(0, packet.ToArray(), packet.Count);
                packet.AddRange(BitConverter.GetBytes(crc));

                if (currentDevice != null)
                {
                    Debug.Log("sendSwitchHDLinks: " + byteArrayToString(packet.ToArray()));

                    //ble.WriteCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 0], packet.ToArray(), packet.Count, true);
                    addPacketToQueue(packet, deviceCharacteristic[deviceId, 0]);
                }
                else
                {
                    Debug.Log("Error Data: " + byteArrayToString(packet.ToArray()));
                }

                switchHDLinkIndex++;

                if (switchHDLinkIndex >= 4)
                {
                    switchHDLinkIndex = 0;
                    switchHDNeedsSaved = false;
                    ProStatus.needsSync = false;
                }

            }
        }


        public void sendTsBlinkPacket(SwitchStatus switchStatus)
        {
            Debug.Log("try sendTsBlinkPacket: " + switchStatus.id + ", " + switchStatus.proStrobeOn + "/" + switchStatus.proStrobeOff);

            if (!ProStatus.isPro ||
                deviceId != sPODDeviceTypes.Touchscreen ||
                CurrentDevOTA.AppVer < OTAInfo.VER_LR_TOUCH_BLINK_FIX ||
                (switchStatus.proStrobeOn == 0 || switchStatus.proStrobeOff == 0))
            {
                Debug.Log("fail... " + ProStatus.isPro + ", " + deviceId + ", " + CurrentDevOTA.AppVer);

                return;
            }

            lastSwitchPacketSent = Time.realtimeSinceStartup;

            List<byte> packet = new List<byte>();


            packet.Add(0x55); //delimiter
            packet.Add(12); //length
            packet.Add(0); //type

            packet.Add(0);
            packet.Add(0);

            packet.Add((byte)((switchStatus.id / 8) + 0x80));

            switch (switchStatus.id % 8)
            {
                case 0:
                    packet.Add(0x08);
                    break;
                case 1:
                    packet.Add(0x10);
                    break;
                case 2:
                    packet.Add(0x20);
                    break;
                case 3:
                    packet.Add(0x40);
                    break;
                case 4:
                    packet.Add(0x80);
                    break;
                case 5:
                    packet.Add(0x01);
                    break;
                case 6:
                    packet.Add(0x02);
                    break;
                case 7:
                    packet.Add(0x04);
                    break;
            }

            packet.Add(0);
            packet.Add((byte)switchStatus.proStrobeOn);
            packet.Add((byte)switchStatus.proStrobeOff);


            uint crc = crc32(0, packet.ToArray(), packet.Count);
            packet.AddRange(BitConverter.GetBytes(crc));


            addPacketToQueue(packet, sPOD_BLE.COMM_CHAR);
        }

        public void sendTsSwitchPacket(SwitchStatus switchStatus)
        {
            lastSwitchPacketSent = Time.realtimeSinceStartup;

            List<byte> packet = new List<byte>();

            //			packet.Add (Convert.ToByte(links[offset + 0] >> offset & 0xFF));

            int offset = switchStatus.id - (switchStatus.id % 8);

            packet.Add(12);// 10);
            packet.Add(0x00);
            packet.Add((byte)switchStatus.id);
            packet.Add(0x00);
            packet.Add((byte)(switchStatus.isMomentary ? 0x01 : 0x00));
            packet.Add((byte)(switchStatus.isDimmable ? 0x01 : 0x00));
            packet.Add((byte)(switchStatus.canFlash ? 0x01 : 0x00));
            packet.Add((byte)(switchStatus.canStrobe ? 0x01 : 0x00));

            packet.Add((byte)(links[switchStatus.id] >> 0 & 0xFF));
            packet.Add((byte)(links[switchStatus.id] >> 8 & 0xFF));
            packet.Add((byte)(links[switchStatus.id] >> 16 & 0xFF));
            packet.Add((byte)(links[switchStatus.id] >> 24 & 0xFF));

            //packet.Add ((byte)(links[switchStatus.id] >> offset & 0xFF));



            //			packet.Add ((byte)(sourceTemps[switchStatus.id/4] ? 0x01 : 0x00));

            //			packet.Add ((byte)links[currentSwitch.id]);
            //			packet.Add ((byte)(sourceTemps[currentSwitch.id/4] ? 0x01 : 0x00));

            //			Debug.Log("ts packet: " + switchStatus.id + ", " links[currentSwitch.IDictionary] + ", " links[switchStatus.IDictionary]);

            //addPacketToQueue (packet, deviceCharacteristic[deviceId, 4]);
            addPacketToQueue(packet, sPOD_BLE.TOUCH_CHAR);

            //			if (queueSent) {
            //				sendQueue ();
            //			}

            //			if(currentDevice != null)
            //			{
            //				Debug.Log("sendTsSwitchPacket: " + byteArrayToString(packet.ToArray()));
            //
            //				ble.WriteCharacteristic(currentDevice, deviceService, touchscreenCharacteristic, packet.ToArray(), 9, true);
            //			}
            //			else
            //			{
            //				Debug.Log("Error Data: " + byteArrayToString(packet.ToArray()));
            //			}
        }

        public void sendTsTextPacket(SwitchStatus switchStatus)
        {
            int i;

            List<byte> packet = new List<byte>();

            packet.Add(19);
            packet.Add(0x01);
            packet.Add((byte)switchStatus.id);
            packet.Add(0x00);


            for (i = 0; i < 10; i++)
            {

                if (i < switchStatus.label1.Length)
                {
                    packet.Add((byte)switchStatus.label1[i]);
                }
                else
                {
                    packet.Add(0x00);
                }
            }

            for (i = 0; i < 5; i++)
            {
                if (i < switchStatus.label2.Length)
                {
                    packet.Add((byte)switchStatus.label2[i]);
                }
                else
                {
                    packet.Add(0x00);
                }
            }

            //addPacketToQueue (packet, deviceCharacteristic[deviceId, 4]);
            addPacketToQueue(packet, sPOD_BLE.TOUCH_CHAR);


            packet.Clear();

            //			List<byte> packet2 = new List<byte>();

            packet.Add(19);
            packet.Add(0x01);
            packet.Add((byte)switchStatus.id);
            packet.Add(0x01);

            for (i = 5; i < 10; i++)
            {
                if (i < switchStatus.label2.Length)
                {
                    packet.Add((byte)switchStatus.label2[i]);
                }
                else
                {
                    packet.Add(0x00);
                }
            }

            for (i = 0; i < 10; i++)
            {
                if (i < switchStatus.label3.Length)
                {
                    packet.Add((byte)switchStatus.label3[i]);
                }
                else
                {
                    packet.Add(0x00);
                }
            }

            //addPacketToQueue (packet, deviceCharacteristic[deviceId, 4]);
            addPacketToQueue(packet, sPOD_BLE.TOUCH_CHAR);


            //			if(currentDevice != null)
            //			{
            //				Debug.Log("sendTsTextPacket: " + byteArrayToString(packet.ToArray()));
            //
            //				ble.WriteCharacteristic(currentDevice, deviceService, touchscreenCharacteristic, packet.ToArray(), 19, true);
            //			}
            //			else
            //			{
            //				Debug.Log("Error Data: " + byteArrayToString(packet.ToArray()));
            //			}

        }


        public bool processIcon(Sprite iconSprite)
        {
            //			Texture2D newtexture = new Texture2D (iconTexture.width, iconTexture.height, iconTexture.format, false);
            //
            //
            //			newtexture.LoadRawTextureData (iconTexture.GetRawTextureData ());

            Texture2D iconTexture = getTextureFromSprite(iconSprite);

            if (iconTexture == null)
            {
                Debug.Log("getTextureFromSprite() == null...");
                return false;
            }

            float scale;
            float finalSize = 64.0f;
            //			int finalSizeI = 64;

            int newWidth, newHeight;

            //			Debug.Log (iconTexture.format);

            float xsize = (float)iconTexture.width;
            float ysize = (float)iconTexture.height;

            if (xsize > ysize)
            {
                scale = xsize / finalSize;

            }
            else
            {
                scale = ysize / finalSize;
            }

            Debug.Log("Texture size: " + xsize + ", " + ysize + ", " + scale);

            newWidth = (int)(xsize / scale + 0.5f);
            newHeight = (int)(ysize / scale + 0.5f);

            Debug.Log("New texture size: " + newWidth + ", " + newHeight);


            int texturePadW = ((int)(finalSize) - newWidth) / 2;
            int texturePadH = ((int)(finalSize) - newHeight + 1) / 2;


            float incX = (1.0f / (float)newWidth);
            float incY = (1.0f / (float)newHeight);

            int pbloc = 0;


            for (int k = 0; k < 512; k++)
            {
                iconData[k] = 0;
            }

            float pixelRatio = 0.0f;

            for (int i = 0; i < newHeight; ++i)
            {
                for (int j = 0; j < newWidth; ++j)
                {

                    Color pixelColor = iconTexture.GetPixelBilinear((float)j * incX, (float)i * incY);

                    if (pixelColor.a > 0.5f && pixelColor.grayscale > 0.5f)
                    {
                        pbloc = (63 - texturePadH - i) * 8 + ((j + texturePadW) / 8);

                        iconData[pbloc] = (byte)(iconData[pbloc] | (0x80 >> ((j + texturePadW) % 8)));

                        pixelRatio += 1.0f / 4096.0f;

                    }
                }
            }

            pixelRatio = pixelRatio * 4096.0f / (float)(newWidth * newHeight);  // scale to portion of icon actually used

            if (pixelRatio > 0.95 || pixelRatio < 0.05)
            {
                /// either all white or all black, not a good monochrome icon
                return false;
            }

            return true;

            Texture2D getTextureFromSprite(Sprite sprite)
            {
                return sprite.texture;
            }

        }

        public void sendIconSelectPacket(SwitchStatus switchStatus)
        {

            if (switchStatus.isLegend && (isEnableCustomIcons || switchStatus.legendId != 255))
            {

                //				if (switchStatus.legendId == -1) {	// non-icon image
                //					return;
                //				}

                Debug.Log("Process Icon: " + switchStatus.legendId);

                if (processIcon(switchStatus.sprite))
                {
                    Debug.Log("Send Icon data");

                    sendIconDataPacket(switchStatus.id);
                }
            }



            List<byte> packet = new List<byte>();

            packet.Add(5);
            packet.Add(0x02);
            packet.Add((byte)switchStatus.id);

            if (isEnableCustomIcons || switchStatus.legendId != 255)
            {
                packet.Add((byte)(switchStatus.isLegend ? 0x01 : 0x00));
            }
            else
            {
                packet.Add(0x00);
            }


            packet.Add((byte)switchStatus.legendId);



            //			if (switchStatus.isLegend		) {
            //				packet.Add (0x01);
            //				packet.Add ((byte)(switchStatus.id+1));
            //				packet.Add ((byte)(switchStatus.legendId + 1));		// 0 for off, 1-32 for select
            //			} else {
            //				packet.Add (0x00);
            //			}

            //addPacketToQueue (packet, deviceCharacteristic[deviceId, 4]);
            addPacketToQueue(packet, sPOD_BLE.TOUCH_CHAR);

        }

        public byte[] iconData = new byte[512];

        public void sendIconDataPacket(int iconId)
        {
            int i, pn;

            if (iconId >= 32)
                return;

            List<byte> packet = new List<byte>();

            int dataPerPack = sizeOfMtu - 3 - 4;

            int numOfPackets = 512 / dataPerPack + (512 % dataPerPack > 0 ? 1 : 0);

            //			for (pn = 0; pn < 32; pn++) {
            for (pn = 0; pn < numOfPackets; pn++)
            {

                //				packet.Add (20);
                packet.Add((byte)(dataPerPack + 4));
                packet.Add(0x03);
                packet.Add((byte)iconId);
                packet.Add((byte)pn);

                //				for (i = 0; i < 16; i++) {
                for (i = 0; i < dataPerPack; i++)
                {

                    if ((pn * dataPerPack + i) >= 512)
                    {

                        //						packet.ind

                        packet[0] = (byte)(4 + i);

                        break;
                    }

                    //					packet.Add ((byte)(iconData [iconId, (pn * 16) + i]));
                    //					packet.Add ((byte)(iconData [(pn * 16) + i]));
                    packet.Add((byte)(iconData[(pn * dataPerPack) + i]));


                }


                //addPacketToQueue (packet, deviceCharacteristic[deviceId, 4]);
                addPacketToQueue(packet, sPOD_BLE.TOUCH_CHAR);

                packet.Clear();
            }

            //			if (queueSent) {
            //				sendQueue ();
            //			}

            //			if(currentDevice != null)
            //			{
            //				Debug.Log("sendIconDataPacket: " + byteArrayToString(packet.ToArray()));
            //
            //				ble.WriteCharacteristic(currentDevice, deviceService, touchscreenCharacteristic, packet.ToArray(), 20, true);
            //
            //
            //			}
            //			else
            //			{
            //				Debug.Log("Error Data: " + byteArrayToString(packet.ToArray()));
            //			}
        }


        public void sendSourceSESwitchPacket(SwitchStatus switchStatus)
        {
            byte[] packet = new byte[7];



            packet[0] = (byte)(sourceSEPin & 0xFF);
            packet[1] = (byte)((sourceSEPin >> 8) & 0xFF);
            //			packet[2] = (byte)(currentSourceId + 0x80);
            packet[2] = (byte)(switchStatus.id / 8 + 0x80);

            switch (switchStatus.id % 8)
            {
                case 0:
                    packet[3] = 0x08;
                    break;
                case 1:
                    packet[3] = 0x10;
                    break;
                case 2:
                    packet[3] = 0x20;
                    break;
                case 3:
                    packet[3] = 0x40;
                    break;
                case 4:
                    packet[3] = 0x80;
                    break;
                case 5:
                    packet[3] = 0x01;
                    break;
                case 6:
                    packet[3] = 0x02;
                    break;
                case 7:
                    packet[3] = 0x04;
                    break;
            }


            if (isStrobeSend)
            {
                packet[4] = 255;
                packet[5] = (byte)switchStatus.proStrobeOn;
                packet[6] = (byte)switchStatus.proStrobeOff;
            }
            else if (switchStatus.isOn)
            {
                if (switchStatus.isDimmable)
                {
                    packet[4] = (byte)Math.Round(switchStatus.value * 255);
                }
                else
                {
                    packet[4] = 255;
                }

                if (switchStatus.canFlash && switchStatus.isFlashing && !ProStatus.isPro)
                {
                    packet[5] = 10;
                    packet[6] = 10;
                }
                else if (switchStatus.canStrobe && switchStatus.isFlashing)
                {
                    if (ProStatus.isPro)
                    {
                        packet[5] = (byte)switchStatus.proStrobeOn;
                        packet[6] = (byte)switchStatus.proStrobeOff;
                    }
                    else
                    {
                        packet[5] = 1;
                        packet[6] = 4;
                    }
                }
                else
                {
                    packet[5] = 255;
                    packet[6] = 0;
                }

            }
            else
            {
                packet[4] = 0;
                packet[5] = 255;
                packet[6] = 0;
            }

            if (currentDevice != null)
            {
                Debug.Log("SE Data: " + byteArrayToString(packet));

                ble.WriteCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 1], packet, 7, true);
                /*
				OutputStatus output = new OutputStatus ();
				output.id = switchStatus.id;
				output.value = switchStatus.value;
				output.status = switchStatus.value > 0 ? 1 : 0;

				systemResponseSignal.Dispatch (SystemResponseEvents.OutputStatus, new Dictionary<string, object>{ {
						"OutputStatus",
						output
					} });
					*/


            }
            else
            {
                Debug.Log("SE Error Data: " + byteArrayToString(packet));
            }
        }

        int[] currentRCPMState = new int[6];

        public void sendRCPMSwitchPacket(SwitchStatus switchStatus)
        {
            if (isStrobeSend)
                return;

            byte[] packet = new byte[3];

            if (switchStatus.id < 6)
            {
                currentRCPMState[switchStatus.id] = switchStatus.isOn ? 1 : 0;
            }


            packet[0] = (byte)(sourceSEPin & 0xFF);
            packet[1] = (byte)((sourceSEPin >> 8) & 0xFF);

            int switchData = 0;

            for (int i = 0; i < 6; i++)
            {

                int mask = currentRCPMState[i] << i;

                switchData = switchData | mask;

            }

            packet[2] = (byte)switchData;

            if (currentDevice != null)
            {
                Debug.Log("sendRCPMSwitchPacket: " + byteArrayToString(packet));

                ble.WriteCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 1], packet, 3, true);


            }
            else
            {
                Debug.Log("Error Data: " + byteArrayToString(packet));
            }
        }

        //private bool needsInputsSettingsIgnore = true;

        public void sendCanProSwitchPacket(SwitchStatus switchStatus)
        {

            if (switchStatus == null || switchStatus.id < 0 || switchStatus.id > 31)
            {
                Debug.Log("sendCanProSwitchPacket() err: " + (switchStatus == null ? "switchStatus == null" : switchStatus.id.ToString()));
                return;
            }

            Debug.Log("sendCanProSwitchPacket(" + switchStatus.id + ") ");// + needsInputsSettingsIgnore);


            //if(needsInputsSettingsIgnore)
            //         {
            //	//return;
            //         }

            List<byte> packet = new List<byte>();


            packet.Add(0x55); //delimiter
            packet.Add(12); //length
            packet.Add(0); //type

            if (deviceId == sPODDeviceTypes.SwitchHD)
            {
                packet.Add((byte)(switchHDPin & 0xFF));
                packet.Add((byte)((switchHDPin >> 8) & 0xFF));
            }
            else
            {
                packet.Add((byte)(/*optionsModel.pin*/ 0 & 0xFF));
                packet.Add((byte)((/*optionsModel.pin*/ 0 >> 8) & 0xFF));
            }

            packet.Add((byte)((switchStatus.id / 8) + 0xC0));

            packet.Add(getSwitchOneHot(switchStatus.id));

            //switch(switchStatus.id % 8)
            //{
            //case 0:
            //	packet.Add (0x08);
            //	break;
            //case 1:
            //	packet.Add (0x10);
            //	break;
            //case 2:
            //	packet.Add (0x20);
            //	break;
            //case 3:
            //	packet.Add (0x40);
            //	break;
            //case 4:
            //	packet.Add (0x80);
            //	break;
            //case 5:
            //	packet.Add (0x01);
            //	break;
            //case 6:
            //	packet.Add (0x02);
            //	break;
            //case 7:
            //	packet.Add (0x04);
            //	break;
            //}

            //byte inputVal = (byte)((switchStatus.proIsInputEnabled ? 0x04 : 0) | (switchStatus.proIsInputLockout ? 0x02 : 0) | (switchStatus.proIsInputLockInvert ? 0x01 : 0));
            byte inputVal = (byte)(
                //0x80 | (ProStatus.isInputLinking ? 0x40 : 0) |
                0x20 |
                (switchStatus.proIsInputEnabled ? 0x04 : 0) |
                (switchStatus.proIsInputLockout ? 0x02 : 0) |
                (switchStatus.proIsInputLockInvert ? 0x01 : 0)
                );

            byte inputVal2 = (byte)(inputVal & 0x7F | 0x40);        // to indicate packet #2

            List<byte> packet2 = new List<byte>(packet);            // packet #2 same up till this point
                                                                    //List<byte> packet2 = packet.ToList<byte>();

            packet.Add(inputVal);
            packet2.Add(inputVal2);

            packet2.Add((byte)((links[switchStatus.id] >> 24) & 0xFF));
            packet2.Add((byte)((links[switchStatus.id] >> 16) & 0xFF));

            packet.Add((byte)((links[switchStatus.id] >> 8) & 0xFF));
            packet.Add((byte)((links[switchStatus.id] >> 0) & 0xFF));

            //int offset = switchStatus.id & ~0x07;		// num - num % 8

            //int mask = 0x01 << (switchStatus.id % 8);
            //int linkVal = (links[switchStatus.id] >> offset) & 0xFF;

            //packet.Add((byte)(linkVal | mask));
            ////packet.Add (0x00);

            //packet.Add (0x00);


            uint crc = crc32(0, packet.ToArray(), packet.Count);
            uint crc2 = crc32(0, packet2.ToArray(), packet2.Count);

            //Debug.Log ("CRC: " + crc);

            packet.AddRange(BitConverter.GetBytes(crc));
            packet2.AddRange(BitConverter.GetBytes(crc2));


            if (currentDevice != null)
            {
                Debug.Log("Data: & dev/name (sendCanProSwitchPacket()) " + byteArrayToString(packet.ToArray()) + " / " + byteArrayToString(packet2.ToArray()));

                addPacketToQueue(packet, deviceCharacteristic[deviceId, 0]);
                addPacketToQueue(packet2, deviceCharacteristic[deviceId, 0]);
            }
            else
            {
                Debug.Log("Error Data: " + byteArrayToString(packet.ToArray()));
            }
        }

        public void sendCanCOMMSwitchPacket(SwitchStatus switchStatus)
        {

            //Debug.Log("sendSwitchPacket");
            //Debug.Log ("Id: " + switchStatus.id);
            //Debug.Log ("Switch: " + (switchStatus.id % 8));
            //Debug.Log ("Source: " + (switchStatus.id / 8));

            //delimiter, length, type, data, crc
            //1, 1, 1, 7, 4
            //length = type + data + crc
            //length = 12

            List<byte> packet = new List<byte>();


            packet.Add(0x55); //delimiter
            packet.Add(12); //length
            packet.Add(0); //type

            if (deviceId == sPODDeviceTypes.SwitchHD)
            {
                packet.Add((byte)(switchHDPin & 0xFF));
                packet.Add((byte)((switchHDPin >> 8) & 0xFF));
            }
            else
            {
                packet.Add((byte)(/*optionsModel.pin*/ 0 & 0xFF));
                packet.Add((byte)((/*optionsModel.pin*/ 0 >> 8) & 0xFF));
            }

            packet.Add((byte)((switchStatus.id / 8) + 0x80));

            switch (switchStatus.id % 8)
            {
                case 0:
                    packet.Add(0x08);
                    break;
                case 1:
                    packet.Add(0x10);
                    break;
                case 2:
                    packet.Add(0x20);
                    break;
                case 3:
                    packet.Add(0x40);
                    break;
                case 4:
                    packet.Add(0x80);
                    break;
                case 5:
                    packet.Add(0x01);
                    break;
                case 6:
                    packet.Add(0x02);
                    break;
                case 7:
                    packet.Add(0x04);
                    break;
            }

            byte dimValue = 0;
            byte strobeOn = 255;
            byte strobeOff = 0;

            if (isStrobeSend)
            {
                dimValue = 255;
                strobeOn = (byte)switchStatus.proStrobeOn;
                strobeOff = (byte)switchStatus.proStrobeOff;
                //packet.Add (255);
                //packet.Add ((byte)switchStatus.proStrobeOn);
                //packet.Add ((byte)switchStatus.proStrobeOff);
            }
            else if (switchStatus.isOn)
            {
                if (switchStatus.isDimmable)
                {

                    dimValue = (byte)(Math.Round(switchStatus.value * 254f) + 1);
                    //packet.Add ((byte)(Math.Round(switchStatus.value * 254f) + 1));

                }
                else
                {

                    dimValue = 255;
                    //packet.Add (255);
                }


                if (switchStatus.isFlashing && (switchStatus.canStrobe || switchStatus.canFlash))
                {
                    if (ProStatus.isPro)
                    {
                        strobeOn = (byte)switchStatus.proStrobeOn;
                        strobeOff = (byte)switchStatus.proStrobeOff;
                    }
                    else if (switchStatus.canFlash)
                    {
                        strobeOn = 10;
                        strobeOff = 10;
                    }
                    else
                    {
                        strobeOn = 1;
                        strobeOff = 4;
                    }
                }
                else
                {
                    strobeOn = 255;
                    strobeOff = 0;
                }


                //if (switchStatus.canFlash && switchStatus.isFlashing && !ProStatus.isPro) {

                //	packet.Add (10);
                //	packet.Add (10);

                //} else if (switchStatus.canStrobe && switchStatus.isFlashing) {

                //	if (ProStatus.isPro) {
                //		packet.Add ((byte)switchStatus.proStrobeOn);
                //		packet.Add ((byte)switchStatus.proStrobeOff);
                //	} else {
                //		packet.Add (1);
                //		packet.Add (4);
                //	}

                //} else {

                //	packet.Add (255);
                //	packet.Add (0);
                //}



            }
            else
            {
                //packet.Add (0);
                //packet.Add (255);
                //packet.Add (0);
            }

            packet.Add(dimValue);
            packet.Add(strobeOn);
            packet.Add(strobeOff);

            uint crc = crc32(0, packet.ToArray(), packet.Count);

            //Debug.Log ("CRC: " + crc);

            packet.AddRange(BitConverter.GetBytes(crc));

            if (currentDevice != null)
            {
                Debug.Log("Data: & dev/name (sendCanCOMMSwitchPacket()) " + byteArrayToString(packet.ToArray()));

                //if (sendingLinking) {
                //ble.WriteCharacteristic(currentDevice, deviceService, deviceCharacteristic, packet.ToArray(), packet.Count, false);
                //} else {
                addPacketToQueue(packet, deviceCharacteristic[deviceId, 0]);
                //ble.WriteCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 0], packet.ToArray(), packet.Count, true);
                //}


            }
            else
            {
                Debug.Log("Error Data: " + byteArrayToString(packet.ToArray()));
            }
        }


        public void sendSwitchPacket(SwitchStatus switchStatus)
        {
            Debug.Log("sendCanCOMMSwitchPacket() " + switchStatus.id);
            lastSwitchPacketSent = Time.realtimeSinceStartup;
            //activityTimer.Stop();
            //activityTimer.Reset();
            //activityTimer.Start();

            if (isLink && !isStrobeSend)
            {

                int switchIndex = switchStatus.id;
                //				int sourceIndex = (switchStatus.id / 8);

                Debug.Log("linkIndex: " + linkIndex);

                if (linkIndex < 0)
                {
                    linkIndex = switchIndex;


                    for (int i = 0; i < 32; i++)
                    {
                        if (i != switchIndex)
                        {
                            Debug.Log("i: " + i);

                            if ((links[linkIndex] & (0x01 << i)) > 0)
                            {
                                switches[i].isOn = true;
                            }
                            else
                            {
                                switches[i].isOn = false;
                            }

                        }

                    }
                    Debug.Log("sendSwitchPacket");
                    sendSystemData();
                }

                if (switchStatus.isOn)
                {
                    links[linkIndex] |= 0x01 << switchIndex;
                }
                else
                {
                    if (linkIndex == switchIndex)
                    {
                        finishUpdatingLinking();

                        linkIndex = -1;
                        for (int i = 0; i < 32; i++)
                        {
                            switches[i].isOn = false;
                        }
                        sendSystemData();

                    }
                    else
                    {
                        links[linkIndex] &= ~(0x01 << switchIndex);
                    }
                }


                if (linkIndex >= 0)
                {

                    Debug.Log(linkIndex + " : " + Convert.ToString(links[linkIndex], 2).PadLeft(32, '0'));

                    if (links[linkIndex] == 0)
                        linkIndex = -1;
                }


                //				sendTsSwitchPacket (switchStatus);

                //if (ProStatus.isPro && ProStatus.isInputLinking)
                //{
                //	sendProSwitchPacket(linkIndex);
                //}

                systemRequestSignal.Dispatch(SystemRequestEvents.SendTsPackets, new Dictionary<string, object> { { SystemRequestEvents.SendTsPackets, switchStatus } });

                return;
            }


            switch (deviceId)
            {
                case sPODDeviceTypes.RCPM:
                    sendRCPMSwitchPacket(switchStatus);
                    break;
                case sPODDeviceTypes.SourceSE:
                    sendSourceSESwitchPacket(switchStatus);
                    break;
                default:
                    sendCanCOMMSwitchPacket(switchStatus);
                    break;
            }




        }


        #region otaCode

        OTAFile currentOTAFile = new OTAFile();

        public string[] otaFilePath;

        List<OTAFile> FoundOtaFiles = new List<OTAFile>();

        //		public int findOTAFiles ()
        IEnumerator findOTAFiles()
        {
            FoundOtaFiles = new List<OTAFile>();

            //int i = 0;

            UnityEngine.Object[] FoundFiles = Resources.LoadAll("otaFiles", typeof(TextAsset));

            foreach (TextAsset ta in FoundFiles)
            {

                if (ta.name.StartsWith("sPOD") || (ProStatus.isAppProEnabled && ta.name.StartsWith("PRO_sPOD")))
                {
                    //				if (ta.name.StartsWith ("sPOD")) {

                    currentOTAFile = new OTAFile();

                    currentOTAFile.FileName = ta.name;

                    string[] nameParts = currentOTAFile.FileName.Split(' ');
                    if (nameParts.Length < 5)
                        continue;

                    switch (nameParts[1])
                    {
                        case "Bantam":
                        case "BantamX":
                            currentOTAFile.BoardId = OTAInfo.BOARD_ID_BANTAM;
                            break;
                        case "BleTouch":
                            currentOTAFile.BoardId = OTAInfo.BOARD_ID_TOUCHSCREEN;
                            break;
                        case "SwitchHD":
                            currentOTAFile.BoardId = OTAInfo.BOARD_ID_SWITCH_HD;
                            break;
                        case "SourceLT":
                            currentOTAFile.BoardId = OTAInfo.BOARD_ID_SOURCE_LT;
                            break;
                        default:
                            continue;
                            //						return 0;
                    }

                    for (int j = 0; j < (nameParts[2].Length < currentOTAFile.BoardRev.Length ? nameParts[2].Length : currentOTAFile.BoardRev.Length); j++)
                    {

                        if ((char)nameParts[2][j] >= 'A' && (char)nameParts[2][j] <= 'Z')
                        {
                            currentOTAFile.BoardRev[j] = (byte)(nameParts[2][j] - 64);
                        }
                        else
                        {
                            continue;
                            //							return 0;
                        }
                    }

                    switch (nameParts[3])
                    {
                        case "app":
                            currentOTAFile.AppId = 01;
                            break;
                        case "stk":
                            currentOTAFile.AppId = 00;
                            break;
                        default:
                            continue;
                            //						return 0;
                    }

                    string[] verPart = nameParts[4].Split('_');
                    if (verPart.Length < 3)
                        continue;
                    //					verPart [0] = verPart [0].Substring (1, verPart [0].Length - 1);

                    uint p1, p2, p3;

                    if (!uint.TryParse((verPart[0].Substring(1, verPart[0].Length - 1)), out p1))
                        Debug.Log("Parse err (p1) ");

                    if (!uint.TryParse((verPart[1].Substring(0, verPart[1].Length)), out p2))
                        Debug.Log("Parse err (p2) ");

                    if (!uint.TryParse((verPart[2].Substring(0, verPart[2].Length)), out p3))
                        Debug.Log("Parse err (p3) ");

                    currentOTAFile.AppVer = (int)(((p1 << 8) & 0xFF00) | ((p2 << 4) & 0xF0) | (p3 & 0x0F));


                    parseOTAFile();

                    FoundOtaFiles.Add(currentOTAFile);


                    //Debug.Log("TextAsset Found: " + FoundOtaFiles[i].FileName +
                    //    " -> " + FoundOtaFiles[i].BoardId.ToString() +
                    //    ", " + byteArrayToString(FoundOtaFiles[i].BoardRev) +
                    //    ", " + FoundOtaFiles[i].AppId +
                    //    ", " + FoundOtaFiles[i].AppVer.ToString("X"));

                    //i++;
                }

                yield return null;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                try
                {
                    loadOta();
                    //				CurrentDevOTA = ES2.Load<DeviceOTA> ("CurrentDevOTA.txt?tag=CurrentDevOTA");
                    if (CurrentDevOTA.NewAppFile >= 0)
                        Debug.Log("CurrentDevOta loaded: " + CurrentDevOTA.NeedUpgrade + ", " + FoundOtaFiles[CurrentDevOTA.NewAppFile].FileName);
                    else
                        Debug.Log("CurrentDevOta empty");
                }
                catch (Exception e)
                {
                    CurrentDevOTA = new DeviceOTA();
                    saveOta();
                    Debug.Log("CurrentDevOta empty: " + e);
                }
            }

            yield break;
            //			return 0;
        }

        public void parseOTAFile()
        {
            List<string> lines = new List<string>();

            //			OTAFile file1 = new OTAFile();

            int i = 0;

            //			currentOTAFile.FileName

            TextAsset mytxtData = (TextAsset)Resources.Load("otaFiles/" + currentOTAFile.FileName);

            //file1 = board,id,version from filename


            string txt = mytxtData.text;
            lines = txt.Split(':').ToList();

            string identifier = lines.ElementAt(0);
            lines.RemoveAt(0);


            if (!uint.TryParse((identifier.Substring(0, 8)), System.Globalization.NumberStyles.AllowHexSpecifier, null, out currentOTAFile.SiliconId))
                Debug.Log("Parse err (SiI) ");

            if (!int.TryParse((identifier.Substring(8, 2)), System.Globalization.NumberStyles.AllowHexSpecifier, null, out currentOTAFile.SiliconRev))
                Debug.Log("Parse err (SiR) ");

            if (!int.TryParse((identifier.Substring(10, 2)), System.Globalization.NumberStyles.AllowHexSpecifier, null, out currentOTAFile.ChecksumType))
                Debug.Log("Parse err (Cks) ");



            //			Debug.Log("s id: " + file1.SiliconId.ToString("X"));


            string[] fileData = lines.ToArray();


            int val = 0;
            i = 0;

            currentOTAFile.RowID = new RowIDs[2];

            foreach (string n in fileData)
            {

                fileData[i] = fileData[i].Trim();

                if (!int.TryParse((fileData[i].Substring(0, 2)), System.Globalization.NumberStyles.AllowHexSpecifier, null, out val))
                    Debug.Log("Parse err " + i);

                //if (currentOTAFile.RowID [val].Count != null) {		/// invalid syntax, needs different check?
                currentOTAFile.RowID[val].Count++;
                //} else {
                //	currentOTAFile.RowID [val].Count = 1;
                //}

                i++;
            }

            currentOTAFile.TotalRowCount = i;

            currentOTAFile.RowData = new OTARows[currentOTAFile.TotalRowCount];
            i = 0;

            //			System.Globalization.NumberStyles.HexNumber

            foreach (string n in fileData)
            {

                if (!int.TryParse((fileData[i].Substring(0, 2)), System.Globalization.NumberStyles.AllowHexSpecifier, null, out currentOTAFile.RowData[i].RowId))
                    Debug.Log("Parse err (Rid) " + i);

                if (!int.TryParse((fileData[i].Substring(2, 4)), System.Globalization.NumberStyles.AllowHexSpecifier, null, out currentOTAFile.RowData[i].RowNum))
                    Debug.Log("Parse err (num) " + i);

                if (!int.TryParse((fileData[i].Substring(6, 4)), System.Globalization.NumberStyles.AllowHexSpecifier, null, out currentOTAFile.RowData[i].DataLen))
                    Debug.Log("Parse err (len) " + i);

                if (!int.TryParse((fileData[i].Substring(fileData[i].Length - 2, 2)), System.Globalization.NumberStyles.AllowHexSpecifier, null, out currentOTAFile.RowData[i].Checksum))
                    Debug.Log("Parse err (cks) " + i);

                currentOTAFile.RowData[i].DataArray = new byte[currentOTAFile.RowData[i].DataLen];

                for (int j = 0; j < currentOTAFile.RowData[i].DataLen; j++)
                {

                    if (!byte.TryParse((fileData[i].Substring(10 + (j * 2), 2)), System.Globalization.NumberStyles.AllowHexSpecifier, null, out currentOTAFile.RowData[i].DataArray[j]))
                        Debug.Log("Parse err (dat) " + i + "," + j);
                }

                i++;

            }

            //			Debug.Log("parseOTAFile: " + file1.RowData[0].DataArray[0].ToString("X") + " " + file1.RowData[0].DataArray[file1.RowData [0].DataLen - 1].ToString("X") );

            //if no err
            currentOTAFile.parseGood = true;

        }

        public void upgradeOTA()
        {

            //			if(CurrentDevOTA.NeedUpgrade == 2) {
            //
            //				Debug.Log ("Starting upload: " + FoundOtaFiles [CurrentDevOTA.NewStkFile].FileName + " " + FoundOtaFiles [CurrentDevOTA.NewAppFile].FileName);
            //
            //				utils.showAlert ("Uploading Firmware", "Preparing to Upload:\nStack:  " + firmVer(CurrentDevOTA.StkVer) + 
            //					" -> " + firmVer(FoundOtaFiles [CurrentDevOTA.NewStkFile].AppVer) + 
            //					"\nApp:  " + firmVer(CurrentDevOTA.AppVer) + 
            //					" -> " + firmVer(FoundOtaFiles [CurrentDevOTA.NewAppFile].AppVer) + "\nNote: this process will reset all device settings", logo);
            //			} else {
            //
            //				Debug.Log ("Starting upload: " + FoundOtaFiles [CurrentDevOTA.NewAppFile].FileName);
            //
            //				utils.showAlert ("Uploading Firmware", "Preparing to Upload:\nApplication:  " + firmVer(CurrentDevOTA.AppVer) + 
            //					" -> " + firmVer(FoundOtaFiles [CurrentDevOTA.NewAppFile].AppVer) + "\nNote: this process will reset all device settings", logo);
            //			}
            //		
            //			if (CurrentDevOTA.BoardId == 4500 && CurrentDevOTA.BoardRev == 4 && (CurrentDevOTA.StkVer <= 0x0104 || CurrentDevOTA.StkVer == 0xFFFF))
            //				utils.showAlert ("Warning:", "Go to mobile device settings > Bluetooth > " + CurrentDevOTA.DeviceName + " > Forget Device \nand then return to the app to continue with firmware upload", logo);



            enterBootloader();

            utils.showAlert("Please Wait", "Entering bootloader mode...", logo);


        }

        public string firmVer(int ver)
        {

            string verString = "v" + (ver / 0x0100) + "." + (ver % 0x0100 / 0x0010) + "." + (ver % 0x0010);

            return verString;
        }

        public void checkDevVersion()
        {
            //needsInputsSettingsIgnore = false;

            Debug.Log("checkDevVersion() " + CurrentDevOTA.BoardId);

            switch (CurrentDevOTA.BoardId)
            {   // check if the version is able to be upgraded
                case OTAInfo.BOARD_ID_BANTAM:

                    //if(CurrentDevOTA.AppVer < ProInfo.FIRST_PRO_BANTAM)
                    //               {
                    //	needsInputsSettingsIgnore = true;
                    //}

                    if (CurrentDevOTA.AppVer < OTAInfo.FIRST_VER_BANTAM)
                    {
                        CurrentDevOTA.NeedUpgrade = OTAInfo.NOT_COMPATIBLE;

                        //needsInputsSettingsIgnore = true;

                        return;
                    }

                    break;
                case OTAInfo.BOARD_ID_TOUCHSCREEN:

                    if (CurrentDevOTA.BoardRev == OTAInfo.BOARD_REV_STD_TOUCHSCREEN)
                    {
                        if (CurrentDevOTA.AppVer < OTAInfo.FIRST_VER_STD_TOUCHSCREEN)
                        {
                            CurrentDevOTA.NeedUpgrade = OTAInfo.NOT_COMPATIBLE;
                            return;
                        }
                    }
                    else
                    {
                        if (CurrentDevOTA.AppVer < OTAInfo.FIRST_VER_LR_TOUCHSCREEN)
                        {
                            CurrentDevOTA.NeedUpgrade = OTAInfo.NOT_COMPATIBLE;
                            return;
                        }
                    }

                    break;
                case OTAInfo.BOARD_ID_SWITCH_HD:

                    if (CurrentDevOTA.AppVer < OTAInfo.FIRST_VER_SWITCH_HD)
                    {
                        CurrentDevOTA.NeedUpgrade = OTAInfo.NOT_COMPATIBLE;
                        return;
                    }

                    break;
                case OTAInfo.BOARD_ID_SOURCE_LT_OLD:
                case OTAInfo.BOARD_ID_SOURCE_LT:
                    //needsInputsSettingsIgnore = true;

                    if (CurrentDevOTA.AppVer < OTAInfo.FIRST_VER_SOURCE_LT)
                    {
                        CurrentDevOTA.NeedUpgrade = OTAInfo.NOT_COMPATIBLE;
                        return;
                    }

                    break;
                //case OTAInfo.BOARD_ID_SOURCE_LT_OLD:

                //		needsInputsSettingsIgnore = true;

                //		return;

                //		break;
                default:
                    return;
            }

            bool valGood = false;
            int fileNum = -1;

            CurrentDevOTA.NeedUpgrade = OTAInfo.NEED_READ;
            CurrentDevOTA.NewAppVer = 0;

            Debug.Log("read ota: " + CurrentDevOTA.BoardId + "r" + CurrentDevOTA.BoardRev + " " + firmVer(CurrentDevOTA.AppVer) + "/" + firmVer(CurrentDevOTA.StkVer));

            foreach (OTAFile cyacd in FoundOtaFiles)
            {

                fileNum++;

                if (CurrentDevOTA.BoardId != cyacd.BoardId)
                {   // check board
                    continue;
                }


                valGood = false;

                for (int i = 0; i < cyacd.BoardRev.Length; i++)
                {
                    if (CurrentDevOTA.BoardRev == cyacd.BoardRev[i])
                    {
                        valGood = true;
                    }
                }

                if (!valGood)
                {                                   // check board rev
                    continue;
                }

                //				if ((CurrentDevOTA.StkVer <= cyacd.AppVer) && (cyacd.AppVer > CurrentDevOTA.NewStkVer) && (cyacd.AppId == 0)) {		/// debug... 
                if ((CurrentDevOTA.StkVer < cyacd.AppVer) && (cyacd.AppVer > CurrentDevOTA.NewStkVer) && (cyacd.AppId == 0))
                {       // check for new stack

                    CurrentDevOTA.NeedUpgrade = OTAInfo.NEED_STK_UPDATE;
                    CurrentDevOTA.UpgradeStk = true;
                    CurrentDevOTA.NewStkVer = cyacd.AppVer;
                    CurrentDevOTA.NewStkFile = fileNum;

                }
                else if ((CurrentDevOTA.AppVer <= cyacd.AppVer) && (cyacd.AppVer > CurrentDevOTA.NewAppVer) && (cyacd.AppId == 1))
                {           // check for new app (or current app with new stack)


                    //					if (cyacd.AppVer >= CurrentDevOTA.AppVer) {		/// debug...
                    if (cyacd.AppVer > CurrentDevOTA.AppVer)
                    {

                        CurrentDevOTA.NewAppVer = cyacd.AppVer;
                        CurrentDevOTA.NewAppFile = fileNum;

                        if (CurrentDevOTA.NeedUpgrade < 2)
                        {

                            CurrentDevOTA.NeedUpgrade = OTAInfo.NEED_APP_UPDATE;
                        }

                    }
                    else if (cyacd.AppVer == CurrentDevOTA.AppVer)
                    {

                        CurrentDevOTA.NewAppVer = cyacd.AppVer;
                        CurrentDevOTA.NewAppFile = fileNum;
                    }
                }
                else if (isEnableGlobalOTA && (cyacd.AppVer > CurrentDevOTA.NewAppVer) && (cyacd.AppId == 1))
                {
                    CurrentDevOTA.NewAppVer = cyacd.AppVer;
                    CurrentDevOTA.NewAppFile = fileNum;
                }

                //				if(CurrentDevOTA.NeedUpgrade < 1){
                //					
                //					CurrentDevOTA.NeedUpgrade = 0;
                //				}

            }

            if (CurrentDevOTA.NeedUpgrade < 1)
            {

                CurrentDevOTA.NeedUpgrade = OTAInfo.NEED_NO_UPDATE;
            }


            if (CurrentDevOTA.NeedUpgrade == OTAInfo.NEED_STK_UPDATE && CurrentDevOTA.NewAppVer == 0)
            {   // prevent attempt to upgrade stack only

                CurrentDevOTA.NeedUpgrade = OTAInfo.NEED_NO_UPDATE;
            }

            Debug.Log("Need Upgrade: " + CurrentDevOTA.NeedUpgrade);

            if (CurrentDevOTA.NeedUpgrade > 1)
                Debug.Log("New Stack: " + FoundOtaFiles[CurrentDevOTA.NewStkFile].FileName);

            if (CurrentDevOTA.NeedUpgrade > 0)
                Debug.Log("New App: " + FoundOtaFiles[CurrentDevOTA.NewAppFile].FileName);



            //			saveOta();

        }

        public void SendCheckOTAPacket()
        {

            List<byte> packet = new List<byte>();

            packet.Add(0x02);
            packet.Add(0x01);


            if (currentDevice != null)
            {
                Debug.Log("SendCheckOTAPacket: " + byteArrayToString(packet.ToArray()));

                //ble.WriteCharacteristic (currentDevice, deviceService [deviceId], deviceCharacteristic [deviceId, 2], packet.ToArray (), packet.Count, true);
                //ble.WriteCharacteristic(currentDevice, deviceService[deviceId], sPOD_BLE.OTA_CHAR, packet.ToArray(), packet.Count, true);
                addPacketToQueue(packet, sPOD_BLE.OTA_CHAR);
            }
            else
            {
                Debug.Log("Error Data: " + byteArrayToString(packet.ToArray()));
            }
        }


        public void enterBootloader()
        {

            List<byte> packet = new List<byte>();

            /// << full packet?

            packet.Add(0x02);
            packet.Add(0xFF);


            if (currentDevice != null) // && current device == 
            {
                CurrentDevOTA.Upgrading = true;

                Debug.Log("enterBootloader: " + byteArrayToString(packet.ToArray()));

                //ble.WriteCharacteristic (currentDevice, deviceService [deviceId], deviceCharacteristic [deviceId, 2], packet.ToArray (), packet.Count, true);
                //ble.WriteCharacteristic(currentDevice, deviceService[deviceId], sPOD_BLE.OTA_CHAR, packet.ToArray(), packet.Count, true);
                clearPacketQueue();
                addPacketToQueue(packet, sPOD_BLE.OTA_CHAR);
            }
            else
            {
                Debug.Log("Error Data: " + byteArrayToString(packet.ToArray()));
            }



        }

        OTACommands OTACommand = new OTACommands();
        OTAErrorCodes OTAErrorCode = new OTAErrorCodes();

        byte lastCommand = 0x3B; // exit boot

        //		bool transferIdle = true;
        int currentRow = 0;
        int currentRowId = -1;
        int iOffset = 0;

        int tryBootCount = 0;

        public void createCommandPacket(byte command, UInt16 dataLength)
        {
            UInt16 newDataLength = dataLength;
            int checksum = 0;

            List<byte> packet = new List<byte>();

            packet.Add(OTACommand.COMMAND_START_BYTE);
            packet.Add(command);
            packet.Add((byte)(dataLength & 0xFF));
            packet.Add((byte)((dataLength >> 8) & 0xFF));



            if (command == OTACommand.GET_FLASH_SIZE)
            {
                packet.Add((byte)(currentOTAFile.RowData[currentRow].RowId));
            }

            if (command == OTACommand.VERIFY_ROW || command == OTACommand.PROGRAM_ROW)
            {
                packet.Add((byte)(currentOTAFile.RowData[currentRow].RowId));
                packet.Add((byte)(currentOTAFile.RowData[currentRow].RowNum & 0xFF));
                packet.Add((byte)((currentOTAFile.RowData[currentRow].RowNum >> 8) & 0xFF));

                newDataLength -= 3;
            }

            if (command == OTACommand.SEND_DATA || command == OTACommand.PROGRAM_ROW)
            {

                for (int i = 0; i < newDataLength; i++)
                {

                    packet.Add(currentOTAFile.RowData[currentRow].DataArray[i + iOffset]);
                }
            }


            if (currentOTAFile.ChecksumType > 0 ? true : false)
            {
                checksum = 0; // > crc
            }
            else
            {
                //				checksum = packet.Sum();
                //				checksum = ~(checksum) + 1;

                foreach (byte n in packet)
                {
                    checksum += n;
                }

                checksum = ~checksum + 1;
            }

            packet.Add((byte)(checksum & 0xFF));
            packet.Add((byte)((checksum >> 8) & 0xFF));
            packet.Add(OTACommand.COMMAND_END_BYTE);


            lastCommand = command;


            if (deviceId != sPODDeviceTypes.Uninit)
            {//null) {
             //				Debug.Log ("XXXXXXXXXXXXXXXXXXXXXX");
                ble.WriteCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 0], packet.ToArray(), packet.Count, true);
            }

            //			Debug.Log ("ota Comm: " + command + ", " + currentDevice + ", " + deviceService [deviceId] + ", " + deviceCharacteristic [deviceId, 0] + ", " + byteArrayToString(packet.ToArray ()));
            //			Debug.Log ("ota Comm: " + command.ToString("X") + ", " + byteArrayToString(packet.ToArray ()));

            //			if(otaTimeoutInstance != null)
            routineRunner.StopCoroutine(otaTimeoutInstance);

            if (command != OTACommand.EXIT_BOOTLOADER)
            {
                otaTimeoutInstance = otaTimeout();
                routineRunner.StartCoroutine(otaTimeoutInstance);
            }
            else
            {
                Screen.sleepTimeout = SleepTimeout.SystemSetting;
            }




        }

        public void transmitOTAFile()
        {
            if (currentDevice == null)
                return;

            if (tryBootCount == 2)
            {
                //			if (tryBootCount > 2 && CurrentDevOTA.NeedUpgrade != -2) {

                //				CurrentDevOTA.NeedUpgrade = -2;
                utils.hideAlert();
                systemResponseSignal.Dispatch(SystemResponseEvents.SelectOtaDevice, null);

            }

            if (CurrentDevOTA.NeedUpgrade > 0)
            {

                //			if (true || transferIdle) {

                utils.showAlert("Please Wait", "Uploading Firmware\nStarting...", logo);

                //				return; ///

                //				transferIdle = false;

                tryBootCount = 0;

                currentRow = 0;
                iOffset = 0;

                currentOTAFile = new OTAFile();

                if (CurrentDevOTA.NeedUpgrade == 2)
                {
                    currentOTAFile = FoundOtaFiles[CurrentDevOTA.NewStkFile];
                }
                else
                {
                    currentOTAFile = FoundOtaFiles[CurrentDevOTA.NewAppFile];
                }

                //				createCommandPacket (OTACommand.EXIT_BOOTLOADER, 0);

                createCommandPacket(OTACommand.ENTER_BOOTLOADER, 0);        ///

                Screen.sleepTimeout = SleepTimeout.NeverSleep;

            }
            else
            {

                if (CurrentDevOTA.lastOtaIdentifier == currentDevice)
                {
                    tryBootCount++;
                }
                else
                {
                    tryBootCount = 0;
                    CurrentDevOTA.lastOtaIdentifier = currentDevice;
                }

                if (tryBootCount != 3)
                {
                    createCommandPacket(OTACommand.EXIT_BOOTLOADER, 0);
                }
                //				ble.DisconnectFromPeripheral (currentDevice);
                //				bleSignalHandler ("Tick", null);

            }

        }

        public void HandleOTAPacket(string characteristic, byte[] packetData)
        {

            //			Debug.Log ("Received OTA: " + byteArrayToString (packetData.ToArray ()));

            if (characteristic == deviceCharacteristic[deviceId, 0])
            {

                byte command = lastCommand;
                byte error = packetData[1];
                bool err = false;
                bool fault = false;

                if (error != OTAErrorCode.SUCCESS)
                {
                    err = true;
                    Debug.Log("ERROR: " + error + " " + command + " " + currentRow);

                    //					utils.showAlert ("Please Wait", "Uploading Firmware\nERROR: " + error + " " + command + " " + currentRow, logo);
                }


                if (!err && command == OTACommand.ENTER_BOOTLOADER)
                {

                    uint readSiId = 0;
                    int readSiRv = -1;

                    //					utils.showAlert ("Please Wait", "Uploading Firmware\nCheck si", logo);

                    for (int i = 0; i < 4; i++)
                    {
                        readSiId |= (uint)(packetData[4 + i] << (8 * i));

                    }

                    Debug.Log("Read Id: " + readSiId.ToString("X"));

                    readSiRv = (int)packetData[8];

                    if (readSiId == currentOTAFile.SiliconId && readSiRv == currentOTAFile.SiliconRev)
                    {

                        createCommandPacket(OTACommand.GET_FLASH_SIZE, 1);

                    }
                    else
                    {

                        if (!CurrentDevOTA.RecDevOta &&
                                CurrentDevOTA.BoardId == OTAInfo.BOARD_ID_TOUCHSCREEN &&
                                CurrentDevOTA.BoardRev == OTAInfo.BOARD_REV_LR_TOUCHSCREEN)
                        {

                            CurrentDevOTA.BoardRev = OTAInfo.BOARD_REV_STD_TOUCHSCREEN;
                            CurrentDevOTA.AppVer = OTAInfo.FIRST_VER_STD_TOUCHSCREEN;

                            CurrentDevOTA.NeedUpgrade = -1;

                            checkDevVersion();

                            if (CurrentDevOTA.NeedUpgrade > 0)
                            {
                                transmitOTAFile();
                            }
                            else
                            {
                                createCommandPacket(OTACommand.EXIT_BOOTLOADER, 0);
                            }

                            return;
                        }

                        fault = true;
                        Debug.Log("Silicon Mismatch: " + readSiId.ToString("X") + " " + readSiRv.ToString("X"));
                        Debug.Log("Silicon Mismatch: " + currentOTAFile.SiliconId.ToString("X") + " " + currentOTAFile.SiliconRev.ToString("X"));
                    }

                }
                else if (!err && command == OTACommand.GET_FLASH_SIZE)
                {

                    //					utils.showAlert ("Please Wait", "Uploading Firmware\ngfs", logo);

                    uint firstRow = 0;
                    uint lastRow = 0;

                    for (int i = 0; i < 2; i++)
                    {
                        firstRow |= (uint)(packetData[4 + i] << (8 * i));
                    }

                    for (int i = 0; i < 2; i++)
                    {
                        lastRow |= (uint)(packetData[6 + i] << (8 * i));
                    }

                    currentRowId = currentOTAFile.RowData[currentRow].RowId;

                    currentOTAFile.RowID[currentOTAFile.RowData[currentRow].RowId].StartRow = firstRow;
                    currentOTAFile.RowID[currentOTAFile.RowData[currentRow].RowId].EndRow = lastRow;

                    Debug.Log("Flash size " + currentOTAFile.RowData[currentRow].RowId + ": " + firstRow.ToString("X") + "/" + lastRow.ToString("X"));

                    writeNextOTARowData();

                }
                else if (command == OTACommand.SEND_DATA)
                {
                    //					utils.showAlert ("Please Wait", "Uploading Firmware\nSend dat", logo);
                    if (!err)
                    {
                        writeOTARowData();
                    }
                    else
                    {
                        fault = true;
                        Debug.Log("Error writing data: " + currentRow + " " + error);
                    }
                }
                else if (command == OTACommand.PROGRAM_ROW)
                {
                    if (!err)
                    {

                        createCommandPacket(OTACommand.VERIFY_ROW, 3);

                    }
                    else
                    {
                        fault = true;
                        Debug.Log("Error writing row: " + currentRow + " " + error);
                    }
                }
                else if (!err && command == OTACommand.VERIFY_ROW)
                {

                    uint deviceCks = (uint)packetData[4];

                    //					Debug.Log ("rec cks: " + deviceCks);

                    uint svdCks = (uint)currentOTAFile.RowData[currentRow].Checksum;
                    uint rowID = (uint)currentOTAFile.RowData[currentRow].RowId;
                    uint rowNum1 = (uint)(currentOTAFile.RowData[currentRow].RowNum);
                    uint rowNum2 = (uint)(currentOTAFile.RowData[currentRow].RowNum >> 8);
                    uint rowLen1 = (uint)(currentOTAFile.RowData[currentRow].DataLen);
                    uint rowLen2 = (uint)(currentOTAFile.RowData[currentRow].DataLen >> 8);

                    byte sum = (byte)(svdCks + rowID + rowNum1 + rowNum2 + rowLen1 + rowLen2);

                    if (sum == deviceCks)
                    {

                        currentRow++;

                        int perc = currentRow * 100 / currentOTAFile.TotalRowCount;

                        if (currentRow % 20 == 0)
                        {
                            Debug.Log("completed: " + perc + "%");
                        }

                        if (CurrentDevOTA.UpgradeStk)
                        {
                            if (CurrentDevOTA.NeedUpgrade == 2)
                            {
                                utils.showAlert("Please Wait", "Uploading Firmware\nStack: " + perc + "%" + (perc == 100 ? " Installing" : "") + "\nApplication: Pending", logo);
                            }
                            else
                            {
                                utils.showAlert("Please Wait", "Uploading Firmware\nStack: 100% Done!\nApplication: " + perc + "%", logo);
                            }
                        }
                        else
                        {
                            utils.showAlert("Please Wait", "Uploading Firmware\nApplication: " + perc + "%", logo);
                        }

                        //						utils.showAlert ("Please Wait", "Uploading Firmware\nCompleted: " + perc + "%", logo);



                        if (currentRow < currentOTAFile.TotalRowCount)
                        {
                            writeNextOTARowData();
                        }
                        else
                        {
                            createCommandPacket(OTACommand.VERIFY_CHECKSUM, 0);
                        }

                    }
                    else
                    {
                        fault = true;
                        Debug.Log("Error with row checksum: " + currentRow + " " + sum + " " + deviceCks);
                    }

                }
                else if (!err && command == OTACommand.VERIFY_CHECKSUM)
                {

                    bool appValid = packetData[4] > 0 ? true : false;

                    if (appValid)
                    {

                        //						transferIdle = true;

                        CurrentDevOTA.NeedUpgrade--;

                        Debug.Log("Upload Done");
                        createCommandPacket(OTACommand.EXIT_BOOTLOADER, 0);
                        //						CurrentDevOTA = new DeviceOTA ();

                        //						ble.DisconnectFromPeripheral (currentDevice);
                        //						bleSignalHandler ("Tick", null);


                        if (CurrentDevOTA.NeedUpgrade == 0)
                        {
                            //							utils.showAlert ("Upload Finished", "Go to your mobile device settings -> 'Bluetooth' -> '" + CurrentDevOTA.DeviceName + "' -> 'Forget Device' before reconnecting", logo);

                            if (CurrentDevOTA.DeviceName.Length < 1)
                            {
                                switch (CurrentDevOTA.BoardId)
                                {
                                    case OTAInfo.BOARD_ID_BANTAM:
                                        CurrentDevOTA.DeviceName = "sPOD Link #00******";
                                        break;
                                    case OTAInfo.BOARD_ID_TOUCHSCREEN:
                                        CurrentDevOTA.DeviceName = "sPOD Link #01******";
                                        break;
                                    case OTAInfo.BOARD_ID_SWITCH_HD:
                                        CurrentDevOTA.DeviceName = "sPOD Link #02******";
                                        break;
                                    case OTAInfo.BOARD_ID_SOURCE_LT:
                                        CurrentDevOTA.DeviceName = "sPOD Link #03******";
                                        break;
                                    default:
                                        CurrentDevOTA.DeviceName = "sPOD Link #********";
                                        break;
                                }
                            }

                            string otaUnpairMessage = "    Upload Finished!" +
                                "\nImportant: If you have not done so yet, go to: " +
                                "\n -> mobile device settings" +
                                "\n -> Bluetooth" +
                                "\n -> " + CurrentDevOTA.DeviceName + "" +
                                "\n -> Forget/Unpair" +
                                "\n   Then rescan and go through the pairing process as described in your user manual";

                            systemResponseSignal.Dispatch(SystemResponseEvents.SelectOtaUpgrade, new Dictionary<string, object>{
                                {"otaUpgradeMessage", otaUnpairMessage},
								//{"CancelOn",false},
								//{"QuitOnOk",false},
								{"isRestoreText", false },
                                {"isInfoOnly", true }

                            });

                            CurrentDevOTA = new DeviceOTA();
                            saveOta();      // clear 

                            lastDeviceName = "null";        // to prevent immediate reconnections
                            lastDeviceId = "null";
                            ES2.Save(lastDeviceName, "lastDevice.txt?tag=lastDevice");
                            ES2.Save(lastDeviceId, "lastDevice.txt?tag=lastDeviceId");

                            Screen.sleepTimeout = SleepTimeout.SystemSetting;
                        }

                    }
                    else
                    {
                        fault = true;
                        Debug.Log("App Invalid: " + error);
                    }



                }

                if (fault)
                {
                    utils.hideAlert();
                }
            }



        }

        void writeNextOTARowData()
        {

            if (currentRowId != currentOTAFile.RowData[currentRow].RowId)
            {

                createCommandPacket(OTACommand.GET_FLASH_SIZE, 1);

            }
            else if (currentOTAFile.RowData[currentRow].RowNum >= currentOTAFile.RowID[currentOTAFile.RowData[currentRow].RowId].StartRow &&
               currentOTAFile.RowData[currentRow].RowNum <= currentOTAFile.RowID[currentOTAFile.RowData[currentRow].RowId].EndRow)
            {

                iOffset = 0;

                writeOTARowData();


            }
            else
            {
                Debug.Log("Row out of bounds: " + currentRow + " " + currentOTAFile.RowData[currentRow].RowNum.ToString("X"));
            }

        }

        void writeOTARowData()
        {

            if ((currentOTAFile.RowData[currentRow].DataLen - iOffset) > OTACommand.MAX_DATA_SIZE)
            {

                createCommandPacket(OTACommand.SEND_DATA, OTACommand.MAX_DATA_SIZE);

                iOffset += OTACommand.MAX_DATA_SIZE;

            }
            else
            {
                createCommandPacket(OTACommand.PROGRAM_ROW, (ushort)(currentOTAFile.RowData[currentRow].DataLen - iOffset + 3));
            }

        }
        #endregion

        public void finishUpdatingLinking()
        {
            Debug.Log("finishUpdatingLinking(): " + linkIndex + ", " + ProStatus.isInputLinking);

            if (linkIndex >= 0 && linkIndex < 32)
            {
                if (ProStatus.isPro)// && ProStatus.isInputLinking)
                {
                    //sendProSwitchPacket(linkIndex); ///-> sendCanProSwitchPacket ?
                    sendCanProSwitchPacket(switches[linkIndex]);
                }

                sendCommSwitchSettingsPacket(switches[linkIndex]);
            }
        }


        void proSyncSettings()
        {

            Debug.Log("Sync Now");

            if (!ProStatus.isPro)
                return;

            Debug.Log("is compat");

            if (ProStatus.isSyncFromApp)
            {

                needsSyncToApp = false;

                //if (sendProCanPacketsInstance != null)
                //	routineRunner.StopCoroutine (sendProCanPacketsInstance);

                //sendProCanPacketsInstance = sendProCanPackets ();
                //routineRunner.StartCoroutine (sendProCanPacketsInstance);

                //				List<byte> packet = new List<byte> ();
                //				byte devId;

                switch (deviceId)
                {
                    case sPODDeviceTypes.Bantam:
                        Debug.Log("start sync to Bantam");

                        sendDeepSleepProPacket();

                        //					startProHdSyncTime = Time.realtimeSinceStartup;

                        ProStatus.needsSync = true;

                        if (sendProBantamSwitchesInstance != null)
                            routineRunner.StopCoroutine(sendProBantamSwitchesInstance);

                        sendProBantamSwitchesInstance = sendProBantamSwitches();
                        routineRunner.StartCoroutine(sendProBantamSwitchesInstance);

                        //					sendProSwitchPacket ();

                        // send switch timers/auto on(16B)
                        // send switches on/off

                        break;
                    case sPODDeviceTypes.Touchscreen:
                        Debug.Log("start sync to Touchscreen");
                        sendDeepSleepProPacket();
                        // send isDeepSleepDisabled
                        ProStatus.needsSync = true;

                        // send switch dimmable(32x1b)/momentary(32x1b)/strobe(32x1b)/flash(32x1b)/link(32x1B)
                        // send switches on/off
                        // send switch text (32 x 30B) start with current address?
                        // send icon data (32 x 4B-516B)

                        // possibly...
                        // sleep timer (1B)
                        // address (1B-5B)
                        // brightness (1B)
                        // on/off road (1b)
                        // current states of flash(32 x 2b)/strobe(32 x 2b)

                        break;
                    case sPODDeviceTypes.SwitchHDv2:
                        Debug.Log("start sync to SwitchHD");


                        ProStatus.needsSync = true;

                        sendDeepSleepProPacket();

                        sendSwitchHDSettingsPacket(false);


                        if (sendCommSettingsPacketsInstance != null)
                            routineRunner.StopCoroutine(sendCommSettingsPacketsInstance);

                        sendCommSettingsPacketsInstance = sendCommSettingsPackets();
                        routineRunner.StartCoroutine(sendCommSettingsPacketsInstance);


                        startProHdSyncTime = Time.realtimeSinceStartup;



                        //sendDeepSleepProPacket() triggers sendSwitchHDSettingsPacket (true);
                        // send switch dimmable(8x1b)/momentary(8x1b)/strobe(8x1b)/flash(8x1b)
                        // send sleep timer (1B)
                        // send address (1B-5B)
                        // send backlight/indicator leds (4x1B)

                        // sendSwitchHDSettingsPacket(true) triggers sendSwitchHDLinks() x 4
                        // send switches link (8-32x1B)



                        // send switches on/off


                        break;
                    default:
                        ProStatus.needsSync = false;
                        return;
                }


            }
            else
            {

                needsSyncToApp = true;

                Debug.Log("Try sync read");

                // read Command

                List<byte> packet = new List<byte>();
                byte devId;
                byte isWritable;
                byte isTempWritable;
                byte needsRead;

                packet.Add(0x03);   // length

                switch (deviceId)
                {
                    case sPODDeviceTypes.Bantam:
                        Debug.Log("start sync from Bantam");
                        devId = 0;
                        break;
                    case sPODDeviceTypes.Touchscreen:
                        Debug.Log("start sync from Touchscreen");
                        devId = 1;
                        break;
                    case sPODDeviceTypes.SwitchHDv2:
                        Debug.Log("start sync from SwitchHD");
                        devId = 2;
                        break;
                    //				case sPODDeviceTypes.SourceLT:
                    //					Debug.Log ("start sync from SourceLT");
                    //					devId = 3;
                    //					break;
                    default:
                        return;
                }

                needsRead = 1;
                isWritable = (byte)(ProStatus.isSyncFromApp ? 1 : 0);
                isTempWritable = 0;

                packet.Add((byte)((devId << 6) | isTempWritable << 2 | isWritable << 1 | needsRead));  // r/w

                if (devId == 1 || devId == 2)
                {
                    packet.Add(0x01);       // indicating 32sw linking
                }
                else
                {
                    packet.Add(0x00);
                }

                if (currentDevice != null)
                {
                    Debug.Log("proSyncSettings: " + byteArrayToString(packet.ToArray()));

                    //ble.WriteCharacteristic (currentDevice, deviceService [deviceId], deviceCharacteristic [deviceId, 3], packet.ToArray (), packet.Count, true);
                    //ble.WriteCharacteristic(currentDevice, deviceService[deviceId], sPOD_BLE.PRO_CHAR, packet.ToArray(), packet.Count, true);
                    addPacketToQueue(packet, sPOD_BLE.PRO_CHAR);
                }
                else
                {
                    Debug.Log("Error Data: " + byteArrayToString(packet.ToArray()));
                }

            }

        }

        //		private int lastSwitchId = 0;

        private void disableSetup()
        {
            if (isLink)
            {
                finishUpdatingLinking();
                isLink = false;
                for (int i = 0; i < 32; i++)
                {
                    switches[i].isOn = false;
                    switches[i].isMomentary = lastMomentary[i];
                    switches[i].isDimmable = lastDim[i];
                }
                sendSystemData();
            }

            isSetup = false;

            if (setupButton != null)
                setupButton.GetComponent<ButtonView>().setOn(false);


            if (linkButton != null)
                linkButton.GetComponent<ButtonView>().setOn(false);

            systemResponseSignal.Dispatch(SystemResponseEvents.LinkOff, null);
            save();
        }

        private void otaDisableSetup()
        {
            if (isLink)
            {
                finishUpdatingLinking();
                isLink = false;
                for (int i = 0; i < 32; i++)
                {
                    switches[i].isOn = false;
                    switches[i].isMomentary = lastMomentary[i];
                    switches[i].isDimmable = lastDim[i];
                }
                //				sendSystemData ();
            }

            isSetup = false;

            if (setupButton != null)
                setupButton.GetComponent<ButtonView>().setOn(false);


            if (linkButton != null)
                linkButton.GetComponent<ButtonView>().setOn(false);

            systemResponseSignal.Dispatch(SystemResponseEvents.LinkOff, null);
            //			save();
        }

        bool isStrobeSend = false;

        SwitchStatus currentSwitch = null;
        IEnumerator sendLinksInstance;
        IEnumerator sendLinks()
        {
            //			yield return null; //delay a frame
            //			yield return new WaitForSeconds(0.04f)// delay for 40 ms

            //			Debug.Log ("link check");

            if (!isLink && currentSwitch != null)
            {

                for (int i = 0; i < 32; i++)
                {

                    if (i != currentSwitch.id)
                    {


                        if (((links[currentSwitch.id] >> i) & 0x01) != 0)
                        {

                            switches[i].isOn = currentSwitch.isOn;
                            //Debug.Log ("Send switch: " + i);

                            sendSwitchPacket(switches[i]);
                            //yield return new WaitForSeconds (0.02f);// delay for 20 ms
                        }
                    }
                }
                sendSystemData();
            }
            //sendingLinking = false;

            yield break;
        }



        public void systemRequestHandler(string key, Dictionary<string, object> data)
        {

            Debug.Log("System Request: " + key);

            switch (key)
            {

                case SystemRequestEvents.EnableDebugLog:

                    System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
                    int debugStop = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds + (60 * 30);

                    debugTimer = (60 * 30);

                    PlayerPrefs.SetInt("DebugTimerStop", debugStop);
                    enableDebugLog();

                    break;

                case SystemRequestEvents.Save:
                    save();
                    break;

                case SystemRequestEvents.IsPaused:

                    bool isPaused = utils.getValueForKey<bool>(data, SystemRequestEvents.IsPaused);

                    if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android)
                    {
                        if (isPaused)
                        {
                            Debug.Log("isPaused =  " + isPaused);

                            //						if (Time.realtimeSinceStartup > 30.0f && Time.realtimeSinceStartup - lastConnectingTime > 10.0f) {
                            if (Time.realtimeSinceStartup - lastConnectingTime > 10.0f)
                            {
                                Debug.Log("App is paused disconnect...");

                                //ble.DisconnectFromPeripheral(currentDevice);
                                bleDisconnectAsync();
                                //ble.PauseWithState(true);
                            }

                        }
                        else
                        {
                            Debug.Log("isPaused =  " + isPaused);
                            //						if (Time.realtimeSinceStartup > 30.0f && Time.realtimeSinceStartup - lastConnectingTime > 10.0f) {
                            //if (Time.realtimeSinceStartup - lastConnectingTime > 30.0f) {
                            if (Time.realtimeSinceStartup - lastConnectingTime > 20.0f)
                            {

                                Debug.Log("App unpaused disconnect...");

                                //ble.PauseWithState(false);
                                bleDisconnectAsync();

                                //ble.DisconnectFromPeripheral(currentDevice);		/// need time to disconnect?
                                //                        nextBleState = "Disconnected";
                                //                        bleSignalHandler("Tick", null);
                                //                        bleSignalHandler("Tick", null);
                            }
                        }
                    }

                    break;
                case SystemRequestEvents.SelectDevice:
                    DeviceItem deviceItem = utils.getValueForKey<DeviceItem>(data, SystemRequestEvents.SelectDevice);

                    if (deviceItem != null)
                    {
                        deviceFound = false;
                        lastConnectingTime = 0;
                        nextBleState = "Connecting";
                        currentDevice = deviceItem.deviceInfo.Value;
                        currentDeviceName = deviceItem.deviceInfo.Key;
                        deviceId = getDeviceId(currentDeviceName);
                        foundDeviceCharacteritics = new bool[deviceCharacteristicCount[deviceId]];
                        bleSignalHandler("Tick", null);
                    }

                    break;
                case SystemRequestEvents.SwitchUpdate:
                    SwitchStatus status = utils.getValueForKey<SwitchStatus>(data, "SwitchStatus");

                    if (status != null)
                    {
                        //Debug.Log ("SwitchStatus: " + status.isOn);

                        //					lastSwitchId = status.id;



                        //					Debug.Log ("packet sentq: " + status.id);
                        //
                        sendSwitchPacket(status);

                        Debug.Log("packet sent: " + status.id);

                        //if (ProStatus.isPro && !needsInputsSettingsIgnore) {

                        //	sendCanProSwitchPacket (status);
                        //}

                        currentSwitch = status;

                        //if already running stop
                        routineRunner.StopCoroutine(sendLinksInstance);

                        sendLinksInstance = sendLinks();

                        //then start a new one
                        routineRunner.StartCoroutine(sendLinksInstance);

                        systemResponseSignal.Dispatch(SystemResponseEvents.SwitchUpdate, data);
                    }

                    break;
                case SystemRequestEvents.ShowSwitchSetup:
                    disableSetup();
                    systemResponseSignal.Dispatch(SystemResponseEvents.ShowSwitchSetup, data);

                    break;
                case SystemRequestEvents.ShowSourceSetup:
                    disableSetup();
                    systemResponseSignal.Dispatch(SystemResponseEvents.ShowSourceSetup, data);
                    break;
                case SystemRequestEvents.SwitchHdSettings:

                    if (data.ContainsKey("switchHDWake"))
                    {
                        bool wake = utils.getValueForKey<bool>(data, "switchHDWake");
                        switchHDWake = wake;
                    }

                    if (data.ContainsKey("switchHDTimer"))
                    {
                        int timer = utils.getValueForKey<int>(data, "switchHDTimer");
                        switchHDTimer = timer;
                    }

                    break;
                case SystemRequestEvents.ShowProPanel:
                    disableSetup();
                    systemResponseSignal.Dispatch(SystemResponseEvents.ShowProPanel, data);
                    break;
                case SystemRequestEvents.ShowHelp:
                    disableSetup();


                    break;
                case SystemRequestEvents.SystemData:
                    sendSystemData();
                    break;
                case SystemRequestEvents.UpdateSourceId:

                    if (data.ContainsKey("Id"))
                    {
                        int tempId = utils.getValueForKey<int>(data, "Id");
                        currentSourceId = tempId;
                    }

                    //int tempId = utils.getValueForKey<int>(data, "Id");

                    //if (tempId != null) {
                    //	currentSourceId = tempId;
                    //}

                    if (deviceId == sPODDeviceTypes.SwitchHD || deviceId == sPODDeviceTypes.SwitchHDv2)
                    {
                        sendSwitchHDSettingsPacket(false);
                    }

                    systemResponseSignal.Dispatch(SystemResponseEvents.SourceIdUpdated, data);
                    break;
                case SystemRequestEvents.SendTsPackets:
                    SwitchStatus tsSwitches = utils.getValueForKey<SwitchStatus>(data, SystemRequestEvents.SendTsPackets);

                    if (tsSwitches != null && deviceId == sPODDeviceTypes.Touchscreen)
                    {

                        //					Debug.Log ("Send TS switch settings");
                        sendTsSwitchPacket(tsSwitches);
                        sendTsBlinkPacket(tsSwitches);
                        //					Debug.Log ("Send TS switch Text");
                        sendTsTextPacket(tsSwitches);
                        //					Debug.Log ("Send TS switch Icon");
                        sendIconSelectPacket(tsSwitches);
                        //					Debug.Log ("Sent all TS info");

                        //					} else {
                        //						if (tsSwitches.id == 32) {
                        //							sendTsSwitchPacket (tsSwitches);
                        //						}
                        //					}
                    }

                    break;
                //case SystemRequestEvents.SendTsLinkPackets:
                //	SwitchStatus tslSwitches = utils.getValueForKey<SwitchStatus> (data, SystemRequestEvents.SendTsLinkPackets);

                //		if (tslSwitches != null && deviceId == sPODDeviceTypes.Touchscreen)
                //		{
                //			//					if (tsSwitches.id < 32) {

                //			Debug.Log("Send TS switch (link) settings");

                //			sendTsSwitchPacket(tslSwitches);
                //			//					Debug.Log ("Send TS switch Text");
                //			//					sendTsTextPacket (tsSwitches);
                //			//					Debug.Log ("Send TS switch Icon");
                //			//					sendIconSelectPacket (tsSwitches);
                //			//					Debug.Log ("Sent all TS info");

                //			//					} else {
                //			//						if (tsSwitches.id == 32) {
                //			//							sendTsSwitchPacket (tsSwitches);
                //			//						}
                //			//					}
                //		}

                //			break;
                case SystemRequestEvents.UpdateSystemData:
                    SwitchStatus[] tSwitches = utils.getValueForKey<SwitchStatus[]>(data, "switches");
                    //				ProSwitchStatus[] tProSwitches = utils.getValueForKey<ProSwitchStatus[]> (data, "proSwitches");


                    if (tSwitches != null && tSwitches.Length == 32)
                    {
                        switches = tSwitches;
                    }

                    string[] tSourceNames = utils.getValueForKey<string[]>(data, "sourceNames");
                    if (tSourceNames != null && tSourceNames.Length == 4)
                    {
                        sourceNames = tSourceNames;
                    }

                    Vector3[] tSourceColors = utils.getValueForKey<Vector3[]>(data, "sourceColors");
                    if (tSourceColors != null && tSourceColors.Length == 4)
                    {
                        sourceColors = tSourceColors;
                    }

                    bool[] tSourceTemps = utils.getValueForKey<bool[]>(data, "sourceTemps");
                    if (tSourceTemps != null && tSourceTemps.Length == 4)
                    {
                        sourceTemps = tSourceTemps;
                    }

                    sendProData();
                    sendSystemData();

                    //if(sendProBantamSwitchesInstance != null)
                    //	routineRunner.StopCoroutine (sendProBantamSwitchesInstance);

                    //sendProBantamSwitchesInstance = sendProBantamSwitches ();
                    //routineRunner.StartCoroutine (sendProBantamSwitchesInstance);


                    //if(sendProCanPacketsInstance != null)
                    //	routineRunner.StopCoroutine (sendProCanPacketsInstance);

                    //sendProCanPacketsInstance = sendProCanPackets ();
                    //routineRunner.StartCoroutine (sendProCanPacketsInstance);


                    break;
                case SystemRequestEvents.SelectOtaDevice:
                    {
                        string otaButton = utils.getValueForKey<string>(data, "otaButton");

                        //				Debug.Log ("rec ota button press: " + otaButton);

                        if (otaButton == "Cancel")
                            break;

                        CurrentDevOTA.NeedUpgrade = OTAInfo.NEED_READ;
                        CurrentDevOTA.RecDevOta = false;

                        switch (otaButton)
                        {
                            case "Bantam":
                            case "BantamX":
                                CurrentDevOTA.BoardId = OTAInfo.BOARD_ID_BANTAM;
                                CurrentDevOTA.BoardRev = OTAInfo.BOARD_REV_BANTAM;
                                CurrentDevOTA.StkVer = 0xFFFF;
                                //				CurrentDevOTA.StkVer = 0x0100;
                                CurrentDevOTA.AppVer = OTAInfo.FIRST_VER_BANTAM;
                                break;
                            case "Touchscreen":
                                CurrentDevOTA.BoardId = OTAInfo.BOARD_ID_TOUCHSCREEN;
                                //				CurrentDevOTA.BoardRev = 3;
                                CurrentDevOTA.BoardRev = OTAInfo.BOARD_REV_LR_TOUCHSCREEN;
                                CurrentDevOTA.StkVer = 0xFFFF;
                                //				CurrentDevOTA.StkVer = 0x0100;
                                CurrentDevOTA.AppVer = OTAInfo.FIRST_VER_LR_TOUCHSCREEN;
                                break;
                            case "SwitchHD":
                                CurrentDevOTA.BoardId = OTAInfo.BOARD_ID_SWITCH_HD;
                                CurrentDevOTA.BoardRev = OTAInfo.BOARD_REV_SWITCH_HD;
                                CurrentDevOTA.StkVer = 0xFFFF;
                                //				CurrentDevOTA.StkVer = 0x0100;
                                CurrentDevOTA.AppVer = OTAInfo.FIRST_VER_SWITCH_HD;
                                break;
                            case "SourceLt":
                                CurrentDevOTA.BoardId = OTAInfo.BOARD_ID_SOURCE_LT;
                                CurrentDevOTA.BoardRev = OTAInfo.BOARD_REV_SOURCE_LT;
                                CurrentDevOTA.StkVer = 0xFFFF;
                                //				CurrentDevOTA.StkVer = 0x0100;
                                CurrentDevOTA.AppVer = OTAInfo.FIRST_VER_SOURCE_LT;
                                break;
                            default:
                                CurrentDevOTA.NeedUpgrade = 0;
                                break;
                        }


                        if (CurrentDevOTA.NeedUpgrade != 0)
                        {
                            checkDevVersion();
                        }

                        transmitOTAFile();
                    }
                    break;
                case SystemRequestEvents.SelectOtaUpgrade:
                    {
                        //				string otaButton1 = utils.getValueForKey<string> (data, "otaButton");

                        //				Debug.Log ("rec ota button press: " + otaButton);

                        //				if (otaButton == "Cancel")
                        //					break;
                        //
                        //				CurrentDevOTA.NeedUpgrade = -1;
                        //				CurrentDevOTA.RecDevOta = false;

                        //				CurrentDevOTA.Upgrading;

                        if (isEnableGlobalOTA && CurrentDevOTA.NeedUpgrade == OTAInfo.NEED_NO_UPDATE)
                        {
                            CurrentDevOTA.NeedUpgrade = OTAInfo.NEED_APP_UPDATE;
                        }

                        upgradeOTA();

                        otaDisableSetup();

                        //				if(true){
                        if (CurrentDevOTA.BoardId == OTAInfo.BOARD_ID_TOUCHSCREEN &&
                           CurrentDevOTA.BoardRev == OTAInfo.BOARD_REV_LR_TOUCHSCREEN &&
                           (CurrentDevOTA.StkVer <= 0x0104 || CurrentDevOTA.StkVer == 0xFFFF))
                        {

                            string otaUnpairMessage = "Warning:" +
                                                     "\n   If the firmware update doen't start in 10 seconds, go to your mobile device settings" +
                                                     "\n -> Bluetooth" +
                                                     "\n -> " + CurrentDevOTA.DeviceName + "" +
                                                     "\n -> Forget/Unpair" +
                                                     "\n   and then return to the app to continue with firmware upload" +
                                                     "\n   Note: you may have to \"Upgrade\" a second time for full device update";

                            systemResponseSignal.Dispatch(SystemResponseEvents.SelectOtaUpgrade, new Dictionary<string, object> {
                            {"otaUpgradeMessage", otaUnpairMessage},
                            {"isInfoOnly", true }
							 //{"CancelOn", false},
							 //{"QuitOnOk", false}
						});
                        }
                    }
                    break;
                case SystemRequestEvents.DisplayPermissionMessage:
                    {
#if UNITY_ANDROID
                        string permissionString = utils.getValueForKey<string>(data, "PermissionString");
                        int permissionResp = utils.getValueForKey<int>(data, "PermissionResponse");

                        if (!String.IsNullOrEmpty(permissionString) && permissionResp >= 0)
                        {
                            Debug.Log("rec SystemRequestEvents.DisplayPermissionMessage: " + permissionString + ", " + permissionResp);

                            if (permissionResp == (int)AndroidRuntimePermissions.Permission.ShouldAsk)
                            {
                                AndroidRuntimePermissions.RequestPermission(permissionString);
                            }
                            else if (permissionResp == (int)AndroidRuntimePermissions.Permission.Denied)
                            {
                                AndroidRuntimePermissions.OpenSettings();
                            }
                        }
                        else
                        {
                            Debug.Log("rec SystemRequestEvents.DisplayPermissionMessage null....");
                        }
#endif
                    }
                    break;
                case SystemRequestEvents.SelectPurchasePro:
                    {
                        bool isUnPurchase = utils.getValueForKey<bool>(data, "isUnPurchase");

                        if (isUnPurchase)
                        {
                            ProStatus.isPro = false;    // for testing
                            Debug.Log("UNPURCHASE");
                            utils.showAlert("Pro-Series", "Purchase Failed", logo);
                        }
                        else
                        {
                            ProStatus.isPro = true;
                            Debug.Log("PURCHASE");
                            utils.showAlert("Pro-Series", "Purchase Successful!", logo);
                        }
                        /// purchase logic here before proceeding <<<

                        savePro();
                        sendProData();

                        if (ProStatus.isPro)
                        {
                            sendTurnOnProPacket();
                        }

                    }
                    break;
                case SystemRequestEvents.UpdateProSettings:
                    {

                        ProStatus.isAutoSync = utils.getValueForKey<bool>(data, "EnableSync");
                        ProStatus.isSyncFromApp = utils.getValueForKey<bool>(data, "SyncFromApp");
                        ProStatus.isDisableDeepSleep = utils.getValueForKey<bool>(data, "DisableDeepSleep");

                        bool lastInputLink = ProStatus.isInputLinking;
                        ProStatus.isInputLinking = utils.getValueForKey<bool>(data, "EnableInputLink");

                        bool syncNow = utils.getValueForKey<bool>(data, "SyncNow");

                        savePro();

                        if (syncNow)
                        {

                            proSyncSettings();
                        }
                        else
                        {
                            sendDeepSleepProPacket();



                            //if(ProStatus.isInputLinking != lastInputLink)
                            //                  {
                            //	sendCanProSwitchPacket(switches[0]);		// for each source
                            //	sendCanProSwitchPacket(switches[8]);
                            //	sendCanProSwitchPacket(switches[16]);
                            //	sendCanProSwitchPacket(switches[24]);

                            //	//if (sendProBantamSwitchesInstance != null)
                            //	//	routineRunner.StopCoroutine(sendProBantamSwitchesInstance);

                            //	//sendProBantamSwitchesInstance = sendProBantamSwitches();
                            //	//routineRunner.StartCoroutine(sendProBantamSwitchesInstance);
                            //}
                        }


                    }
                    break;
                case SystemRequestEvents.ResetAppSettings:
                    {
                        /// reset app with flag to not load?

                        //					ProStatus = new ProSeriesStatus();
                        //					ProSwitch = new ProSwitchStatus[32];
                        //					for (int i = 0; i < 32; i++) {
                        //						//					Debug.Log ("ps" + i);
                        //						if (ProSwitch [i] == null) {
                        //							ProSwitch [i] = new ProSwitchStatus ();
                        //						}
                        //					}

                        ProStatus.isDisableDeepSleep = false;
                        ProStatus.isInputLinking = false;

                        savePro();

                        Debug.Log("reset app settings");

                        for (int i = 0; i < 32; i++)
                        {
                            switches[i] = new SwitchStatus(i);
                            //switches [i].label1 = "";
                            //switches [i].label2 = "Switch " + (i + 1);
                            //switches [i].label3 = "";

                            switches[i].sprite = logo.GetComponent<Image>().sprite;
                        }

                        for (int i = 0; i < 4; i++)
                        {
                            sourceNames[i] = "Source " + (i + 1);
                            sourceColors[i] = new Vector3(1f, 1f, 1f);
                        }

                        for (int i = 0; i < 32; i++)
                        {
                            links[i] = 0;
                        }

                        save();

                        //					sendProData ();
                        sendSystemData();
                        //					sendProSwitchPacket ();

                        if (sendProBantamSwitchesInstance != null)
                            routineRunner.StopCoroutine(sendProBantamSwitchesInstance);

                        sendProBantamSwitchesInstance = sendProBantamSwitches();
                        routineRunner.StartCoroutine(sendProBantamSwitchesInstance);


                        if (sendProCanPacketsInstance != null)
                            routineRunner.StopCoroutine(sendProCanPacketsInstance);

                        sendProCanPacketsInstance = sendProCanPackets();
                        routineRunner.StartCoroutine(sendProCanPacketsInstance);
                    }
                    break;
                case SystemRequestEvents.UpdateProName:
                    {
                        string rawName = utils.getValueForKey<string>(data, "RawName");
                        string newName = utils.getValueForKey<string>(data, "NewName");
                        string devUuid = utils.getValueForKey<string>(data, "DevUuid");

                        //					savePro ();

                        if (newName == getDeviceString(rawName, null))
                        {
                            if (friendlyNames.ContainsKey(devUuid))
                            {
                                friendlyNames.Remove(devUuid);
                            }
                        }
                        else if (friendlyNames.ContainsKey(devUuid))
                        {
                            friendlyNames[devUuid] = newName;
                        }
                        else
                        {
                            friendlyNames.Add(devUuid, newName);
                        }

                        Debug.Log("New name: " + devUuid + ", " + newName);

                        savePro();

                        sendProData();

                    }
                    break;
                case SystemRequestEvents.EnteredPasskey:
                    {
                        string id = utils.getValueForKey<string>(data, "DevId");
                        uint myPasskey = utils.getValueForKey<uint>(data, "enteredPasskey");


                        Debug.Log("SystemRequestEvents.EnteredPasskey: " + id + "/" + currentDevice + ", " + myPasskey);

                        if (String.Compare(id, currentDevice) == 0 && data.ContainsKey("enteredPasskey") && myPasskey > 0)
                        {
                            if (devicePasskeys.ContainsKey(currentDevice))
                            {
                                devicePasskeys.Remove(currentDevice);
                            }

                            devicePasskeys.Add(currentDevice, myPasskey);
                            savePasskeys();

                            SendSecurityPasskey(myPasskey);
                        }

                        systemResponseSignal.Dispatch(SystemResponseEvents.SecurityData, new Dictionary<string, object> {
                            { "TurnOn", false },
                        });
                    }
                    break;

                case SystemRequestEvents.UpdateStrobe:
                    SwitchStatus strobeSwitch = utils.getValueForKey<SwitchStatus>(data, "UpdateStrobe");

                    if (strobeSwitch != null)
                    {// && deviceId == sPODDeviceTypes.Touchscreen) {

                        isStrobeSend = true;

                        sendSwitchPacket(strobeSwitch);

                        isStrobeSend = false;


                    }

                    break;
                //			case SystemRequestEvents.UpdateProCanData:
                ////				SwitchStatus status = utils.getValueForKey<SwitchStatus> (data, "SwitchStatus");
                //				SwitchStatus proCanSwitch = utils.getValueForKey<SwitchStatus> (data, "UpdateStrobe");
                //
                //				if (strobeSwitch != null){// && deviceId == sPODDeviceTypes.Touchscreen) {
                //
                //					isStrobeSend = true;
                //
                //					sendSwitchPacket (strobeSwitch);
                //
                //					isStrobeSend = false;
                //
                ////						lastData, switchSetupId);
                //				}
                //
                //				break;

                case SystemRequestEvents.BantamLowPowerTog:
                    {
                        if (data.ContainsKey("isBantamLowPower"))
                        {
                            bool isBantamLowPower = utils.getValueForKey<bool>(data, "isBantamLowPower");

                            sendDeepSleepBantamPacket(isBantamLowPower, false);
                        }
                    }
                    break;
                case SystemRequestEvents.UpdatedSwitchSettings:
                    {
                        if (data.ContainsKey("SwitchStatus"))
                        {
                            SwitchStatus UpdatedSwitch = utils.getValueForKey<SwitchStatus>(data, "SwitchStatus");

                            if (deviceId == sPODDeviceTypes.SwitchHDv2)
                            {
                                sendCommSwitchSettingsPacket(UpdatedSwitch);
                            }

                            if (ProStatus.isPro)
                            {
                                sendCanProSwitchPacket(UpdatedSwitch);

                                if (deviceId == sPODDeviceTypes.Bantam)
                                {
                                    sendProSwitchPacket(UpdatedSwitch);
                                }
                            }


                        }



                    }
                    break;
            }
        }

        private void sendSystemData()
        {
            systemResponseSignal.Dispatch(SystemResponseEvents.SystemData, new Dictionary<string, object>{
                {"switches", switches},
                {"sourceNames", sourceNames},
                {"sourceColors", sourceColors},
                {"sourceTemps", sourceTemps},
                {"switchHDColors", switchHDColors},
                {"switchHDSource", switchHDSource},
                {"switchHDTimer", switchHDTimer},
                {"switchHDWake", switchHDWake},
            });
        }

        private void sendProData()
        {

            //			if (!isAppProEnabledFlag || (Application.platform != RuntimePlatform.IPhonePlayer && Application.platform != RuntimePlatform.OSXEditor))
            //				return;

            //bool isDeepSleepComp = false;
            bool isBantamComp = false;
            string connMessage = null;
            string verMess = null;

            //			Debug.Log ("ent send pro");

            int Compatibility = ProInfo.NOT_CONNECTED;

            if (currentDevice == null)
            {
                connMessage = "No device connected \n -";
            }
            else
            {

                Compatibility = ProInfo.NOT_COMPATIBLE;

                switch (CurrentDevOTA.BoardId)
                {   // check if the version is able to be upgraded
                    case OTAInfo.BOARD_ID_BANTAM:

                        if (CurrentDevOTA.AppVer >= ProInfo.FIRST_PRO_BANTAM)
                        {
                            Compatibility = ProInfo.IS_COMPATIBLE;
                            //isDeepSleepComp = true;
                        }

                        isBantamComp = true;

                        break;
                    case OTAInfo.BOARD_ID_TOUCHSCREEN:
                        if (CurrentDevOTA.BoardRev >= OTAInfo.BOARD_REV_LR_TOUCHSCREEN)
                        {
                            if (CurrentDevOTA.AppVer >= ProInfo.FIRST_PRO_TOUCHSCREEN)
                            {
                                Compatibility = ProInfo.IS_COMPATIBLE;
                                //isDeepSleepComp = true;
                            }
                        }
                        else
                        {
                            Compatibility = ProInfo.NOT_COMPATIBLE;
                        }
                        break;
                    case OTAInfo.BOARD_ID_SWITCH_HD:

                        if (CurrentDevOTA.AppVer >= ProInfo.FIRST_PRO_SWITCH_HD)
                        {
                            Compatibility = ProInfo.IS_COMPATIBLE;
                            //isDeepSleepComp = true;
                        }

                        break;
                    default:
                        Compatibility = ProInfo.NOT_COMPATIBLE;
                        //					return;
                        break;
                }

                if (Compatibility == ProInfo.IS_COMPATIBLE)
                {
                    ProStatus.deviceIsCompatible = true;
                }
                else
                {
                    ProStatus.deviceIsCompatible = false;
                }

                if (CurrentDevOTA.NeedUpgrade == OTAInfo.NEED_READ)
                {
                    verMess = "(Checking Version Number)";
                    Compatibility = ProInfo.NOT_CONNECTED;
                }
                else
                {
                    verMess = firmVer(CurrentDevOTA.AppVer);

                    if (CurrentDevOTA.NeedUpgrade == OTAInfo.NOT_COMPATIBLE)
                    {
                        if (CurrentDevOTA.AppVer == 0)
                        {
                            verMess = "(Legacy Firmware)";
                        }
                        else
                        {
                            verMess += " (Legacy Firmware)";
                        }
                        //						verMess += " (Legacy Firmware)";
                        Compatibility = ProInfo.NOT_COMPATIBLE;
                    }
                    else if (CurrentDevOTA.NeedUpgrade == OTAInfo.NEED_NO_UPDATE)
                    {
                        verMess += " (Latest Firmware)";
                    }
                    else
                    {
                        verMess += " (Needs Upgrade)";
                    }

                }



                connMessage = "Connected to: " + getDeviceString(currentDeviceName, currentDevice) + "\n" + verMess;
                //					firmVer (CurrentDevOTA.AppVer) + (CurrentDevOTA.NeedUpgrade > 0 ? "(Needs Upgrade)" : "(Latest)") ;
            }

            //			isBantamComp = true;

            string niceDevName = string.Empty;

            if (currentDevice != null)
            {
                niceDevName = getDeviceString(currentDeviceName, currentDevice);
            }

            byte devAdd = CurrentDevOTA.addressRead;

            systemResponseSignal.Dispatch(SystemResponseEvents.ProData, new Dictionary<string, object>{
                {"ProStatus", ProStatus},
//				{"ProSwitch", ProSwitches},
				{"ConnMess", connMessage},
                {"Compatibility", Compatibility},
//				{"isDeepSleepComp", isDeepSleepComp},
				{"isBantamComp", isBantamComp},
                {"DeviceUuid", currentDevice},
                {"RawDevName", currentDeviceName},
                {"NiceDevName", niceDevName},
                {"ConnAddress", devAdd},
            });



        }

        GameObject setupButton;
        GameObject linkButton;

        bool isLink = false;
        bool[] lastMomentary = new bool[32];
        bool[] lastDim = new bool[32];
        int[] links = new int[32];
        int linkIndex = -1;

        public void uiHandler(string key, string type, Dictionary<string, object> data)
        {

            //Debug.Log("asc: UI: " + key + ", " + type);

            if (key == "Logo" && type == UiEvents.Presence)
            {
                logo = utils.getValueForKey<GameObject>(data, "AttachedObject");
                utils.showAlert("Please Wait", "Starting up...", logo);

                sendSystemData();
                systemRequestSignal.Dispatch(SystemRequestEvents.UpdateSourceId, new Dictionary<string, object> { { "Id", 0 } });

                load();

                //				Debug.Log ("logo load?");
                //				Debug.Log (data);

            }

            if (key == "Dismiss Alert")
            {
                utils.hideAlert();
            }

            switch (type)
            {
                case UiEvents.Click:
                    switch (key)
                    {
                        case "IndicatorTest On":
                            systemResponseSignal.Dispatch(SystemResponseEvents.CurrentIndicatorOn, null);
                            break;
                        case "IndicatorTest Off":
                            systemResponseSignal.Dispatch(SystemResponseEvents.CurrentIndicatorOff, null);
                            break;
                        case "Source SE Pair":

                            GameObject pins = utils.getAttachedObject(data);
                            InputField newPin = pins.transform.Find("New PIN").GetComponentInChildren<InputField>();
                            InputField oldPin = pins.transform.Find("Old PIN").GetComponentInChildren<InputField>();

                            if (newPin != null && oldPin != null)
                            {

                                int oldPinNum = Int32.Parse(oldPin.text);
                                int newPinNum = Int32.Parse(newPin.text);

                                Debug.Log(oldPin.text + ", " + oldPinNum);
                                Debug.Log(newPin.text + ", " + newPinNum);


                                if (oldPin.text == "0000" && newPinNum == 0 && sourceSEPin != 0)
                                {
                                    oldPinNum = 0;
                                    newPinNum = 0;
                                    sourceSEPin = 0;
                                    sourceSEPinTemp = 0;
                                    save();
                                }

                                if (oldPinNum == sourceSEPin)
                                {

                                    sourceSEPinRequest = true;
                                    sourceSEPin = newPinNum;
                                    sourceSEPinTemp = newPinNum;

                                    //sourceSEPin = newPinNum;

                                    //byte[] packet = new byte[4];

                                    //packet[0] = (byte)(oldPinNum & 0xFF);
                                    //packet[1] = (byte)((oldPinNum >> 8) & 0xFF);

                                    //packet[2] = (byte)(newPinNum & 0xFF);
                                    //packet[3] = (byte)((newPinNum >> 8) & 0xFF);

                                    List<byte> packet = new List<byte>();

                                    packet.Add((byte)(oldPinNum & 0xFF));
                                    packet.Add((byte)((oldPinNum >> 8) & 0xFF));

                                    packet.Add((byte)(newPinNum & 0xFF));
                                    packet.Add((byte)((newPinNum >> 8) & 0xFF));



                                    if (deviceId == sPODDeviceTypes.SourceSE)
                                    {
                                        utils.showAlert("Pairing", "Sending new PIN to Source SE...", logo);
                                    }
                                    else
                                    {
                                        utils.showAlert("Pairing", "Sending new PIN to RCPM...", logo);
                                    }


                                    if (currentDevice != null)
                                    {
                                        //Debug.Log ("Data: " + byteArrayToString (packet.ToArray ()));

                                        //ble.WriteCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 2], packet, packet.Length, true);
                                        addPacketToQueue(packet, deviceCharacteristic[deviceId, 2]);

                                    }
                                    else
                                    {
                                        Debug.Log("Error Data: " + byteArrayToString(packet.ToArray()));
                                    }
                                }




                            }

                            break;
                        case "Switch HD Pair":

                            if (switchHDPin == 0)
                            {
                                if (tempPin >= 0)
                                {
                                    switchHDPin = tempPin;
                                    save();
                                    sendSwitchHDPinPacket();
                                    utils.showAlert("Paired!", "Switch HD has been paired...", logo);
                                }
                                else
                                {
                                    utils.showAlert("Hmmm...", "Please put the Switch HD in pairing mode...", logo);
                                }
                            }
                            else
                            {
                                if (!switchHDUnpairRequest)
                                {
                                    switchHDUnpairRequest = true;
                                    utils.showAlert("Are you sure?", "There is currently a Switch HD already paired. Press the pair button again to unpair the current Switch HD...", logo);
                                }
                                else
                                {
                                    switchHDUnpairRequest = false;
                                    utils.showAlert("Unpaired!", "The Switch HD has been unpaired from your device...", logo);
                                    switchHDPin = 0;
                                    save();
                                }
                            }

                            break;
                        case "Switch HD Setup Ok":

                            InputField timer = utils.getAttachedObject(data).GetComponent<InputField>();

                            if (timer != null)
                            {
                                switchHDTimer = Int32.Parse(timer.text);
                            }
                            else
                            {
                                switchHDTimer = 5;
                            }


                            switchHDUnpairRequest = false;


                            save();


                            break;
                        case "Switch HD Source 1":
                            switchHDSource = 0;
                            break;
                        case "Switch HD Source 2":
                            switchHDSource = 1;
                            break;
                        case "Switch HD Source 3":
                            switchHDSource = 2;
                            break;
                        case "Switch HD Source 4":
                            switchHDSource = 3;
                            break;
                        case "Link On":
                            linkIndex = -1;
                            isLink = true;
                            linkButton = utils.getAttachedObject(data);
                            systemResponseSignal.Dispatch(SystemResponseEvents.LinkOn, null);
                            for (int i = 0; i < 32; i++)
                            {
                                switches[i].isOn = false;
                                lastMomentary[i] = switches[i].isMomentary;
                                lastDim[i] = switches[i].isDimmable;
                                switches[i].isMomentary = false;
                                switches[i].isDimmable = false;
                            }
                            sendSystemData();

                            break;
                        case "Link Off":
                            finishUpdatingLinking();
                            isLink = false;
                            routineRunner.StartCoroutine(SetupFlash());
                            for (int i = 0; i < 32; i++)
                            {
                                switches[i].isOn = false;
                                switches[i].isMomentary = lastMomentary[i];
                                switches[i].isDimmable = lastDim[i];
                            }
                            sendSystemData();
                            systemResponseSignal.Dispatch(SystemResponseEvents.LinkOff, null);


                            break;
                        case "Setup On":
                            isSetup = true;

                            if (CurrentDevOTA.NeedUpgrade == -1)
                            {
                                SendCheckOTAPacket();
                            }

                            setupButton = utils.getAttachedObject(data);

                            int mask = 0;

                            if (deviceId == sPODDeviceTypes.SwitchHD)
                                mask = 1;

                            if (deviceId == sPODDeviceTypes.SourceSE)
                                mask = 2;

                            if (deviceId == sPODDeviceTypes.RCPM)
                                mask = 4;

                            if (deviceId == sPODDeviceTypes.SwitchHDv2)
                                mask = 8;


                            bool isRestoreText = false;

                            if (CurrentDevOTA.NeedUpgrade == OTAInfo.NEED_APP_UPDATE || CurrentDevOTA.NeedUpgrade == OTAInfo.NEED_STK_UPDATE)
                            {
                                mask += 16;
                            }
                            else if (isEnableGlobalOTA && CurrentDevOTA.NeedUpgrade == OTAInfo.NEED_NO_UPDATE && currentDevice != null)
                            {
                                mask += 16;
                                isRestoreText = true;
                            }



                            sendProData();

                            //					if (isAppProEnabledFlag && (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXEditor)) {
                            //						mask += 64;		// show pro button
                            //					}




                            Debug.Log("deviceID " + deviceId + ", mask " + mask);


                            systemResponseSignal.Dispatch(SystemResponseEvents.SetupOn, new Dictionary<string, object>{
                        {"Mask", mask},
                        {"isRestoreText", isRestoreText }
                    });


                            routineRunner.StartCoroutine(SetupFlash());
                            break;
                        case "Setup Off":
                            disableSetup();

                            break;
                        case "Scan":
                            lastDeviceName = "null";
                            lastDeviceId = "null";
                            //lastConnectingTime = 0;
                            ES2.Save(lastDeviceName, "lastDevice.txt?tag=lastDevice");
                            ES2.Save(lastDeviceId, "lastDevice.txt?tag=lastDeviceId");
                            //bleDisconnectAsync();
                            nextBleState = "Disconnected";
                            bleSignalHandler("Tick", null);
                            bleSignalHandler("Tick", null);
                            break;
                        case "Upgrade":                                                                                                 /// <<<<<<<<<<<<<<<<<<<<<

                            Debug.Log("Upgrade button pressed");

                            //if (CurrentDevOTA.NeedUpgrade == OTAInfo.NEED_APP_UPDATE || CurrentDevOTA.NeedUpgrade == OTAInfo.NEED_STK_UPDATE)
                            //{


                            //						string warningString;
                            //
                            //						if (CurrentDevOTA.BoardId == 4500 && CurrentDevOTA.BoardRev == 4 && (CurrentDevOTA.StkVer <= 0x0104 || CurrentDevOTA.StkVer == 0xFFFF))
                            //						{
                            //							warningString = "\nWarning: go to mobile device settings > Bluetooth > " + CurrentDevOTA.DeviceName + " > Forget/Unpair and then return to the app to continue with firmware upload";
                            //						}else{
                            //							warningString = "";
                            //						}

                            string devName;

                            switch (deviceId)
                            {
                                case sPODDeviceTypes.Bantam:
                                    devName = "BantamX #" + currentDeviceName.Substring(currentDeviceName.Length - 6);
                                    break;
                                case sPODDeviceTypes.Touchscreen:
                                    devName = "Touchscreen #" + currentDeviceName.Substring(currentDeviceName.Length - 6);
                                    break;
                                case sPODDeviceTypes.SwitchHDv2:
                                    devName = "Switch HD #" + currentDeviceName.Substring(currentDeviceName.Length - 6);
                                    break;
                                case sPODDeviceTypes.SourceLT:
                                    devName = "SourceLT #" + currentDeviceName.Substring(currentDeviceName.Length - 6);
                                    break;
                                default:
                                    devName = currentDeviceName;

                                    break;
                            }

                            if (CurrentDevOTA.NeedUpgrade == OTAInfo.NEED_APP_UPDATE || CurrentDevOTA.NeedUpgrade == OTAInfo.NEED_STK_UPDATE)
                            {
                                //string otaUpgradeMessage = "Do you want to upgrade: " + devName + "?" +
                                //                    (CurrentDevOTA.NeedUpgrade == OTAInfo.NEED_STK_UPDATE ?
                                //                    "\n   Stack: " + firmVer(CurrentDevOTA.StkVer) + "  >  " + firmVer(FoundOtaFiles[CurrentDevOTA.NewStkFile].AppVer)
                                //                    : "") +
                                //                        "\n   Application: " + firmVer(CurrentDevOTA.AppVer) + "  >  " + firmVer(FoundOtaFiles[CurrentDevOTA.NewAppFile].AppVer) +
                                //                    "\nNote: Stay within ble range, do not exit the app," +
                                //                    "\n   and do NOT unplug the device during upgrade" +
                                //                        "\n   This process will reset all settings for the device" +
                                //                        "\n   Pair with \"OTA Bootloader\" if prompted";

                                string otaUpgradeMessage = "Do you want to upgrade: " + devName + "?" +
                                                    (CurrentDevOTA.NeedUpgrade == OTAInfo.NEED_STK_UPDATE ?
                                                    "\nStack: " + firmVer(CurrentDevOTA.StkVer) + "  >  " + firmVer(FoundOtaFiles[CurrentDevOTA.NewStkFile].AppVer)
                                                    : "") +

                                                        "\nApplication: " + firmVer(CurrentDevOTA.AppVer) + "  >  " + firmVer(FoundOtaFiles[CurrentDevOTA.NewAppFile].AppVer) +

                                                    "\n\nBy upgrading the firmware, all settings will be lost and the system will be reset to factory default settings" +
                                                    "\n\nPlease take note of any custom settings before upgrading the firmware" +

                                                    "\n\nWhile upgrading, stay within Bluetooth range. Do not exit the app and do not unplug the device during the process" +
                                                    "\n\nIf prompted, pair with \"OTA Bootloader\"";



                                systemResponseSignal.Dispatch(SystemResponseEvents.SelectOtaUpgrade, new Dictionary<string, object> {
                            { "otaUpgradeMessage", otaUpgradeMessage },
                            {"isInfoOnly", false }
							//{ "CancelOn", true },
							//{ "QuitOnOk", false },
						});

                                //						upgradeOTA ();
                            }
                            else if (isEnableGlobalOTA && CurrentDevOTA.NeedUpgrade == OTAInfo.NEED_NO_UPDATE)
                            {
                                //string otaUpgradeMessage = "Do you want to reflash: " + devName + "?" +

                                //						"\n   with firmware: " + firmVer(CurrentDevOTA.AppVer) + "  >  " + firmVer(FoundOtaFiles[CurrentDevOTA.NewAppFile].AppVer) +
                                //					"\nNote: Stay within ble range, do not exit the app," +
                                //					"\n   and do NOT unplug the device during upgrade" +
                                //						"\n   This process will reset all settings for the device" +
                                //						"\n   Pair with \"OTA Bootloader\" if prompted";

                                string otaUpgradeMessage =
                                                    "By restoring the firmware for " + devName + ", to " + firmVer(FoundOtaFiles[CurrentDevOTA.NewAppFile].AppVer) + ", all settings will be lost and the system will be reset to factory default settings" +
                                                    "\n\nPlease take note of any custom settings before restoring the firmware" +

                                                    "\n\nWhile restoring, stay within Bluetooth range. Do not exit the app and do not unplug the device during the process" +
                                                    "\n\nIf prompted, pair with \"OTA Bootloader\"";

                                //string otaUpgradeMessage;


                                systemResponseSignal.Dispatch(SystemResponseEvents.SelectOtaUpgrade, new Dictionary<string, object> {
                            { "otaUpgradeMessage", otaUpgradeMessage },
							//{ "CancelOn", true },
							//{ "QuitOnOk", false },
							{ "isRestore", true },
                            {"isInfoOnly", false }
                        });

                            }
                            else
                            {
                                //int mask2 = 0;

                                //if (deviceId == sPODDeviceTypes.SwitchHDv2)
                                //	mask2 = 8;

                                //						systemResponseSignal.Dispatch (SystemResponseEvents.SetupOn, new Dictionary<string, object>{{"Mask", mask2}});	// stop display of upgrade button?
                            }

                            break;

                    }
                    break;


            }

            if (type == UiEvents.Drag || type == UiEvents.EndDrag)
            {
                GameObject sliderObj = utils.getAttachedObject(data);
                if (sliderObj != null)
                {
                    Slider slider = sliderObj.GetComponent<Slider>();

                    //Debug.Log (key + ", " + slider.value);


                    int color = -1;

                    switch (key)
                    {
                        case "Switch HD Red Slider":
                            //Debug.Log ("Red");
                            color = 0;
                            switchHDColors[0] = slider.value;
                            break;
                        case "Swtich HD Green Slider":
                            //Debug.Log ("Green");
                            color = 1;
                            switchHDColors[1] = slider.value;
                            break;
                        case "Switch HD Blue Slider":
                            color = 2;
                            //Debug.Log ("Blue");
                            switchHDColors[2] = slider.value;
                            break;
                        case "Switch HD Indicator Slider":
                            color = 3;
                            //Debug.Log ("Indicator");
                            switchHDColors[3] = slider.value;
                            break;
                    }

                    if (color >= 0 && packetTimer.ElapsedMilliseconds >= 50)
                    {
                        //Debug.Log ("Sending Color...");

                        List<byte> packet = new List<byte>();
                        packet.Add(0x55);
                        packet.Add(0x07);
                        packet.Add(0x01);
                        packet.Add(Convert.ToByte(color));


                        packet.Add(getColorByte(slider.value));

                        uint crc = crc32(0, packet.ToArray(), 5);

                        packet.AddRange(BitConverter.GetBytes(crc));

                        if (currentDevice != null)
                        {
                            //Debug.Log ("Data: " + byteArrayToString (packet.ToArray ()));

                            //ble.WriteCharacteristic(currentDevice, deviceService[deviceId], deviceCharacteristic[deviceId, 0], packet.ToArray(), packet.Count, true);
                            addPacketToQueue(packet, deviceCharacteristic[deviceId, 0]);

                        }
                        else
                        {
                            Debug.Log("Error Data: " + byteArrayToString(packet.ToArray()));
                        }


                        packetTimer.Stop();
                        packetTimer.Reset();
                        packetTimer.Start();
                    }


                }
            }
        }

        private byte getColorByte(float value)
        {
            return Convert.ToByte(Math.Exp((value * 100) / 21.66790653) - 1);
        }

        private float getColorFloat(byte value)
        {
            //			Convert.ToByte(Math.Exp ((value * 100) / 21.66790653) - 1);

            return (float)(Math.Log((float)(value) + 1.0f) * 21.66790653 / 100);

        }

        public void systemResponseHandler(string key, Dictionary<string, object> data)
        {

            //Debug.Log ("System Response: " + key);
            switch (key)
            {
                case SystemResponseEvents.DeviceList:
                    utils.hideAlert();
                    break;
            }
        }

        IEnumerator SetupFlash()
        {

            bool isOn = false;
            while (isSetup)
            {
                isOn = !isOn;

                if (isOn && !isLink)
                    systemResponseSignal.Dispatch(SystemResponseEvents.SetupIndicatorOn, null);
                else
                    systemResponseSignal.Dispatch(SystemResponseEvents.SetupIndicatorOff, null);


                yield return new WaitForSeconds(0.75f);
            }
            systemResponseSignal.Dispatch(SystemResponseEvents.SetupIndicatorOff, null);
            systemResponseSignal.Dispatch(SystemResponseEvents.SetupOff, null);
            yield break;
        }


        private Int32 version = 5;
        private SwitchStatus[] switches = new SwitchStatus[32];
        private string[] sourceNames = new string[4];
        private Vector3[] sourceColors = new Vector3[4];
        private bool[] sourceTemps = new bool[4];

        private Int32 pinNumber = 0;

        private void saveVersion(ES2Writer writer)
        {

            Debug.Log("sv: " + version);

            switch (version)
            {
                case 1:
                    for (int i = 0; i < 32; i++)
                    {
                        if (switches[i].isDirty)
                        {
                            switches[i].isDirty = false;
                            writer.Write<SwitchStatus>(switches[i], "switches" + i);
                        }
                    }

                    writer.Write<Int32>(version, "pinNumber");

                    for (int i = 0; i < 4; i++)
                    {
                        writer.Write<string>(sourceNames[i], "sourceNames" + i);
                        writer.Write<Vector3>(sourceColors[i], "sourceColors" + i);
                        writer.Write<bool>(sourceTemps[i], "sourceTemps" + i);
                    }

                    break;
                case 2:
                    for (int i = 0; i < 32; i++)
                    {
                        if (switches[i].isDirty)
                        {
                            switches[i].isDirty = false;
                            writer.Write<SwitchStatus>(switches[i], "switches" + i);
                        }
                    }

                    writer.Write<Int32>(version, "pinNumber");

                    for (int i = 0; i < 4; i++)
                    {
                        writer.Write<string>(sourceNames[i], "sourceNames" + i);
                        writer.Write<Vector3>(sourceColors[i], "sourceColors" + i);
                        writer.Write<bool>(sourceTemps[i], "sourceTemps" + i);
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        writer.Write<byte>((byte)links[i], "links" + i);
                    }

                    break;
                case 3:
                    for (int i = 0; i < 32; i++)
                    {
                        if (switches[i].isDirty)
                        {
                            switches[i].isDirty = false;
                            writer.Write<SwitchStatus>(switches[i], "switches" + i);
                        }
                    }
                    writer.Write<Int32>(version, "pinNumber");

                    for (int i = 0; i < 4; i++)
                    {
                        writer.Write<string>(sourceNames[i], "sourceNames" + i);
                        writer.Write<Vector3>(sourceColors[i], "sourceColors" + i);
                        writer.Write<bool>(sourceTemps[i], "sourceTemps" + i);
                    }

                    for (int i = 0; i < 32; i++)
                    {
                        writer.Write<int>(links[i], "links" + i);
                    }
                    break;
                case 4:
                    for (int i = 0; i < 32; i++)
                    {
                        if (switches[i].isDirty)
                        {
                            switches[i].isDirty = false;
                            writer.Write<SwitchStatus>(switches[i], "switches" + i);
                        }
                    }
                    writer.Write<Int32>(version, "pinNumber");

                    for (int i = 0; i < 4; i++)
                    {
                        writer.Write<string>(sourceNames[i], "sourceNames" + i);
                        writer.Write<Vector3>(sourceColors[i], "sourceColors" + i);
                        writer.Write<bool>(sourceTemps[i], "sourceTemps" + i);
                    }

                    for (int i = 0; i < 32; i++)
                    {
                        writer.Write<int>(links[i], "links" + i);
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        //					Debug.Log ("switchHDColors" + i + ", " + switchHDColors [i]);
                        writer.Write<float>(switchHDColors[i], "switchHDColors" + i);
                    }

                    writer.Write<int>(switchHDTimer, "switchHDTimer");
                    writer.Write<int>(switchHDSource, "switchHDSource");
                    writer.Write<int>(switchHDPin, "switchHDPin");

                    break;
                case 5:
                    for (int i = 0; i < 32; i++)
                    {
                        if (switches[i].isDirty)
                        {
                            switches[i].isDirty = false;
                            writer.Write<SwitchStatus>(switches[i], "switches" + i);
                        }
                    }
                    writer.Write<Int32>(version, "pinNumber");

                    for (int i = 0; i < 4; i++)
                    {
                        writer.Write<string>(sourceNames[i], "sourceNames" + i);
                        writer.Write<Vector3>(sourceColors[i], "sourceColors" + i);
                        writer.Write<bool>(sourceTemps[i], "sourceTemps" + i);
                    }

                    for (int i = 0; i < 32; i++)
                    {
                        writer.Write<int>(links[i], "links" + i);
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        //					Debug.Log ("switchHDColors" + i + ", " + switchHDColors [i]);
                        writer.Write<float>(switchHDColors[i], "switchHDColors" + i);
                    }

                    writer.Write<int>(switchHDTimer, "switchHDTimer");
                    writer.Write<int>(switchHDSource, "switchHDSource");
                    writer.Write<int>(switchHDPin, "switchHDPin");

                    writer.Write<int>(sourceSEPin, "sourceSEPin");

                    writer.Write<bool>(switchHDWake, "switchHDWake");

                    break;
            }

            bleSignalHandler(BluetoothLeEvents.StateUpdate, new Dictionary<string, object> { { "didInitialSystemDataLoad", true } });
        }

        private IEnumerator loadVersion(int savedVersion, ES2Reader reader)
        //		private void loadVersion(int savedVersion, ES2Reader reader)
        {

            Debug.Log("loadVersion: " + savedVersion + ", currentVersion:" + version);

            if (savedVersion < 1 || savedVersion > version)
            {
                throw new Exception("Non-matching save file version...");
            }

            switch (savedVersion)
            {
                case 1:
                    for (int i = 0; i < 32; i++)
                    {
                        switches[i] = reader.Read<SwitchStatus>("switches" + i);
                        switches[i].isDirty = false;
                    }

                    pinNumber = reader.Read<Int32>("pinNumber");

                    for (int i = 0; i < 4; i++)
                    {
                        sourceNames[i] = reader.Read<string>("sourceNames" + i);
                        sourceColors[i] = reader.Read<Vector3>("sourceColors" + i);
                        sourceTemps[i] = reader.Read<bool>("sourceTemps" + i);
                    }

                    break;
                case 2:
                    for (int i = 0; i < 32; i++)
                    {
                        switches[i] = reader.Read<SwitchStatus>("switches" + i);
                        switches[i].isDirty = false;
                    }

                    pinNumber = reader.Read<Int32>("pinNumber");

                    for (int i = 0; i < 4; i++)
                    {
                        sourceNames[i] = reader.Read<string>("sourceNames" + i);
                        sourceColors[i] = reader.Read<Vector3>("sourceColors" + i);
                        sourceTemps[i] = reader.Read<bool>("sourceTemps" + i);
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        links[i] = reader.Read<byte>("links" + i);
                    }

                    break;
                case 3:
                    for (int i = 0; i < 32; i++)
                    {
                        switches[i] = reader.Read<SwitchStatus>("switches" + i);
                        switches[i].isDirty = false;
                    }

                    pinNumber = reader.Read<Int32>("pinNumber");

                    for (int i = 0; i < 4; i++)
                    {
                        sourceNames[i] = reader.Read<string>("sourceNames" + i);
                        sourceColors[i] = reader.Read<Vector3>("sourceColors" + i);
                        sourceTemps[i] = reader.Read<bool>("sourceTemps" + i);
                    }

                    for (int i = 0; i < 32; i++)
                    {
                        links[i] = reader.Read<int>("links" + i);
                    }

                    break;
                case 4:
                    for (int i = 0; i < 32; i++)
                    {
                        switches[i] = reader.Read<SwitchStatus>("switches" + i);
                        switches[i].isDirty = false;
                    }

                    pinNumber = reader.Read<Int32>("pinNumber");

                    for (int i = 0; i < 4; i++)
                    {
                        sourceNames[i] = reader.Read<string>("sourceNames" + i);
                        sourceColors[i] = reader.Read<Vector3>("sourceColors" + i);
                        sourceTemps[i] = reader.Read<bool>("sourceTemps" + i);
                    }

                    for (int i = 0; i < 32; i++)
                    {
                        links[i] = reader.Read<int>("links" + i);
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        switchHDColors[i] = reader.Read<float>("switchHDColors" + i);
                        //					Debug.Log ("switchHDColors" + i + ", " + switchHDColors [i]);
                    }

                    switchHDTimer = reader.Read<int>("switchHDTimer");
                    switchHDSource = reader.Read<int>("switchHDSource");
                    switchHDPin = reader.Read<int>("switchHDPin");

                    //switchHDPin = 10;

                    break;
                case 5:
                    Debug.Log("loading version: " + version + ", A");
                    systemRequestSignal.Dispatch(SystemRequestEvents.UpdateSourceId, new Dictionary<string, object> { { "Id", 0 } });

                    for (int i = 0; i < 32; i++)
                    {

                        try
                        {
                            switches[i] = reader.Read<SwitchStatus>("switches" + i);
                        }
                        catch (Exception e)
                        {
                            Debug.Log("Error loading switch " + i + "...");
                            Debug.Log(e);

                            switches[i].sprite = logo.GetComponent<Image>().sprite;
                            switches[i].isLegend = false;
                        }

                        switches[i].isDirty = false;
                        sendSystemData();
                        yield return null;
                    }


                    Debug.Log("loading version: " + version + ", B");

                    pinNumber = reader.Read<Int32>("pinNumber");

                    for (int i = 0; i < 4; i++)
                    {
                        sourceNames[i] = reader.Read<string>("sourceNames" + i);
                        sourceColors[i] = reader.Read<Vector3>("sourceColors" + i);
                        sourceTemps[i] = reader.Read<bool>("sourceTemps" + i);
                    }


                    Debug.Log("loading version: " + version + ", C");

                    for (int i = 0; i < 32; i++)
                    {
                        links[i] = reader.Read<int>("links" + i);
                        yield return null;
                    }

                    Debug.Log("loading version: " + version + ", D");

                    for (int i = 0; i < 4; i++)
                    {
                        switchHDColors[i] = reader.Read<float>("switchHDColors" + i);
                        //					Debug.Log ("switchHDColors" + i + ", " + switchHDColors [i]);
                    }

                    Debug.Log("loading version: " + version + ", E");

                    switchHDTimer = reader.Read<int>("switchHDTimer");
                    switchHDSource = reader.Read<int>("switchHDSource");
                    switchHDPin = reader.Read<int>("switchHDPin");

                    sourceSEPin = reader.Read<int>("sourceSEPin");

                    if (reader.TagExists("switchHDWake"))
                    {
                        switchHDWake = reader.Read<bool>("switchHDWake");
                    }
                    else
                    {
                        Debug.Log("needed init of switchHDWake...");
                        switchHDWake = true;
                    }

                    Debug.Log("loading version: " + version + ", F");

                    break;
                    //				for (int i = 0; i < 32; i++) {
                    //					switches [i] = reader.Read<SwitchStatus> ("switches" + i);
                    //					switches [i].isDirty = false;
                    //				}
                    //
                    //				pinNumber = reader.Read<Int32> ("pinNumber");
                    //
                    //				for (int i = 0; i < 4; i++) {
                    //					sourceNames [i] = reader.Read<string> ("sourceNames" + i);
                    //					sourceColors [i] = reader.Read<Vector3> ("sourceColors" + i);
                    //					sourceTemps [i] = reader.Read<bool> ("sourceTemps" + i);
                    //				}
                    //
                    //				for (int i = 0; i < 32; i++) {
                    //					links [i] = reader.Read<int> ("links" + i);
                    //				}
                    //
                    //				for (int i = 0; i < 4; i++) {
                    //					switchHDColors [i] = reader.Read<float> ("switchHDColors" + i);
                    //					Debug.Log ("switchHDColors" + i + ", " + switchHDColors [i]);
                    //				}
                    //
                    //				switchHDTimer = reader.Read<int> ("switchHDTimer");
                    //				switchHDSource = reader.Read<int> ("switchHDSource");
                    //				switchHDPin = reader.Read<int> ("switchHDPin");
                    //
                    //				sourceSEPin =  reader.Read<int> ("sourceSEPin");
                    //
                    //				break;
            }

            sendSystemData();
            systemRequestSignal.Dispatch(SystemRequestEvents.UpdateSourceId, new Dictionary<string, object> { { "Id", 0 } });

            didInitialSystemDataLoad = true;

            bleSignalHandler(BluetoothLeEvents.StateUpdate, new Dictionary<string, object> { { "didInitialSystemDataLoad", true } });

            yield break;


        }

        private void load()
        {
            try
            {
                ES2Reader reader = ES2Reader.Create("save.txt");

                if (reader.TagExists("version"))
                {
                    int savedVersion = (int)reader.Read<int>("version");

                    //reader.Read<SwitchStatus> ("switches" + 31);

                    //					if (reader.TagExists ("version")) {
                    ////						throw Exception ;
                    //					}

                    routineRunner.StartCoroutine(loadVersion(savedVersion, reader));
                    //					loadVersion(savedVersion, reader);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Error loading file...");
                Debug.Log(e);

                for (int i = 0; i < 32; i++)
                {
                    switches[i].sprite = logo.GetComponent<Image>().sprite;
                    switches[i].isLegend = false;
                }

                save();
            }


            sendSystemData();
            systemRequestSignal.Dispatch(SystemRequestEvents.UpdateSourceId, new Dictionary<string, object> { { "Id", 0 } });

            //systemResponseSignal.Dispatch(SystemResponseEvents.VoicePhrase, new Dictionary<string, object>{{SystemResponseEvents.VoicePhrase, "ok google turn on switch one" }});

        }

        private void save()
        {


            ES2Writer writer = ES2Writer.Create("save.txt");

            writer.Write<Int32>(version, "version");


            saveVersion(writer);


            writer.Save();


            if ((deviceId == sPODDeviceTypes.SwitchHD || deviceId == sPODDeviceTypes.SwitchHDv2) && !CurrentDevOTA.Upgrading)
            {

                if (ProStatus.isPro && ProStatus.needsSync && (Time.realtimeSinceStartup - startProHdSyncTime) <= 5.0f)
                {
                    Debug.Log("cancel hd send");
                    return;
                }


                sendSwitchHDSettingsPacket(true);
            }
        }

        private ProSeriesStatus ProStatus = new ProSeriesStatus();
        //		private ProSwitchStatus[] ProSwitches = new ProSwitchStatus[32];

        private void savePasskeys()
        {
            ES2Writer writer = ES2Writer.Create("passkeys.txt");

            int index = 0;

            foreach (KeyValuePair<string, UInt32> kvp in devicePasskeys)
            {

                //				Debug.Log("save passkey: " + "devId" + index +  ", " + kvp.Key);
                //				Debug.Log("save passkey: " + "passkey" + index + ", " + kvp.Value);

                writer.Write<string>(kvp.Key, "devId" + index);
                writer.Write<UInt32>(kvp.Value, "passkey" + index);

                index++;
            }

            writer.Write<int>(index, "numOfDevPesskeys");

            writer.Save();

        }

        private void loadPasskeys()
        {
            ES2Reader reader = ES2Reader.Create("passkeys.txt");

            //if (!reader.TagExists("numOfDevPesskeys"))
            //    return;

            devicePasskeys = new Dictionary<string, UInt32>();

            try
            {
                int numOfNames = reader.Read<int>("numOfDevPesskeys");

                Debug.Log("numOfDevPesskeys: " + numOfNames);

                if (numOfNames > 0)
                {
                    string devId;
                    UInt32 myPasskey;

                    for (int i = 0; i < numOfNames; i++)
                    {
                        devId = reader.Read<string>("devId" + i);
                        myPasskey = reader.Read<UInt32>("passkey" + i);

                        devicePasskeys.Add(devId, myPasskey);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("dnl passkeys: " + e);

                devicePasskeys = new Dictionary<string, UInt32>();

                savePasskeys();
            }
        }

        private void savePro()
        {

            //			if (!isAppProEnabledFlag || (Application.platform != RuntimePlatform.IPhonePlayer && Application.platform != RuntimePlatform.OSXEditor))
            if (!ProStatus.isAppProEnabled)
                return;

            ES2Writer writer = ES2Writer.Create("currPro.txt");

            writer.Write<bool>(ProStatus.isPro, "isPro");
            writer.Write<bool>(ProStatus.isAutoSync, "isAutoSync");
            writer.Write<bool>(ProStatus.isSyncFromApp, "isSyncFromApp");
            writer.Write<bool>(ProStatus.isDisableDeepSleep, "isDisableDeepSleep");
            writer.Write<bool>(ProStatus.isInputLinking, "isInputLinking");


            int index = 0;

            foreach (KeyValuePair<string, string> kvp in friendlyNames)
            {

                //				Debug.Log("save name: " + "devId" + index +  ", " + kvp.Key);
                //				Debug.Log("save name: " + "niceName" + index + ", " + kvp.Value);

                writer.Write<string>(kvp.Key, "devId" + index);
                writer.Write<string>(kvp.Value, "niceName" + index);

                index++;
            }

            writer.Write<int>(index, "numOfNames");

            //			Debug.Log("index: " + index);


            writer.Save();
            //			writer.Save (false);	// to overwrite/reset

        }

        private void loadPro()
        {

            //			if (!isAppProEnabledFlag || (Application.platform != RuntimePlatform.IPhonePlayer && Application.platform != RuntimePlatform.OSXEditor))
            if (!ProStatus.isAppProEnabled)
                return;

            ES2Reader reader = ES2Reader.Create("currPro.txt");

            if (!reader.TagExists("isPro") || !reader.TagExists("isDisableDeepSleep"))
                return;

            ProStatus = new ProSeriesStatus();
            //			ProSwitches = new ProSwitchStatus[32];

            ProStatus.isPro = reader.Read<bool>("isPro");
            ProStatus.isAutoSync = reader.Read<bool>("isAutoSync");
            ProStatus.isSyncFromApp = reader.Read<bool>("isSyncFromApp");
            ProStatus.isDisableDeepSleep = reader.Read<bool>("isDisableDeepSleep");

            if (reader.TagExists("isInputLinking"))
                ProStatus.isInputLinking = reader.Read<bool>("isInputLinking");

            friendlyNames = new Dictionary<string, string>();

            try
            {
                int numOfNames = reader.Read<int>("numOfNames");

                Debug.Log("numOfNames: " + numOfNames);

                if (numOfNames > 0)
                {
                    string devId, devName;

                    for (int i = 0; i < numOfNames; i++)
                    {

                        devId = reader.Read<string>("devId" + i);
                        devName = reader.Read<string>("niceName" + i);

                        friendlyNames.Add(devId, devName);

                        //						Debug.Log("add name: " + devId + ", " + devName);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("dnl names: " + e);

                friendlyNames = new Dictionary<string, string>();

                savePro();
            }
        }

        private void saveOta()
        {

            if (Application.platform != RuntimePlatform.Android)
                return;

            ES2Writer writer = ES2Writer.Create("currOta.txt");


            writer.Write<int>(CurrentDevOTA.BoardId, "BoardId");
            writer.Write<int>(CurrentDevOTA.BoardRev, "BoardRev");
            writer.Write<int>(CurrentDevOTA.AppVer, "AppVer");
            writer.Write<int>(CurrentDevOTA.StkVer, "StkVer");
            writer.Write<int>(CurrentDevOTA.NewAppVer, "NewAppVer");
            writer.Write<int>(CurrentDevOTA.NewStkVer, "NewStkVer");
            writer.Write<int>(CurrentDevOTA.NewAppFile, "NewAppFile");
            writer.Write<int>(CurrentDevOTA.NewStkFile, "NewStkFile");
            writer.Write<int>(CurrentDevOTA.NeedUpgrade, "NeedUpgrade");
            writer.Write<bool>(CurrentDevOTA.UpgradeStk, "UpgradeStk");
            writer.Write<bool>(CurrentDevOTA.RecDevOta, "RecDevOta");
            writer.Write<bool>(CurrentDevOTA.Upgrading, "Upgrading");
            writer.Write<string>(CurrentDevOTA.lastOtaIdentifier, "lastOtaIdentifier");
            writer.Write<string>(CurrentDevOTA.DeviceName, "DeviceName");

            writer.Save();

        }

        private void loadOta()
        {

            if (Application.platform != RuntimePlatform.Android)
                return;

            ES2Reader reader = ES2Reader.Create("currOta.txt");

            if (!reader.TagExists("DeviceName"))
                return;

            CurrentDevOTA = new DeviceOTA();

            CurrentDevOTA.BoardId = reader.Read<int>("BoardId");
            CurrentDevOTA.BoardRev = reader.Read<int>("BoardRev");
            CurrentDevOTA.AppVer = reader.Read<int>("AppVer");
            CurrentDevOTA.StkVer = reader.Read<int>("StkVer");
            CurrentDevOTA.NewAppVer = reader.Read<int>("NewAppVer");
            CurrentDevOTA.NewStkVer = reader.Read<int>("NewStkVer");
            CurrentDevOTA.NewAppFile = reader.Read<int>("NewAppFile");
            CurrentDevOTA.NewStkFile = reader.Read<int>("NewStkFile");
            CurrentDevOTA.NeedUpgrade = reader.Read<int>("NeedUpgrade");
            CurrentDevOTA.UpgradeStk = reader.Read<bool>("UpgradeStk");
            CurrentDevOTA.RecDevOta = reader.Read<bool>("RecDevOta");
            CurrentDevOTA.Upgrading = reader.Read<bool>("Upgrading");
            CurrentDevOTA.lastOtaIdentifier = reader.Read<string>("lastOtaIdentifier");
            CurrentDevOTA.DeviceName = reader.Read<string>("DeviceName");
            //CurrentDevOTA.isAddressRead = false;
            //CurrentDevOTA.addressRead = 0;

        }

        //Everything starts here...
        public override void Execute()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

            getPassKeyInstance = getPassKey();
            scanTimeoutInstance = scanTimout();
            sendLinksInstance = sendLinks();

            getOtaDataInstance = getOtaData();
            otaTimeoutInstance = otaTimeout();
            checkProDataInstance = checkProData();
            connectTimeoutInstance = connectTimout(0);

            checkSecDataInstance = checkSecData();

            //			androidDisconnectInstance = androidDisconnect ();

            for (int i = 0; i < 32; i++)
            {
                switches[i] = new SwitchStatus(i);
                //switches[i].label1 = "";
                //switches[i].label2 = "Switch " + (i + 1);
                //switches[i].label3 = "";
            }

            for (int i = 0; i < 4; i++)
            {
                sourceNames[i] = "Source " + (i + 1);
                sourceColors[i] = new Vector3(1f, 1f, 1f);
            }

            for (int i = 0; i < 32; i++)
            {
                links[i] = 0;
            }

            routineRunner.StartCoroutine(findOTAFiles());
            //			findOTAFiles ();

            try
            {
                loadPasskeys();
                Debug.Log("devicePasskeys loaded: ");
            }
            catch (Exception e)
            {
                devicePasskeys = new Dictionary<string, UInt32>();
                savePasskeys();
                Debug.Log("devicePasskeys empty: " + e);
            }


            Debug.Log("AppStartCommand: Execute()");

            if (isAppProEnabledFlag && (
                Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXEditor ||
                (Application.platform == RuntimePlatform.Android && isAppProAndroidFlag)
                ))
            {

                ProStatus = new ProSeriesStatus();

                ProStatus.isAppProEnabled = true;

                try
                {
                    Debug.Log("Pro: try load");
                    loadPro();
                    Debug.Log("Pro settings Loaded -> ProMode: " + ProStatus.isPro);

                    if (isAppDefaultProFlag && !ProStatus.isPro)
                    {
                        ProStatus.isPro = true;
                        savePro();

                        Debug.Log("isAppDefaultProFlag");
                    }

                }
                catch (Exception e)
                {
                    Debug.Log("Pro: try save: " + e);

                    if (isAppDefaultProFlag)
                    {
                        ProStatus.isPro = true;
                        Debug.Log("isAppDefaultProFlag2");
                    }

                    savePro();
                    Debug.Log("Pro initialized");
                }

                ProStatus.isAppProEnabled = true;
                sendProData();

            }
            else
            {
                ProStatus.isPro = false;
                ProStatus.isAppProEnabled = false;
            }

#if UNITY_ANDROID

            //            Debug.Log("try get permission");

            string blePerString = "android.permission.ACCESS_FINE_LOCATION";

            AndroidRuntimePermissions.Permission blePer = AndroidRuntimePermissions.CheckPermission(blePerString);

            //if (blePer != AndroidRuntimePermissions.Permission.Granted)
            //         {
            if (blePer == AndroidRuntimePermissions.Permission.ShouldAsk)
            {
                AndroidRuntimePermissions.RequestPermission(blePerString);
            }
            //}
#endif


            packetTimer.Start();

            bleSignal.AddListener(bleSignalHandler);
            uiSignal.AddListener(uiHandler);
            systemRequestSignal.AddListener(systemRequestHandler);
            systemResponseSignal.AddListener(systemResponseHandler);


#if DEVELOPMENT_BUILD
			ble.EnableDebug (true);
#else
            ble.EnableDebug(false);
#endif
            //			ble.EnableDebug (true);

            System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            int currentTime = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;

            int debugTimerTemp = PlayerPrefs.GetInt("DebugTimerStop", currentTime);

            debugTimer = debugTimerTemp - currentTime;

            if (debugTimer > 0)
            {
                enableDebugLog();
            }

            ble.Startup();

            systemResponseSignal.Dispatch(SystemResponseEvents.Start, null);

            systemRequestSignal.Dispatch(SystemRequestEvents.UpdateSourceId, new Dictionary<string, object> { { "Id", 0 }, { "Type", UiEvents.EndDrag } });

            //routineRunner.StartCoroutine (indicatorTest());

            //			routineRunner.StartCoroutine (blestartco());
        }
    }
}