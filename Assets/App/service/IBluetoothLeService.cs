using UnityEngine;
using System.Collections;

namespace startechplus.ble
{
	public interface IBluetoothLeService 
	{

		BluetoothLeEventSignal bluetoothLeEventSignal{get;set;}

		void EnableDebug(bool isDebug);

		void Startup();
		
		void Shutdown();
		
		void PauseWithState(bool isPaused);
		
		void ScanForPeripheralsWithServiceUUIDs(string[] serviceUUIDS);
		
		void RetrieveListOfPeripheralsWithServiceUUIDs (string[] serviceUUIDs);

		void RetrieveListOfPeripheralsWithUUIDs (string[] serviceUUIDs);
		
		void StopScanning ();
		
		void ConnectToPeripheral(string identifier);
		
		void DisconnectFromPeripheral(string identifier);
		
		void ReadCharacteristic(string identifier, string service, string characteristic);
		
		void WriteCharacteristic(string identifier, string service, string characteristic, byte[] data, int length, bool withResponse);
		
		void SubscribeToCharacteristic(string identifier, string service, string characteristic, bool isIndication);
		
		void UnSubscribeFromCharacteristic(string identifier, string service, string characteristic);

		void ReadDescriptor(string identifier, string service, string characteristic, string descriptor);

		void WriteDescriptor(string identifier, string service, string characteristic, string descriptor, byte[] data, int length);


	}

}
