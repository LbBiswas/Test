using UnityEngine;


public class SwitchStatus {

	public bool isDirty = true;
	public int id = 0;
	public float value = 1.0f;
	public bool isOn = false;
	public bool canStrobe = false;
	public bool canFlash = false;
	public bool isDimmable = false;
	public bool isMomentary = false;
	public float red = 1.0f;
	public float green = 1.0f;
	public float blue = 1.0f;
	public bool isLegend = false;
	public int legendId = 255;
	public string label1 = "";
	public string label2 = "Switch 1";
	public string label3 = "";
	public bool isFlashing = false;
	public bool wasFlashing = false;
	public bool proIsAutoOn = false;
	public bool proIsIgnCtrl = false;
	public bool proIsLockout = false;
	public int proOnTimer = 0;
	public int proStrobeOn = 1;
	public int proStrobeOff = 4;
	public bool proIsInputLatch = true;
	public bool proIsInputEnabled = true;
	public bool proIsInputLockout = false;
	public bool proIsInputLockInvert = false;
	public bool proIsCurrentRestart = false;
	public int proCurrentLimit = 30;
	public Sprite sprite = Sprite.Create(null, new Rect(0.0f,0.0f,0.0f,0.0f), new Vector2(0.5f, 0.5f));//new Sprite();

	public SwitchStatus(int num = 0)
	{
		if (num >= 0 && num < 32)
		{
			id = num;
			label2 = "Switch " + (num + 1);
		}
	}
}
