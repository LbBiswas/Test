using UnityEngine;
using System.Runtime.InteropServices;

public class Bugfender : MonoBehaviour {

	public string APP_KEY;

    public enum LogLevel { Debug, Warning, Error };

#if UNITY_ANDROID && !UNITY_EDITOR
	private static AndroidJavaClass bugfender;
#elif UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void BugfenderActivateLogger(string key);
      
    [DllImport ("__Internal")]
    private static extern void BugfenderEnableCrashReporting();
  
    [DllImport ("__Internal")]
    private static extern void BugfenderSetDeviceString(string key, string value);
  
    [DllImport ("__Internal")]
    private static extern void BugfenderRemoveDeviceKey(string key);
  
    [DllImport ("__Internal")]
    private static extern void BugfenderLog(int logLevel, string tag, string message);
  
    [DllImport ("__Internal")]
    private static extern void BugfenderSendIssue(string title, string markdown);
#endif

    // Automatically called when scene starts
    void Start()
    {
        Debug.Log("[BF] *** INITIALIZING BUGFENDER ***");
#if UNITY_ANDROID && !UNITY_EDITOR
		if (bugfender == null) {
			using (AndroidJavaClass activityClass = new AndroidJavaClass ("com.unity3d.player.UnityPlayer")) {
				var activityContext = activityClass.GetStatic<AndroidJavaObject> ("currentActivity");

				bugfender = new AndroidJavaClass ("com.bugfender.sdk.Bugfender");
				if (bugfender != null) {
					bugfender.CallStatic ("init", activityContext, APP_KEY, false);
                    //bugfender.CallStatic ("enableLogcatLogging"); // optional, uncomment if you want it (Android only)
				}
			}
		}
#elif UNITY_IOS && !UNITY_EDITOR
        BugfenderActivateLogger(APP_KEY);
        BugfenderEnableCrashReporting();
#endif
        Bugfender.Log("BF Initialized");
        Bugfender.SetDeviceString("key","value");
        Bugfender.SetDeviceString("key2", "will remove");
        Bugfender.RemoveDeviceKey("key2");
        Bugfender.SendIssue("test", "this is a test");
    }

    public static void SetDeviceString(string key, string value)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bugfender != null) {
            bugfender.CallStatic ("setDeviceString", key, value);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        BugfenderSetDeviceString(key, value);
#else
        Debug.Log("[BF] Set device key:" + key + " value:" + value);
#endif
    }

    public static void RemoveDeviceKey(string key)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bugfender != null) {
            bugfender.CallStatic ("removeDeviceKey", key);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        BugfenderRemoveDeviceKey(key);
#else
        Debug.Log("[BF] Remove device key: " + key);
#endif
    }

    public static void Log(string message)
    {
        Log(LogLevel.Debug, "", message);
    }

    public static void Log(LogLevel logLevel, string tag, string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bugfender != null) {
        AndroidJavaClass levelClass = new AndroidJavaClass ("com.bugfender.sdk.LogLevel");
            AndroidJavaObject level = levelClass.GetStatic<AndroidJavaObject>(logLevel.ToString());
            bugfender.CallStatic ("log", 0, "", "", level, tag, message);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        //  Log levels: Default = 0, Warning = 1, Error = 2,
        int intLevel = (int)logLevel;
        BugfenderLog(intLevel, tag, message);
#else
        Debug.Log("[BF] Sending log to Bugfender: [" + logLevel + "][" + tag + "] " + message);
#endif
    }

    public static void SendIssue(string title, string markdown) {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bugfender != null) {
            bugfender.CallStatic ("sendIssue", title, markdown);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        BugfenderSendIssue(title, markdown);
#else
        Debug.Log("[BF] Send issue: " + title + " : " + markdown);
#endif
    }
}