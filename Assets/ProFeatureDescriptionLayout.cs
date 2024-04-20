using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProFeatureDescriptionLayout : MonoBehaviour {

	public GameObject LastEntry;

	public int LineNum0 = 1;
	public int LineNum1 = 1;
	public int LineNum2 = 5;

	public bool isFirst = false;


	public float nextOffset {get {return location - totalHeight - 20.0f;}}


	float lineHeight0 = 20;
	float lineHeight1 = 20;
	float lineHeight2 = 18;

	GameObject DisciptionObj0;

	float location;
	float totalHeight;

	// Use this for initialization
	void Start () {
		
		if (!isFirst) {	
			
			ProFeatureDescriptionLayout lastPos = LastEntry.GetComponent<ProFeatureDescriptionLayout> ();

			if (lastPos != null) {
				
				Debug.Log ("last pos" + transform.localPosition.y + "/" + lastPos.nextOffset);

				transform.localPosition = new Vector2 (transform.localPosition.x, lastPos.nextOffset);
			}
		}

		location = transform.localPosition.y;
		totalHeight = LineNum0 * lineHeight0 + LineNum1 * lineHeight1 + LineNum2 * lineHeight2;

//		Debug.Log ("position: " + location);

		RectTransform DisciptionRect0 = GetComponent<RectTransform> ();

		if (DisciptionRect0.childCount != 2) 
			return;

		float height0 = lineHeight0 * LineNum0;
		float height1 = lineHeight1 * LineNum1;
		float height2 = lineHeight2 * LineNum2;

//		Debug.Log ("heights: " + height0 + "/" + height1 + "/" + height2);


		RectTransform DisciptionRect1 = DisciptionRect0.GetChild (0).GetComponent<RectTransform> ();
		RectTransform DisciptionRect2 = DisciptionRect0.GetChild (1).GetComponent<RectTransform> ();


//		Debug.Log ("offsets: " + DisciptionRect0.offsetMin.y + "/" + DisciptionRect0.offsetMax.y + "/" +
//			DisciptionRect1.offsetMin.y + "/" + DisciptionRect1.offsetMax.y + "/"  +
//			DisciptionRect2.offsetMin.y + "/" + DisciptionRect2.offsetMax.y);

		DisciptionRect0.offsetMin = new Vector2 (DisciptionRect0.offsetMin.x, location - height0 / 2);
		DisciptionRect0.offsetMax = new Vector2 (DisciptionRect0.offsetMax.x, location + height0 / 2);

		DisciptionRect1.offsetMin = new Vector2 (DisciptionRect1.offsetMin.x, 0 - height0 - height1);
		DisciptionRect1.offsetMax = new Vector2 (DisciptionRect1.offsetMax.x, 0 - height0 - 0);
	
		DisciptionRect2.offsetMin = new Vector2 (DisciptionRect2.offsetMin.x, 0 - height0 - height1 - height2);
		DisciptionRect2.offsetMax = new Vector2 (DisciptionRect2.offsetMax.x, 0 - height0 - height1 - 0);


	}
	
	// Update is called once per frame
//	void Update () {
//		
//	}
}
