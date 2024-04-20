using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using strange.extensions.context.api;
using strange.extensions.injector.api;
using startechplus;
using startechplus.ble;

namespace startechplus.ble
{
	
	public class BluetoothLeService : IBluetoothLeService 
	{
		[Inject]
		public BluetoothLeEventSignal bluetoothLeEventSignal{get;set;}

		[Inject]
		public IBleBridge bluetoothLeBridge{get;set;}

		private bool isDebugEnabled = false;

		private void Log(string message){
			if (isDebugEnabled) {
				Debug.Log (message);
			}
		}

		private string byteArrayToString(byte[] ba)
		{
			StringBuilder hex = new StringBuilder(ba.Length * 2);
			foreach (byte b in ba)
				hex.AppendFormat("{0:x2}", b);
			return hex.ToString();
		}

		private void DidUpdateRssiAction(string identifier, string rssi)
		{
			Log("BluetoothLeService : DidUpdateRssiAction() " + identifier + ",  " + rssi);
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("rssi", rssi);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DidUpdateRssi, dict);
		}

		private void LogAction(string logString){
			Log ("BluetoothNativeLog : " + logString);
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("logString", logString);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.NativeLog, dict);
		}

		private void StateUpdateAction(string state)
		{
			Log("BluetoothLeService : StateUpdateAction() " + state);
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("state", state);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.StateUpdate, dict);
		}

		private void StartupAction()
		{
			Log("BluetoothLeService : StartupAction()");
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.Startup, null);
		}

		private void DeinitializedAction()
		{
			Log("BluetoothLeService : DeinitializedAction()");
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.Shutdown, null);
		}

		private void ErrorAction(string error)
		{
			Log("BluetoothLeService : ErrorAction() " + error);
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("error", error);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.Error, dict);
		}

		private void ServiceAddedAction(string service)
		{
			Log("BluetoothLeService : ServiceAddedAction() " + service);
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("service", service);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.ServiceAdded, dict);
		}

		private void DiscoveredPeripheralAction(string identifier, string name)
		{
			Log("BluetoothLeService : DiscoveredPeripheralAction() " + identifier + ",  " + name);
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("name", name);
			dict.Add("identifier", identifier);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DiscoveredPeripheral, dict);
		}

		private void RetrievedConnectedPeripheralAction(string identifier, string name)
		{
			Log("BluetoothLeService : RetrievedConnectedPeripheralAction() " + identifier + ",  " + name);
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("name", name);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.RetrievedConnectedPeripheral, dict);
		}

		private void ConnectedPeripheralAction(string identifier, string name)
		{
			Log("BluetoothLeService : ConnectedPeripheralAction() " + identifier);
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("name", name);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.ConnectedPeripheral, dict);
		}
		private void DisconnectedPeripheralAction(string identifier, string name)
		{
			Log("BluetoothLeService : DisconnectedPeripheralAction() " + identifier);
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("name", name);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DisconnectedPeripheral, dict);
		}

		private void DiscoveredServiceAction(string identifier, string service)
		{
			Log("BluetoothLeService : DiscoveredServiceAction() " + identifier + ", " + service);
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("service", service);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DiscoveredService, dict);
		}

		private void DiscoveredCharacteristicAction(string identifier, string service, string characteristic)
		{
			Log("BluetoothLeService : DiscoveredCharacteristicAction() " + identifier + ", " + service +  ", " + characteristic);
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("service", service);
			dict.Add("characteristic",characteristic);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DiscoveredCharacteristic, dict);
		}

		private void DidWriteCharacteristicAction(string identifier, string service, string characteristic)
		{
			//Log("BluetoothLeService : DidWriteCharacteristicAction() " + identifier);
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("service", service);
			dict.Add("characteristic", characteristic);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DidWriteCharacteristic, dict);
		}

		private void DidUpdateNotifiationStateForCharacteristicAction(string identifier, string service, string uuid)
		{
			Log("BluetoothLeService : DidUpdateNotifiationStateForCharacteristicAction() " + identifier);
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("service", service);
			dict.Add("characteristic", uuid);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DidUpdateNotificationStateForCharacteristic, dict);
		}

		private void DidUpdateCharacteristicValueAction(string identifier, string service, string characteristic, byte[] data)
		{
			//Log("BluetoothLeService : DidUpdateCharacteristicValueAction() " + identifier + ", " + characteristic + "," + byteArrayToString(data));
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("service", service);
			dict.Add("characteristic", characteristic);
			dict.Add("data", data);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DidUpdateValueForCharacteristic, dict);
		}

		private void DidWriteDescriptorAction(string identifier, string service, string characteristic, string desctriptor)
		{
			Log("BluetoothLeService : DidWriteDescriptorAction() " + identifier);
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("service", service);
			dict.Add("characteristic", characteristic);
			dict.Add("desctriptor", desctriptor);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DidWriteDescriptor, dict);
		}

		private void DidReadDescriptorAction(string identifier, string service, string characteristic, string descriptor,  byte[] data)
		{
			Log("BluetoothLeService : DidReadDescriptorAction() " + identifier + ", " + characteristic + "," + descriptor + "," + byteArrayToString(data));
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("characteristic", characteristic);
			dict.Add("service", service);
			dict.Add("desctriptor", descriptor);
			dict.Add("data", data);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DidUpdateValueForCharacteristic, dict);
		}

		private void DiscoveredDescriptorAction(string identifier, string service, string characteristic, string descriptor)
		{
			Log("BluetoothLeService : DiscoveredDescriptorAction() " + identifier + ", " + service +  ", " + characteristic+  ", " + descriptor);
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("service", service);
			dict.Add("characteristic",characteristic);
			dict.Add("descriptor", descriptor);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DiscoveredDescriptor, dict);
		}

		private void AdvertiseLocalNameAction(string identifier, string localName)
		{
			Log("BluetoothLeService: AdvertiseLocalNameAction,  " + identifier + ", " + localName);

			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("key", "LocalName");
			dict.Add ("value", localName);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DidUpdateAdvertisementData, dict);

		}

		private void AdvertiseManufactureDataAction(string identifier, byte[]data)
		{
			Log("BluetoothLeService: AdvertiseManufactureDataAction, " + identifier + ", " + BitConverter.ToString(data));

			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("key", "ManufactureData");
			dict.Add ("value", data);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DidUpdateAdvertisementData, dict);
		}

		private void AdvertiseServiceDataAction(string identifier, string service, byte[] data)
		{
			Log("BluetoothLeService: AdvertiseServiceDataAction, " + identifier + ", " + service + ", " + BitConverter.ToString(data));

			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("key", "ServiceData");
			dict.Add ("value", data);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DidUpdateAdvertisementData, dict);
		}

		private void AdvertiseServiceAction(string identifier, string service)
		{
			Log("BluetoothLeService: AdvertiseServiceAction, " + identifier + ", " + service);
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("key", "Service");
			dict.Add ("value", service);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DidUpdateAdvertisementData, dict);
		}

		private void AdvertiseOverflowServiceAction(string identifier, string service)
		{
			Log("BluetoothLeService: AdvertiseOverflowServiceAction, " + identifier + ", " + service);

			Log("BluetoothLeService: AdvertiseServiceAction, " + identifier + ", " + service);
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("key", "OverflowService");
			dict.Add ("value", service);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DidUpdateAdvertisementData, dict);
		}

		private void AdvertiseTxPowerLevelActionAction(string identifier, string txPowerLevel)
		{
			Log("BluetoothLeService: AdvertiseTxPowerLevelActionAction, " + identifier + ", " + txPowerLevel);

			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("key", "TxPowerLevel");
			dict.Add ("value", txPowerLevel);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DidUpdateAdvertisementData, dict);
		}

		private void AdvertiseIsConnectableAction(string identifier, string isConnectable)
		{
			Log("BluetoothLeService: AdvertiseIsConnectableAction, " + identifier + ", " + isConnectable);

			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("key", "IsConnectable");
			dict.Add ("value", isConnectable);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DidUpdateAdvertisementData, dict);
		}

		private void AdvertiseSolicitedServiceAction(string identifier, string solicitedServiceID)
		{
			Log("BluetoothLeService: AdvertiseSolicitedServiceAction, " + identifier + ", " + solicitedServiceID);

			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("identifier", identifier);
			dict.Add("key", "SolicitedService");
			dict.Add ("value", solicitedServiceID);
			bluetoothLeEventSignal.Dispatch(BluetoothLeEvents.DidUpdateAdvertisementData, dict);
		}

		public void Startup()
		{

			bluetoothLeBridge.Startup(true, StartupAction, ErrorAction, StateUpdateAction, DidUpdateRssiAction);
		}

		public void Shutdown()
		{
			bluetoothLeBridge.Shutdown(DeinitializedAction);
		}

		public void PauseWithState(bool isPaused)
		{
			bluetoothLeBridge.PauseWithState(isPaused);
		}

		public void ScanForPeripheralsWithServiceUUIDs(string[] serviceUUIDS)
		{
			bluetoothLeBridge.AddAdvertisementDataListeners(AdvertiseLocalNameAction,
				AdvertiseManufactureDataAction,
				AdvertiseServiceDataAction,
				AdvertiseServiceAction,
				AdvertiseOverflowServiceAction,
				AdvertiseTxPowerLevelActionAction,
				AdvertiseIsConnectableAction,
				AdvertiseSolicitedServiceAction);

			bluetoothLeBridge.ScanForPeripheralsWithServiceUUIDs(serviceUUIDS, DiscoveredPeripheralAction);
		}

		public void RetrieveListOfPeripheralsWithServiceUUIDs(string[] serviceUUIDs)
		{
			bluetoothLeBridge.RetrieveListOfPeripheralsWithServiceUUIDs(serviceUUIDs, RetrievedConnectedPeripheralAction);
		}

		public void RetrieveListOfPeripheralsWithUUIDs(string[] uuids)
		{
			bluetoothLeBridge.RetrieveListOfPeripheralsWithUUIDs(uuids, RetrievedConnectedPeripheralAction);
		}

		public void StopScanning ()
		{
			bluetoothLeBridge.StopScanning();
		}

		public void ConnectToPeripheral(string identifier)
		{
			bluetoothLeBridge.ConnectToPeripheralWithIdentifier(identifier, ConnectedPeripheralAction, DiscoveredServiceAction, DiscoveredCharacteristicAction, DiscoveredDescriptorAction,  DisconnectedPeripheralAction);
		}

		public void DisconnectFromPeripheral (string identifier)
		{
			bluetoothLeBridge.DisconnectFromPeripheralWithIdentifier(identifier, DisconnectedPeripheralAction);
		}

		public void ReadCharacteristic (string identifier, string service, string characteristic)
		{
			bluetoothLeBridge.ReadCharacteristicWithIdentifiers(identifier, service, characteristic, DidUpdateCharacteristicValueAction);
		}

		public void WriteCharacteristic (string identifier, string service, string characteristic, byte[] data, int length, bool withResponse)
		{
			bluetoothLeBridge.WriteCharacteristicWithIdentifiers(identifier, service, characteristic, data, length, withResponse, DidWriteCharacteristicAction);
		}

		public void SubscribeToCharacteristic (string identifier, string service, string characteristic, bool isIndication)
		{
			bluetoothLeBridge.SubscribeToCharacteristicWithIdentifiers(identifier, service, characteristic, DidUpdateNotifiationStateForCharacteristicAction, DidUpdateCharacteristicValueAction, isIndication);
		}

		public void UnSubscribeFromCharacteristic (string identifier, string service, string characteristic)
		{
			bluetoothLeBridge.UnSubscribeFromCharacteristicWithIdentifiers(identifier, service, characteristic, null);
		}

		public void ReadDescriptor(string identifier, string service, string characteristic, string descriptor)
		{
			bluetoothLeBridge.ReadDescriptorWithIdentifiers(identifier, service, characteristic, descriptor, DidReadDescriptorAction);
		}
		
		public void WriteDescriptor(string identifier, string service, string characteristic, string descriptor, byte[] data, int length)
		{
			bluetoothLeBridge.WriteDescriptorWithIdentifiers(identifier, service, characteristic, descriptor, data, length, DidWriteDescriptorAction);
		}

		public void EnableDebug(bool isDebug){
			isDebugEnabled = isDebug;
			bluetoothLeBridge.EnableDebug (isDebug, LogAction);
		}

	}
}
