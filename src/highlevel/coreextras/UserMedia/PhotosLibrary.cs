#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.Core;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;
using U3DXT.iOS.Native.AssetsLibrary;
using System.Collections.Generic;

namespace U3DXT.iOS.UserMedia {
	/// <summary>
	/// The <c>PhotosLibrary</c> class provides high-level API access to photos and videos in the Photos app.</summary>
	/// <remarks>
	/// </remarks>
	public class PhotosLibrary {

#region events
		private static EventHandler<PhotosLibraryExportedEventArgs> _exportCompletedHandlers;
		/// <summary>
		/// Occurs when an export operation has completed.
		/// </summary>
		public static event EventHandler<PhotosLibraryExportedEventArgs> ExportCompleted {
			add { _exportCompletedHandlers += value; }
			remove { _exportCompletedHandlers -= value; }
		}

		private static EventHandler<U3DXTErrorEventArgs> _exportFailedHandlers;
		/// <summary>
		/// Occurs when an export operation has failed.
		/// </summary>
		public static event EventHandler<U3DXTErrorEventArgs> ExportFailed {
			add { _exportFailedHandlers += value; }
			remove { _exportFailedHandlers -= value; }
		}
#endregion

		private static ALAssetsLibrary _library;

		/// <summary>
		/// The low-level native assets library.
		/// </summary>
		/// <value>The low-level native assets library.</value>
		public static ALAssetsLibrary assetsLibrary {
			get {
				if (_library == null)
					_library = new ALAssetsLibrary();
				return _library;
			}
		}

		/// <summary>
		/// Exports a texture in PNG format to Photos album.
		/// Raises ExportCompleted or ExportFailed events on completion.
		/// </summary>
		/// <param name="texture">Texture.</param>
		public static void ExportPNG(Texture2D texture) {
			ExportPNG(UIImage.FromTexture2D(texture));
		}

		/// <summary>
		/// Exports an UIImage in PNG format to Photos album.
		/// Raises ExportCompleted or ExportFailed events on completion.
		/// </summary>
		/// <param name="image">Image.</param>
		public static void ExportPNG(UIImage image) {
			NSData data = image.PNGRepresentation();
			assetsLibrary.WriteImageDataToSavedPhotosAlbum(data, new Dictionary<object, object>(), _OnWriteCompletion);
		}

		/// <summary>
		/// Exports a texture in JPEG format to Photos album.
		/// Raises ExportCompleted or ExportFailed events on completion.
		/// </summary>
		/// <param name="texture">Texture.</param>
		/// <param name="quality">Quality of the resulting JPEG from 0.0 to 1.0.</param>
		public static void ExportJPEG(Texture2D texture, float quality) {
			ExportJPEG(UIImage.FromTexture2D(texture), quality);
		}

		/// <summary>
		/// Exports an UIImage in JPEG format to Photos album.
		/// Raises ExportCompleted or ExportFailed events on completion.
		/// </summary>
		/// <param name="image">Image.</param>
		/// <param name="quality">Quality of the resulting JPEG from 0.0 to 1.0.</param>
		public static void ExportJPEG(UIImage image, float quality) {
			NSData data = image.JPEGRepresentation(quality);
			assetsLibrary.WriteImageDataToSavedPhotosAlbum(data, new Dictionary<object, object>(), _OnWriteCompletion);
		}

		private static void _OnWriteCompletion(NSURL assetURL, NSError error) {
			if (error == null) {
				if (_exportCompletedHandlers != null)
					_exportCompletedHandlers(null, new PhotosLibraryExportedEventArgs(assetURL.AbsoluteString()));
			} else {
				if (_exportFailedHandlers != null)
					_exportFailedHandlers(null, new U3DXTErrorEventArgs(error));
			}
		}
	}
}

#endif
