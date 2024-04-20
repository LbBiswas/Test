using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScaleSafeArea : MonoBehaviour
{
    public Canvas mainCanvas;
    // Start is called before the first frame update

#if UNITY_ANDROID
    void Start()
    {
        Rect mySafeArea = Screen.safeArea;
        Resolution myRes = Screen.currentResolution;

        Vector2 refRes = mainCanvas.GetComponent<CanvasScaler>().referenceResolution;

        //float scale = mainCanvas.GetComponent<CanvasScaler>().scaleFactor;

        float scaleX = myRes.width/refRes.x;
        float scaleY = myRes.height / refRes.y;

        float side = mySafeArea.x;
        float bottom = mySafeArea.y;
        float top = myRes.height - (mySafeArea.y + mySafeArea.height);
        float side2 = myRes.width - (mySafeArea.x + mySafeArea.width);


        if(side2 > 0 && side2 > side)
        {
            side = side2;
        }

        side = side / scaleX;
        top = top / scaleY;
        bottom = bottom / scaleY;

        Debug.Log("ScaleSafeArea: " + myRes.width + "x" + myRes.height + " - " + mySafeArea.x + "," + mySafeArea.y + " - " + mySafeArea.width + "x" + mySafeArea.height);
        Debug.Log("SafeArea calc: " + side + " " + side2 + " " + bottom + " " + top);

        //this.transform.

        RectTransform myRt = this.GetComponent<RectTransform>();

        // scale?

        myRt.offsetMin = new Vector2(side, bottom); 
        myRt.offsetMax = new Vector2(-1*side, -1*top);
    }

#endif
}
