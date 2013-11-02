#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.StoreKit;

namespace U3DXT.iOS.IAP.Helpers {
	internal class StoreProductViewControllerDelegate : SKStoreProductViewControllerDelegate {

		private static StoreProductViewControllerDelegate _instance;
		public static StoreProductViewControllerDelegate instance {
			get {
				if (_instance == null)
					_instance = new StoreProductViewControllerDelegate();
				return _instance;
			}
		}

		public StoreProductViewControllerDelegate() {
		}

		public override void DidFinish(SKStoreProductViewController viewController) {
			viewController.presentingViewController.DismissViewController(true, null);
		}
	}
}

#endif
