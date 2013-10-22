using System;
using U3DXT.iOS.Native.MessageUI;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;

namespace U3DXT.iOS.Social.Helpers
{
	internal class MessageComposeViewControllerDelegate : MFMessageComposeViewControllerDelegate
	{
		private static MessageComposeViewControllerDelegate _instance;
		public static MessageComposeViewControllerDelegate instance {
			get {
				if (_instance == null)
					_instance = new MessageComposeViewControllerDelegate();
				return _instance;
			}
		}

		private MessageComposeViewControllerDelegate()
		{
		}
		
		public override void DidFinish(MFMessageComposeViewController controller, MessageComposeResult result)
		{
			controller.DismissViewController(true, null);
			SocialXT.OnSMSCompleted(result);
		}
	}
}
