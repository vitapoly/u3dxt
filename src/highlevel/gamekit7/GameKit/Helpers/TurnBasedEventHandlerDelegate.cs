using System;
using U3DXT.iOS.Native.GameKit;
using System.Linq;
using U3DXT.iOS.Internals;

namespace U3DXT.iOS.GameKit.Helpers {
	internal class TurnBasedEventHandlerDelegate : GKTurnBasedEventHandlerDelegate {
		private static TurnBasedEventHandlerDelegate _instance;
		public static TurnBasedEventHandlerDelegate instance {
			get {
				if (_instance == null)
					_instance = new TurnBasedEventHandlerDelegate();
				return _instance;
			}
		}

		public TurnBasedEventHandlerDelegate() {
		}

		public override void HandleInviteFromGameCenter(object[] playersToInvite) {
			Player.LoadPlayersByIDs(playersToInvite.Cast<string>().ToArray(), delegate(Player[] players) {
				if (players.Length > 0) {
					TurnBasedMatchesController._OnInvitePlayers(players);
				}
			});
		}

		public override void HandleMatchEnded(GKTurnBasedMatch match) {

			TurnBasedMatchesController._OnMatchEnded(match);
		}

		public override void HandleTurnEvent(GKTurnBasedMatch match, bool didBecomeActive) {

			TurnBasedMatchesController._OnTurnEvent(match, didBecomeActive);
		}
	}
}

