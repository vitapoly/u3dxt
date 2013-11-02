#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.Core;
using U3DXT.iOS.Native.MediaPlayer;
using U3DXT.iOS.Native.AVFoundation;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;
using U3DXT.Utils;
using U3DXT.iOS.Native.MobileCoreServices;
using System.IO;

namespace U3DXT.iOS.MediaPlayer {
	/// <summary>
	/// High-level class to export media to storage.
	/// </summary>
	public class MediaExporter {

#region events
		private static EventHandler<MediaExportedEventArgs> _exportCompletedHandlers;
		/// <summary>
		/// Occurs when an export operation has completed.
		/// </summary>
		public static event EventHandler<MediaExportedEventArgs> ExportCompleted {
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

		private static AVAssetExportSession _exporter;
		private static AVURLAsset _avAsset;
		private static string _tmpPath;
		private static string _outputPath;

		/// <summary>
		/// Exports the audio of a media item to storage.
		/// </summary>
		/// <returns><c>true</c> if audio started exporting, <c>false</c> if it is exporting another audio or the media item has DRM.</returns>
		/// <param name="mediaItem">Media item.</param>
		/// <param name="outputFolder">Absolute output folder or specify null to use Documents folder.</param>
		/// <param name="outputFile">Output file name or specify null to use <c>[artist] - [title].[extension]</c>.</param>
		/// <param name="overwrite">Whether to overwrite the output file.</param>
		public static bool ExportAudio(MPMediaItem mediaItem, string outputFolder = null, string outputFile = null, bool overwrite = false) {

			if (_exporter != null)
				return false;
			if (mediaItem == null)
				return false;

			if (outputFolder == null)
				outputFolder = Application.persistentDataPath;

			NSURL assetURL = mediaItem.Value(MPMediaItem.PropertyAssetURL) as NSURL;
			if (assetURL == null)
				return false;

			if (outputFile == null) {
				string artist = mediaItem.Value(MPMediaItem.PropertyArtist) as string;
				string title = mediaItem.Value(MPMediaItem.PropertyTitle) as string;
				string extension = Path.GetExtension(assetURL.AbsoluteString().Split('?')[0]);
				outputFile = artist + " - " + title + extension;
			}

			_outputPath = outputFolder + "/" + outputFile;
			if (!overwrite && File.Exists(_outputPath))
				return false;

			_avAsset = AVURLAsset.Asset(assetURL);
			_exporter = new AVAssetExportSession(_avAsset, AVAssetExportSession.AVAssetExportPresetPassthrough);
			_exporter.outputFileType = "com.apple.quicktime-movie";
			_exporter.shouldOptimizeForNetworkUse = true;

			string tmpExt = UTType.CopyPreferredTag(_exporter.outputFileType, UTType.kUTTagClassFilenameExtension);
			_tmpPath = Application.persistentDataPath + "/" + UUID.Generate() + "." + tmpExt;
			_exporter.outputURL = new NSURL(_tmpPath, false);

			_exporter.ExportAsynchronously(_OnExportDone);

			return true;
		}

		private static void _OnExportDone() {
			var oldExporter = _exporter;
			_exporter = null;
			_avAsset = null;

			if (oldExporter.status == AVAssetExportSessionStatus.Completed) {
				if (_tmpPath != _outputPath) {
					if (File.Exists(_outputPath))
						File.Delete(_outputPath);
					File.Move(_tmpPath, _outputPath);
				}

				if (_exportCompletedHandlers != null)
					_exportCompletedHandlers(null, new MediaExportedEventArgs(_outputPath));
			} else {
				if (_exportFailedHandlers != null)
					_exportFailedHandlers(null, new U3DXTErrorEventArgs(oldExporter.error));
			}
		}
	}
}

#endif
