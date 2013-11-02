#if (UNITY_EDITOR || UNITY_IPHONE)

using System;


namespace U3DXT.iOS.Personal{
	/// <summary>
	/// Granted arguments.
	/// </summary>
	public class GrantedEventArgs : EventArgs {

		/// <summary>
		/// The granted.
		/// </summary>
		public readonly bool granted;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.Personal.GrantedEventArgs"/> class.
		/// </summary>
		/// <param name="granted">If set to <c>true</c> granted.</param>
		public GrantedEventArgs(bool granted) {
			this.granted = granted;
		}
	}
}

#endif
