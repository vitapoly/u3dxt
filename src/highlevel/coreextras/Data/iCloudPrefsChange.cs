#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.Foundation;

namespace U3DXT.iOS.Data {
	/// <summary>
	/// iCloudPrefs change.
	/// </summary>
	public class iCloudPrefsChange {

		/// <summary>
		/// The key.
		/// </summary>
		public readonly string key;

		/// <summary>
		/// The new value.
		/// </summary>
		public readonly object newValue;

		/// <summary>
		/// The old value.
		/// </summary>
		public readonly object oldValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.Data.iCloudPrefsChange"/> class.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="newValue">New value.</param>
		/// <param name="oldValue">Old value.</param>
		public iCloudPrefsChange(string key, object newValue, object oldValue) {
			this.key = key;
			this.newValue = newValue;
			this.oldValue = oldValue;
		}
	}
}

#endif
