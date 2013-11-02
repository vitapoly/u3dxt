#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.Foundation;

namespace U3DXT.iOS.Data {
	/// <summary>
	/// iCloudPrefs changed event arguments.
	/// </summary>
	public class iCloudPrefsChangedEventArgs : EventArgs {

		/// <summary>
		/// The reason.
		/// </summary>
		public readonly NSUbiquitousKeyValueStoreChangeReasonValues reason;

		/// <summary>
		/// The changes.
		/// </summary>
		public readonly iCloudPrefsChange[] changes;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.Data.iCloudPrefsChangedEventArgs"/> class.
		/// </summary>
		/// <param name="reason">Reason.</param>
		/// <param name="changes">Changes.</param>
		public iCloudPrefsChangedEventArgs(NSUbiquitousKeyValueStoreChangeReasonValues reason, iCloudPrefsChange[] changes) {
			this.reason = reason;
			this.changes = changes;
		}
	}
}

#endif
