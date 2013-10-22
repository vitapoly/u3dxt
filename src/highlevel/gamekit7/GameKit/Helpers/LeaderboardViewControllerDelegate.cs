using System;
using U3DXT.iOS.Native.GameKit;

namespace U3DXT.iOS.GameKit.Helpers {
	internal class LeaderboardViewControllerDelegate : GKLeaderboardViewControllerDelegate {

		private static LeaderboardViewControllerDelegate _instance;
		public static LeaderboardViewControllerDelegate instance {
			get {
				if (_instance == null)
					_instance = new LeaderboardViewControllerDelegate();
				return _instance;
			}
		}

		public LeaderboardViewControllerDelegate() {
		}

		public override void DidFinish(GKLeaderboardViewController viewController) {
			viewController.presentingViewController.DismissViewController(true, null);
		}
	}
}

