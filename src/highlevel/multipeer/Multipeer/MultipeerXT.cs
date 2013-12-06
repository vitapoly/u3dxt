#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using U3DXT.Core;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using U3DXT.iOS.Native.MultipeerConnectivity;
using UnityEngine;
using U3DXT.Utils;
using U3DXT.iOS.Multipeer.Helpers;


namespace U3DXT.iOS.Multipeer {
	/// <summary>
	/// Main class for iOS Multipeer plugin. High-level API.</summary>
	/// <remarks>
	/// </remarks>
	public class MultipeerXT {

#region events
		
		private static EventHandler<EventArgs> _browserCompletedHandlers;
		/// <summary>
		/// Occurs when a share operation has completed.
		/// </summary>
		public static event EventHandler<EventArgs> BrowserCompleted {
			add { _browserCompletedHandlers += value; }
			remove { _browserCompletedHandlers -= value; }
		}		

		private static EventHandler<EventArgs> _browserCancelledHandlers;
		/// <summary>
		/// Occurs when a share operation has completed.
		/// </summary>
		public static event EventHandler<EventArgs> BrowserCancelled {
			add { _browserCancelledHandlers += value; }
			remove { _browserCancelledHandlers -= value; }
		}		

		
		private static EventHandler<EventArgs> _advertiserDismissedInvitationHandlers;
		/// <summary>
		/// Occurs when a share operation has completed.
		/// </summary>
		public static event EventHandler<EventArgs> AdvertiserDismissedInvitation {
			add { _advertiserDismissedInvitationHandlers += value; }
			remove { _advertiserDismissedInvitationHandlers -= value; }
		}		

		private static EventHandler<EventArgs> _advertiserWillPresentInvitationHandlers;
		/// <summary>
		/// Occurs when a share operation has completed.
		/// </summary>
		public static event EventHandler<EventArgs> AdvertiserWillPresentInvitation {
			add { _advertiserWillPresentInvitationHandlers += value; }
			remove { _advertiserWillPresentInvitationHandlers -= value; }
		}		


		private static EventHandler<SessionChangedEventArgs> _sessionChangedHandlers;
		/// <summary>
		/// Occurs when a share operation has completed.
		/// </summary>
		public static event EventHandler<SessionChangedEventArgs> SessionChanged {
			add { _sessionChangedHandlers += value; }
			remove { _sessionChangedHandlers -= value; }
		}		

		private static EventHandler<SessionFinishedReceivingResourceWithNameEventArgs> _sessionFinishedReceivingResourceWithNameHandlers;
		/// <summary>
		/// Occurs when a share operation has completed.
		/// </summary>
		public static event EventHandler<SessionFinishedReceivingResourceWithNameEventArgs> SessionFinishedReceivingResourceWithName {
			add { _sessionFinishedReceivingResourceWithNameHandlers += value; }
			remove { _sessionFinishedReceivingResourceWithNameHandlers -= value; }
		}		

		private static EventHandler<SessionReceivedEventArgs> _sessionReceivedHandlers;
		/// <summary>
		/// Occurs when a share operation has completed.
		/// </summary>
		public static event EventHandler<SessionReceivedEventArgs> SessionReceived {
			add { _sessionReceivedHandlers += value; }
			remove { _sessionReceivedHandlers -= value; }
		}		

		private static EventHandler<SessionStartedReceivingResourceWithNameEventArgs> _sessionStartedReceivingResourceWithNameHandlers;
		/// <summary>
		/// Occurs when a share operation has completed.
		/// </summary>
		public static event EventHandler<SessionStartedReceivingResourceWithNameEventArgs> SessionStartedReceivingResourceWithName {
			add { _sessionStartedReceivingResourceWithNameHandlers += value; }
			remove { _sessionStartedReceivingResourceWithNameHandlers -= value; }
		}		
	
#endregion

#region vars
		private static MCPeerID _peerId;
		private static MCSession _session;
		private static MCBrowserViewController _browserVC;
		private static MCAdvertiserAssistant _advertiserAssistant;
#endregion

#region methods
		
		/// <summary>
		/// Shows the default iOS7.0 Browser view that enables users to invite nearby devices to a session.
		/// </summary>
		/// <param name='displayName'>
		/// The display name for the local peer
		/// </param>
		/// <param name='serviceType'>
		/// The type of service to browse for. Must be 1-15 characters long, only ASCII lower case characters.
		/// </param>
		public static void ShowBrowser(string displayName, string serviceType)
		{
			_peerId = new MCPeerID(displayName);
			_session = new MCSession(_peerId);
			_browserVC = new MCBrowserViewController(serviceType, _session);
			_browserVC.Delegate = BrowserViewControllerDelegate.instance;
			_session.Delegate = SessionDelegate.instance;

				
			UIApplication.deviceRootViewController.PresentViewController(_browserVC, false, null);
		}
		
		/// <summary>
		/// Starts the advertiser assistant.
		/// </summary>
		/// <param name='displayName'>
		/// The display name for the local peer
		/// </param>
		/// <param name='serviceType'>
		/// The type of service to advertise. Must be 1-15 characters long, only ASCII lower case characters.
		/// </param>
		public static void StartAdvertiserAssistant(string displayName, string serviceType)
		{
			_peerId = new MCPeerID(displayName);
			_session = new MCSession(_peerId);
			_advertiserAssistant = new MCAdvertiserAssistant(serviceType, null, _session);
			_advertiserAssistant.Start();

			_session.Delegate = SessionDelegate.instance;
			_advertiserAssistant.Delegate = AdvertiserAssistantDelegate.instance;


		}



		internal static void OnBrowserCancelled()
		{
			if (_browserCancelledHandlers != null)
				_browserCancelledHandlers(null, EventArgs.Empty);
		}
		
		internal static void OnBrowserCompleted()
		{
			if (_browserCompletedHandlers != null)
				_browserCompletedHandlers(null, EventArgs.Empty);
		}

		internal static void OnAdvertiserWillPresentInvitation()
		{
			if (_advertiserWillPresentInvitationHandlers != null)
				_advertiserWillPresentInvitationHandlers(null, EventArgs.Empty);
		}

		internal static void OnAdvertiserDismissedInvitation()
		{
			if (_advertiserDismissedInvitationHandlers != null)
				_advertiserDismissedInvitationHandlers(null, EventArgs.Empty);
		}

		internal static void OnSessionDidChange(MCSession session, MCPeerID peerID, MCSessionState state)
		{
			if (_sessionChangedHandlers != null)
				_sessionChangedHandlers(null, new SessionChangedEventArgs(session, peerID, state));
		}

		internal static void OnSessionDidFinishReceivingResourceWithName(MCSession session, string resourceName, MCPeerID peerID, U3DXT.iOS.Native.Foundation.NSURL localURL, U3DXT.iOS.Native.Foundation.NSError error)
		{
			if (_sessionFinishedReceivingResourceWithNameHandlers != null)
				_sessionFinishedReceivingResourceWithNameHandlers(null, 
				     new SessionFinishedReceivingResourceWithNameEventArgs(session, resourceName, peerID, localURL, error));
		}

		internal static void OnSessionDidRecieve(MCSession session, U3DXT.iOS.Native.Foundation.NSData data, MCPeerID peerID)
		{
			if (_sessionReceivedHandlers != null)
				_sessionReceivedHandlers(null, new SessionReceivedEventArgs(session, data, peerID));
		}

		internal static void OnSessionDidStartReceivingResourceWithName(MCSession session, string resourceName, MCPeerID peerID, U3DXT.iOS.Native.Foundation.NSProgress progress) 
		{
			if (_sessionStartedReceivingResourceWithNameHandlers != null)
				_sessionStartedReceivingResourceWithNameHandlers(null, 
				     new SessionStartedReceivingResourceWithNameEventArgs(session, resourceName, peerID, progress));
		}


			
				
				


		
		
#endregion
	}
}

#endif
