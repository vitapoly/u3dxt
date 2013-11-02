#if (UNITY_EDITOR || UNITY_IPHONE)

using System;

namespace U3DXT.iOS.MediaPlayer {
	/// <summary>
	/// Media exported event arguments.
	/// </summary>
	public class MediaExportedEventArgs : EventArgs {

		/// <summary>
		/// The output URL.
		/// </summary>
		public readonly string outputURL;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.MediaPlayer.MediaExportedEventArgs"/> class.
		/// </summary>
		/// <param name="outputURL">Output URL.</param>
		public MediaExportedEventArgs(string outputURL) {
			this.outputURL = outputURL;
		}
	}
}

#endif
