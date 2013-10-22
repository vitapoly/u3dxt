using System;
using U3DXT.iOS.Multipeer;
using U3DXT.iOS.Native.MultipeerConnectivity;
using UnityEngine;

namespace U3DXT.iOS.Multipeer.Helpers
{
	internal class AdvertiserAssistantDelegate : MCAdvertiserAssistantDelegate
	{
		private static AdvertiserAssistantDelegate _instance;
		public static AdvertiserAssistantDelegate instance {
			get {
				if (_instance == null)
					_instance = new AdvertiserAssistantDelegate();
				return _instance;
			}
		}

		private AdvertiserAssistantDelegate()
		{
		}


		public override void DidDismissInvitation(MCAdvertiserAssistant advertiserAssistant) {
			MultipeerXT.OnAdvertiserDismissedInvitation();
		}

		public override void AssitantWillPresentInvitation(MCAdvertiserAssistant advertiserAssistant) {
			MultipeerXT.OnAdvertiserWillPresentInvitation();
		}


	}
}
