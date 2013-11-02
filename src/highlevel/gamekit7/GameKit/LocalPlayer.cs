#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.GameKit;
using System.Linq;
using U3DXT.iOS.Native.Foundation;

namespace U3DXT.iOS.GameKit {

	/// <summary>
	/// This class represents the authenticated local player.
	/// <p></p>
	/// Use GameKitXT.LocalPlayer to get local player after successful authentication.
	/// </summary>
	public class LocalPlayer : Player {

		private Player[] _friends;

		/// <summary>
		/// Default constructor. You should not create a LocalPlayer manually.
		/// Use GameKitXT.LocalPlayer to get local player after successful authentication.
		/// </summary>
		public LocalPlayer() {
		}

		/// <summary>
		/// The low-level GKLocalPlayer object if you need more advanced functionalities.
		/// </summary>
		/// <value>The low-level GKLocalPlayer object.</value>
		public GKLocalPlayer gkLocalPlayer {
			get { return _nsObject as GKLocalPlayer; }
		}

		/// <summary>
		/// Loads the friends of this player.
		/// </summary>
		/// <param name="completionCallback">Completion callback.</param>
		public void LoadFriends(Action<Player[]> completionCallback) {
			gkLocalPlayer.LoadFriends(_CreateLoadFriendsCallback(completionCallback));
		}

		private Action<object[], NSError> _CreateLoadFriendsCallback(Action<Player[]> callback) {
			return delegate(object[] friends, NSError error) {
				LoadPlayersByIDs(friends.Cast<string>().ToArray(), delegate(Player[] players) {
					_friends = players;
					callback(_friends);
					callback = null;
				});
			};
		}

		/// <summary>
		/// An array of Player objects representing the local player's friends.
		/// </summary>
		/// <value>The friends.</value>
		public Player[] friends {
			get { return _friends; }
		}

		/// <summary>
		/// Indicates whether the local player is under age.
		/// </summary>
		/// <value><c>true</c> if the local player is underage; otherwise, <c>false</c>.</value>
		public bool isUnderage {
			get { return gkLocalPlayer.underage; }
		}
	}
}

#endif
