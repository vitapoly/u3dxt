#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.GameKit;
using U3DXT.iOS.Native.Foundation;
using U3DXT.iOS.GameKit.Helpers;
using System.Collections.Generic;
using System.Linq;
using U3DXT.Core;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Internals;
using U3DXT.Utils;
using System.Text;

namespace U3DXT.iOS.GameKit {
	/// <summary>
	/// This class represents a real-time match setup by Game Center's match maker.
	/// Use this to send data to other players and start voice chats.
	/// </summary>
	public class RealTimeMatch : GameCenterMatch {

		private EventHandler<PlayerStateChangedEventArgs> _playerStateChangedHandlers;
		/// <summary>
		/// Occurs when a player's state has changed.
		/// </summary>
		public event EventHandler<PlayerStateChangedEventArgs> PlayerStateChanged {
			add { _playerStateChangedHandlers += value; }
			remove { _playerStateChangedHandlers -= value; }
		}

		private EventHandler<DataReceivedEventArgs> _dataReceivedHandlers;
		/// <summary>
		/// Occurs when the local player receives data from another player.
		/// </summary>
		public event EventHandler<DataReceivedEventArgs> DataReceived {
			add { _dataReceivedHandlers += value; }
			remove { _dataReceivedHandlers -= value; }
		}

		private Player[] _players;
		private Dictionary<string, VoiceChat> _voiceChats = null;

		/// <summary>
		/// Default constructor. You should not call this directly.
		/// See <see cref="RealTimeMatchesController.StartMatch()"/> for more info.
		/// </summary>
		public RealTimeMatch() {
		}

		/// <summary>
		/// Initializes the wrapper.
		/// </summary>
		/// <param name="nsObj">The NSObject.</param>
		/// <param name="id">Identifier.</param>
		protected override void _InitWrapper(NSObject nsObj, string id = null) {
			base._InitWrapper(nsObj, id);

			gkMatch.Delegate = MatchDelegate.instance;
		}

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="U3DXT.iOS.GameKit.RealTimeMatch"/> is reclaimed by garbage collection.
		/// </summary>
		~RealTimeMatch() {
			Disconnect();

			// leave voice chats
			foreach (var channel in _voiceChats.Values) {
				channel.Leave();
			}

			if (RealTimeMatchesController._currentMatch == this)
				RealTimeMatchesController._currentMatch = null;
		}

		/// <summary>
		/// The low-level GKMatch object if you need more advanced functionalities.
		/// </summary>
		/// <value>The low-level GKMatch object.</value>
		public GKMatch gkMatch {
			get { return _nsObject as GKMatch; }
		}

		/// <summary>
		/// Gets the players.
		/// </summary>
		/// <value>The players.</value>
		public Player[] players {
			get { return _players; }
		}

		/// <summary>
		/// Reloads the players.
		/// </summary>
		/// <param name="callback">Callback.</param>
		internal void ReloadPlayers(Action callback) {
			Player.LoadPlayersByIDs(gkMatch.playerIDs.Cast<string>().ToArray(), delegate(Player[] Players) {
				_players = Players;
				callback();
				callback = null;
			});
		}

		/// <summary>
		/// The number of players still expected to join the match before it can start or continue.
		/// Start your game when this number equals to 0.
		/// </summary>
		/// <value>The expected player count.</value>
		public uint expectedPlayerCount {
			get { return gkMatch.expectedPlayerCount; }
		}

		/// <summary>
		/// Brings up the Game Center match maker interface to add players to the game after some players have disconnected.
		/// Use the same params as when you called RealTimeMatchesController.StartMatch().
		/// </summary>
		/// <param name="minPlayers">Minimum players.</param>
		/// <param name="maxPlayers">Max players.</param>
		/// <param name="playerGroup">Player group.</param>
		/// <param name="playerAttributes">Player attributes.</param>
		/// <seealso cref="RealTimeMatchesController.StartMatch()"/>
		public void AddPlayers(uint minPlayers, uint maxPlayers, uint playerGroup = 0, uint playerAttributes = 0) {
			if ((minPlayers < 2) || (minPlayers > 4) || (maxPlayers < 2) || (maxPlayers > 4) || (maxPlayers < minPlayers))
				throw new U3DXTException("minPlayers and maxPlayers must be between 2 and 4.");

			var request = new GKMatchRequest();
			request.minPlayers = minPlayers;
			request.maxPlayers = maxPlayers;
			request.playerGroup = playerGroup;
			request.playerAttributes = playerAttributes;

			var mmvc = new GKMatchmakerViewController(request);
			mmvc.matchmakerDelegate = MatchmakerViewControllerDelegate.instance;

			UIApplication.SharedApplication().keyWindow.rootViewController.PresentViewController(mmvc, true, null);
		}

		/// <summary>
		/// Disconnects the local player from the match.
		/// </summary>
		public void Disconnect() {
			gkMatch.Disconnect();
		}

		/// <summary>
		/// Sends data to all players in the match.
		/// </summary>
		/// <returns>The error code; 0 means successful.</returns>
		/// <param name="data">A byte array representing the data to be sent.</param>
		/// <param name="reliable">Whether to send it using reliable method; using false for this sends it faster but does not guarantee delivery or the order of delivering multiple data packets.</param>
		public int SendDataToAll(byte[] data, bool reliable) {

			int code = 0;
			var error = new NSError();

			if (!gkMatch.SendDataToAllPlayers(NSData.FromByteArray(data),
			                                      reliable ? GKMatchSendDataMode.Reliable : GKMatchSendDataMode.Unreliable,
			                                      error)) {
				code = error.Code();
			}

			error = null;
			return code;
		}

		/// <summary>
		/// Sends data to all players in the match.
		/// </summary>
		/// <returns>The error code; 0 means successful.</returns>
		/// <param name="data">A string representing the data to be sent.</param>
		/// <param name="reliable">Whether to send it using reliable method; using false for this sends it faster but does not guarantee delivery or the order of delivering multiple data packets.</param>
		public int SendDataToAll(string data, bool reliable) {
			return SendDataToAll(data.ToStraightBytes(), reliable);
		}

		/// <summary>
		/// Sends data to some players in the match.
		/// </summary>
		/// <returns>The error code; 0 means successful.</returns>
		/// <param name="players">An array of Player objects to send the data to.</param>
		/// <param name="data">A byte array representing the data to be sent.</param>
		/// <param name="reliable">Whether to send it using reliable method; using false for this sends it faster but does not guarantee delivery or the order of delivering multiple data packets.</param>
		public int SendData(Player[] players, byte[] data, bool reliable) {

			int code = 0;
			var error = new NSError();

			if (!gkMatch.SendData(NSData.FromByteArray(data),
			                          Player.PlayersToIDs(players),
			                          reliable ? GKMatchSendDataMode.Reliable : GKMatchSendDataMode.Unreliable,
			                          error)) {
				code = error.Code();
			}

			error = null;
			return code;
		}

		/// <summary>
		/// Sends data to some players in the match.
		/// </summary>
		/// <returns>The error code; 0 means successful.</returns>
		/// <param name="players">An array of Player objects to send the data to.</param>
		/// <param name="data">A string representing the data to be sent.</param>
		/// <param name="reliable">Whether to send it using reliable method; using false for this sends it faster but does not guarantee delivery or the order of delivering multiple data packets.</param>
		public int SendData(Player[] players, string data, bool reliable) {
			return SendData(players, data.ToStraightBytes(), reliable);
		}

		internal void _OnReceiveData(Player player, NSData data) {
			if (_dataReceivedHandlers != null)
				_dataReceivedHandlers(this, new DataReceivedEventArgs(player, data.ToByteArray()));
		}

		internal void _OnPlayerStateChanged(Player player, GKPlayerConnectionState state) {
			if (_playerStateChangedHandlers != null)
				_playerStateChangedHandlers(this, new PlayerStateChangedEventArgs(player, (state == GKPlayerConnectionState.Connected)));
		}

		/// <summary>
		/// Returns a VoiceChat object with the channel name.  Use this to start a voice chat with other players.
		/// All players can talk to each other if using the same channel name.
		/// Or you can have the red team join one channel while the blue team join another channel, and have an "all" channel
		/// for all players to talk to all others.
		/// </summary>
		/// <returns>The voice chat.</returns>
		/// <param name="channel">Channel name.</param>
		public VoiceChat GetVoiceChat(string channel) {

			RealTimeMatchesController.EnableVoiceChat();

			// return if already created
			if (_voiceChats == null)
				_voiceChats = new Dictionary<string, VoiceChat>();

			VoiceChat chat = null;
			if (_voiceChats.TryGetValue(channel, out chat))
				return chat;

			// get it and create a wrapper
			var gkchat = gkMatch.VoiceChat(channel);
			if (gkchat != null) {
				chat = NSObjectWrapper.CreateWrapper(typeof(VoiceChat), gkchat) as VoiceChat;
				_voiceChats[channel] = chat;
			}

			return chat;
		}
	}
}

#endif
