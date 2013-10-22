using System;
using U3DXT;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;

namespace U3DXT.iOS.Social {
	/// <summary>
	/// Provides data for ShareCompleted event.
	/// </summary>
	public class ShareCompletedEventArgs : EventArgs {
		/// <summary>
		/// The type of the activity.
		/// See <see cref="UIActivity">Constants in UIActivity</see>.
		/// </summary>
		public readonly string activityType;
		
//		/// <summary>
//		/// The type of the activity.
//		/// See <see cref="UIActivity">Constants in UIActivity</see>.
//		/// </summary>
//		[Obsolete("Use the activityType property.")]
//		public readonly string ActivityType;

		/// <summary>
		/// Whether the operation was completed.
		/// </summary>
		public readonly bool completed;
		
//		/// <summary>
//		/// Whether the operation was completed.
//		/// </summary>
//		[Obsolete("Use the completed property.")]
//		public readonly bool Completed;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.Social.ShareCompletedEventArgs"/> class.
		/// </summary>
		/// <param name='activityType'>
		/// Activity type.
		/// </param>
		/// <param name='completed'>
		/// Completed.
		/// </param>
		public ShareCompletedEventArgs(string activityType, bool completed) {
			this.activityType = activityType;
			this.completed = completed;
//			this.ActivityType = activityType;
//			this.Completed = completed;
		}
	}
}

