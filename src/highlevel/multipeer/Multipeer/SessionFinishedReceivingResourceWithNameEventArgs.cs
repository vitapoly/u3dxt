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
	public class SessionFinishedReceivingResourceWithNameEventArgs : EventArgs {
//		/// <summary>
//		/// The type of the activity.
//		/// See <see cref="UIActivity">Constants in UIActivity</see>.
//		/// </summary>
		//		public readonly string activityType;

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
		/// The local UR.
		/// </summary>
		public readonly U3DXT.iOS.Native.Foundation.NSURL localURL;

		/// <summary>
		/// The error.
		/// </summary>
		public readonly U3DXT.iOS.Native.Foundation.NSError error;

		/// <summary>
		/// Initializes a new instance of the
		/// <see cref="U3DXT.iOS.Multipeer.SessionFinishedReceivingResourceWithNameEventArgs"/> class.
		/// </summary>
		/// <param name="_session">_session.</param>
		/// <param name="_resourceName">_resource name.</param>
		/// <param name="_peerID">_peer I.</param>
		/// <param name="_localURL">_local UR.</param>
		/// <param name="_error">_error.</param>
		public SessionFinishedReceivingResourceWithNameEventArgs(MCSession _session, string _resourceName, MCPeerID _peerID, U3DXT.iOS.Native.Foundation.NSURL _localURL, U3DXT.iOS.Native.Foundation.NSError _error)
		{
			session = _session;
			resourceName = _resourceName;
			peerID = _peerID;
			localURL = _localURL;
			error = _error;

		}
	}
}

