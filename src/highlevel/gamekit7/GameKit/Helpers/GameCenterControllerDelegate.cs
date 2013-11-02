#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.GameKit;
using U3DXT.iOS.Native.UIKit;

namespace U3DXT.iOS.GameKit.Helpers {
	/// <summary>
	/// Game center controller delegate.
	/// </summary>
	internal class GameCenterControllerDelegate : GKGameCenterControllerDelegate {

		private static GameCenterControllerDelegate _instance;
		public static GameCenterControllerDelegate instance {
			get {
				if (_instance == null)
					_instance = new GameCenterControllerDelegate();
				return _instance;
			}
		}

		public GameCenterControllerDelegate() {
		}

		public override void DidFinish(GKGameCenterViewController gameCenterViewController) {
			gameCenterViewController.presentingViewController.DismissViewController(true, null);
		}
	}
}

#endif
