using System;
using U3DXT.iOS.Native.GameKit;

namespace U3DXT.iOS.GameKit {
	/// <summary>
	/// Voice chat player state changed event arguments.
	/// </summary>
	public class VoiceChatPlayerStateChangedEventArgs : EventArgs {

		/// <summary>
		/// The voice chat.
		/// </summary>
		public readonly VoiceChat voiceChat;

		/// <summary>
		/// The player.
		/// </summary>
		public readonly Player player;

		/// <summary>
		/// The state.
		/// </summary>
		public readonly GKVoiceChatPlayerState state;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.GameKit.VoiceChatPlayerStateChangedEventArgs"/> class.
		/// </summary>
		/// <param name="voiceChat">Voice Chat.</param>
		/// <param name="player">Player.</param>
		/// <param name="state">State.</param>
		public VoiceChatPlayerStateChangedEventArgs(VoiceChat voiceChat, Player player, GKVoiceChatPlayerState state) {
			this.voiceChat = voiceChat;
			this.player = player;
			this.state = state;
		}
	}
}

