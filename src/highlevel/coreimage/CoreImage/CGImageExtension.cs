using System;
using U3DXT.iOS.Native.CoreGraphics;
using UnityEngine;

namespace U3DXT.iOS.CoreImage {
	/// <summary>
	/// CGImage extension methods.
	/// </summary>
	public static class CGImageExtension {
		/// <summary>
		/// Create a native CGImage object from a WebCamTexture.
		/// </summary>
		/// <returns>The newly created native CGImage object or null if error.</returns>
		/// <param name="texture">Texture.</param>
		/// <param name="scale">Scale.</param>
		public static CGImage FromWebCamTexture(WebCamTexture texture, float scale = 1.0f) {
			if (texture == null)
				return null;

			var data = texture.GetPixels32();
			return CGImage.FromPixels32(data, texture.width, texture.height, scale);
		}
	}
}

