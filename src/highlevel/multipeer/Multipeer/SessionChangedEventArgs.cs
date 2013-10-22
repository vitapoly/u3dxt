using System;
using U3DXT;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using U3DXT.iOS.Native.MultipeerConnectivity;
using UnityEngine;

namespace U3DXT.iOS.Multipeer {
	/// <summary>
	/// Provides data for ShareCompleted event.
	/// </summary>
	public class SessionChangedEventArgs : EventArgs {

		/// <summary>
		/// The session.
		/// </summary>
		public readonly MCSession session;

		/// <summary>
		/// The peer I.
		/// </summary>
		public readonly MCPeerID peerID;

		/// <summary>
		/// The state.
		/// </summary>
		public readonly MCSessionState state;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.Multipeer.SessionChangedEventArgs"/> class.
		/// </summary>
		/// <param name="_session">_session.</param>
		/// <param name="_peerID">_peer I.</param>
		/// <param name="_state">_state.</param>
		public SessionChangedEventArgs(MCSession _session, MCPeerID _peerID, MCSessionState _state)
		{
			session = _session;
			peerID = _peerID;
			state = _state;

		}
	}
}

