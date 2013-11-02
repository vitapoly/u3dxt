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
	public class SessionStartedReceivingResourceWithNameEventArgs : EventArgs {

		/// <summary>
		/// The session.
		/// </summary>
		public readonly MCSession session;

		/// <summary>
		/// The name of the resource.
		/// </summary>
		public readonly string resourceName;

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
		public SessionStartedReceivingResourceWithNameEventArgs(MCSession _session, string _resourceName, MCPeerID _peerID, U3DXT.iOS.Native.Foundation.NSProgress _progress) 
		{
			session = _session;
			resourceName = _resourceName;
			peerID = _peerID;
			progress = _progress;
		}
	}
}

#endif
