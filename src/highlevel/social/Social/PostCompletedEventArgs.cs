using System;
using U3DXT;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;

namespace U3DXT.iOS.Social {
	/// <summary>
	/// Provides data for PostCompleted event.
	/// </summary>
	public class PostCompletedEventArgs : EventArgs {
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
		/// Initializes a new instance of the <see cref="U3DXT.iOS.Social.PostCompletedEventArgs"/> class.
		/// </summary>
		/// <param name='completed'>
		/// Completed.
		/// </param>
		public PostCompletedEventArgs(bool completed) {
			this.completed = completed;
//			this.Completed = this.completed;
		}
	}
}

