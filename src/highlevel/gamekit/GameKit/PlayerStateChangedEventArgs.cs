using System;

namespace U3DXT.iOS.GameKit {
	/// <summary>
	/// Player state changed event arguments.
	/// </summary>
	public class PlayerStateChangedEventArgs : EventArgs {
		/// <summary>
		/// The player.
		/// </summary>
		public readonly Player player;

		/// <summary>
		/// Whether player is connected.
		/// </summary>
		public readonly bool isConnected;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.GameKit.PlayerStateChangedEventArgs"/> class.
		/// </summary>
		/// <param name="player">Player.</param>
		/// <param name="isConnected">If set to <c>true</c> is connected.</param>
		public PlayerStateChangedEventArgs(Player player, bool isConnected) {
			this.player = player;
			this.isConnected = isConnected;
		}
	}
}

