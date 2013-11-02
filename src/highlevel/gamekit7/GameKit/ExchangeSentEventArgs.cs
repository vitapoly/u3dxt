#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.GameKit;
using U3DXT.iOS.Native.Foundation;

namespace U3DXT.iOS.GameKit {
	/// <summary>
	/// Turn based exchange sent event arguments.
	/// </summary>
	public class ExchangeSentEventArgs : EventArgs {

		/// <summary>
		/// The exchange.
		/// </summary>
		public readonly GKTurnBasedExchange exchange;

		/// <summary>
		/// The error.
		/// </summary>
		public readonly NSError error;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.GameKit.ExchangeSentEventArgs"/> class.
		/// </summary>
		/// <param name="exchange">Exchange.</param>
		/// <param name="error">Error.</param>
		public ExchangeSentEventArgs(GKTurnBasedExchange exchange, NSError error) {
			this.exchange = exchange;
			this.error = error;
		}
	}
}

#endif
