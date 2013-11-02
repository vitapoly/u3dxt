#if (UNITY_EDITOR || UNITY_IPHONE)

using System;

namespace U3DXT.iOS.GameKit {
	/// <summary>
	/// Invite players event arguments.
	/// </summary>
	public class PlayersInvitedEventArgs : EventArgs {

		/// <summary>
		/// The players to invite.
		/// </summary>
		public readonly Player[] playersToInvite;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.GameKit.PlayersInvitedEventArgs"/> class.
		/// </summary>
		/// <param name="playersToInvite">Players to invite.</param>
		public PlayersInvitedEventArgs(Player[] playersToInvite) {
			this.playersToInvite = playersToInvite;
		}
	}
}

#endif
