#if (UNITY_EDITOR || UNITY_IPHONE)

using System;

namespace U3DXT.iOS.GameKit {
	/// <summary>
	/// Match event arguments.
	/// </summary>
	public class MatchEventArgs : EventArgs {

		/// <summary>
		/// The match.
		/// </summary>
		public readonly GameCenterMatch match;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.GameKit.MatchEventArgs"/> class.
		/// </summary>
		/// <param name="match">Match.</param>
		public MatchEventArgs(GameCenterMatch match) {
			this.match = match;
		}

		/// <summary>
		/// Gets the real time match.
		/// </summary>
		/// <value>The real time match.</value>
		public RealTimeMatch realTimeMatch {
			get { return match as RealTimeMatch; }
		}

		/// <summary>
		/// Gets the turn based match.
		/// </summary>
		/// <value>The turn based match.</value>
		public TurnBasedMatch turnBasedMatch {
			get { return match as TurnBasedMatch; }
		}
	}
}

#endif
