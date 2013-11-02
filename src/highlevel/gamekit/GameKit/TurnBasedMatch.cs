#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.Foundation;
using U3DXT.iOS.Native.GameKit;
using U3DXT.Core;
using U3DXT.iOS.Internals;
using System.Linq;
using U3DXT.Utils;
using UnityEngine;
using System.Collections.Generic;

namespace U3DXT.iOS.GameKit {
	/// <summary>
	/// This class represents a turn-based match setup by Game Center's match maker.
	/// You use this to advance turns, end match, and make a rematch.
	/// </summary>
	public class TurnBasedMatch : GameCenterMatch {

		private EventHandler<EventArgs> _advanceTurnCompletedHandlers;
		/// <summary>
		/// Occurs when the advance turn operation completed successfully.
		/// </summary>
		public event EventHandler<EventArgs> AdvanceTurnCompleted {
			add { _advanceTurnCompletedHandlers += value; }
			remove { _advanceTurnCompletedHandlers -= value; }
		}

		private EventHandler<U3DXTErrorEventArgs> _advanceTurnFailedHandlers;
		/// <summary>
		/// Occurs when the advance turn operation failed.
		/// </summary>
		public event EventHandler<U3DXTErrorEventArgs> AdvanceTurnFailed {
			add { _advanceTurnFailedHandlers += value; }
			remove { _advanceTurnFailedHandlers -= value; }
		}

		private EventHandler<EventArgs> _saveTurnCompletedHandlers;
		/// <summary>
		/// Occurs when the save turn operation completed successfully.
		/// </summary>
		public event EventHandler<EventArgs> SaveTurnCompleted {
			add { _saveTurnCompletedHandlers += value; }
			remove { _saveTurnCompletedHandlers -= value; }
		}

		private EventHandler<U3DXTErrorEventArgs> _saveTurnFailedHandlers;
		/// <summary>
		/// Occurs when the save turn operation failed.
		/// </summary>
		public event EventHandler<U3DXTErrorEventArgs> SaveTurnFailed {
			add { _saveTurnFailedHandlers += value; }
			remove { _saveTurnFailedHandlers -= value; }
		}

		private EventHandler<EventArgs> _endMatchCompletedHandlers;
		/// <summary>
		/// Occurs when the end match operation completed successfully.
		/// </summary>
		public event EventHandler<EventArgs> EndMatchCompleted {
			add { _endMatchCompletedHandlers += value; }
			remove { _endMatchCompletedHandlers -= value; }
		}

		private EventHandler<U3DXTErrorEventArgs> _endMatchFailedHandlers;
		/// <summary>
		/// Occurs when the end match operation failed.
		/// </summary>
		public event EventHandler<U3DXTErrorEventArgs> EndMatchFailed {
			add { _endMatchFailedHandlers += value; }
			remove { _endMatchFailedHandlers -= value; }
		}

		private EventHandler<EventArgs> _quitMatchCompletedHandlers;
		/// <summary>
		/// Occurs when the quit match operation completed successfully.
		/// </summary>
		public event EventHandler<EventArgs> QuitMatchCompleted {
			add { _quitMatchCompletedHandlers += value; }
			remove { _quitMatchCompletedHandlers -= value; }
		}

		private EventHandler<U3DXTErrorEventArgs> _quitMatchFailedHandlers;
		/// <summary>
		/// Occurs when the quit match operation failed.
		/// </summary>
		public event EventHandler<U3DXTErrorEventArgs> QuitMatchFailed {
			add { _quitMatchFailedHandlers += value; }
			remove { _quitMatchFailedHandlers -= value; }
		}

		private TurnBasedParticipant[] _participants;

		/// <summary>
		/// Default constructor. You should not call this directly.
		/// </summary>
		/// <seealso cref="TurnBasedMatchesController.StartMatch()"/>
		public TurnBasedMatch() {
		}

		/// <summary>
		/// Initializes the wrapper.
		/// </summary>
		/// <param name="nsObj">The NSObject.</param>
		/// <param name="id">Identifier.</param>
		protected override void _InitWrapper(NSObject nsObj, string id = null) {
			base._InitWrapper(nsObj, id);

			// get wrappers for participants
			_participants = TurnBasedParticipant.ToParticipants(gkTurnBasedMatch.participants);
		}

		/// <summary>
		/// The low-level GKTurnBasedMatch object if you need more advanced functionalities.
		/// </summary>
		/// <value>The low-level GKTurnBasedMatch object.</value>
		public GKTurnBasedMatch gkTurnBasedMatch {
			get { return _nsObject as GKTurnBasedMatch; }
		}

		/// <summary>
		/// The participant whose turn it is to act next.
		/// The current participant is the only participant that is allowed to update the match data with advanceTurn().
		/// </summary>
		/// <value>The current participant.</value>
		public TurnBasedParticipant currentParticipant {
			get {
				var participant = gkTurnBasedMatch.currentParticipant;
				return NSObjectWrapper.CreateWrapper(typeof(TurnBasedParticipant), participant) as TurnBasedParticipant;
			}
		}

		/// <summary>
		/// Game-specific data that reflects the details of the match.
		/// </summary>
		/// <remarks>
		/// Although Game Center knows who is participating in the match and who is expected to act next,
		/// it does not know anything about your game’s internal logic.
		/// Your game provides the match data and all the programming logic required to interpret it.
		/// This data should include the current state of the game and provide any necessary details about what actions the current player is expected to take.
		/// It can also be helpful for your game to record information about recent moves made by other players.
		/// The game can then replay those moves visually for the player to show exactly how the match reached the state it is in now.
		/// <p></p>
		/// Your game never directly updates the match state associated with this property.
		/// Instead, when the data is updated to reflect the actions of the current player,
		/// your game serializes the updated state into memory and calls the match’s AdvanceTurn() or EndMatch()
		/// that transmit the updated state to Game Center.
		/// </remarks>
		/// <value>The match data.</value>
		public byte[] matchData {
			get { return gkTurnBasedMatch.matchData.ToByteArray(); }
		}

		/// <summary>
		/// Game-specific data as string that reflects the details of the match.
		/// </summary>
		/// <remarks>
		/// Although Game Center knows who is participating in the match and who is expected to act next,
		/// it does not know anything about your game’s internal logic.
		/// Your game provides the match data and all the programming logic required to interpret it.
		/// This data should include the current state of the game and provide any necessary details about what actions the current player is expected to take.
		/// It can also be helpful for your game to record information about recent moves made by other players.
		/// The game can then replay those moves visually for the player to show exactly how the match reached the state it is in now.
		/// <p></p>
		/// Your game never directly updates the match state associated with this property.
		/// Instead, when the data is updated to reflect the actions of the current player,
		/// your game serializes the updated state into memory and calls the match’s AdvanceTurn() or EndMatch()
		/// that transmit the updated state to Game Center.
		/// </remarks>
		/// <value>The match data.</value>
		public string matchDataString {
			get { return matchData.ToStraightString(); }
		}

		/// <summary>
		/// A string that uniquely identifies the match.
		/// </summary>
		/// <remarks>
		/// This string is not intended to be displayed to players.
		/// Your game should use this string whenever it needs to identify a specific match.
		/// For example, if you want your game to store additional information on a device or in iCloud,
		/// it might store it in a database using the match ID as a key.
		/// </remarks>
		/// <value>The match ID.</value>
		public string matchID {
			get { return gkTurnBasedMatch.matchID; }
		}

		/// <summary>
		/// A message displayed to all players in the match.
		/// </summary>
		/// <remarks>
		/// This property is displayed by the standard user interface;
		/// this allows your game to use the message to inform players of the current state of the match.
		/// <p></p>
		/// <strong>Important:</strong> This property can be changed only by an instance of your game associated with the current player.
		/// If an instance of your game associated with another player in the match attempts to write to this property,
		/// an exception is thrown.
		/// </remarks>
		/// <value>The message.</value>
		public string message {
			get { return gkTurnBasedMatch.message; }
			set { gkTurnBasedMatch.message = value; }
		}

		/// <summary>
		/// Information about the players participating in the match.
		/// </summary>
		/// <remarks>
		/// This property represents 
		/// all participants in the match. Your game uses these objects to retrieve more information 
		/// about the participants in the match. 
		/// Your game also uses one of the objects in this array as a parameter whenever it calls 
		/// a method that sets a different participant to act in the match.
		/// <p></p>
		/// The size of the array and the order in which the participants appear in the array are set when the match is first created, and never changes.
		/// When a match is first created, some participants may not hold actual players yet.
		/// Game Center searches for a player to fill that spot in the match only after your game sets that participant as the current player.
		/// </remarks>
		/// <value>The participants.</value>
		public TurnBasedParticipant[] participants {
			get { return _participants; }
		}

		/// <summary>
		/// Gets all other participants.
		/// </summary>
		/// <value>All other participants.</value>
		public TurnBasedParticipant[] allOtherParticipants {
			get {
				var arr = new List<TurnBasedParticipant>();
				foreach (var part in _participants) {
					if (part.player != GameKitXT.localPlayer)
						arr.Add(part);
				}
				return arr.ToArray();
			}
		}

		/// <summary>
		/// The current state of the match.
		/// </summary>
		/// <value>The status.</value>
		public GKTurnBasedMatchStatus status {
			get { return gkTurnBasedMatch.status; }
		}

		/// <summary>
		/// Reloads the players.
		/// </summary>
		/// <param name="callback">Callback.</param>
		public void ReloadPlayers(Action callback) {
			var playerIDs = _participants.Select(x => x.playerID).ToArray();
//			Debug.Log("ReloadPlayers IDs: " + Json.Serialize(playerIDs));
			Player.LoadPlayersByIDs(playerIDs, delegate(Player[] players) {
				for (int i=0; i<_participants.Length; i++) {
					_participants[i]._player = players[i];
				}
				callback();
				callback = null;
			});
		}

		/// <summary>
		/// Indicates whether it is the local player's turn for this match.
		/// </summary>
		/// <value><c>true</c> if it is the local player's turn for this match; otherwise, <c>false</c>.</value>
		public bool isLocalPlayerTurn {
			get {
				return ((gkTurnBasedMatch.currentParticipant != null)
					&& (gkTurnBasedMatch.currentParticipant.playerID == GKLocalPlayer.LocalPlayer().playerID));
			}
		}

		/// <summary>
		/// Indicates whether this match is the current match held by TurnBasedMatchesController.
		/// </summary>
		/// <value><c>true</c> if this match is the current match held by TurnBasedMatchesController; otherwise, <c>false</c>.</value>
		public bool isCurrentMatch {
			get {
				return ((TurnBasedMatchesController.currentMatch != null)
					&& (TurnBasedMatchesController.currentMatch.matchID == matchID));
			}
		}

		/// <summary>
		/// Indicates whether this match is a new match created by the local player.
		/// </summary>
		/// <value><c>true</c> if is new match; otherwise, <c>false</c>.</value>
		public bool isNewMatch {
			get { return (gkTurnBasedMatch.matchData.Length() == 0);}
		}

		/// <summary>
		/// Indicates whether this match has ended.
		/// </summary>
		/// <value><c>true</c> if is match ended; otherwise, <c>false</c>.</value>
		public bool isMatchEnded {
			get { return (status == GKTurnBasedMatchStatus.Ended); }
		}

		/// <summary>
		/// Indicates whether the local player is in this match. If the local player quit this match, this would be false.
		/// </summary>
		/// <value><c>true</c> if is local player in match; otherwise, <c>false</c>.</value>
		public bool isLocalPlayerInMatch {
			get {
				foreach (var participant in participants) {
					if (participant.player == GameKitXT.localPlayer)
						return (participant.matchOutcome == GKTurnBasedMatchOutcome.None);
				}
				return false;
			}
		}

		private TurnBasedParticipant[] _GetNextActiveParticipants() {
			var index = Array.IndexOf(_participants, currentParticipant);
			var arr = _participants.Skip(index + 1).ToList();
			arr = arr.Concat(_participants.Take(index)).ToList();
			for (var i=arr.Count - 1; i >= 0; i--) {
				if (arr[i].matchOutcome != GKTurnBasedMatchOutcome.None)
					arr.RemoveAt(i);
			}
			if (arr.Count == 0)
				arr.Add(currentParticipant);
			return arr.ToArray();
		}

		/// <summary>
		/// Ends the match. Raises EndMatchCompleted and EndMatchFailed events for success and error completion.
		/// </summary>
		/// <param name="matchData">A serialized blob of data reflecting the end state for the match.</param>
		/// <param name="aMessage">A message to display reflecting the state of the match.</param>
		public void EndMatch(byte[] matchData, string aMessage = null) {
			if (aMessage != null)
				gkTurnBasedMatch.message = aMessage;

			gkTurnBasedMatch.EndMatchInTurn(NSData.FromByteArray(matchData),
			                                _CreateCompleteFunction(_endMatchCompletedHandlers, _endMatchFailedHandlers));
		}

		/// <summary>
		/// Ends the match. Raises EndMatchCompleted and EndMatchFailed events for success and error completion.
		/// </summary>
		/// <param name="matchData">A serialized string of data reflecting the end state for the match.</param>
		/// <param name="aMessage">A message to display reflecting the state of the match.</param>
		public void EndMatch(string matchData, string aMessage = null) {
			EndMatch(matchData.ToStraightBytes(), aMessage);
		}

		private Action<NSError> _CreateCompleteFunction(EventHandler<EventArgs> completeHandlers, EventHandler<U3DXTErrorEventArgs> errorHandlers) {
			return delegate(NSError error) {
//				Debug.Log("completion: " + ((error != null) ? error.LocalizedDescription() : "success"));
				if (error == null) {
					if (completeHandlers != null)
						completeHandlers(this, EventArgs.Empty);
				} else {
					if (errorHandlers != null)
						errorHandlers(this, new U3DXTErrorEventArgs(error));
				}
			};
		}

		/// <summary>
		/// Updates the data stored on Game Center for the current match but does not advance the turn.
		/// Raises SaveTurnCompleted and SaveTurnFailed events for success and error completion.
		/// Available in iOS 6.0 and later.
		/// </summary>
		/// <param name="matchData">A serialized blob of data reflecting the game-specific state for the match.</param>
		/// <param name="aMessage">A message to display reflecting the state of the match.</param>
		public void SaveTurn(byte[] matchData, string aMessage = null) {
			if (aMessage != null)
				gkTurnBasedMatch.message = aMessage;

			gkTurnBasedMatch.SaveCurrentTurn(NSData.FromByteArray(matchData),
			                                _CreateCompleteFunction(_saveTurnCompletedHandlers, _saveTurnFailedHandlers));
		}

		/// <summary>
		/// Updates the data stored on Game Center for the current match but does not advance the turn.
		/// Raises SaveTurnCompleted and SaveTurnFailed events for success and error completion.
		/// Available in iOS 6.0 and later.
		/// </summary>
		/// <param name="matchData">A serialized string of data reflecting the game-specific state for the match.</param>
		/// <param name="aMessage">A message to display reflecting the state of the match.</param>
		public void SaveTurn(string matchData, string aMessage = null) {
			SaveTurn(matchData.ToStraightBytes(), aMessage);
		}

		/// <summary>
		/// Updates the data stored on Game Center for the current match and advances the turn.
		/// Raises AdvanceTurnCompleted and AdvanceTurnFailed events for success and error completion.
		/// On iOS 5.0, it only takes one next participant, and ignores the timeout.
		/// </summary>
		/// <param name="matchData">A serialized blob of data reflecting the game-specific state for the match.</param>
		/// <param name="aMessage">A message to display reflecting the state of the match.</param>
		/// <param name="nextParticipants">An array of TurnBasedParticipant objects reflecting the order in which the players should act next.
		/// Each object in the array must be one of the objects stored in the match’s participants property.
		/// If null or not specified, it would use the next player in the order of the participants property.</param>
		/// <param name="timeout">The length of time the next player has to complete their turn; in seconds.</param>
		public void AdvanceTurn(byte[] matchData, string aMessage = null, TurnBasedParticipant[] nextParticipants = null, double timeout = 0) {
			if (aMessage != null)
				gkTurnBasedMatch.message = aMessage;

			if (nextParticipants == null)
				nextParticipants = _GetNextActiveParticipants();

			//hack: apple server doesn't work if it takes an array, so popping all except 1 now
			if (nextParticipants.Length > 1)
				nextParticipants = new TurnBasedParticipant[] { nextParticipants[0] };

			if (gkTurnBasedMatch.RespondsToSelector("endTurnWithNextParticipants:turnTimeout:matchData:completionHandler:")) {
				gkTurnBasedMatch.EndTurn(
					TurnBasedParticipant.ToGKParticipants(nextParticipants),
					timeout, NSData.FromByteArray(matchData),
					_CreateCompleteFunction(_advanceTurnCompletedHandlers, _advanceTurnFailedHandlers));
			} else {
				gkTurnBasedMatch.EndTurn(
					TurnBasedParticipant.ToGKParticipants(nextParticipants)[0],
					NSData.FromByteArray(matchData),
					_CreateCompleteFunction(_advanceTurnCompletedHandlers, _advanceTurnFailedHandlers));
			}
		}

		/// <summary>
		/// Updates the data stored on Game Center for the current match and advances the turn.
		/// Raises AdvanceTurnCompleted and AdvanceTurnFailed events for success and error completion.
		/// On iOS 5.0, it only takes one next participant, and ignores the timeout.
		/// </summary>
		/// <param name="matchData">A serialized string of data reflecting the game-specific state for the match.</param>
		/// <param name="aMessage">A message to display reflecting the state of the match.</param>
		/// <param name="nextParticipants">An array of TurnBasedParticipant objects reflecting the order in which the players should act next.
		/// Each object in the array must be one of the objects stored in the match’s participants property.
		/// If null or not specified, it would use the next player in the order of the participants property.</param>
		/// <param name="timeout">The length of time the next player has to complete their turn; in seconds.</param>
		public void AdvanceTurn(string matchData, string aMessage = null, TurnBasedParticipant[] nextParticipants = null, double timeout = 0) {
			AdvanceTurn(matchData.ToStraightBytes(), aMessage, nextParticipants, timeout);
		}

		/// <summary>
		/// Resigns the current player from the match without ending the match.
		/// Raises QuitMatchCompleted and QuitMatchFailed events for success and error completion.
		/// On iOS 5.0, it only takes one next participant, and ignores the timeout.
		/// </summary>
		/// <param name="matchOutcome">The end outcome of the current player in the match.</param>
		/// <param name="matchData">A serialized blob of data reflecting the game-specific state for the match.</param>
		/// <param name="aMessage">A message to display reflecting the state of the match.</param>
		/// <param name="nextParticipants">An array of TurnBasedParticipant objects reflecting the order in which the players should act next.
		/// Each object in the array must be one of the objects stored in the match’s participants property.
		/// If null or not specified, it would use the next player in the order of the participants property.</param>
		/// <param name="timeout">The length of time the next player has to complete their turn; in seconds.</param>
		public void QuitDuringTurn(GKTurnBasedMatchOutcome matchOutcome, byte[] matchData, string aMessage = null, TurnBasedParticipant[] nextParticipants = null, double timeout = 0) {
			if (aMessage != null)
				gkTurnBasedMatch.message = aMessage;

			if (nextParticipants == null)
				nextParticipants = _GetNextActiveParticipants();

			//hack: apple server doesn't work if it takes an array, so popping all except 1 now
			if (nextParticipants.Length > 1)
				nextParticipants = new TurnBasedParticipant[] { nextParticipants[0] };

			if (gkTurnBasedMatch.RespondsToSelector("participantQuitInTurnWithOutcome:nextParticipants:turnTimeout:matchData:completionHandler:")) {
				gkTurnBasedMatch.ParticipantQuitInTurn(
					matchOutcome,
					TurnBasedParticipant.ToGKParticipants(nextParticipants),
					timeout, NSData.FromByteArray(matchData),
					_CreateCompleteFunction(_quitMatchCompletedHandlers, _quitMatchFailedHandlers));
			} else {
				gkTurnBasedMatch.ParticipantQuitInTurn(
					matchOutcome,
					TurnBasedParticipant.ToGKParticipants(nextParticipants)[0],
					NSData.FromByteArray(matchData),
					_CreateCompleteFunction(_quitMatchCompletedHandlers, _quitMatchFailedHandlers));
			}
		}

		/// <summary>
		/// Resigns the current player from the match without ending the match.
		/// Raises QuitMatchCompleted and QuitMatchFailed events for success and error completion.
		/// On iOS 5.0, it only takes one next participant, and ignores the timeout.
		/// </summary>
		/// <param name="matchOutcome">The end outcome of the current player in the match.</param>
		/// <param name="matchData">A serialized string of data reflecting the game-specific state for the match.</param>
		/// <param name="aMessage">A message to display reflecting the state of the match.</param>
		/// <param name="nextParticipants">An array of TurnBasedParticipant objects reflecting the order in which the players should act next.
		/// Each object in the array must be one of the objects stored in the match’s participants property.
		/// If null or not specified, it would use the next player in the order of the participants property.</param>
		/// <param name="timeout">The length of time the next player has to complete their turn; in seconds.</param>
		public void QuitDuringTurn(GKTurnBasedMatchOutcome matchOutcome, string matchData, string aMessage = null, TurnBasedParticipant[] nextParticipants = null, double timeout = 0) {
			QuitDuringTurn(matchOutcome, matchData.ToStraightBytes(), aMessage, nextParticipants, timeout);
		}

		/// <summary>
		/// Resigns the player from the match when that player is not the current player. This action does not end the match.
		/// Raises QuitMatchCompleted and QuitMatchFailed events for success and error completion.
		/// </summary>
		/// <param name="matchOutcome">The end outcome of the current player in the match.</param>
		public void QuitOutOfTurn(GKTurnBasedMatchOutcome matchOutcome) {
			gkTurnBasedMatch.ParticipantQuitOutOfTurn(
				matchOutcome,
				_CreateCompleteFunction(_quitMatchCompletedHandlers, _quitMatchFailedHandlers));
		}

		/// <summary>
		/// Create a new turn-based match with the same participants as this match.
		/// Raises TurnChanged and MatchMakerFailed events in TurnBasedMatchesController for success and error completion.
		/// Available in iOS 6.0 and later.
		/// </summary>
		public void Rematch() {
			gkTurnBasedMatch.Rematch(_RematchFound);
		}

		private void _RematchFound(GKTurnBasedMatch match, NSError error) {
			if (match != null) {
				TurnBasedMatchesController._OnMatchMakerFoundMatch(match);
			} else if (error != null) {
				TurnBasedMatchesController._OnMatchMakerFailed(error);			
			}
		}
	}
}

#endif
