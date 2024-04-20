using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace app{
	
	public class Utils {

		public T getValueForKey<T>(Dictionary<string, object> dic, string key)
		{
			object refObj;

			if(key != null && dic.TryGetValue(key, out refObj)){
				if (refObj != null && refObj.GetType ().Equals (typeof(T))) {
					return (T)refObj;
				} else {
					return default(T);
				}
			}

			return default(T);
		}

		public GameObject getAttachedObject(Dictionary<string, object> dic)
		{
			return getValueForKey<GameObject> (dic, "AttachedObject");
		}

		public T getAttachedComponent<T>(Dictionary<string, object> dic)
		{
			GameObject obj = getAttachedObject (dic);
			if (obj != null) {
				return obj.GetComponent<T> ();
			}

			return default(T);
		}

		public int? getIntFromString(string s)
		{
			string sInt = Regex.Replace (s, "[^0-9]+", string.Empty);
			int i = 0;

			if (int.TryParse (sInt, out i)) {
				return i;
			}

			return null;

		}
			

		private GameObject lastAlert = null;

		public void hideAlert()
		{
			if (lastAlert != null) {
				UnityEngine.Object.Destroy (lastAlert);
				lastAlert = null;
			}
		}

		public void showAlert(string title, string message, GameObject go){
			if (lastAlert != null) {
				UnityEngine.Object.Destroy (lastAlert);
				lastAlert = null;
			}

			if (go == null)
				return;

			Canvas[] c = go.GetComponentsInParent<Canvas>();
			Canvas canvas = c[c.Length-1];

			lastAlert = GameObject.Instantiate(Resources.Load("Prefabs/GUI/Alert", typeof(GameObject))) as GameObject;
			AlertView view = lastAlert.GetComponent<AlertView> ();
			view.title.text = title;
			view.message.text = message;
			//lastAlert.transform.parent = canvas.transform;
			lastAlert.transform.SetParent(canvas.transform, false);
			RectTransform rect = lastAlert.GetComponent<RectTransform> ();
			rect.localPosition = Vector3.zero;
		}

	}
}
