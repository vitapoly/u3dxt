#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.GameKit;

namespace U3DXT.iOS.GameKit.Helpers {
	internal class AchievementViewControllerDelegate : GKAchievementViewControllerDelegate {

		private static AchievementViewControllerDelegate _instance;
		public static AchievementViewControllerDelegate instance {
			get {
				if (_instance == null)
					_instance = new AchievementViewControllerDelegate();
				return _instance;
			}
		}

		public AchievementViewControllerDelegate() {
		}

		public override void DidFinish(GKAchievementViewController viewController) {
			viewController.presentingViewController.DismissViewController(true, null);
		}
	}
}

#endif
