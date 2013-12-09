#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Multipeer;
using U3DXT.iOS.Native.MultipeerConnectivity;
using UnityEngine;

namespace U3DXT.iOS.Multipeer.Helpers
{
	internal class SessionDelegate : MCSessionDelegate
	{
		private static SessionDelegate _instance;
		public static SessionDelegate instance {
			get {
				if (_instance == null)
					_instance = new SessionDelegate();
				return _instance;
			}
		}

		private SessionDelegate()
		{
		}

		public override void DidChange(MCSession session, MCPeerID peerID, MCSessionState state) {
			MultipeerXT.OnSessionDidChange(session, peerID, state);
		
		}

		public override void DidFinishReceivingResourceWithName(MCSession session, string resourceName, MCPeerID peerID, U3DXT.iOS.Native.Foundation.NSURL localURL, U3DXT.iOS.Native.Foundation.NSError error) {
			MultipeerXT.OnSessionDidFinishReceivingResourceWithName(session, resourceName, peerID, localURL, error);
		}

		public override void DidReceive(MCSession session, U3DXT.iOS.Native.Foundation.NSData data, MCPeerID peerID) {
			MultipeerXT.OnSessionDidRecieve(session, data, peerID);
		}


		public override void DidStartReceivingResourceWithName(MCSession session, string resourceName, MCPeerID peerID, U3DXT.iOS.Native.Foundation.NSProgress progress) {
			MultipeerXT.OnSessionDidStartReceivingResourceWithName(session, resourceName, peerID, progress);
		}

		/*
		 * always return certicate to be accept
		 */
		public override bool DidReceiveCertificate(MCSession session, object[] certificate, MCPeerID peerID) {
			return MultipeerXT.OnSessionDidReceiveCertificate(session, certificate, peerID);
		}


	}
}

#endif
