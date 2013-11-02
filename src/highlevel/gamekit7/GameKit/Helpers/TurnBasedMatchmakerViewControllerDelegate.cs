#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.GameKit;
using U3DXT.iOS.Native.Foundation;
using U3DXT.iOS.Native.UIKit;

namespace U3DXT.iOS.GameKit.Helpers {
	internal class TurnBasedMatchmakerViewControllerDelegate : GKTurnBasedMatchmakerViewControllerDelegate {
		private static TurnBasedMatchmakerViewControllerDelegate _instance;
		public static TurnBasedMatchmakerViewControllerDelegate instance {
			get {
				if (_instance == null)
					_instance = new TurnBasedMatchmakerViewControllerDelegate();
				return _instance;
			}
		}

		public TurnBasedMatchmakerViewControllerDelegate() {
		}

		public override void DidFail(GKTurnBasedMatchmakerViewController viewController, NSError error) {
			UIApplication.SharedApplication().keyWindow.rootViewController.DismissViewController(true, null);

			TurnBasedMatchesController._OnMatchMakerFailed(error);
		}

		public override void WasCancelled(GKTurnBasedMatchmakerViewController viewController) {
			UIApplication.SharedApplication().keyWindow.rootViewController.DismissViewController(true, null);

			TurnBasedMatchesController._OnMatchMakerCancelled();
		}

		public override void DidFind(GKTurnBasedMatchmakerViewController viewController, GKTurnBasedMatch match) {
			UIApplication.SharedApplication().keyWindow.rootViewController.DismissViewController(true, null);

			TurnBasedMatchesController._OnMatchMakerFoundMatch(match);
		}

		public override void PlayerQuit(GKTurnBasedMatchmakerViewController viewController, GKTurnBasedMatch match) {
			TurnBasedMatchesController._OnPlayerQuit(match);
		}
	}
}

#endif
