using System;
using System.Collections;

namespace startechplus.ble
{
	public static class BluetoothLeEvents  {

		public const string StateUpdate = "StateUpdate";
		public const string Startup = "Startup";
		public const string Shutdown = "Shutdown";
		public const string Error = "Error";
		public const string ServiceAdded = "ServiceAdded";
		public const string StartedAdvertising = "StartedAdvertising";
		public const string StoppedAdvertising = "StoppedAdvertising";
		public const string DiscoveredPeripheral = "DiscoveredPeripheral";
		public const string RetrievedConnectedPeripheral = "RetrievedConnectedPeripheral";
		public const string ConnectedPeripheral = "ConnectedPeripheral";
		public const string DisconnectedPeripheral = "DisconnectedPeripheral";
		public const string DiscoveredService = "DiscoveredService";
		public const string DiscoveredCharacteristic = "DiscoveredCharacteristic";
		public const string DiscoveredDescriptor = "DiscoveredDescriptor";
		public const string DidWriteCharacteristic = "DidWriteCharacteristic";
		public const string DidUpdateNotificationStateForCharacteristic = "DidUpdateNotificationStateForCharacteristic";
		public const string DidUpdateValueForCharacteristic = "DidUpdateValueForCharacteristic";
		public const string DidWriteDescriptor = "DidWriteDescriptor";
		public const string DidReadDescriptor = "DidReadDescriptor";
		public const string DidUpdateRssi = "DidUpdateRssi";
		public const string DidUpdateAdvertisementData =  "DidUpdateAdvertisementData";
		public const string ScanTimeout =  "ScanTimeout";
		public const string SelectDevice =  "SelectDevice";
		public const string ConnectTimeout= "ConnectTimeout";
		public const string PairingTimeout = "PairingTimeout";

		public const string StateUnknown = "Unknown";
		public const string StateResetting = "Resetting";
		public const string StateUnsupported = "Unsupported";
		public const string StateUnauthorized = "Unauthorized";
		public const string StatePoweredOff = "Powered Off";
		public const string StatePoweredOn = "Powered On";

		public const string NativeLog = "NativeLog";



	}
}
