#if (UNITY_EDITOR || UNITY_IPHONE)

using System;


namespace U3DXT.iOS.Personal{
	/// <summary>
	/// Reminder arguments.
	/// </summary>
	public class ReminderArgs : EventArgs {

		/// <summary>
		/// The object list.
		/// </summary>
		public readonly object[] objList;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.Personal.ReminderArgs"/> class.
		/// </summary>
		/// <param name="objList">Object list.</param>
		public ReminderArgs(object[] objList) {
			this.objList = objList;
		}
	}
}

#endif
