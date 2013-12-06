#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.Core;
using U3DXT.iOS.Native.GameKit;
using U3DXT.iOS.Native.UIKit;
using System.Linq;
using U3DXT.iOS.GameKit.Helpers;
using U3DXT.iOS.Internals;
using U3DXT.iOS.Native.Foundation;
using System.Runtime.InteropServices;

namespace U3DXT.iOS.GameKit {

	/// <summary>
	/// This controller class creates and controls real-time Game Center matches.
	/// After subscribing to events, call Init().
	/// Call StartMatch() to start a match. The started match will be stored in the CurrentMatch property.
	/// </summary>
	public class RealTimeMatchesController {

		private static EventHandler<U3DXTErrorEventArgs> _matchMakerFailedHandlers;
		/// <summary>
		/// Occurs when the matchmaker cannot find a match.
		/// </summary>
		public static event EventHandler<U3DXTErrorEventArgs> MatchMakerFailed {
			add { _matchMakerFailedHandlers += value; }
			remove { _matchMakerFailedHandlers -= value; }
		}

		private static EventHandler<EventArgs> _matchMakerCancelledHandlers;
		/// <summary>
		/// Occurs when the user cancels the matchmaking action.
		/// </summary>
		public static event EventHandler<EventArgs> MatchMakerCancelled {
			add { _matchMakerCancelledHandlers += value; }
			remove { _matchMakerCancelledHandlers -= value; }
		}

		private static EventHandler<MatchEventArgs> _matchMakerFoundMatchHandlers;
		/// <summary>
		/// Occurs when the matchmaker finds a match.
		/// </summary>
		public static event EventHandler<MatchEventArgs> MatchMakerFoundMatch {
			add { _matchMakerFoundMatchHandlers += value; }
			remove { _matchMakerFoundMatchHandlers -= value; }
		}

		private static EventHandler<InviteAcceptedEventArgs> _inviteAcceptedHandlers;
		/// <summary>
		/// Occurs when the user accepts an invite from another player.
		/// </summary>
		public static event EventHandler<InviteAcceptedEventArgs> InviteAccepted {
			add { _inviteAcceptedHandlers += value; }
			remove { _inviteAcceptedHandlers -= value; }
		}

		private static EventHandler<PlayersInvitedEventArgs> _playersInvitedHandlers;
		/// <summary>
		/// Occurs when the user invites other players to play this game from Game Center app.
		/// </summary>
		public static event EventHandler<PlayersInvitedEventArgs> PlayersInvited {
			add { _playersInvitedHandlers += value; }
			remove { _playersInvitedHandlers -= value; }
		}

		/// <summary>U3DXT internal.</summary>
		[DllImport ("__Internal")]
		private static extern void UP_GameKitXT_enableVoiceChat();
		private static bool _initVoiceChat = false;


		internal static bool _init = false;
		internal static RealTimeMatch _currentMatch;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.GameKit.RealTimeMatchesController"/> class.
		/// </summary>
		private RealTimeMatchesController() {
		}

		/// <summary>
		/// Initializes the controller. You must call this after subscribing to real-time match events.
		/// </summary>
		public static void Init() {
			if (!_init) {
				if (GKLocalPlayer.InstancesRespondToSelector("registerListener:")) {
					LocalPlayerListener.instance.Register();
				} else {
					GKMatchmaker.SharedMatchmaker().inviteHandler = _MatchInviteHandler;
				}
				_init = true;
			}
		}

		internal static void _MatchInviteHandler(GKInvite acceptedInvite, object[] playersToInvite) {
			if (acceptedInvite != null)
			{
				// show new view controller
				var mmvc = new GKMatchmakerViewController(acceptedInvite);
				mmvc.matchmakerDelegate = MatchmakerViewControllerDelegate.instance;

				UIApplication.deviceRootViewController.PresentViewController(mmvc, true, null);

				// load players and dispatch event
				Player.LoadPlayersByIDs(new string[] {acceptedInvite.inviter}, delegate(Player[] players) {
					if (players.Length > 0) {
						if (_inviteAcceptedHandlers != null)
							_inviteAcceptedHandlers(null, new InviteAcceptedEventArgs(players[0], acceptedInvite.playerGroup, acceptedInvite.playerAttributes));
					}

					acceptedInvite = null;
				});

				mmvc = null;
			}
			else if (playersToInvite != null)
			{
				// load players and dispatch event
				Player.LoadPlayersByIDs(playersToInvite.Cast<string>().ToArray(), delegate(Player[] players) {
					if (players.Length > 0) {
						if (_playersInvitedHandlers != null)
							_playersInvitedHandlers(null, new PlayersInvitedEventArgs(players));
					}
				});
			}
		}

		/// <summary>
		/// U3DXT internal.
		/// </summary>
		/// <returns>The current match.</returns>
		/// <param name="">.</param>
		public static void SetCurrentMatch(GKMatch gkMatch) {

			if (gkMatch == null) {
				_currentMatch = null;
				return;
			}

			// resolve to wrapper and then dispatch event
			var rtmatch = NSObjectWrapper.CreateWrapper(typeof(RealTimeMatch), gkMatch) as RealTimeMatch;
			gkMatch = null;

			rtmatch.ReloadPlayers(delegate() {
				_currentMatch = rtmatch;
				if (_matchMakerFoundMatchHandlers != null)
					_matchMakerFoundMatchHandlers(null, new MatchEventArgs(rtmatch));
				rtmatch = null;
			});
		}

		/// <summary>
		/// The current real-time match, or null if none.
		/// </summary>
		/// <value>The current match.</value>
		public static RealTimeMatch currentMatch {
			get { return _currentMatch; }
		}

		/// <summary>
		/// Brings up the match making interface to start a real-time match with other players.
		/// Raises MatchMakerFoundMatch, MatchMakerCancelled, and MatchMakerFailed events.
		/// </summary>
		/// <param name="minPlayers">The minimum nubmer of players that can join a match; between 2 and 4 inclusively.</param>
		/// <param name="maxPlayers">The maximum number of players that can join a match; between 2 and 4 inclusively.</param>
		/// <param name="playerGroup">The group this player belongs to such as skill level; Game Center will match players with the same playerGroup.</param>
		/// <param name="playerAttributes">The attributes of this player such as white or black pieces in chest; Game Center will try to match players so that all bits of this attribute are filled by all players of a game.</param>
		/// <param name="playersToInvite">An array of Player instances; this is passed in from the PlayersInvited event.</param>
		public static void StartMatch(uint minPlayers, uint maxPlayers, uint playerGroup = 0, uint playerAttributes = 0, Player[] playersToInvite = null) {
			if ((minPlayers < 2) || (minPlayers > 4) || (maxPlayers < 2) || (maxPlayers > 4) || (maxPlayers < minPlayers))
				throw new U3DXTException("minPlayers and maxPlayers must be between 2 and 4.");

			_currentMatch = null;

			// create request
			var request = new GKMatchRequest();
			request.minPlayers = minPlayers;
			request.maxPlayers = maxPlayers;
			request.playerGroup = playerGroup;
			request.playerAttributes = playerAttributes;
			if (playersToInvite != null)
				request.playersToInvite = Player.PlayersToIDs(playersToInvite);

			// create view controller
			var mmvc = new GKMatchmakerViewController(request);

			// set delegate
			mmvc.matchmakerDelegate = MatchmakerViewControllerDelegate.instance;

			// show it
			UIApplication.deviceRootViewController.PresentViewController(mmvc, true, null);
		}

		internal static void _OnMatchMakerFailed(NSError error) {
			_currentMatch = null;

			if (_matchMakerFailedHandlers != null)
				_matchMakerFailedHandlers(null, new U3DXTErrorEventArgs(error));
		}

		internal static void _OnMatchMakerCancelled() {
			_currentMatch = null;

			if (_matchMakerCancelledHandlers != null)
				_matchMakerCancelledHandlers(null, EventArgs.Empty);
		}

		
		/// <summary>
		/// Enables voice chat.
		/// </summary>
		public static void EnableVoiceChat() {
			if (_initVoiceChat)
				return;

			UP_GameKitXT_enableVoiceChat();
			_initVoiceChat = true;
		}
	}
}

#endif
