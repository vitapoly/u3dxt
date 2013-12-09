#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT;
using U3DXT.iOS.Native.MultipeerConnectivity;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;

namespace U3DXT.iOS.Multipeer {
	/// <summary>
	/// Provides data for ShareCompleted event.
	/// </summary>
	public class SessionDidReceiveCertificateEventArgs : EventArgs {

		// change this when you get the event args if you want to deny the certificate after inspection
		public bool acceptCertificate;

		/// <summary>
		/// The session.
		/// </summary>
		public readonly MCSession session;

		public readonly object[] certificate;

		/// <summary>
		/// The peer I.
		/// </summary>
		public readonly MCPeerID peerID;

		/// <summary>
		/// The progress.
		/// </summary>
		public readonly U3DXT.iOS.Native.Foundation.NSProgress progress; 

		/// <summary>
		/// Initializes a new instance of the
		/// <see cref="U3DXT.iOS.Multipeer.SessionStartedReceivingResourceWithNameEventArgs"/> class.
		/// </summary>
		/// <param name="_session">_session.</param>
		/// <param name="_resourceName">_resource name.</param>
		/// <param name="_peerID">_peer I.</param>
		/// <param name="_progress">_progress.</param>
		public SessionDidReceiveCertificateEventArgs(MCSession _session, object[] _certificate, MCPeerID _peerID, bool _acceptCertificate=true) 
		{
			acceptCertificate = _acceptCertificate;
			session = _session;
			certificate = _certificate;
			peerID = _peerID;
		}
	}
}

#endif
