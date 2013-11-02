#if (UNITY_EDITOR || UNITY_IPHONE)

using System;

namespace U3DXT.iOS.GameKit {
	/// <summary>
	/// Invite accepted event arguments.
	/// </summary>
	public class InviteAcceptedEventArgs : EventArgs {

		/// <summary>
		/// The inviter.
		/// </summary>
		public readonly Player inviter;

		/// <summary>
		/// The player group.
		/// </summary>
		public readonly uint playerGroup;

		/// <summary>
		/// The player attributes.
		/// </summary>
		public readonly uint playerAttributes;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.GameKit.InviteAcceptedEventArgs"/> class.
		/// </summary>
		/// <param name="inviter">Inviter.</param>
		/// <param name="playerGroup">Player group.</param>
		/// <param name="playerAttributes">Player attributes.</param>
		public InviteAcceptedEventArgs(Player inviter, uint playerGroup, uint playerAttributes) {
			this.inviter = inviter;
			this.playerGroup = playerGroup;
			this.playerAttributes = playerAttributes;
		}
	}
}

#endif
