#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.GameKit;
using U3DXT.iOS.Native.Foundation;
using U3DXT.iOS.Internals;

namespace U3DXT.iOS.GameKit.Helpers {
	internal class MatchDelegate : GKMatchDelegate {

		private static MatchDelegate _instance;
		public static MatchDelegate instance {
			get {
				if (_instance == null)
					_instance = new MatchDelegate();
				return _instance;
			}
		}

		public MatchDelegate() {
		}

		public override void DidReceive(GKMatch match, NSData data, string playerID) {
			Player.LoadPlayersByIDs(new string[] {playerID}, delegate(Player[] players) {
				var rtMatch = NSObjectWrapper.GetWrapper(match.Uuid) as RealTimeMatch;
				if ((rtMatch != null) && (players.Length > 0)) {
					rtMatch._OnReceiveData(players[0], data);
				}

				match = null;
				data = null;
			});
		}

		public override void PlayerDidChangeState(GKMatch match, string playerID, GKPlayerConnectionState state) {
			Player.LoadPlayersByIDs(new string[] {playerID}, delegate(Player[] players) {
				var rtMatch = NSObjectWrapper.GetWrapper(match.Uuid) as RealTimeMatch;
				if ((rtMatch != null) && (rtMatch.gkMatch != null) && (players.Length > 0)) {
					rtMatch._OnPlayerStateChanged(players[0], state);
				}

				match = null;
			});
		}
	}
}

#endif
