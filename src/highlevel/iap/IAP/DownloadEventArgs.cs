#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.StoreKit;
using System.Linq;

namespace U3DXT.iOS.IAP {
	/// <summary>
	/// Download event arguments.
	/// </summary>
	public class DownloadEventArgs : EventArgs {

		/// <summary>
		/// The downloads.
		/// </summary>
		public readonly SKDownload[] downloads;

//		/// <summary>
//		/// The downloads.
//		/// </summary>
//		[Obsolete("Use the downloads property.")]
//		public readonly SKDownload[] Downloads;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.IAP.DownloadEventArgs"/> class.
		/// </summary>
		/// <param name="downloads">Downloads.</param>
		public DownloadEventArgs(object[] downloads) {
			this.downloads = downloads.Cast<SKDownload>().ToArray();
//			this.Downloads = this.downloads;
		}
	}
}

#endif
