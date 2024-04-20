using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections;
using UnityEditor.iOS.Xcode;
using System.IO;

public class AddBleUsageDescription {

	[PostProcessBuild]
	public static void ChangeXcodePlist(BuildTarget buildTarget, string pathToBuiltProject) {

		if (buildTarget == BuildTarget.iOS) {

			// Add dependencies
			Debug.Log("uBleBridge Unity: Adding NSBluetoothPeripheralUsageDescription, and CoreBluetooth Framework");

			// Get plist
			string plistPath = pathToBuiltProject + "/Info.plist";
			PlistDocument plist = new PlistDocument();
			plist.ReadFromString(File.ReadAllText(plistPath));

			// Get root
			PlistElementDict rootDict = plist.root;

			// Change value of CFBundleVersion in Xcode plist
			var buildKey = "NSBluetoothPeripheralUsageDescription";
			rootDict.SetString(buildKey,"Bluetooth will be used to connect to a device.");

			var buildKey2 = "NSBluetoothAlwaysUsageDescription";
			rootDict.SetString(buildKey2,"Bluetooth will be used to connect to a device.");

			// Write to file
			File.WriteAllText(plistPath, plist.WriteToString());

			// Get target for Xcode project
			string projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);

			PBXProject proj = new PBXProject();
			proj.ReadFromString(File.ReadAllText(projPath));

			//string targetName = PBXProject.GetUnityTargetName();
			//string projectTarget = proj.TargetGuidByName(targetName);

			string projectTarget = proj.GetUnityMainTargetGuid();

			// Add dependencies
			proj.AddFrameworkToProject(projectTarget, "CoreBluetooth.framework", true);

			File.WriteAllText(projPath, proj.WriteToString());
		}
	}
}
