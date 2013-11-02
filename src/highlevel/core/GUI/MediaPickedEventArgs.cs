#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.UIKit;

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
		/// Initializes a new instance of the <see cref="U3DXT.iOS.GUI.MediaPickedEventArgs"/> class.
		/// </summary>
		/// <param name="image">Image.</param>
		public MediaPickedEventArgs(UIImage image) {
			this.image = image;
		}
	}
}

#endif
