using System;
using U3DXT;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;
using U3DXT.iOS.Native.MessageUI;

namespace U3DXT.iOS.Social {
	/// <summary>
	/// Provides data for MailCompleted event.
	/// </summary>
	public class MailCompletedEventArgs : EventArgs {
		/// <summary>
		/// The result.
		/// </summary>
		public readonly MFMailComposeResult result;

//		/// <summary>
//		/// The result.
//		/// </summary>
//		[Obsolete("Use the result property.")]
//		public readonly MFMailComposeResult Result;

		/// <summary>
		/// The error if any, or null.
		/// </summary>
		public readonly NSError error;

//		/// <summary>
//		/// The error if any, or null.
//		/// </summary>
//		[Obsolete("Use the error property.")]
//		public readonly NSError Error;

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
		/// Initializes a new instance of the <see cref="U3DXT.iOS.Social.MailCompletedEventArgs"/> class.
		/// </summary>
		/// <param name='result'>
		/// Result.
		/// </param>
		/// <param name='error'>
		/// Error.
		/// </param>
		public MailCompletedEventArgs(MFMailComposeResult result, NSError error) {
			this.result = result;
			this.error = error;
			this.completed = (result == MFMailComposeResult.Sent);
//			this.Result = result;
//			this.Error = error;
//			this.Completed = (result == MFMailComposeResult.Sent);
		}
	}
}

