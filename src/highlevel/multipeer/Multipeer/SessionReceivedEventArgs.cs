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
	public class SessionReceivedEventArgs : EventArgs {

		/// <summary>
		/// The session.
		/// </summary>
		public readonly MCSession session;

		/// <summary>
		/// The data.
		/// </summary>
		public readonly U3DXT.iOS.Native.Foundation.NSData data;

		/// <summary>
		/// The peer I.
		/// </summary>
		public readonly MCPeerID peerID;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.Multipeer.SessionReceivedEventArgs"/> class.
		/// </summary>
		/// <param name="_session">_session.</param>
		/// <param name="_data">_data.</param>
		/// <param name="_peerID">_peer I.</param>
		public SessionReceivedEventArgs(MCSession _session, U3DXT.iOS.Native.Foundation.NSData _data, MCPeerID _peerID)
		{
			session = _session;
			data = _data;
			peerID = _peerID;
		}
	}
}

#endif
