#if (UNITY_EDITOR || UNITY_IPHONE)

using System;

namespace U3DXT.iOS.UserMedia {
	/// <summary>
	/// Photos Library exported event arguments.
	/// </summary>
	public class PhotosLibraryExportedEventArgs : EventArgs {

		/// <summary>
		/// The asset URL.
		/// </summary>
		public readonly string assetURL;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.UserMedia.PhotosLibraryExportedEventArgs"/> class.
		/// </summary>
		/// <param name="assetURL">Asset UR.</param>
		public PhotosLibraryExportedEventArgs(string assetURL) {
			this.assetURL = assetURL;
		}
	}
}

#endif
