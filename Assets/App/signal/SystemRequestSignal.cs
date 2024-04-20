using System;
using System.Collections.Generic;
using strange.extensions.signal.impl;

using UnityEngine.EventSystems;


namespace app{
	public class SystemRequestSignal : Signal<string, Dictionary<string, object>>{}
}