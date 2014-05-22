#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using UnityEngine;

namespace U3DXT.iOS.CoreImage {

	/// <summary>
	/// A Face object contains info on the location and size of face features in an image.
	/// This is returned by FaceDetector.DetectInImage().
	/// </summary>
	public class Face {

		/// <summary>
		/// The location and size of the face.
		/// </summary>
		/// <value>The bounds.</value>
		public Rect bounds {
			get;
			internal set;
		}

		/// <summary>
		/// The position of the mouth.
		/// </summary>
		/// <value>The mouth position.</value>
		public Vector2 mouthPosition {
			get;
			internal set;
		}

		/// <summary>
		/// Indicates whether the mouth is detected.
		/// </summary>
		/// <value><c>true</c> if the mouth is detected; otherwise, <c>false</c>.</value>
		public bool hasMouthPosition {
			get;
			internal set;
		}

		/// <summary>
		/// The position of the left eye.
		/// </summary>
		/// <value>The left eye position.</value>
		public Vector2 leftEyePosition {
			get;
			internal set;
		}

		/// <summary>
		/// Indicates whether the left eye is detected.
		/// </summary>
		/// <value><c>true</c> if the left eye is detected; otherwise, <c>false</c>.</value>
		public bool hasLeftEyePosition {
			get;
			internal set;
		}

		/// <summary>
		/// The position of the right eye.
		/// </summary>
		/// <value>The right eye position.</value>
		public Vector2 rightEyePosition {
			get;
			internal set;
		}

		/// <summary>
		/// Indicates whether the right eye is detected.
		/// </summary>
		/// <value><c>true</c> if the right eye is detected; otherwise, <c>false</c>.</value>
		public bool hasRightEyePosition {
			get;
			internal set;
		}

		/// <summary>
		/// The tracking ID of this face.
		/// </summary>
		/// <value>The tracking ID.</value>
		public int trackingID {
			get;
			internal set;
		}

		/// <summary>
		/// Indicates whether the face has a tracking ID.
		/// </summary>
		/// <value><c>true</c> if the face has a tracking ID; otherwise, <c>false</c>.</value>
		public bool hasTrackingID {
			get;
			internal set;
		}

		/// <summary>
		/// The tracking frame count of this face.
		/// </summary>
		/// <value>The tracking frame count.</value>
		public int trackingFrameCount {
			get;
			internal set;
		}

		/// <summary>
		/// Indicates whether the face has a tracking frame count.
		/// </summary>
		/// <value><c>true</c> if the face has a tracking frame count; otherwise, <c>false</c>.</value>
		public bool hasTrackingFrameCount {
			get;
			internal set;
		}
		
		/// <summary>
		/// The rotation of the face in radians. Available in iOS 7.0 and later.
		/// </summary>
		/// <value>
		/// The face angle.
		/// </value>
		public float faceAngle {
			get;
			internal set;
		}
		
		/// <summary>
		/// Indicates whether this <see cref="U3DXT.iOS.CoreImage.Face"/> has face angle. Available in iOS 7.0 and later.
		/// </summary>
		/// <value>
		/// <c>true</c> if has face angle; otherwise, <c>false</c>.
		/// </value>
		public bool hasFaceAngle {
			get;
			internal set;
		}
		
		/// <summary>
		/// Indicates whether this <see cref="U3DXT.iOS.CoreImage.Face"/> has a smile. Available in iOS 7.0 and later.
		/// </summary>
		/// <value>
		/// <c>true</c> if has a smile; otherwise, <c>false</c>.
		/// </value>
		public bool hasSmile {
			get;
			internal set;
		}
		
		/// <summary>
		/// Indicates whether this <see cref="U3DXT.iOS.CoreImage.Face"/>'s left eye is closed. Available in iOS 7.0 and later.
		/// </summary>
		/// <value>
		/// <c>true</c> if left eye is closed; otherwise, <c>false</c>.
		/// </value>
		public bool leftEyeClosed {
			get;
			internal set;
		}
		
		/// <summary>
		/// Indicates whether this <see cref="U3DXT.iOS.CoreImage.Face"/>'s right eye is closed. Available in iOS 7.0 and later.
		/// </summary>
		/// <value>
		/// <c>true</c> if right eye is closed; otherwise, <c>false</c>.
		/// </value>
		public bool rightEyeClosed {
			get;
			internal set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.CoreImage.Face"/> class.
		/// </summary>
		public Face() {
		}
	}
}

#endif
