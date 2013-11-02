#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.Core;
using U3DXT.iOS.Native.GameKit;
using U3DXT.iOS.Internals;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;
using U3DXT.iOS.GameKit.Helpers;

namespace U3DXT.iOS.GameKit {

	/// <summary>
	/// This controller class creates and controls turn-based Game Center matches.
	/// After subscribing to events, call Init() so events can be sent.
	/// Call StartMatch() to start a match. The started match will be stored in the CurrentMatch property.
	/// </summary>
	public class TurnBasedMatchesController {

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

		private static EventHandler<TurnChangedEventArgs> _turnChangedHandlers;
		/// <summary>
		/// Occurs when it is the turn in a match changes.
		/// </summary>
		public static event EventHandler<TurnChangedEventArgs> TurnChanged {
			add { _turnChangedHandlers += value; }
			remove { _turnChangedHandlers -= value; }
		}

		private static EventHandler<MatchEventArgs> _matchEndedHandlers;
		/// <summary>
		/// Occurs when a match has ended.
		/// </summary>
		public static event EventHandler<MatchEventArgs> MatchEnded {
			add { _matchEndedHandlers += value; }
			remove { _matchEndedHandlers -= value; }
		}

		private static EventHandler<MatchEventArgs> _localPlayerQuitHandlers;
		/// <summary>
		/// Occurs when the local player quits a match from the matchmaking interface.
		/// </summary>
		public static event EventHandler<MatchEventArgs> LocalPlayerQuit {
			add { _localPlayerQuitHandlers += value; }
			remove { _localPlayerQuitHandlers -= value; }
		}

		private static EventHandler<PlayersInvitedEventArgs> _playersInvitedHandlers;
		/// <summary>
		/// Occurs when the user invites other players to play this game from Game Center app.
		/// </summary>
		public static event EventHandler<PlayersInvitedEventArgs> PlayersInvited {
			add { _playersInvitedHandlers += value; }
			remove { _playersInvitedHandlers -= value; }
		}

		internal static bool _init = false;
		internal static TurnBasedMatch _currentMatch;

		private TurnBasedMatchesController() {
		}

		/// <summary>
		/// Initializes the controller. You must call this after subscribing to turn-based match events.
		/// </summary>
		public static void Init() {
			if (!_init) {
				GKTurnBasedEventHandler.SharedTurnBasedEventHandler().Delegate = TurnBasedEventHandlerDelegate.instance;
				_init = true;
			}
		}

		internal static void _OnInvitePlayers(Player[] players) {
			if (_playersInvitedHandlers != null)
				_playersInvitedHandlers(null, new PlayersInvitedEventArgs(players));
		}

		internal static void _OnMatchEnded(GKTurnBasedMatch match) {

			var tbmatch = NSObjectWrapper.CreateWrapper(typeof(TurnBasedMatch), match) as TurnBasedMatch;
			match = null;

			tbmatch.ReloadPlayers(delegate() {
				// set current match if it matches the id
				_CheckSetCurrentMatch(tbmatch);

				if (_matchEndedHandlers != null)
					_matchEndedHandlers(null, new MatchEventArgs(tbmatch));

				tbmatch = null;
			});
		}

		internal static void _OnTurnEvent(GKTurnBasedMatch match, bool didBecomeActive) {

			var tbmatch = NSObjectWrapper.CreateWrapper(typeof(TurnBasedMatch), match) as TurnBasedMatch;
			match = null;

			tbmatch.ReloadPlayers(delegate() {
				// set current match if it matches the id
				if (didBecomeActive)
					SetCurrentMatch(tbmatch);
				else
					_CheckSetCurrentMatch(tbmatch);

				if (_turnChangedHandlers != null)
					_turnChangedHandlers(null, new TurnChangedEventArgs(tbmatch));

				tbmatch = null;
			});
		}

		/// <summary>
		/// The current turn-based match, or null if none.
		/// </summary>
		/// <value>The current match.</value>
		public static TurnBasedMatch currentMatch {
			get { return _currentMatch; }
		}

		/// <summary>
		/// Sets a TurnBasedMatch as the current match.
		/// </summary>
		/// <param name="match">The match to be set as the current match.</param>
		public static void SetCurrentMatch(TurnBasedMatch match) {
			_currentMatch = match;
		}

		/// <summary>
		/// U3DXT internal.
		/// </summary>
		/// <param name="match">Match.</param>
		internal static void _CheckSetCurrentMatch(TurnBasedMatch match) {
			if (match.isCurrentMatch)
				_currentMatch = match;
		}

		/// <summary>
		/// Sets a low-level GKTurnBasedMatch object as the current match.
		/// Some processing needs to happen in the background, and when it is done,
		/// one of the two events: TurnChanged or MatchEnded, will
		/// be raised. At that time, the currentMatch property will be set to a
		/// new high-level TurnBasedMatch object.
		/// <p></p>
		/// Be careful when using this method because it mixes the low-level and high-level API.
		/// </summary>
		/// <param name="match">Match.</param>
		public static void SetNativeMatchAsCurrentMatch(GKTurnBasedMatch match) {
			var tbmatch = NSObjectWrapper.CreateWrapper(typeof(TurnBasedMatch), match) as TurnBasedMatch;
			match = null;

			tbmatch.ReloadPlayers(delegate() {

				SetCurrentMatch(tbmatch);

				if (tbmatch.status == GKTurnBasedMatchStatus.Ended) {
					if (_matchEndedHandlers != null)
						_matchEndedHandlers(null, new MatchEventArgs(tbmatch));
				} else {
					if (_turnChangedHandlers != null)
						_turnChangedHandlers(null, new TurnChangedEventArgs(tbmatch));
				}

				tbmatch = null;
			});
		}

		/// <summary>
		/// Brings up the match making interface to start or join a turn-based match with other players.
		/// Raises TurnChanged, MatchMakerCancelled, and MatchMakerFailed events.
		/// </summary>
		/// <param name="minPlayers">The minimum nubmer of players that can join a match; between 2 and 4 inclusively.</param>
		/// <param name="maxPlayers">The maximum number of players that can join a match; between 2 and 4 inclusively.</param>
		/// <param name="playersToInvite">An array of Player instances; this is passed in from the PlayersInvited event.</param>
		/// <param name="showExistingMatches">If set to <c>true</c> show existing matches.</param>
		public static void StartMatch(uint minPlayers, uint maxPlayers, Player[] playersToInvite = null, bool showExistingMatches = true) {
			if ((minPlayers < 2) || (minPlayers > 16) || (maxPlayers < 2) || (maxPlayers > 16) || (maxPlayers < minPlayers))
				throw new U3DXTException("minPlayers and maxPlayers must be between 2 and 16.");

			// create request
			var request = new GKMatchRequest();
			request.minPlayers = minPlayers;
			request.maxPlayers = maxPlayers;
			if (playersToInvite != null)
				request.playersToInvite = Player.PlayersToIDs(playersToInvite);

			// create view controller
			var mmvc = new GKTurnBasedMatchmakerViewController(request);

			mmvc.showExistingMatches = showExistingMatches;
			mmvc.turnBasedMatchmakerDelegate = TurnBasedMatchmakerViewControllerDelegate.instance;

			// show it
			UIApplication.SharedApplication().keyWindow.rootViewController.PresentViewController(mmvc, true, null);
		}

		internal static void _OnMatchMakerFailed(NSError error) {
			if (_matchMakerFailedHandlers != null)
				_matchMakerFailedHandlers(null, new U3DXTErrorEventArgs(error));
		}

		internal static void _OnMatchMakerCancelled() {
			if (_matchMakerCancelledHandlers != null)
				_matchMakerCancelledHandlers(null, EventArgs.Empty);
		}

		internal static void _OnMatchMakerFoundMatch(GKTurnBasedMatch match) {

			var tbmatch = NSObjectWrapper.CreateWrapper(typeof(TurnBasedMatch), match) as TurnBasedMatch;
			match = null;

			tbmatch.ReloadPlayers(delegate() {
				// set current match if it matches the id
				SetCurrentMatch(tbmatch);

				if (_turnChangedHandlers != null)
					_turnChangedHandlers(null, new TurnChangedEventArgs(tbmatch));
			
				tbmatch = null;
			});
		}

		internal static void _OnPlayerQuit(GKTurnBasedMatch match) {

			var tbmatch = NSObjectWrapper.CreateWrapper(typeof(TurnBasedMatch), match) as TurnBasedMatch;
			match = null;

			tbmatch.ReloadPlayers(delegate() {

				if (_localPlayerQuitHandlers != null)
					_localPlayerQuitHandlers(null, new MatchEventArgs(tbmatch));

				tbmatch = null;
			});
		}

		/// <summary>
		/// Loads all the turn-based matches involving the local player and calls the callback on completion.
		/// </summary>
		/// <param name="callback">Callback.</param>
		public static void LoadAllMyMatches(Action<TurnBasedMatch[], NSError> callback) {
			GKTurnBasedMatch.LoadMatches(delegate(object[] gkmatches, NSError error) {
				if ((gkmatches == null) || (gkmatches.Length == 0)) {
					callback(null, error);
					callback = null;
				} else {
					var matches = new TurnBasedMatch[gkmatches.Length];
					int totalLoaded = 0;
					for (int i=0; i<gkmatches.Length; i++) {
						var gkmatch = gkmatches[i] as GKTurnBasedMatch;
						matches[i] = NSObjectWrapper.CreateWrapper(typeof(TurnBasedMatch), gkmatch) as TurnBasedMatch;
						gkmatch = null;

						matches[i].ReloadPlayers(delegate() {
							totalLoaded++;
							if (totalLoaded == gkmatches.Length) {
								callback(matches, error);
								callback = null;
								matches = null;
								error = null;
							}
						});
					}
				}

				gkmatches = null;
			});
		}

		/// <summary>
		/// A helper function to end and remove all turn-based matches that this player is participating in.
		/// This should be used for testing only.
		/// </summary>
		public static void RemoveAllMyMatches() {
			GKTurnBasedMatch.LoadMatches(_FoundMyMatches);
		}

		private static void _FoundMyMatches(object[] matches, NSError error) {
			if (matches != null) {
				foreach (var matchObj in matches) {
					var match = matchObj as GKTurnBasedMatch;
					_EndAndRemoveMatch(match);
				}
			}
		}

		private static void _EndAndRemoveMatch(GKTurnBasedMatch match) {

			try {
				(match.participants[0] as GKTurnBasedParticipant).matchOutcome = GKTurnBasedMatchOutcome.Won;
				for (var i=match.participants.Length-1; i>0; i--)
					(match.participants[i] as GKTurnBasedParticipant).matchOutcome = GKTurnBasedMatchOutcome.Lost;

				match.EndMatchInTurn((match.matchData != null) ? match.matchData : new NSData(), delegate(NSError error) {
					if (error != null) {
						Debug.Log("end match error: " + error.LocalizedDescription());
					}

					match.Remove(delegate(NSError error2) {
						if (error2 != null)
							Debug.Log("remove match error: " + error2.LocalizedDescription());
					});
				});
			} catch (Exception e) {
				Debug.Log("remove match error: " + e + ": " + e.StackTrace);
			}
		}
	}
}

#endif
