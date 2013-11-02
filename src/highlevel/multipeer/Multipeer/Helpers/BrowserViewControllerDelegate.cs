#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Multipeer;
using U3DXT.iOS.Native.MultipeerConnectivity;
using UnityEngine;

namespace U3DXT.iOS.Multipeer.Helpers
{
	internal class BrowserViewControllerDelegate : MCBrowserViewControllerDelegate
	{
		private static BrowserViewControllerDelegate _instance;
		public static BrowserViewControllerDelegate instance {
			get {
				if (_instance == null)
					_instance = new BrowserViewControllerDelegate();
				return _instance;
			}
		}

		private BrowserViewControllerDelegate()
		{
		}
		
		public override void DidFinish(MCBrowserViewController controller)
		{
			controller.DismissViewController(true, null);
			MultipeerXT.OnBrowserCompleted();
		}

		public override void WasCancelled(MCBrowserViewController controller)
		{
			controller.DismissViewController(true, null);
			MultipeerXT.OnBrowserCancelled();
		}
	}
}

#endif
