#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.GameKit;
using U3DXT.iOS.GameKit;
using System.Linq;

namespace U3DXT.iOS.GameKit.Helpers {
	internal class LocalPlayerListener : GKLocalPlayerListener {

		private static LocalPlayerListener _instance;
		public static LocalPlayerListener instance {
			get {
				if (_instance == null)
					_instance = new LocalPlayerListener();
				return _instance;
			}
		}

		private bool _registered = false;

		public LocalPlayerListener() {
		}

		public void Register() {
			if (_registered)
				return;

			GKLocalPlayer.LocalPlayer().RegisterListener(this);

			_registered = true;
		}

		public override void DidRequestMatchWithPlayers(GKPlayer player, object[] playerIDsToInvite) {
			// send it to both realtime and turn-based if they are init'd
			if (RealTimeMatchesController._init)
				RealTimeMatchesController._MatchInviteHandler(null, playerIDsToInvite);

			if (TurnBasedMatchesController._init) {
				Player.LoadPlayersByIDs(playerIDsToInvite.Cast<string>().ToArray(), delegate(Player[] players) {
					if (players.Length > 0) {
						TurnBasedMatchesController._OnInvitePlayers(players);
					}
				});
			}
		}

		public override void DidAccept(GKPlayer player, GKInvite invite) {
			RealTimeMatchesController._MatchInviteHandler(invite, null);
		}

		public override void Ended(GKPlayer player, GKTurnBasedMatch match) {
			TurnBasedMatchesController._OnMatchEnded(match);
		}

		public override void ReceivedTurnEvent(GKPlayer player, GKTurnBasedMatch match, bool didBecomeActive) {
			TurnBasedMatchesController._OnTurnEvent(match, didBecomeActive);
		}

		public override void ReceivedExchangeCancellation(GKPlayer player, GKTurnBasedExchange exchange, GKTurnBasedMatch match) {
			TurnBasedMatchesController._OnExchangeCancellation(match, exchange);
		}

		public override void ReceivedExchangeReplies(GKPlayer player, object[] replies, GKTurnBasedExchange exchange, GKTurnBasedMatch match) {
			TurnBasedMatchesController._OnExchangeCompleted(match, exchange, replies);
		}

		public override void ReceivedExchangeRequest(GKPlayer player, GKTurnBasedExchange exchange, GKTurnBasedMatch match) {
			TurnBasedMatchesController._OnExchangeRequest(match, exchange);
		}

		public override void DidReceive(GKPlayer player, GKChallenge challenge) {
		}

		public override void WantsToPlay(GKPlayer player, GKChallenge challenge) {
		}

		public override void DidComplete(GKPlayer player, GKChallenge challenge, GKPlayer friendPlayer) {
		}

		public override void IssuedChallengeWasCompleted(GKPlayer player, GKChallenge challenge, GKPlayer friendPlayer) {
		}
	}
}

#endif
