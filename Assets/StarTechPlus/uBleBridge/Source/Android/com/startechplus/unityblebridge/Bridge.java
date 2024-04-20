package com.startechplus.unityblebridge;

import com.unity3d.player.UnityPlayer;

import android.app.Activity;
import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothGatt;
import android.bluetooth.BluetoothGattCallback;
import android.bluetooth.BluetoothGattCharacteristic;
import android.bluetooth.BluetoothGattDescriptor;
import android.bluetooth.BluetoothGattService;
import android.bluetooth.BluetoothManager;
import android.bluetooth.BluetoothProfile;
import android.bluetooth.le.BluetoothLeScanner;
import android.bluetooth.le.ScanCallback;
import android.bluetooth.le.ScanResult;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.pm.PackageManager;
import android.os.Handler;
import android.os.Looper;
import android.os.ParcelUuid;
import android.util.Base64;
import android.util.SparseArray;

import java.lang.reflect.Method;
import java.util.Arrays;
import java.util.Iterator;
import java.util.LinkedList;
import java.util.List;
import java.util.Map.Entry;
import java.util.Queue;
import java.util.UUID;
import java.util.HashMap;
import java.util.Set;

import android.util.Log;

import static android.bluetooth.BluetoothDevice.BOND_BONDED;
import static android.bluetooth.BluetoothDevice.BOND_BONDING;
import static android.bluetooth.BluetoothDevice.BOND_NONE;
import static android.bluetooth.BluetoothDevice.TRANSPORT_LE;

public class Bridge {

	private HashMap<String, BluetoothPeripheral> peripherals = new HashMap<String, BluetoothPeripheral>();
	

	Context context;
	private static final String TAG = "UnityBleBridge";
	private boolean isDebug = false;
	
	private static final UUID CLIENT_CHARACTERISTIC_CONFIGURATION = UUID.fromString("00002902-0000-1000-8000-00805f9b34fb");

	public static String gameObjectName = "BleBridge";
	private static long scanPeriod = 60000;
	private BluetoothAdapter mBluetoothAdapter;
	private boolean mScanning = false;
	//private Handler mHandler;
	private PeripheralFilter peripheralFilter = new PeripheralFilter(PeripheralFilter.NONE);

	private Queue<GattComm> gattQueue = new LinkedList<GattComm>();
	private boolean gattQueueIsPending = false;

	java.util.Locale l = java.util.Locale.US;

	private void logMe(String _tag, String message)
	{
		if(isDebug) {
			Log.i(_tag, message);
			UnityPlayer.UnitySendMessage(gameObjectName, "OnLog", message);
		}
	}
	
	private void sendGattComm(final GattComm gattComm)
	{
		Handler handler = new Handler(Looper.getMainLooper());
		handler.post(new Runnable() {
			@Override
			public void run() {

				switch(gattComm.type)
				{
					case GattComm.READ_CHARACTERISTIC:

						logMe(TAG, "sendGattComm(READ_C)");

						gattComm.gatt.readCharacteristic((BluetoothGattCharacteristic)gattComm.actor);
						break;
					case GattComm.READ_DESCRIPTOR:

						logMe(TAG, "sendGattComm(READ_D)");

						gattComm.gatt.readDescriptor((BluetoothGattDescriptor)gattComm.actor);
						break;
					case GattComm.READ_RSSI:

						logMe(TAG, "sendGattComm(READ_R)");

						gattComm.gatt.readRemoteRssi();
						break;
					case GattComm.WRITE_CHARACTERISTIC:

						logMe(TAG, "sendGattComm(WRITE_C) " + Arrays.toString(gattComm.data));

						if(gattComm.data.length > 0)
						{
							((BluetoothGattCharacteristic)gattComm.actor).setValue(gattComm.data);
							((BluetoothGattCharacteristic)gattComm.actor).setWriteType(gattComm.writeType);
							gattComm.gatt.writeCharacteristic((BluetoothGattCharacteristic)gattComm.actor);
						}
						break;
					case GattComm.WRITE_DESCRIPTOR:

						logMe(TAG, "sendGattComm(WRITE_D) " + Arrays.toString(gattComm.data));

						if(gattComm.data.length > 0)
						{
							((BluetoothGattDescriptor)gattComm.actor).setValue(gattComm.data);
							gattComm.gatt.writeDescriptor((BluetoothGattDescriptor)gattComm.actor);
						}
						break;
				}
			}
		});


	}
	
	private void processGattComm(GattComm gattComm)
	{
		if(!gattQueueIsPending)
		{
			logMe(TAG, "processGattComm() :  Empty Queue Sending...");

			gattQueueIsPending = true;
			sendGattComm(gattComm);
		}
		else
		{
			logMe(TAG, "processGattComm() :  Enqueuing...");

			gattQueue.add(gattComm);
		}
	}
	
	private void checkGattQueue()
	{
		if(gattQueue.isEmpty())
		{
			logMe(TAG, "checkGattQueue() :  Empty Queue...");

			gattQueueIsPending = false;
		}
		else
		{
			logMe(TAG, "checkGattQueue(" + gattQueue.size() + ") :  Sending...");

			sendGattComm(gattQueue.poll());
		}
	}


	private void clearGattQueue()
	{
		gattQueue.clear();

		checkGattQueue();
	}

	static Bridge _instance = null;

	public Bridge() {
		_instance = this;
	}

	public static Bridge instance() {
		if (_instance == null)
			_instance = new Bridge();

		return _instance;

	}

	public void setContext(Context ctx) {
		this.context = ctx;

		logMe(TAG, "Application context set...");

	}

	private static final BroadcastReceiver mReceiver = new BroadcastReceiver() {
		@Override
		public void onReceive(Context context, Intent intent) {
			final String action = intent.getAction();

			if (action.equals(BluetoothAdapter.ACTION_STATE_CHANGED)) {
				final int state = intent.getIntExtra(BluetoothAdapter.EXTRA_STATE, BluetoothAdapter.ERROR);
				switch (state) {
				case BluetoothAdapter.STATE_OFF:
					UnityPlayer.UnitySendMessage(gameObjectName, "OnBleStateUpdate", "Powered Off");
					break;
				case BluetoothAdapter.STATE_TURNING_OFF:
					UnityPlayer.UnitySendMessage(gameObjectName, "OnBleStateUpdate", "Unknown");
					break;
				case BluetoothAdapter.STATE_ON:
					UnityPlayer.UnitySendMessage(gameObjectName, "OnBleStateUpdate", "Powered On");
					break;
				case BluetoothAdapter.STATE_TURNING_ON:
					UnityPlayer.UnitySendMessage(gameObjectName, "OnBleStateUpdate", "Unknown");
					break;
				}
			} else if (action.equals(BluetoothDevice.ACTION_FOUND)) {
				//BluetoothDevice device = intent.getParcelableExtra(BluetoothDevice.EXTRA_DEVICE);

			} else if (action.equals(BluetoothDevice.ACTION_ACL_CONNECTED)) {
				//BluetoothDevice device = intent.getParcelableExtra(BluetoothDevice.EXTRA_DEVICE);

			} else if (action.equals(BluetoothDevice.ACTION_ACL_DISCONNECTED)) {
				//BluetoothDevice device = intent.getParcelableExtra(BluetoothDevice.EXTRA_DEVICE);

			}
		}
	};

	public static void RegisterBroadcastReciever(Activity activity) {
		// Register for broadcasts on BluetoothAdapter state change
		IntentFilter filter = new IntentFilter();
		filter.addAction(BluetoothAdapter.ACTION_STATE_CHANGED);
		filter.addAction(BluetoothDevice.ACTION_FOUND);
		filter.addAction(BluetoothDevice.ACTION_ACL_CONNECTED);
		filter.addAction(BluetoothDevice.ACTION_ACL_DISCONNECTED);
		activity.registerReceiver(mReceiver, filter);
	}

	public static void DeregisterBroadcastReciever(Activity activity) {
		activity.unregisterReceiver(mReceiver);
	}

	/*
	 * public static boolean onActivityResult(int requestCode, int resultCode,
	 * Intent data){ boolean retVal = true;
	 * 
	 * switch (requestCode) { case ActivityResultCodes.REQUEST_ENABLE_BT:
	 * if(resultCode == Activity.RESULT_OK) {
	 * UnityPlayer.UnitySendMessage(gameObjectName, "OnBleStateUpdate",
	 * "Powered On"); } else { UnityPlayer.UnitySendMessage (gameObjectName,
	 * "OnBleStateUpdate", "Powered Off"); } break;
	 * 
	 * default: retVal = false; break; }
	 * 
	 * return retVal; }
	 */

	private String onUpdatePeripheral(BluetoothPeripheral peripheral, String event, String identifier) {
		String message;

		String ident = identifier == null ? "Unknown" : identifier;

		String newKey = UUID.randomUUID().toString();

		if (peripheral.device.getAddress() != null && peripheral.device.getAddress().length() > 0)
			newKey = peripheral.device.getAddress();

		if(peripherals.containsKey(newKey))
		{
			BluetoothPeripheral oldPeripheral = peripherals.get(newKey);

			if(oldPeripheral.device.equals(peripheral.device)){
				logMe(TAG, "onUpdatePeripheral() : using existing peripheral : " + newKey);
			}else{
				logMe(TAG, "onUpdatePeripheral() : updating peripheral : " + newKey);
				peripherals.put(newKey, peripheral);
			}

		}
		else
		{
			logMe(TAG, "onUpdatePeripheral() : adding peripheral : " + newKey);

			peripherals.put(newKey, peripheral);
		}

		/*
		Iterator<Entry<java.lang.String, BluetoothPeripheral>> it = peripherals.entrySet().iterator();

		while (it.hasNext()) {
			Entry<java.lang.String, BluetoothPeripheral> kvp = it.next();

			BluetoothPeripheral listPeripheral = kvp.getValue();

			if (listPeripheral.equals(peripheral)) {

				message = String.format("%d:%s%d:%s", kvp.getKey().length(), kvp.getKey(), ident.length(), ident);

				UnityPlayer.UnitySendMessage(gameObjectName, event, message);

				return kvp.getKey();
			}
		}
		*/





		message = String.format(l, "%d:%s%d:%s", newKey.length(), newKey, ident.length(), ident);

		UnityPlayer.UnitySendMessage(gameObjectName, event, message);
		
		return newKey;

	}
	
	private void unitySendUpdate(String peripherialID,  String key, String value)
	{
		//logMe(TAG, "onLeScan() : unitySendUpdate " + peripherialID + ", " + key + ", "+ value);
		String message = String.format(l, "%d:%s%d:%s", peripherialID.length(), peripherialID, value.length(), value);
		UnityPlayer.UnitySendMessage(gameObjectName, key, message);	
	}

	// Device scan callback.
	private final android.bluetooth.le.ScanCallback mLeScanCallback = new android.bluetooth.le.ScanCallback() {
//	private BluetoothAdapter.LeScanCallback mLeScanCallback = new BluetoothAdapter.LeScanCallback() {
		@Override
//		public void onLeScan(final BluetoothDevice device, int rssi, byte[] scanRecord) {
		public void onScanResult(int type, ScanResult result) {

			final BluetoothDevice device = result.getDevice();
			int rssi = result.getRssi();
			byte[] scanRecord = result.getScanRecord().getBytes();

			BluetoothPeripheral peripheral = new BluetoothPeripheral(device, rssi, scanRecord);

			String deviceName = device.getName();
			
			if (deviceName == null) {
				deviceName = peripheral.advertisedData.getName();
			}
						
			if(peripheralFilter.filterWith(peripheral.device.getAddress(), PeripheralFilter.PERIPHERAL_UUID))
			{

				logMe(TAG, "onLeScan() : " + device.getAddress().toString() + ", " + rssi + ", matched filter...");

				
				String peripherialID = onUpdatePeripheral(peripheral, "OnDiscoveredPeripheral", deviceName);
				
				if(peripheral.advertisedData.getName() != null)
				{
					unitySendUpdate(peripherialID, "OnAdvertisementDataLocalName", peripheral.advertisedData.getName());
				}
				
				//logMe(TAG, "onLeScan() : D1 " + scanRecord.length);
				
				ScanRecord sRecord = ScanRecord.parseFromBytes(scanRecord);
				
				SparseArray<byte[]> manufactureData = sRecord.getManufacturerSpecificData();
				
				if(manufactureData != null)
				{
					for(int i = 0, nsize = manufactureData.size(); i < nsize; i++) {
					    byte[] data = manufactureData.valueAt(i);
					    unitySendUpdate(peripherialID, "OnAdvertisementDataManufactureData", Base64.encodeToString(data, Base64.DEFAULT));
					}
				}
				
				//logMe(TAG, "onLeScan() : D2");
				List<ParcelUuid> sUUIDs = sRecord.getServiceUuids();
				//logMe(TAG, "onLeScan() : D2a ");
				
				if(sUUIDs != null)
				{
					//logMe(TAG, "onLeScan() : D2b");

					for(int i = 0; i < sUUIDs.size(); i++)
					{
						//logMe(TAG, "onLeScan() : D2c");
						ParcelUuid sUUID = sUUIDs.get(i);
						if(sUUID != null)
						{
							unitySendUpdate(peripherialID, "OnAdvertisementDataServiceUUID", sUUID.toString());
							//logMe(TAG, "onLeScan() : D2d");
							byte[] data = sRecord.getServiceData(sUUID);
							//logMe(TAG, "onLeScan() : D2e");
							if(data != null)
							{

								/*
								NSString *message = [NSString stringWithFormat:@"%d:%@%d:%@%d:%@",
                                     (int)strlen([peripheralId UTF8String]), peripheralId,
                                     (int)strlen([serviceUUID UTF8String]), serviceUUID,
                                     (int)strlen([dataString  UTF8String]), dataString];

                                 unitySendUpdate(peripherialID, "OnAdvertisementDataServiceData", Base64.encodeToString(data, Base64.DEFAULT));

								 */

								String dataString = Base64.encodeToString(data, Base64.DEFAULT);

								//logMe(TAG, "onLeScan() : D3, " + dataString);

								String message = String.format(l, "%d:%s%d:%s%d:%s",
										peripherialID.length(), peripherialID,
										sUUID.toString().length(), sUUID.toString(),
										dataString.length(), dataString);

								UnityPlayer.UnitySendMessage(gameObjectName, "OnAdvertisementDataServiceData", message);

							}
						}
					}
				}
				
				//logMe(TAG, "onLeScan() : D3");
				if(sRecord.getTxPowerLevel() != java.lang.Integer.MIN_VALUE)
				{
					unitySendUpdate(peripherialID, "OnAdvertisementDataTxPowerLevel", "" + sRecord.getTxPowerLevel());
				}
				
				//logMe(TAG, "onLeScan() : D4");
				if(sRecord.getAdvertiseFlags() != -1)
				{
					unitySendUpdate(peripherialID, "OnAdvertisementDataIsConnectable", "" + sRecord.getAdvertiseFlags());
				}
				
				//logMe(TAG, "onLeScan() : D5");
				unitySendUpdate(peripherialID, "OnRssiUpdate", "" + rssi);
				
				/*
				Iterator<Entry<java.lang.String, BluetoothPeripheral>> it = peripherals.entrySet().iterator();

				while (it.hasNext()) {
					Entry<java.lang.String, BluetoothPeripheral> kvp = it.next();

					BluetoothPeripheral listPeripheral = kvp.getValue();

					if (listPeripheral.equals(peripheral)) {

						String message = String.format("%d:%s%d:%s", kvp.getKey().length(), kvp.getKey(), (rssi+"").length(), rssi);

						UnityPlayer.UnitySendMessage(gameObjectName, "OnRssiUpdate", message);

						return;
					}
				}
				*/
				
			}
			else
			{

				logMe(TAG, "onLeScan() : " + device.getAddress().toString() + ", " + rssi + ", mismatched filter...");

			}
		}
	};

	public void startup(String goName, boolean asCentral) {
		logMe(TAG, "startup(" + goName + ", " + asCentral + ")");


		gameObjectName = goName;

		if (context == null) {
			logMe(TAG, "startup() : " + "context == null");

			return;
		}

		UnityPlayer.UnitySendMessage(gameObjectName, "OnStartup", "Active");

		Handler handler = new Handler(Looper.getMainLooper());
		handler.post(new Runnable() {
			@Override
			public void run() {
				if (context.getPackageManager().hasSystemFeature(PackageManager.FEATURE_BLUETOOTH_LE)) {
					final BluetoothManager bluetoothManager = (BluetoothManager) context.getSystemService(Context.BLUETOOTH_SERVICE);
					mBluetoothAdapter = bluetoothManager.getAdapter();
					RegisterBroadcastReciever(UnityPlayer.currentActivity);

					if (mBluetoothAdapter == null || !mBluetoothAdapter.isEnabled()) {
						logMe(TAG, "startup() : " + "ble disabled, asking user...");


						Intent enableBtIntent = new Intent(BluetoothAdapter.ACTION_REQUEST_ENABLE);
						UnityPlayer.currentActivity.startActivityForResult(enableBtIntent, ActivityResultCodes.REQUEST_ENABLE_BT);
					} else {
						logMe(TAG, "startup() : " + "ble enabled...");


						UnityPlayer.UnitySendMessage(gameObjectName, "OnBleStateUpdate", "Powered On");
					}
				} else {
					logMe(TAG, "startup() : " + "ble not supported...");


					UnityPlayer.UnitySendMessage(gameObjectName, "OnBleStateUpdate", "Unsupported");
				}

			}
		});


	}
	
	private void sendCharacteristicUpdate(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
	{
		String cValue = Base64.encodeToString(characteristic.getValue(), Base64.DEFAULT);
		
		String message = String.format(l, "%d:%s%d:%s%d:%s%d:%s", gatt.getDevice().getAddress().length(), gatt.getDevice().getAddress(),
				characteristic.getService().getUuid().toString().length(), characteristic.getService().getUuid().toString(),
				characteristic.getUuid().toString().length(), characteristic.getUuid().toString(), 
				cValue.length(), cValue);
                    
        UnityPlayer.UnitySendMessage(gameObjectName, "OnBluetoothData", message);
	}
	

	private final BluetoothGattCallback mGattCallback = new BluetoothGattCallback() 
	{
		@Override
		public void onReadRemoteRssi (BluetoothGatt gatt, int rssi, int status)
		{

			logMe(TAG, "onReadRemoteRssi() : " + gatt.getDevice().getAddress());

			
			if(status == BluetoothGatt.GATT_SUCCESS)
			{
				String message = String.format(l, "%d:%s%d:%s", gatt.getDevice().getAddress().length(), gatt.getDevice().getAddress(), (rssi+"").length(), rssi);

				UnityPlayer.UnitySendMessage(gameObjectName, "OnRssiUpdate", message);
			}
			
			checkGattQueue();
			
		}
		
		@Override
		public void onCharacteristicChanged (BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
		{
			logMe(TAG, "onCharacteristicChanged() : " + characteristic.getUuid().toString());

			sendCharacteristicUpdate(gatt, characteristic);
		}
		
		@Override
		public void onCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, int status)
		{
			if(status == BluetoothGatt.GATT_SUCCESS)
			{
				logMe(TAG, "onCharacteristicRead() : " + characteristic.getUuid().toString());

				sendCharacteristicUpdate(gatt, characteristic);
			}
			else
			{
				logMe(TAG, "onCharacteristicRead() : failed : " + status);

			}
			
			checkGattQueue();
		}
		
		@Override
		public void onCharacteristicWrite (BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, int status)
		{
			logMe(TAG, "onCharacteristicWrite()");


			if(status == BluetoothGatt.GATT_SUCCESS)
			{
				if( characteristic.getWriteType() == BluetoothGattCharacteristic.WRITE_TYPE_DEFAULT )
				{
					logMe(TAG, "onCharacteristicWrite() : " + characteristic.getUuid().toString());

				
				String peripheralAddress = gatt.getDevice().getAddress();
										
					String message = String.format(l, "%d:%s%d:%s%d:%s", peripheralAddress.length(), peripheralAddress,
							characteristic.getService().getUuid().toString().length(), characteristic.getService().getUuid().toString(),
							characteristic.getUuid().toString().length(), characteristic.getUuid().toString() );
			    				
			    UnityPlayer.UnitySendMessage (gameObjectName, "OnDidWriteCharacteristic", message);
				}
				
			}
			else
			{
				logMe(TAG, "onCharacteristicWrite() : failed : " + status);

			}
			
			checkGattQueue();
		}
		
		@Override
		public void onDescriptorRead (BluetoothGatt gatt, BluetoothGattDescriptor descriptor, int status)
		{
			if(status == BluetoothGatt.GATT_SUCCESS)
			{
				logMe(TAG, "onDescriptorRead() : " + descriptor.getUuid().toString());


				BluetoothGattCharacteristic characteristic = descriptor.getCharacteristic();

				String dValue = Base64.encodeToString(descriptor.getValue(), Base64.DEFAULT);

					String message = String.format(l, "%d:%s%d:%s%d:%s%d:%s%d:%s", gatt.getDevice().getAddress().length(), gatt.getDevice().getAddress(),
							characteristic.getService().getUuid().toString().length(), characteristic.getService().getUuid().toString(),
							characteristic.getUuid().toString().length(), characteristic.getUuid().toString(),
							descriptor.getUuid().toString().length(), descriptor.getUuid().toString(),
							dValue.length(), dValue);

				UnityPlayer.UnitySendMessage(gameObjectName, "OnDescriptorRead", message);
			}
			else
			{
				logMe(TAG, "onDescriptorRead() : failed : " + status);

			}
			
			checkGattQueue();
			
		}
		
		@Override
		public void onDescriptorWrite (BluetoothGatt gatt, BluetoothGattDescriptor descriptor, int status)
		{

			if(status == BluetoothGatt.GATT_SUCCESS)
			{
				BluetoothGattCharacteristic characteristic = descriptor.getCharacteristic();

				logMe(TAG, "onDescriptorWrite() : " + descriptor.getUuid().toString());

				
				String peripheralAddress = gatt.getDevice().getAddress();

				String message = String.format(l, "%d:%s%d:%s%d:%s%d:%s",
						peripheralAddress.length(), peripheralAddress, 
						characteristic.getService().getUuid().toString().length(), characteristic.getService().getUuid().toString(),
						characteristic.getUuid().toString().length(), characteristic.getUuid().toString(), 
						descriptor.getUuid().toString().length(), descriptor.getUuid().toString() );
			    				
			    UnityPlayer.UnitySendMessage (gameObjectName, "OnDidWriteDescriptor", message);
				
			}
			else
			{
				logMe(TAG, "onDescriptorWrite() : failed : " + status);

			}
			
			checkGattQueue();
		}
		
		@Override
		public void onConnectionStateChange(BluetoothGatt gatt, int status, int newState) {

			logMe(TAG, "onConnectionStateChange()");

			if(status != BluetoothGatt.GATT_SUCCESS && newState != BluetoothProfile.STATE_DISCONNECTED) {
				logMe(TAG, "onConnectionStateChange() : status err: " + status + ", ns: " + newState);

				clearGattQueue();
				gatt.close();
				peripherals.clear();

				return;
			}

			// String intentAction;

			if (newState == BluetoothProfile.STATE_CONNECTED) {

				logMe(TAG, "onConnectionStateChange() : BluetoothProfile.STATE_CONNECTED");


				BluetoothDevice device = gatt.getDevice();

				BluetoothPeripheral peripheral = peripherals.get(device.getAddress());

				peripheral.device = device;

				if (peripheral != null) {
					logMe(TAG, "onConnectionStateChange() : BluetoothProfile.STATE_CONNECTED = " + peripheral.device.getAddress());

					peripheral.gatt = gatt;
					onUpdatePeripheral(peripheral, "OnConnectedPeripheral", peripheral.advertisedData.getName());

					final BluetoothGatt g = gatt;


					Handler handler = new Handler(Looper.getMainLooper());

//					handler.post(new Runnable() {
//						@Override
//						public void run() {
//							g.discoverServices();
//						}
//					});

					int bondState = device.getBondState();

					logMe(TAG, "onConnectionStateChange() : device.getBondState() = " + bondState);

					switch (bondState)
					{
						case BOND_NONE:
							handler.post(new Runnable() {
								@Override
								public void run() {
									g.discoverServices();
								}
							});
							break;
						case BOND_BONDING:	// should have its own callback...
							handler.postDelayed(new Runnable() {
								@Override
								public void run() {
									if(g.getDevice().getBondState() == BOND_BONDED && g.getServices().isEmpty()) {
										g.discoverServices();
									}
									else
									{
//										logMe(TAG, "Delayed bond failure...");
										// problem....
									}
								}
							}, 5000);
							break;
						case BOND_BONDED:
							handler.postDelayed(new Runnable() {
								@Override
								public void run() {
									boolean result = g.discoverServices();
									if(!result) {
										logMe(TAG, "discoverServices start err");
									}
								}
							}, 1000);
							break;
					}


				}else{
					logMe(TAG, "onConnectionStateChange() : BluetoothProfile.STATE_CONNECTED, peripheral = null");

				}

			} else if (newState == BluetoothProfile.STATE_DISCONNECTED) {

				logMe(TAG, "onConnectionStateChange() : BluetoothProfile.STATE_DISCONNECTED");


				BluetoothDevice device = gatt.getDevice();

				final BluetoothPeripheral peripheral = peripherals.get(device.getAddress());

				if (peripheral != null && peripheral.gatt != null)
				{

//					Handler handler = new Handler(Looper.getMainLooper());
//					handler.post(new Runnable() {
//						 @Override
//						 public void run() {
//							 peripheral.gatt.close();
//						 }
//					 });

					clearGattQueue();

					peripheral.gatt.close();

					if(device != null && peripherals.containsKey(device.getAddress())) {
						peripherals.remove(device.getAddress());
//						peripherals.clear();
					}

					onUpdatePeripheral(peripheral, "OnDisconnectedPeripheral", peripheral.advertisedData.getName());

				}else if(peripheral != null && peripheral.advertisedData != null && peripheral.advertisedData.getName() != null && peripheral.advertisedData.getName().length() > 1){
					onUpdatePeripheral(peripheral, "OnDisconnectedPeripheral", peripheral.advertisedData.getName());
				}else{
					logMe(TAG, "onConnectionStateChange() : BluetoothProfile.STATE_DISCONNECTED = null");

					onUpdatePeripheral(peripheral, "OnDisconnectedPeripheral", "Unknown");
				}
			}
		}

//		@Override
		// New services discovered
		public void onServiceChanged(BluetoothGatt gatt) {
			logMe(TAG, "onServiceChanged()");
		}

		@Override
		// New services discovered
		public void onServicesDiscovered(BluetoothGatt gatt, int status) {

			logMe(TAG, "onServicesDiscovered()");


			if (status == BluetoothGatt.GATT_SUCCESS) {
				List<BluetoothGattService> services = gatt.getServices();
				for (int i = 0; i < services.size(); i++) {

					String peripheralId = "Unknown";

					BluetoothPeripheral peripheral = peripherals.get(gatt.getDevice().getAddress());

					if (peripheral != null)
						peripheralId = peripheral.device.getAddress();

					String sUuid = services.get(i).getUuid().toString();

					if(peripheralFilter.filterWith(sUuid, PeripheralFilter.SERVICE_UUID) && peripherals.containsKey(peripheralId))
					{

						logMe(TAG, "onServicesDiscovered() : sUuid matched filter = " + sUuid);

	
						PeripheralService peripheralService = new PeripheralService(services.get(i));
						
						peripherals.get(peripheralId).services.put(sUuid, peripheralService);
	
						String message = String.format(l, "%d:%s%d:%s", peripheralId.length(), peripheralId, sUuid.length(), sUuid);
	
						UnityPlayer.UnitySendMessage(gameObjectName, "OnDiscoveredService", message);
	
						List<BluetoothGattCharacteristic> chars = services.get(i).getCharacteristics();
	
						for (int j = 0; j < chars.size(); j++) {
							String cUuid = chars.get(j).getUuid().toString();
							logMe(TAG, "onServicesDiscovered() : cUuid = " + cUuid);

																					
							PeripheralCharacteristic peripheralCharacteristic = new PeripheralCharacteristic(chars.get(j));
								
							peripheralService.characteristics.put(cUuid, peripheralCharacteristic);
								
							message = String.format(l, "%d:%s%d:%s%d:%s", peripheralId.length(), peripheralId,
									sUuid.length(), sUuid,
									cUuid.length(), cUuid);
								
								UnityPlayer.UnitySendMessage(gameObjectName, "OnDiscoveredCharacteristic", message);
								
								List<BluetoothGattDescriptor> descriptors = chars.get(j).getDescriptors();

								logMe(TAG, "onServicesDiscovered() : descriptors.size() = " + descriptors.size());

								
								for(int k = 0; k < descriptors.size(); k++)
								{
									BluetoothGattDescriptor descriptor = descriptors.get(k);
									
									peripheralCharacteristic.descriptors.put(descriptor.getUuid().toString(), descriptor);
									
								message = String.format(l, "%d:%s%d:%s%d:%s%d:%s",
										peripheralId.length(), peripheralId, 
										sUuid.length(), sUuid,
										cUuid.length(), cUuid,
										descriptor.getUuid().toString().length(), descriptor.getUuid().toString());

									logMe(TAG, "onServicesDiscovered() : dUuid = " + descriptor.getUuid().toString());

									
									UnityPlayer.UnitySendMessage(gameObjectName, "OnDiscoveredDescriptor", message);
								
								}//descriptors loop
					

						} //chars loop
					}
					else
					{
						logMe(TAG, "onServicesDiscovered() : sUuid mismatched filter = " + sUuid);
					}
				}
			} else {
				{
					logMe(TAG, "onServicesDiscovered received: " + status);


				}
			}
		}

	};

	public void shutdown() {
		logMe(TAG, "shutdown()");

		UnityPlayer.UnitySendMessage(gameObjectName, "OnShutdown", "Inactive");

		peripherals.clear();

		DeregisterBroadcastReciever(UnityPlayer.currentActivity);

		if (mBluetoothAdapter != null && mBluetoothAdapter.isEnabled()) {
			if (mScanning) {
				mScanning = false;
				Handler handler = new Handler(Looper.getMainLooper());
				handler.post(new Runnable() {
					@Override
					public void run() {
//						mBluetoothAdapter.stopLeScan(mLeScanCallback);
						mBluetoothAdapter.getBluetoothLeScanner().stopScan(mLeScanCallback);
					}
				});
			}
		}
	}

	public void pauseWithState(boolean isPaused) {
		logMe(TAG, "pauseWithState(" + isPaused + ")");
	}

	private void getBondedPeripherals()
	{
		Set<BluetoothDevice> pairedDevices = mBluetoothAdapter.getBondedDevices();

		if (pairedDevices.size() > 0) {

			for (BluetoothDevice device : pairedDevices) {

                BluetoothPeripheral peripheral = new BluetoothPeripheral(device, device.getBondState(), new byte[]{});

                String deviceName = device.getName();

                if (deviceName == null) {
                    deviceName = peripheral.advertisedData.getName();
                }

                if (peripheralFilter.filterWith(peripheral.device.getAddress(), PeripheralFilter.PERIPHERAL_UUID)) {

                    logMe(TAG, "getBondedPeripherals() : " + device.getAddress().toString() + ", matched filter...");


                    String peripherialID = onUpdatePeripheral(peripheral, "OnDiscoveredPeripheral", deviceName);

                    if (peripheral.advertisedData.getName() != null) {
                        unitySendUpdate(peripherialID, "OnAdvertisementDataLocalName", peripheral.advertisedData.getName());
                    }
                }


			}

		} else {
			logMe(TAG, "getBondedPeripherals() : " + "no bonded devices found...");
		}
	}
	
	private void scanForPeripherals()
	{
//		getBondedPeripherals();

		logMe(TAG, "scanForPeripherals()");


		if (mScanning) {
			logMe(TAG, "scanForPeripherals() : " + "already scanning...");

			return;
		}

		if(scanPeriod > 0) {

			Handler handler = new Handler(Looper.getMainLooper());

			// Stops scanning after a pre-defined scan period.
			handler.postDelayed(new Runnable() {
				@Override
				public void run() {
					if (mScanning) {
						mScanning = false;
//						mBluetoothAdapter.stopLeScan(mLeScanCallback);
						mBluetoothAdapter.getBluetoothLeScanner().stopScan(mLeScanCallback);
						logMe(TAG, "scanForPeripherals() : " + "scanning timeout...");

					}
				}
			}, scanPeriod);
		}

		mScanning = true;
		logMe(TAG, "scanForPeripherals() : " + "starting scan...");

		Handler handler = new Handler(Looper.getMainLooper());
		handler.post(new Runnable() {
			@Override
			public void run() {

//				mBluetoothAdapter.startLeScan(mLeScanCallback);
				mBluetoothAdapter.getBluetoothLeScanner().startScan(mLeScanCallback);
			}
		});


	}

	public void scanForPeripheralsWithServiceUUIDs(String serviceUUIDsString) {
		
		if(serviceUUIDsString != null && serviceUUIDsString.length() > 0)
		{
			String[] uuids = serviceUUIDsString.split("\\|");
			
			
			if(uuids.length > 0)
			{
				peripheralFilter = new PeripheralFilter(PeripheralFilter.SERVICE_UUID);

				for(int i = 0; i < uuids.length; i++)
				{
					peripheralFilter.addUuid(uuids[i]);
				}
			}
			else
			{
				peripheralFilter = new PeripheralFilter(PeripheralFilter.NONE);
			}
			
		}
		else
		{
			peripheralFilter = new PeripheralFilter(PeripheralFilter.NONE);
			
		}
				

		if (mBluetoothAdapter != null && mBluetoothAdapter.isEnabled()) {
			scanForPeripherals();
		} else {
			logMe(TAG, "scanForPeripheralsWithServiceUUIDs() : Bluetooth Adapter Disabled...");

		}

	}

	public void connectToPeripheralWithIdentifier(String peripheralId) {

		logMe(TAG, "connectToPeripheralWithIdentifier()");

		
		if (mBluetoothAdapter != null && mBluetoothAdapter.isEnabled()) 
		{

			if(peripherals.containsKey(peripheralId)) {

				logMe(TAG, "connectToPeripheralWithIdentifier(" + (peripheralId == null ? "null" : peripheralId) + ")");

				if (mScanning)
					stopScanning();

				logMe(TAG, "connectToPeripheralWithIdentifier() : connecting 1 ...");

				final BluetoothPeripheral peripheral = peripherals.get(peripheralId);

				final BluetoothDevice device = mBluetoothAdapter.getRemoteDevice(peripheralId);

				Handler handler = new Handler(Looper.getMainLooper());

				handler.postDelayed(new Runnable() {
					@Override
					public void run() {
						if (device != null) {
							logMe(TAG, "connectToPeripheralWithIdentifier() : connecting 2 ...");

							Method connectGattMethod;

							try{
								connectGattMethod = device.getClass().getMethod("connectGatt", Context.class, boolean.class, BluetoothGattCallback.class, int.class);

								logMe(TAG, "connectToPeripheralWithIdentifier() : connecting HIGH...");

								peripheral.gatt = (BluetoothGatt) connectGattMethod.invoke(device, context, false, mGattCallback, TRANSPORT_LE);

							}catch (Throwable e){
								logMe(TAG, "connectToPeripheralWithIdentifier() : connecting LOW... " + e);
								peripheral.gatt = device.connectGatt(context, false, mGattCallback);
							}


//							handler.postDelayed(new Runnable() {
//								@Override
//								public void run() {
//									peripheral.gatt.connect();
//								}
//							}, 1000);

						}
					}
				}, 500);	//2000
			}



		} else {
			logMe(TAG, "connectToPeripheralWithIdentifier() : Bluetooth Adapter Disabled...");

		}

	}

	public void disconnectFromPeripheralWithIdentifier(String peripheralId) {
		if (mBluetoothAdapter != null && mBluetoothAdapter.isEnabled()) {
			logMe(TAG, "disconnectFromPeripheralWithIdentifier()");

			
			if (peripherals.containsKey(peripheralId)) {
				
				if (mScanning)
					stopScanning();
				logMe(TAG, "disconnectFromPeripheralWithIdentifier() : disconnecting...");

				
				final BluetoothPeripheral peripheral = peripherals.get(peripheralId);
				
				if(peripheral.gatt != null)
				{
                    Handler handler = new Handler(Looper.getMainLooper());

					if(gattQueueIsPending) {
						clearGattQueue();

						handler.postDelayed(new Runnable() {
							@Override
							public void run() {
								peripheral.gatt.disconnect();
							}
						}, 50);
					}
					else {

						handler.post(new Runnable() {
							@Override
							public void run() {
								peripheral.gatt.disconnect();
							}
						});

					}
				}
				else
				{
					logMe(TAG, "disconnectFromPeripheralWithIdentifier() : peripheral.gatt == null -> peripherals.remove(peripheralId)");

					peripherals.remove(peripheralId);
				}
			}
			else
			{
				logMe(TAG, "disconnectFromPeripheralWithIdentifier() : !peripherals.containsKey(peripheralId) -> peripherals.clear()");

				peripherals.clear();
			}
			
		} else {
			logMe(TAG, "disconnectFromPeripheralWithIdentifier() :  Bluetooth Adapter Disabled...");

		}
	}

	public void retrieveListOfPeripheralsWithServiceUUIDs(String serviceUUIDsString) {
		
		if(serviceUUIDsString != null && serviceUUIDsString.length() > 0)
		{
			String[] uuids = serviceUUIDsString.split("\\|");
			
			
			if(uuids.length > 0)
			{
				peripheralFilter = new PeripheralFilter(PeripheralFilter.SERVICE_UUID);
				
				for(int i = 0; i < uuids.length; i++)
				{
					peripheralFilter.addUuid(uuids[i]);
					
				}
			}
			else
			{
				peripheralFilter = new PeripheralFilter(PeripheralFilter.NONE);
			}
			
		}
		else
		{
			peripheralFilter = new PeripheralFilter(PeripheralFilter.NONE);
			
		}
		
		if (mBluetoothAdapter != null && mBluetoothAdapter.isEnabled()) {
			logMe(TAG, "retrieveListOfPeripheralsWithServiceUUIDs()" + serviceUUIDsString);

			scanForPeripherals();
		} else {
			logMe(TAG, "retrieveListOfPeripheralsWithServiceUUIDs() :  Bluetooth Adapter Disabled...");

		}
	}

	public void retrieveListOfPeripheralsWithUUIDs(String uuidsString) {
		
		if(uuidsString != null && uuidsString.length() > 0)
		{
			String[] uuids = uuidsString.split("\\|");
			
			
			if(uuids.length > 0)
			{
				peripheralFilter = new PeripheralFilter(PeripheralFilter.PERIPHERAL_UUID);
				
				for(int i = 0; i < uuids.length; i++)
				{
					peripheralFilter.addUuid(uuids[i]);
					
				}
			}
			else
			{
				peripheralFilter = new PeripheralFilter(PeripheralFilter.NONE);
			}
			
		}
		else
		{
			peripheralFilter = new PeripheralFilter(PeripheralFilter.NONE);
			
		}
		
		if (mBluetoothAdapter != null && mBluetoothAdapter.isEnabled()) {
			logMe(TAG, "retrieveListOfPeripheralsWithUUIDs()");

			scanForPeripherals();
		} else {
			logMe(TAG, "retrieveListOfPeripheralsWithUUIDs() :  Bluetooth Adapter Disabled...");

		}
	}

	public void stopScanning() {

		if (mBluetoothAdapter != null && mBluetoothAdapter.isEnabled()) {
			logMe(TAG, "stopScanning(1)");

			
			if (mScanning) {
				logMe(TAG, "stopScanning(2)");

				mScanning = false;

				Handler handler = new Handler(Looper.getMainLooper());
				handler.post(new Runnable() {
					@Override
					public void run() {

//						mBluetoothAdapter.stopLeScan(mLeScanCallback);
						mBluetoothAdapter.getBluetoothLeScanner().stopScan(mLeScanCallback);
					}
				});


			}
			
		} else {
			logMe(TAG, "stopScanning() :  Bluetooth Adapter Disabled...");

		}

		
	}

	public void subscribeToCharacteristicWithIdentifiers(String peripheralId, String serviceId, String characteristicId, boolean isIndication) {
		if (mBluetoothAdapter != null && mBluetoothAdapter.isEnabled()) {
			logMe(TAG, "subscribeToCharacteristicWithIdentifiers()");

			
			if(peripheralId != null && serviceId != null && characteristicId != null)
			{
				if(peripherals.containsKey(peripheralId) && 
						peripherals.get(peripheralId).services.containsKey(serviceId) && 
						peripherals.get(peripheralId).services.get(serviceId).characteristics.containsKey(characteristicId))
				{
					BluetoothGatt gatt = peripherals.get(peripheralId).gatt;
					
					BluetoothGattCharacteristic characteristic = peripherals.get(peripheralId).services.get(serviceId).characteristics.get(characteristicId).characteristic;
					logMe(TAG, "subscribeToCharacteristicWithIdentifiers() : " + characteristicId);

					
					gatt.setCharacteristicNotification(characteristic, true);
					
					BluetoothGattDescriptor descriptor = characteristic.getDescriptor(CLIENT_CHARACTERISTIC_CONFIGURATION);
					
					if(descriptor != null)
					{
						if(isIndication)
						{
							logMe(TAG, "subscribeToCharacteristicWithIdentifiers() : Indication...");

							processGattComm(new GattComm(gatt, descriptor, GattComm.WRITE_DESCRIPTOR, BluetoothGattDescriptor.ENABLE_INDICATION_VALUE));
						}
						else
						{
							logMe(TAG, "subscribeToCharacteristicWithIdentifiers() : Notification...");

							processGattComm(new GattComm(gatt, descriptor, GattComm.WRITE_DESCRIPTOR, BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE));
						}
						
					}
					
				}
			}
			
		} else {
			logMe(TAG, "subscribeToCharacteristicWithIdentifiers() :  Bluetooth Adapter Disabled...");

		}
	}

	public void unSubscribeFromCharacteristicWithIdentifiers(String peripheralId, String serviceId, String characteristicId) {
		if (mBluetoothAdapter != null && mBluetoothAdapter.isEnabled()) {
			logMe(TAG, "unSubscribeFromCharacteristicWithIdentifiers()");

			
			if(peripheralId != null && serviceId != null && characteristicId != null)
			{
				if(peripherals.containsKey(peripheralId) && 
						peripherals.get(peripheralId).services.containsKey(serviceId) && 
						peripherals.get(peripheralId).services.get(serviceId).characteristics.containsKey(characteristicId))
				{
					BluetoothGatt gatt = peripherals.get(peripheralId).gatt;
					BluetoothGattCharacteristic characteristic = peripherals.get(peripheralId).services.get(serviceId).characteristics.get(characteristicId).characteristic;
					
					gatt.setCharacteristicNotification(characteristic, false);
					
					BluetoothGattDescriptor descriptor = characteristic.getDescriptor(CLIENT_CHARACTERISTIC_CONFIGURATION);
					
					if(descriptor != null)
					{
						logMe(TAG, "unSubscribeFromCharacteristicWithIdentifiers() : Disable...");

						processGattComm(new GattComm(gatt, descriptor, GattComm.WRITE_DESCRIPTOR, BluetoothGattDescriptor.DISABLE_NOTIFICATION_VALUE));
					}
					
				}
			}
			
		} else {
			logMe(TAG, "unSubscribeFromCharacteristicWithIdentifiers() :  Bluetooth Adapter Disabled...");
		}
	}

	public void readCharacteristicWithIdentifiers(String peripheralId, String serviceId, String characteristicId) {
		if (mBluetoothAdapter != null && mBluetoothAdapter.isEnabled()) {
			logMe(TAG, "readCharacteristicWithIdentifiers()");

			
			if(peripheralId != null && serviceId != null && characteristicId != null)
			{
				if(peripherals.containsKey(peripheralId) && 
						peripherals.get(peripheralId).services.containsKey(serviceId) && 
						peripherals.get(peripheralId).services.get(serviceId).characteristics.containsKey(characteristicId))
				{
					BluetoothPeripheral peripheral = peripherals.get(peripheralId);
					BluetoothGatt gatt = peripheral.gatt;
					
					BluetoothGattCharacteristic characteristic = peripherals.get(peripheralId).services.get(serviceId).characteristics.get(characteristicId).characteristic;
					
					processGattComm(new GattComm(gatt, characteristic, GattComm.READ_CHARACTERISTIC, null));
				}
			}
			
		} else {
			logMe(TAG, "readCharacteristicWithIdentifiers() :  Bluetooth Adapter Disabled...");

		}
	}
	
	public void readDescriptorWithIdentifiers(String peripheralId, String serviceId, String characteristicId, String descriptorId) {
		if (mBluetoothAdapter != null && mBluetoothAdapter.isEnabled()) {
			logMe(TAG, "readDescriptorWithIdentifiers()");

			
			if(peripheralId != null && serviceId != null && characteristicId != null)
			{
				if(peripherals.containsKey(peripheralId) && 
						peripherals.get(peripheralId).services.containsKey(serviceId) && 
						peripherals.get(peripheralId).services.get(serviceId).characteristics.containsKey(characteristicId))
				{
					BluetoothGatt gatt = peripherals.get(peripheralId).gatt;
					BluetoothGattDescriptor descriptor = peripherals.get(peripheralId).services.get(serviceId).characteristics.get(characteristicId).descriptors.get(descriptorId);
					
					if(descriptor != null)
					{
						processGattComm(new GattComm(gatt, descriptor, GattComm.READ_DESCRIPTOR, null));
					}
					
				}
			}
			
		} else {
			logMe(TAG, "readDescriptorWithIdentifiers() :  Bluetooth Adapter Disabled...");

		}
	}

	public void writeCharacteristicWithIdentifiers(String peripheralId, String serviceId, String characteristicId, byte[] data, int length, boolean withResponse) {
		if (mBluetoothAdapter != null && mBluetoothAdapter.isEnabled()) {
			logMe(TAG, "writeCharacteristicWithIdentifiers()");

			if(peripheralId != null && serviceId != null && characteristicId != null)
			{
				if(peripherals.containsKey(peripheralId) && 
						peripherals.get(peripheralId).services.containsKey(serviceId) && 
						peripherals.get(peripheralId).services.get(serviceId).characteristics.containsKey(characteristicId))
				{
					BluetoothGatt gatt = peripherals.get(peripheralId).gatt;
					BluetoothGattCharacteristic characteristic = peripherals.get(peripheralId).services.get(serviceId).characteristics.get(characteristicId).characteristic;
					
					GattComm gattComm = new GattComm(gatt, characteristic, GattComm.WRITE_CHARACTERISTIC, data);
					
					
					if(withResponse)
						gattComm.setWriteType(BluetoothGattCharacteristic.WRITE_TYPE_DEFAULT);
					else
						gattComm.setWriteType(BluetoothGattCharacteristic.WRITE_TYPE_NO_RESPONSE);

					logMe(TAG, "writeCharacteristicWithIdentifiers() :  Adding Packet to Queue...");


					processGattComm(gattComm);
					
				}else{
					logMe(TAG, "writeCharacteristicWithIdentifiers() :  Peripherial Not In Dictionary 1 ... " + peripheralId  + ", " + serviceId + ", " + characteristicId);
					//logMe(TAG, "writeCharacteristicWithIdentifiers() :  Peripherial Not In Dictionary 2... " + peripherals.containsKey(peripheralId)  + ", " + peripherals.get(peripheralId).services.containsKey(serviceId) + ", " + peripherals.get(peripheralId).services.get(serviceId).characteristics.containsKey(characteristicId));

				}
			}else{
				logMe(TAG, "writeCharacteristicWithIdentifiers() :  Unknown Peripherial Check IDs... ");


			}
		} else {
			logMe(TAG, "writeCharacteristicWithIdentifiers() :  Bluetooth Adapter Disabled...");

		}
	}
	
	
	
	public void writeDescriptorWithIdentifiers(String peripheralId, String serviceId, String characteristicId, String descriptorId, byte[] data, int length) {
		if (mBluetoothAdapter != null && mBluetoothAdapter.isEnabled()) {
			logMe(TAG, "writeDescriptorWithIdentifiers()");

			if(peripheralId != null && serviceId != null && characteristicId != null)
			{
				if(peripherals.containsKey(peripheralId) && 
						peripherals.get(peripheralId).services.containsKey(serviceId) && 
						peripherals.get(peripheralId).services.get(serviceId).characteristics.containsKey(characteristicId))
				{
					BluetoothGatt gatt = peripherals.get(peripheralId).gatt;
					BluetoothGattDescriptor descriptor = peripherals.get(peripheralId).services.get(serviceId).characteristics.get(characteristicId).descriptors.get(descriptorId);
					
					if(descriptor != null)
					{
						processGattComm(new GattComm(gatt, descriptor, GattComm.WRITE_DESCRIPTOR, data));
					}
					
				}
			}
		} else {
			logMe(TAG, "writeDescriptorWithIdentifiers() :  Bluetooth Adapter Disabled...");

		}
	}

	public void enableDebug(boolean _isDebug) {
		isDebug = _isDebug;
	}

	public void setScanPeriod(int milliseconds){ scanPeriod = milliseconds; }
	
	public void readRssiWithIdentifier(String peripheralId) {
		if (mBluetoothAdapter != null && mBluetoothAdapter.isEnabled()) {
			logMe(TAG, "readRssiWithIdentifier()");

			if(peripheralId != null)
			{
				if(peripherals.containsKey(peripheralId))
				{
					BluetoothPeripheral peripheral = peripherals.get(peripheralId);
					BluetoothGatt gatt = peripheral.gatt;
					
					String message = String.format(l, "%d:%s%d:%s", peripheral.device.getAddress().length(), peripheral.device.getAddress(), (peripheral.rssi+"").length(), peripheral.rssi);

					UnityPlayer.UnitySendMessage(gameObjectName, "OnRssiUpdate", message);
					
					processGattComm(new GattComm(gatt, null, GattComm.READ_RSSI, null));

				}
			}
		} else {
			logMe(TAG, "readRssiWithIdentifier() :  Bluetooth Adapter Disabled...");

		}
	}
}
