// filename BuildPostProcessor.cs
// put it in a folder Assets/Editor/
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public class BuildPostProcessor {

	[PostProcessBuild]
	public static void ChangeXcodePlist(BuildTarget buildTarget, string path) {
		
		const string UIApplicationExitsOnSuspend = "UIApplicationExitsOnSuspend";
		const string UIRequiredDeviceCapabilities = "UIRequiredDeviceCapabilities";
		//		Debug.Log("Build Target: " + buildTarget);
		//		Debug.Log("Path: " + path);

		if (buildTarget == BuildTarget.iOS)
		{
			string plistPath = path + "/Info.plist";
			PlistDocument plist = new PlistDocument();
			plist.ReadFromFile(plistPath);

			PlistElementDict root = plist.root;
			var rootDic = root.values;

			if (rootDic.ContainsKey (UIApplicationExitsOnSuspend)) {
				rootDic.Remove (UIApplicationExitsOnSuspend);
			}

			if (rootDic.ContainsKey(UIRequiredDeviceCapabilities)){

				List<PlistElement> reqs = plist.root.values[UIRequiredDeviceCapabilities].AsArray().values;

                //foreach (PlistElement r in reqs)
                //{
                //	if(r.AsString() == "metal")
                //                {
                //		reqs.Remove(r);
                //	}
                //}

                for (int i = reqs.Count - 1; i >= 0; i--)
                {
                    if (reqs[i].AsString() != "armv7")
                    {
                        reqs.RemoveAt(i);
                    }
                }

                //rootDic.Remove(UIRequiredDeviceCapabilities);
            }


			plist.WriteToFile(plistPath);
		}
	}


}