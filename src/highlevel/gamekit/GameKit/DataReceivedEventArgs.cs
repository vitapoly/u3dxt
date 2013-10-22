using System;
using U3DXT.Utils;

namespace U3DXT.iOS.GameKit {
	/// <summary>
	/// Data received event arguments.
	/// </summary>
	public class DataReceivedEventArgs : EventArgs{
		/// <summary>
		/// The player.
		/// </summary>
		public readonly Player player;

		/// <summary>
		/// The received data.
		/// </summary>
		public readonly byte[] data;

		/// <summary>
		/// The received data as a string.
		/// </summary>
		/// <value>The received data as a string.</value>
		public string dataString {
			get { return data.ToStraightString(); }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.GameKit.DataReceivedEventArgs"/> class.
		/// </summary>
		/// <param name="player">Player.</param>
		/// <param name="data">Data.</param>
		public DataReceivedEventArgs(Player player, byte[] data) {
			this.player = player;
			this.data = data;
		}
	}
}

