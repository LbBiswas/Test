using System;

using UnityEngine;

using strange.extensions.context.impl;
using strange.extensions.command.api;
using strange.extensions.command.impl;
using strange.extensions.signal.impl;

using startechplus.ble;
using app;

namespace app {
	public class AppContext : MVCSContext {

		/**
         * Constructor
         */
		public AppContext(MonoBehaviour contextView) : base(contextView) {
		}

		protected override void addCoreComponents() {
			base.addCoreComponents();

			// bind signal command binder
			injectionBinder.Unbind<ICommandBinder>();
			injectionBinder.Bind<ICommandBinder>().To<SignalCommandBinder>().ToSingleton();
		}

		public override void Launch() {
			base.Launch();
			Signal startSignal = injectionBinder.GetInstance<StartSignal>();
			startSignal.Dispatch();
		}

		protected override void mapBindings() {
			base.mapBindings();

			// we bind a command to StartSignal since it is invoked by SignalContext (the parent class) on Launch()
			commandBinder.Bind<StartSignal>().To<AppStartCommand>().Once();

			mediationBinder.Bind<ButtonView> ().To<ButtonMediator> ();
			mediationBinder.Bind<SwitchView> ().To<SwitchMediator> ();
			mediationBinder.Bind<IndicatorView> ().To<IndicatorMediator> ();
			mediationBinder.Bind<AppView> ().To<AppMediator> ();
			mediationBinder.Bind<SwipeControlView> ().To<SwipeControlMediator> ();
			mediationBinder.Bind<StatusView> ().To<StatusMediator> ();
			mediationBinder.Bind<SetupView> ().To<SetupMediator> ();
			mediationBinder.Bind<GlobalView> ().To<GlobalMediator> ();
			mediationBinder.Bind<ColorChangeView> ().To<ColorChangeMediator> ();
			mediationBinder.Bind<RecordingCanvasView> ().To<RecordingCanvasMediator> ();
			mediationBinder.Bind<ScrollPickerView> ().To<ScrollPickerMediator> ();
			mediationBinder.Bind<SwitchHDView> ().To<SwitchHDMediator> ();
			mediationBinder.Bind<ProOptionsView> ().To<ProOptionsMediator> ();
			mediationBinder.Bind<ProPurchaseView> ().To<ProPurchaseMediator> ();
			mediationBinder.Bind<ProNameChangeView> ().To<ProNameChangeMediator> ();
			mediationBinder.Bind<EnterPasskeyView>().To<EnterPasskeyMediator>();
			mediationBinder.Bind<ProView> ().To<ProMediator> ();

            //			mediationBinder.Bind<OTAView> ().To<OTAMediator> ();

            if (Application.platform == RuntimePlatform.IPhonePlayer)
                injectionBinder.Bind<IBleBridge>().To<iOSBleBridge>().ToSingleton();
            else if (Application.platform == RuntimePlatform.Android)
                injectionBinder.Bind<IBleBridge>().To<AndroidBleBridge>().ToSingleton();
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
                injectionBinder.Bind<IBleBridge>().To<OsxBleBridge>().ToSingleton();
            else
                injectionBinder.Bind<IBleBridge>().To<DummyBleBridge>().ToSingleton();

            injectionBinder.Bind<IBluetoothLeService>().To<BluetoothLeService>().ToSingleton();

			injectionBinder.Bind<IRoutineRunner>().To<RoutineRunner>().ToSingleton();


			injectionBinder.Bind<BluetoothLeEventSignal>().ToSingleton();
			injectionBinder.Bind<UiSignal>().ToSingleton();
			injectionBinder.Bind<SystemRequestSignal>().ToSingleton();
			injectionBinder.Bind<SystemResponseSignal>().ToSingleton();
			injectionBinder.Bind<Utils> ().ToSingleton ();

		}

	}
}