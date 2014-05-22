#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.CoreImage;
using System.Collections.Generic;
using U3DXT.iOS.Native.CoreGraphics;
using UnityEngine;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using U3DXT.Core;
using System.Threading;

namespace U3DXT.iOS.CoreImage {
	/// <summary>
	/// The FaceDetector class detects faces in images.
	/// <p></p>
	/// To detect faces in an image, use the DetectInImage() method passing in a Texture2D object
	/// and the orientation of the image. If passing in an UIImage, the orientation is taken from the UIImage itself.
	/// The returned value is an array of Face objects. This method blocks until the native face detection API returns.
	/// <p></p>
	/// <strong>Note on Performance:</strong> Face detection is very processor-intensive, and each detection may take
	/// longer time than allocated to a frame depending on detector settings and device, and your app will appear to be frozen or slow down.
	/// If you are detecting faces in real-time, there are a few settings in this class you can adjust.
	/// The following are recommended:
	/// <ul>
	/// <li>When creating an instance of FaceDetector, pass in false to useHighAccuracy and trackFaces.</li>
	/// <li>The preprocessImageScale property specifies how much to scale down the image before doing face detection.
	///     The smaller the number, the faster it will be, but also less accurate.</li>
	/// <li>When using Unity's WebcamTexture, request a smaller resolution.</li>
	/// </ul>
	/// <p></p>
	/// If you are detecting faces in images and do not need to be real-time, you can adjust these
	/// settings to the opposite extent for better results.
	/// </summary>
	public class FaceDetector {

		private CIDetector _detector;
		private Dictionary<object, object> _imageOpts = new Dictionary<object, object>();

		/// <summary>
		/// This property specifies how much to scale down the image before doing face detection.
		/// It should be between 0 and 1.
		/// The smaller the number, the faster the detection, but also less accurate.
		/// </summary>
		public float preprocessImageScale = 0.125f;

		/// <summary>
		/// This property specifies how much the detected result coordinates should scale up or down.
		/// For example, if the input image and the display have different sizes, you need to set
		/// this property to be (display size)/(input size).  You can then use the results directly
		/// in your display.
		/// </summary>
		public float projectedScale = 1.0f;
		private float _finalScale;

		/// <summary>
		/// Indicates whether the detected results should be in default orientation.
		/// If false, the results will be in the same coordinates as the input image.
		/// </summary>
		public bool correctOrientation = true;

		/// <summary>
		/// Indicates whether the image is mirrored.
		/// </summary>
		public bool isMirrored = false;

		private bool detectSmiles = false;
		private bool detectBlinks = false;
		
//		private static NSOperationQueue _opQueue;

		/// <summary>
		/// Creates an instance of FaceDetector. Once created, the two arguments cannot be changed for this instance.
		/// If using high accuracy, the preprocessImageScale property is set to 1.0, otherwise defaults to 0.125.
		/// </summary>
		/// <param name="useHighAccuracy">Thether to use high accuracy for detection.</param>
		/// <param name="trackFaces">Whether to track faces in live video or successive image detections; available ONLY in iOS 6.0 and later.</param>
		/// <param name="detectSmiles">Whether to detect smiles; available ONLY in iOS 7.0 and later.</param>
		/// <param name="detectBlinks">Whether to detect blinks; available ONLY in iOS 7.0 and later.</param>
		public FaceDetector(bool useHighAccuracy = false, bool trackFaces = false, bool detectSmiles = false, bool detectBlinks = false) {
			// setup face detector
			var context = CIContext.Context((Dictionary<object, object>)null);
			var opts = new Dictionary<object, object>();
			opts[CIDetector.Accuracy] = useHighAccuracy ? CIDetector.AccuracyHigh : CIDetector.AccuracyLow;
			opts[CIDetector.Tracking] = trackFaces;
			
			this.detectSmiles = detectSmiles;
			this.detectBlinks = detectBlinks;

			_detector = CIDetector.DetectorOfType(CIDetector.TypeFace, context, opts);

			if (useHighAccuracy)
				preprocessImageScale = 1.0f;
		}

		/// <summary>
		/// Detects the in image.
		/// </summary>
		/// <returns>The in image.</returns>
		/// <param name="image">Image.</param>
		public Face[] DetectInImage(UIImage image) {
			return DetectInImage(new CIImage(image), image.imageOrientation.ToCGImageOrientation());
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
			var cgimage = CGImage.FromTexture2D(image, preprocessImageScale);
			var ciimage = new CIImage(cgimage);
			return DetectInImage(ciimage, imageOrientation);
		}

		/// <summary>
		/// Detects the in image.
		/// </summary>
		/// <returns>The in image.</returns>
		/// <param name="data">Pixel data.</param>
		/// <param name="width">Width of image.</param>
		/// <param name="height">Height of image.</param>
		/// <param name="imageOrientation">Image orientation.</param>
		public Face[] DetectInPixels32(Color32[] data, int width, int height, CGImageOrientation imageOrientation = CGImageOrientation.Default) {
			var cgimage = CGImage.FromPixels32(data, width, height, preprocessImageScale);
			var ciimage = new CIImage(cgimage);
			return DetectInImage(ciimage, imageOrientation);
		}

/*		
		public void DetectInPixels32Async(Action<Face[]> callback, Color32[] data, int width, int height, CGImageOrientation imageOrientation = CGImageOrientation.Default) {
			if (_opQueue == null)
				_opQueue = new NSOperationQueue();
			
			var cgimage = CGImage.FromPixels32(data, width, height, preprocessImageScale);
			data = null;
			
			_opQueue.AddOperation(delegate() {
				var ciimage = new CIImage(cgimage);
				cgimage = null;
				Face[] faces = DetectInImage(ciimage, imageOrientation);
				ciimage = null;
				
				CoreXT.RunOnMainThread(delegate() {
					callback(faces);
					faces = null;
				});
			});
		}
*/
			
		/// <summary>
		/// Detects the in image.
		/// </summary>
		/// <returns>The in image.</returns>
		/// <param name="ciimage">Ciimage.</param>
		/// <param name="imageOrientation">Image orientation.</param>
		public Face[] DetectInImage(CIImage ciimage, CGImageOrientation imageOrientation = CGImageOrientation.Default) {
			var rect = ciimage.Extent();
			int imageHeight = (int)rect.height;
			int imageWidth = (int)rect.width;
			_finalScale = projectedScale / preprocessImageScale;

			// options
			_imageOpts[CIDetector.ImageOrientation] = (int)imageOrientation;
			_imageOpts[CIDetector.Smile] = detectSmiles;
			_imageOpts[CIDetector.EyeBlink] = detectBlinks;

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
				
				if (feature.RespondsToSelector("faceAngle")) {
					if (feature.hasFaceAngle) {
						face.hasFaceAngle = true;
						face.faceAngle = feature.faceAngle;
					}
					
					face.hasSmile = feature.hasSmile;
					face.leftEyeClosed = feature.leftEyeClosed;
					face.rightEyeClosed = feature.rightEyeClosed;
				}

				faces[i] = face;
			}

			return faces;
		}

		private Rect _FixRect(Rect rect, int imageHeight, int imageWidth, CGImageOrientation imageOrientation) {
			rect.y = imageHeight - rect.y - rect.height;

			// rotate coords
			if (correctOrientation) {
				float temp;
				switch (imageOrientation) {
					case CGImageOrientation.UpsideDown:
						rect.x = imageWidth - rect.x - rect.width;
						rect.y = imageHeight - rect.y - rect.height;
						if (isMirrored)
							rect.x = imageWidth - rect.x - rect.width;
						break;
					case CGImageOrientation.RotatedLeft:
						temp = rect.x;
						rect.x = imageHeight - rect.y - rect.height;
						rect.y = temp;
						temp = rect.width;
						rect.width = rect.height;
						rect.height = temp;
						if (isMirrored)
							rect.x = imageHeight - rect.x - rect.width;
						break;
					case CGImageOrientation.RotatedRight:
						temp = rect.y;
						rect.y = imageWidth - rect.x - rect.width;
						rect.x = temp;
						temp = rect.width;
						rect.width = rect.height;
						rect.height = temp;
						if (isMirrored)
							rect.x = imageHeight - rect.x - rect.width;
						break;
					default:
						if (isMirrored)
							rect.x = imageWidth - rect.x - rect.width;
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
			if (correctOrientation) {
				float temp;
				switch (imageOrientation) {
					case CGImageOrientation.UpsideDown:
						point.x = imageWidth - point.x;
						point.y = imageHeight - point.y;
						if (isMirrored)
							point.x = imageWidth - point.x;
						break;
					case CGImageOrientation.RotatedLeft:
						temp = point.x;
						point.x = imageHeight - point.y;
						point.y = temp;
						if (isMirrored)
							point.x = imageHeight - point.x;
						break;
					case CGImageOrientation.RotatedRight:
						temp = point.y;
						point.y = imageWidth - point.x;
						point.x = temp;
						if (isMirrored)
							point.x = imageHeight - point.x;
						break;
					default:
						if (isMirrored)
							point.x = imageWidth - point.x;
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

#endif
