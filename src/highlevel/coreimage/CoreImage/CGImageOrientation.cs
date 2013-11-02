#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.UIKit;

namespace U3DXT.iOS.CoreImage {
	/// <summary>
	/// Image orientation.
	/// </summary>
	public enum CGImageOrientation {
		/// <summary>
		/// The default image orientation.
		/// </summary>
//		LandscapeLeft = 1,
		Default = 1,

		/// <summary>
		/// The mirrored.
		/// </summary>
		Mirrored = 2,

		/// <summary>
		/// The upside-down image orientation.
		/// </summary>
//		LandscapeRight = 3,
		UpsideDown = 3,

		/// <summary>
		/// The upside down mirrored.
		/// </summary>
		UpsideDownMirrored = 4,

		/// <summary>
		/// The rotated right image orientation.
		/// </summary>
//		PortraitDown = 8,
		RotatedRight = 8,

		/// <summary>
		/// The rotated right mirrored.
		/// </summary>
		RotatedRightMirrored = 5,

		/// <summary>
		/// The rotated left image orientation.
		/// </summary>
//		PortraitUp = 6
		RotatedLeft = 6,

		/// <summary>
		/// The rotated left mirrored.
		/// </summary>
		RotatedLeftMirrored = 7
	}

	/// <summary>
	/// User interface image orientation extensions.
	/// </summary>
	public static class UIImageOrientationExtensions {
		/// <summary>
		/// To the CG image orientation.
		/// </summary>
		/// <returns>The CG image orientation.</returns>
		/// <param name="value">Value.</param>
		public static CGImageOrientation ToCGImageOrientation(this UIImageOrientation value) {
			switch (value) {
				case UIImageOrientation.Up:
					return CGImageOrientation.Default;
				case UIImageOrientation.Down:
					return CGImageOrientation.UpsideDown;
				case UIImageOrientation.Left:
					return CGImageOrientation.RotatedRight;
				case UIImageOrientation.Right:
					return CGImageOrientation.RotatedLeft;
				case UIImageOrientation.UpMirrored:
					return CGImageOrientation.Mirrored;
				case UIImageOrientation.DownMirrored:
					return CGImageOrientation.UpsideDownMirrored;
				case UIImageOrientation.LeftMirrored:
					return CGImageOrientation.RotatedRightMirrored;
				case UIImageOrientation.RightMirrored:
					return CGImageOrientation.RotatedLeftMirrored;
				default:
					return CGImageOrientation.Default;
			}
		}
	}
}

#endif
