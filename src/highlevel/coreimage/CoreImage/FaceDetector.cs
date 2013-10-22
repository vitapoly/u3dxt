using System;
using U3DXT.iOS.Native.CoreImage;
using System.Collections.Generic;
using U3DXT.iOS.Native.CoreGraphics;
using UnityEngine;
using U3DXT.iOS.Native.UIKit;

namespace U3DXT.iOS.CoreImage {
	/// <summary>
	/// TODO
	/// </summary>
	public class FaceDetector {

		private CIDetector _detector;
		private Dictionary<object, object> _imageOpts = new Dictionary<object, object>();

		/// <summary>
		/// The preprocess image scale.
		/// </summary>
		public float PreprocessImageScale = 1.0f;//0.125f;

		/// <summary>
		/// The projected scale.
		/// </summary>
		public float ProjectedScale = 1.0f;
		private float _finalScale;

		/// <summary>
		/// The correct orientation.
		/// </summary>
		public bool CorrectOrientation = true;

		/// <summary>
		/// The is mirrored.
		/// </summary>
		public bool IsMirrored = false;



		/// <summary>
		/// Creates an instance of FaceDetector. Once created, the two arguments cannot be changed for this instance.
		/// If using high accuracy, the preprocessImageScale property is set to 1.0, otherwise defaults to 0.125.
		/// </summary>
		/// <param name="useHighAccuracy">Thether to use high accuracy for detection.</param>
		/// <param name="trackFaces">Whether to track faces in live video or successive image detections; available ONLY in iOS 6.0 and later.</param>
		public FaceDetector(bool useHighAccuracy = false, bool trackFaces = false) {
			// setup face detector
			var context = CIContext.Context((Dictionary<object, object>)null);
			var opts = new Dictionary<object, object>();
			opts[CIDetector.Accuracy] = useHighAccuracy ? CIDetector.AccuracyHigh : CIDetector.AccuracyLow;
			opts[CIDetector.Tracking] = trackFaces ? 1 : 0;

			_detector = CIDetector.DetectorOfType(CIDetector.TypeFace, context, opts);

			if (useHighAccuracy)
				PreprocessImageScale = 1.0f;
		}

		/// <summary>
		/// Detects the in image.
		/// </summary>
		/// <returns>The in image.</returns>
		/// <param name="image">Image.</param>
		/// <param name="imageOrientation">Image orientation.</param>
		public Face[] DetectInImage(Texture2D image, UIImageOrientation imageOrientation) {
			return DetectInImage(image, imageOrientation.ToCGImageOrientation());
		}


		/// <summary>
		/// Detects the in image.
		/// </summary>
		/// <returns>The in image.</returns>
		/// <param name="image">Image.</param>
		/// <param name="imageOrientation">Image orientation.</param>
		public Face[] DetectInImage(Texture2D image, CGImageOrientation imageOrientation = CGImageOrientation.Default) {
			var cgimage = CGImage.FromTexture2D(image);
			var ciimage = new CIImage(cgimage);
//			cgimage.Release();
			return DetectInImage(ciimage, imageOrientation);
		}

		/// <summary>
		/// Detects the in image.
		/// </summary>
		/// <returns>The in image.</returns>
		/// <param name="ciimage">Ciimage.</param>
		/// <param name="imageOrientation">Image orientation.</param>
		public Face[] DetectInImage(CIImage ciimage, CGImageOrientation imageOrientation = CGImageOrientation.Default) {
			var rect = ciimage.Extent();
			int imageHeight = (int)(rect.height * PreprocessImageScale);
			int imageWidth = (int)(rect.width * PreprocessImageScale);
			_finalScale = ProjectedScale / PreprocessImageScale;

			// create CIImage from bitmapdata
//			var ciimage:CIImage = CIImage.fromBitmapData(image, preprocessImageScale); //TODO
//			var ciimage = new CIImage(CGImage.FromTexture2D(image));

			// orientation settings
			_imageOpts[CIDetector.ImageOrientation] = (int)imageOrientation;

			// detect
			var features = _detector.FeaturesInImage(ciimage, _imageOpts);

			// go through features and transform coords
			var faces = new Face[features.Length];
			for (int i=0; i<features.Length; i++) {
				var feature = features[i] as CIFaceFeature;
				var face = new Face();

				face.bounds = _FixRect(feature.bounds, imageHeight, imageWidth, imageOrientation);

				if (feature.hasMouthPosition) {
					face.hasMouthPosition = true;
					face.mouthPosition = _FixPoint(feature.mouthPosition, imageHeight, imageWidth, imageOrientation);
				}

				if (feature.hasLeftEyePosition) {
					face.hasLeftEyePosition = true;
					face.leftEyePosition = _FixPoint(feature.leftEyePosition, imageHeight, imageWidth, imageOrientation);
				}

				if (feature.hasRightEyePosition) {
					face.hasRightEyePosition = true;
					face.rightEyePosition = _FixPoint(feature.rightEyePosition, imageHeight, imageWidth, imageOrientation);
				}

				if (feature.RespondsToSelector("trackingID")) {
					if (feature.hasTrackingID) {
						face.hasTrackingID = true;
						face.trackingID = feature.trackingID;
					}

					if (feature.hasTrackingFrameCount) {
						face.hasTrackingFrameCount = true;
						face.trackingFrameCount = feature.trackingFrameCount;
					}
				}

				faces[i] = face;
			}

			return faces;
		}

		private Rect _FixRect(Rect rect, int imageHeight, int imageWidth, CGImageOrientation imageOrientation) {
			rect.y = imageHeight - rect.y - rect.height;

			// rotate coords
			if (CorrectOrientation) {
				float temp;
				switch (imageOrientation) {
					case CGImageOrientation.UpsideDown:
						rect.x = imageWidth - rect.x - rect.width;
						rect.y = imageHeight - rect.y - rect.height;
						if (IsMirrored)
							rect.x = imageHeight - rect.x;
						break;
					case CGImageOrientation.RotatedLeft:
						temp = rect.x;
						rect.x = imageHeight - rect.y - rect.height;
						rect.y = temp;
						temp = rect.width;
						rect.width = rect.height;
						rect.height = temp;
						if (IsMirrored)
							rect.x = imageWidth - rect.x;
						break;
					case CGImageOrientation.RotatedRight:
						temp = rect.y;
						rect.y = imageWidth - rect.x - rect.width;
						rect.x = temp;
						temp = rect.width;
						rect.width = rect.height;
						rect.height = temp;
						if (IsMirrored)
							rect.x = imageWidth - rect.x;
						break;
					default:
						if (IsMirrored)
							rect.x = imageHeight - rect.x;
						break;
				}
			}

			// scale
			if (_finalScale > 1.0) {
				rect.x *= _finalScale;
				rect.y *= _finalScale;
				rect.width *= _finalScale;
				rect.height *= _finalScale;
			}

			return rect;
		}

		private Vector2 _FixPoint(Vector2 point, int imageHeight, int imageWidth, CGImageOrientation imageOrientation) {
			point.y = imageHeight - point.y;

			// rotate coords
			if (CorrectOrientation) {
				float temp;
				switch (imageOrientation) {
					case CGImageOrientation.UpsideDown:
						point.x = imageWidth - point.x;
						point.y = imageHeight - point.y;
						if (IsMirrored)
							point.x = imageHeight - point.x;
						break;
					case CGImageOrientation.RotatedLeft:
						temp = point.x;
						point.x = imageHeight - point.y;
						point.y = temp;
						if (IsMirrored)
							point.x = imageWidth - point.x;
						break;
					case CGImageOrientation.RotatedRight:
						temp = point.y;
						point.y = imageWidth - point.x;
						point.x = temp;
						if (IsMirrored)
							point.x = imageWidth - point.x;
						break;
					default:
						if (IsMirrored)
							point.x = imageHeight - point.x;
						break;
				}
			}

			// scale
			if (_finalScale > 1.0) {
				point.x *= _finalScale;
				point.y *= _finalScale;
			}

			return point;
		}
	}
}

