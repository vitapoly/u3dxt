using System;
using U3DXT.iOS.Internals;
using UnityEngine;
using U3DXT.iOS.Native.GameKit;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using System.Collections.Generic;
using System.Linq;

namespace U3DXT.iOS.GameKit {
	/// <summary>
	/// This class represents a Game Center player as the local player's friend or a player in a match.
	/// </summary>
	public class Player : NSObjectWrapper {

		/// <summary>
		/// A data object for custom data to associate with this player in your game.
		/// </summary>
		public object data;

		private Texture2D _photo;

		/// <summary>
		/// Default constructor.  You should not have to call this manually.
		/// You get players by calling LocalPlayer instance's Friends property or RealTimeMatch instance's Players property.
		/// </summary>
		public Player() {
		}

		/// <summary>
		/// The low-level GKPlayer object if you need more advanced functionalities.
		/// </summary>
		/// <value>The low-level GKPlayer object.</value>
		public GKPlayer gkPlayer {
			get { return _nsObject as GKPlayer; }
		}

		/// <summary>
		/// The Game Center player ID.
		/// </summary>
		/// <value>The player ID.</value>
		public string playerID {
			get { return gkPlayer.playerID; }
		}

		/// <summary>
		/// The Game Center player's alias.
		/// </summary>
		/// <value>The alias.</value>
		public string alias {
			get { return gkPlayer.alias; }
		}

		/// <summary>
		/// The Game Center player's display name.
		/// </summary>
		/// <value>The display name.</value>
		public string displayName {
			get { return gkPlayer.displayName; }
		}

		/// <summary>
		/// Indicates whether this player is a friend of the authenticated local player.
		/// </summary>
		/// <value><c>true</c> if this player is friend; otherwise, <c>false</c>.</value>
		public bool isFriend {
			get { return gkPlayer.isFriend; }
		}

		/// <summary>
		/// The photo of the player. Call LoadPhoto() before getting this.
		/// </summary>
		/// <value>The photo.</value>
		public Texture2D photo {
			get { return _photo; }
		}

		/// <summary>
		/// Loads the photo.
		/// </summary>
		/// <param name="size">Size.</param>
		/// <param name="completionCallback">Completion callback.</param>
		public void LoadPhoto(GKPhotoSize size, Action<Texture2D> completionCallback) {
			gkPlayer.LoadPhoto(size, (completionCallback == null) ? null : _CreateLoadPhotoCallback(completionCallback));
		}

		private Action<UIImage, NSError> _CreateLoadPhotoCallback(Action<Texture2D> completionCallback) {
			return delegate(UIImage image, NSError error) {
				if (error == null) {
					UnloadPhoto();

					_photo = image.ToTexture2D();
					completionCallback(_photo);
				} else {
					completionCallback(null);
				}
				completionCallback = null;
			};
		}

		/// <summary>
		/// Unloads the Texture2D photo from memory.
		/// </summary>
		public void UnloadPhoto() {
			if (_photo != null) {
				Texture2D.Destroy(_photo);
				_photo = null;
			}
		}

		/// <summary>
		/// Loads the players by IDs.
		/// </summary>
		/// <param name="playerIDs">Player IDs.</param>
		/// <param name="callback">Callback.</param>
		public static void LoadPlayersByIDs(string[] playerIDs, Action<Player[]> callback) {
			// if it's not already there, add it to array
			var loadPlayerIDs = new List<string>();
			for (int i=0; i<playerIDs.Length; i++) {
				if ((playerIDs[i] != null) && (playerIDs[i].Length > 0) && ((GetWrapper(playerIDs[i]) as Player) == null))
					loadPlayerIDs.Add(playerIDs[i]);
			}

			// return it if all are there
			if (loadPlayerIDs.Count == 0) {
				_ReturnPlayersByIDs(playerIDs, callback);
				callback = null;

			} else {

				// load them, and then create wrappers for them
				GKPlayer.LoadPlayers(loadPlayerIDs.ToArray(), delegate(object[] players, NSError error) {
					for (int i=0; i<players.Length; i++) {
						var player = players[i] as GKPlayer;
						if (player != null)
							CreateWrapper(typeof(Player), player, player.playerID);
					}

					_ReturnPlayersByIDs(playerIDs, callback);
					callback = null;
				});
			}
		}

		// return in same order as the original array, even if the player doesn't exist
		private static void _ReturnPlayersByIDs(string[] playerIDs, Action<Player[]> callback) {
			var players = new List<Player>();
			for (int i=0; i<playerIDs.Length; i++) {
				var player = GetWrapper(playerIDs[i]) as Player;
				players.Add(player);
			}

			callback(players.ToArray());
		}

		/// <summary>
		/// Players to player IDs.
		/// </summary>
		/// <returns>The player IDs.</returns>
		/// <param name="players">Players.</param>
		public static string[] PlayersToIDs(Player[] players) {
			return players.Select(x => x.playerID).ToArray();
		}
	}
}

