#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;
using U3DXT.iOS.Native.MessageUI;

namespace U3DXT.iOS.Social {
	/// <summary>
	/// Provides data for SMSCompleted event.
	/// </summary>
	public class SMSCompletedEventArgs : EventArgs {
		/// <summary>
		/// The result.
		/// </summary>
		public readonly MessageComposeResult result;
		
//		/// <summary>
//		/// The result.
//		/// </summary>
//		[Obsolete("Use the result property.")]
//		public readonly MessageComposeResult Result;

		/// <summary>
		/// Whether the operation was completed.
		/// </summary>
		public readonly bool completed;
		
//		/// <summary>
//		/// Whether the operation was completed.
//		/// </summary>
//		[Obsolete("use the completed property.")]
//		public readonly bool Completed;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.Social.SMSCompletedEventArgs"/> class.
		/// </summary>
		/// <param name='result'>
		/// Result.
		/// </param>
		public SMSCompletedEventArgs(MessageComposeResult result) {
			this.result = result;
			this.completed = (result == MessageComposeResult.Sent);
//			this.Result = result;
//			this.Completed = (result == MessageComposeResult.Sent);
		}
	}
}

#endif
