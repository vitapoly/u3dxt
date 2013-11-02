#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.MessageUI;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;

namespace U3DXT.iOS.Social.Helpers
{
	internal class MailComposeViewControllerDelegate : MFMailComposeViewControllerDelegate
	{
		private static MailComposeViewControllerDelegate _instance;
		public static MailComposeViewControllerDelegate instance {
			get {
				if (_instance == null)
					_instance = new MailComposeViewControllerDelegate();
				return _instance;
			}
		}

		private MailComposeViewControllerDelegate()
		{
		}
		
		public override void DidFinish(MFMailComposeViewController controller, MFMailComposeResult result, NSError error)
		{
			controller.DismissViewController(true, null);
			SocialXT.OnMailCompleted(result, error);
		}
	}
}

#endif
