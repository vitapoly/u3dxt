#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.GameKit;

namespace U3DXT.iOS.GameKit {
	/// <summary>
	/// Exchange event arguments.
	/// </summary>
	public class ExchangeEventArgs : EventArgs {

		/// <summary>
		/// The match.
		/// </summary>
		public readonly GKTurnBasedMatch match;

		/// <summary>
		/// The exchange.
		/// </summary>
		public readonly GKTurnBasedExchange exchange;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.GameKit.ExchangeEventArgs"/> class.
		/// </summary>
		/// <param name="match">Match.</param>
		/// <param name="exchange">Exchange.</param>
		public ExchangeEventArgs(GKTurnBasedMatch match, GKTurnBasedExchange exchange) {
			this.match = match;
			this.exchange = exchange;
		}
	}
}

#endif
