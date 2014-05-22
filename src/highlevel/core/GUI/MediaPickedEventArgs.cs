#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;

namespace U3DXT.iOS.GUI {
	/// <summary>
	/// Media picked event arguments.
	/// </summary>
	public class MediaPickedEventArgs : EventArgs {

		/// <summary>
		/// The image.
		/// </summary>
		public readonly UIImage image;

		/// <summary>
		/// The URL.
		/// </summary>
		public readonly NSURL url;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.GUI.MediaPickedEventArgs"/> class.
		/// </summary>
		/// <param name="image">Image.</param>
		/// <param name="url">URL.</param>
		public MediaPickedEventArgs(UIImage image, NSURL url) {
			this.image = image;
			this.url = url;
		}
	}
}

#endif
