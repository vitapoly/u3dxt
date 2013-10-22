using System;
using U3DXT.iOS.Internals;
using U3DXT.iOS.Native.GameKit;
using U3DXT.iOS.Native.Foundation;

namespace U3DXT.iOS.GameKit {
	/// <summary>
	/// This class represents a voice chat between players in a match.
	/// </summary>
	public class VoiceChat : NSObjectWrapper {

		private EventHandler<VoiceChatPlayerStateChangedEventArgs> _playerStateChangedHandlers;
		/// <summary>
		/// Occurs when a player's voice chat state has changed.
		/// </summary>
		public event EventHandler<VoiceChatPlayerStateChangedEventArgs> PlayerStateChanged {
			add { _playerStateChangedHandlers += value; }
			remove { _playerStateChangedHandlers -= value; }
		}

		/// <summary>
		/// Default constructor. You should not call this directly.
		/// See <see cref="RealTimeMatch.GetVoiceChat()"/> for more info.
		/// </summary>
		public VoiceChat() {
		}

		/// <summary>
		/// Initializes the wrapper.
		/// </summary>
		/// <param name="nsObj">The NSObject.</param>
		/// <param name="id">Identifier.</param>
		protected override void _InitWrapper(NSObject nsObj, string id = null) {
			base._InitWrapper(nsObj, id);

			// raise event when player state changes
			// raise event when player state changes
			gkVoiceChat.playerStateUpdateHandler = delegate(string playerID, GKVoiceChatPlayerState state) {
				Player.LoadPlayersByIDs(new string[] {playerID}, delegate(Player[] players) {
					if (_nsObject != null) {
						if (_playerStateChangedHandlers != null)
							_playerStateChangedHandlers(this, new VoiceChatPlayerStateChangedEventArgs(this, (players.Length > 0) ? players[0] : null, state));
					}
				});
			};
			//gkVoiceChat.playerStateUpdateHandler = _PlayerStateUpdateHandler;

			nsObj = null;
		}

//		private static void _PlayerStateUpdateHandler(string playerID, GKVoiceChatPlayerState state) {
//			Player.LoadPlayersByIDs(new string[] {playerID}, _CreatePlayerStateUpdateHandlerLoadPlayersHandler(new WeakReference(this), state));
//		}

//		private Action<Player[]> _CreatePlayerStateUpdateHandlerLoadPlayersHandler(WeakReference thisRef, GKVoiceChatPlayerState state) {
//			return delegate(Player[] players) {
//
//				VoiceChat thisChat = thisRef.Target as VoiceChat;
//				if (thisChat._nsObject != null) {
//					if (thisChat._playerStateChangedHandlers != null)
//						thisChat._playerStateChangedHandlers(thisChat, new VoiceChatPlayerStateChangedEventArgs(thisChat, (players.Length > 0) ? players[0] : null, state));
//				}
//			};
//		}

		/// <summary>
		/// The low-level GKVoiceChat object if you need more advanced functionalities.
		/// </summary>
		/// <value>The low-level GKVoiceChat object.</value>
		public GKVoiceChat gkVoiceChat {
			get { return _nsObject as GKVoiceChat; }
		}

		/// <summary>
		/// The name of this voice chat channel.
		/// </summary>
		/// <value>The name.</value>
		public string name {
			get { return gkVoiceChat.name; }
		}

		/// <summary>
		/// Joins a voice chat channel.  Once called, the local player can hear other players in the same channel.
		/// </summary>
		public void Join() {
			gkVoiceChat.Start();
		}

		/// <summary>
		/// Leaves a voice chat channel.  Once called, the local player can no longer hear other players in this channel.
		/// </summary>
		public void Leave() {
			gkVoiceChat.Stop();
		}

		/// <summary>
		/// Indicates whether the local player is talking in this channel.
		/// A player can only talk in one channel at a time.
		/// Setting this channel to true will set the other active channel to false.
		/// </summary>
		/// <value><c>true</c> if the local player is talking in this channel; otherwise, <c>false</c>.</value>
		public bool isTalking {
			get { return gkVoiceChat.active; }
			set { gkVoiceChat.active = value; }
		}

		/// <summary>
		/// The volume of this channel.
		/// </summary>
		/// <value>The volume.</value>
		public float volume {
			get { return gkVoiceChat.volume; }
			set { gkVoiceChat.volume = value; }
		}
	}
}

