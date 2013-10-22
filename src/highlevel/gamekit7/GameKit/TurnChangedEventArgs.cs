using System;

namespace U3DXT.iOS.GameKit {
	/// <summary>
	/// Turn changed event arguments.
	/// </summary>
	public class TurnChangedEventArgs : MatchEventArgs {

		/// <summary>
		/// Whether it is the local player's turn.
		/// </summary>
		public readonly bool isLocalPlayersTurn;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.GameKit.TurnChangedEventArgs"/> class.
		/// </summary>
		/// <param name="match">Match.</param>
		public TurnChangedEventArgs(TurnBasedMatch match) : base(match) {
			isLocalPlayersTurn = match.isLocalPlayerTurn;
		}
	}
}

