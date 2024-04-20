using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ES2UserType_SwitchStatus : ES2Type
{
	public override void Write(object obj, ES2Writer writer)
	{
		SwitchStatus data = (SwitchStatus)obj;
		// Add your writer.Write calls here.
		writer.Write(data.id);
		writer.Write(data.value);
		writer.Write(data.isOn);
		writer.Write(data.canStrobe);
		writer.Write(data.canFlash);
		writer.Write(data.isDimmable);
		writer.Write(data.isMomentary);
		writer.Write(data.red);
		writer.Write(data.green);
		writer.Write(data.blue);
		writer.Write(data.isLegend);
		writer.Write(data.legendId);
		writer.Write(data.label1);
		writer.Write(data.label2);
		writer.Write(data.label3);
		writer.Write(data.proIsAutoOn);
		writer.Write(data.proIsIgnCtrl);
		writer.Write(data.proIsLockout);
		writer.Write(data.proOnTimer);
		writer.Write(data.proStrobeOn);
		writer.Write(data.proStrobeOff);
		writer.Write(data.proIsInputLatch);
		writer.Write(data.proIsInputEnabled);
		writer.Write(data.proIsInputLockout);
		writer.Write(data.proIsInputLockInvert);
		writer.Write(data.proIsCurrentRestart);
		writer.Write(data.proCurrentLimit);
		writer.Write (data.sprite);

	}

	public override object Read(ES2Reader reader)
	{
		SwitchStatus data = new SwitchStatus();
		Read(reader, data);
		return data;
	}

	public override void Read(ES2Reader reader, object c)
	{
		SwitchStatus data = (SwitchStatus)c;
		// Add your reader.Read calls here to read the data into the object.
		data.id = reader.Read<System.Int32>();
		data.value = reader.Read<System.Single>();
		data.value = 1.0f;
		data.isOn = reader.Read<System.Boolean>();
		data.isOn = false;
		data.canStrobe = reader.Read<System.Boolean>();
		data.canFlash = reader.Read<System.Boolean>();
		data.isDimmable = reader.Read<System.Boolean>();
		data.isMomentary = reader.Read<System.Boolean>();
		data.red = reader.Read<System.Single>();
		data.green = reader.Read<System.Single>();
		data.blue = reader.Read<System.Single>();
		data.isLegend = reader.Read<System.Boolean>();
		data.legendId = reader.Read<System.Int32>();
		data.label1 = reader.Read<System.String>();
		data.label2 = reader.Read<System.String>();
		data.label3 = reader.Read<System.String>();
		data.proIsAutoOn = reader.Read<System.Boolean>();
		data.proIsIgnCtrl = reader.Read<System.Boolean>();
		data.proIsLockout = reader.Read<System.Boolean>();
		data.proOnTimer = reader.Read<System.Int32>();
		data.proStrobeOn = reader.Read<System.Int32>();
		data.proStrobeOff = reader.Read<System.Int32>();
		data.proIsInputLatch = reader.Read<System.Boolean>();
		data.proIsInputEnabled = reader.Read<System.Boolean>();
		data.proIsInputLockout = reader.Read<System.Boolean>();
		data.proIsInputLockInvert = reader.Read<System.Boolean>();
		data.proIsCurrentRestart = reader.Read<System.Boolean>();
		data.proCurrentLimit = reader.Read<System.Int32>();
		data.sprite = reader.Read<Sprite> ();


	}
	
	/* ! Don't modify anything below this line ! */
	public ES2UserType_SwitchStatus():base(typeof(SwitchStatus)){}
}