using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OTAFile {
	public string FileName;
	public int BoardId = 0;
	public byte[] BoardRev = {0, 0, 0, 0};
	public int AppId = 0;
	public int AppVer = 0;
	public uint SiliconId = 0;
	public int SiliconRev = 0;
	public int ChecksumType = 0;
	public int TotalRowCount = 0;
	public RowIDs[] RowID; 
	public OTARows[] RowData;
	public int ChecksumVal = 0;
	public bool parseGood = false;
}
	
public struct OTARows {
	public int RowId;
	public int RowNum;
	public int DataLen;
	public int Checksum;
    public byte[] DataArray;
}

public struct RowIDs {
	public int Count;
	public uint StartRow;
	public uint EndRow;
}
	
public class DeviceOTA {
	public int BoardId = 0;
	public int BoardRev = 0;
	public int AppVer = 0;
	public int StkVer = 0;
	public int NewAppVer = 0;
	public int NewStkVer = 0;
	public int NewAppFile = -1;
	public int NewStkFile = -1;

	public int NeedUpgrade = 0;
	public bool UpgradeStk = false;

	public bool RecDevOta = false;

	public bool Upgrading = false;

	public string lastOtaIdentifier = "";

	public string DeviceName = "";

	public bool isAddressRead = false;
	public byte addressRead = 0;

	//	public int ChecksumType = 0;
	//	public int TotalRowCount = 0;
	//	public RowIDs[] RowID; 
	//	public OTARows[] RowData;
	//	public int ChecksumVal = 0;
	//	public bool parseGood = false;
}

public static class OTAInfo {
	public const int BOARD_ID_BANTAM 			= 5000;
	public const int BOARD_ID_TOUCHSCREEN		= 4500;
	public const int BOARD_ID_SWITCH_HD			= 3000;
	public const int BOARD_ID_SOURCE_LT 		= 7000;
	public const int BOARD_ID_SOURCE_LT_OLD		= 550;

	public const int BOARD_REV_BANTAM 			= 7;
	public const int BOARD_REV_STD_TOUCHSCREEN	= 3;
	public const int BOARD_REV_LR_TOUCHSCREEN	= 4;
	public const int BOARD_REV_SWITCH_HD		= 3;
	public const int BOARD_REV_SOURCE_LT 		= 1;

	public const int FIRST_VER_BANTAM			= 0x0109; 
	public const int FIRST_VER_STD_TOUCHSCREEN	= 0x0109;
	public const int FIRST_VER_LR_TOUCHSCREEN	= 0x0112;
	public const int FIRST_VER_SWITCH_HD		= 0x0106;
	public const int FIRST_VER_SOURCE_LT		= 0x0104;

	public const int VER_LR_TOUCH_BLINK_FIX		= 0x0142;

	public const int NOT_COMPATIBLE				= -2;
	public const int NEED_READ					= -1;
	public const int NEED_NO_UPDATE				= 0;
	public const int NEED_APP_UPDATE			= 1;
	public const int NEED_STK_UPDATE			= 2;

}

public class OTACommands {
	public byte COMMAND_START_BYTE   = 0x01;
	public byte COMMAND_END_BYTE     = 0x17;

	public byte VERIFY_CHECKSUM      = 0x31;
	public byte GET_FLASH_SIZE       = 0x32;
	public byte SEND_DATA            = 0x37;
	public byte ENTER_BOOTLOADER     = 0x38;
	public byte PROGRAM_ROW          = 0x39;
	public byte VERIFY_ROW  		 = 0x3A;
	public byte EXIT_BOOTLOADER 	 = 0x3B;

	public ushort MAX_DATA_SIZE  	 = 133;
}

public class OTAErrorCodes {
	public byte SUCCESS              = 0x00;
	public byte ERROR_FILE           = 0x01;
	public byte ERROR_EOF            = 0x02;
	public byte ERROR_LENGTH         = 0x03;
	public byte ERROR_DATA           = 0x04;
	public byte ERROR_COMMAND        = 0x05;
	public byte ERROR_DEVICE         = 0x06;
	public byte ERROR_VERSION        = 0x07;
	public byte ERROR_CHECKSUM       = 0x08;
	public byte ERROR_ARRAY          = 0x09;
	public byte ERROR_ROW            = 0x0A;
	public byte ERROR_BOOTLOADER     = 0x0B;
	public byte ERROR_APPLICATION    = 0x0C;
	public byte ERROR_ACTIVE         = 0x0D;
	public byte ERROR_UNKNOWN        = 0x0F;
	public byte ERROR_ABORT          = 0xFF;
}



//#define CUSTOM_BOOT_LOADER_SERVICE_UUID          "00060000-F8CE-11E4-ABF4-0002A5D5C51B"
//#define BOOT_LOADER_CHARACTERISTIC_UUID          "00060001-F8CE-11E4-ABF4-0002A5D5C51B"

