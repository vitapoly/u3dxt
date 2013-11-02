#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.GameKit;
using U3DXT.iOS.Native.Foundation;
using U3DXT.iOS.Native.UIKit;

namespace U3DXT.iOS.GameKit.Helpers {
	internal class MatchmakerViewControllerDelegate : GKMatchmakerViewControllerDelegate {

		private static MatchmakerViewControllerDelegate _instance;
		public static MatchmakerViewControllerDelegate instance {
			get {
				if (_instance == null)
					_instance = new MatchmakerViewControllerDelegate();
				return _instance;
			}
		}

		public MatchmakerViewControllerDelegate() {
		}

		public override void DidFail(GKMatchmakerViewController viewController, NSError error) {
			UIApplication.SharedApplication().keyWindow.rootViewController.DismissViewController(true, null);

			RealTimeMatchesController._OnMatchMakerFailed(error);
		}

		public override void WasCancelled(GKMatchmakerViewController viewController) {
			UIApplication.SharedApplication().keyWindow.rootViewController.DismissViewController(true, null);

			RealTimeMatchesController._OnMatchMakerCancelled();
		}

		public override void DidFindMatch(GKMatchmakerViewController viewController, GKMatch match) {
			UIApplication.SharedApplication().keyWindow.rootViewController.DismissViewController(true, null);

			RealTimeMatchesController.SetCurrentMatch(match);
		}
	}
}

#endif
