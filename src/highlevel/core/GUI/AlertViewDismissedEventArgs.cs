#if (UNITY_EDITOR || UNITY_IPHONE)

using System;

namespace U3DXT.iOS.GUI {
	/// <summary>
	/// Alert view dismissed event arguments.
	/// </summary>
	public class AlertViewDismissedEventArgs : EventArgs {

		/// <summary>
		/// The index of the selected button.
		/// </summary>
		public readonly int selectedButtonIndex;

		/// <summary>
		/// The selected button title.
		/// </summary>
		public readonly string selectedButtonTitle;

		/// <summary>
		/// The first input string.
		/// </summary>
		public readonly string inputString1;

		/// <summary>
		/// The second input string.
		/// </summary>
		public readonly string inputString2;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.GUI.AlertViewDismissedEventArgs"/> class.
		/// </summary>
		/// <param name="selectedButtonIndex">Selected button index.</param>
		/// <param name="selectedButtonTitle">Selected button title.</param>
		/// <param name="inputString1">First input string.</param>
		/// <param name="inputString2">Second input string.</param>
		public AlertViewDismissedEventArgs(int selectedButtonIndex, string selectedButtonTitle, string inputString1, string inputString2) {
			this.selectedButtonIndex = selectedButtonIndex;
			this.selectedButtonTitle = selectedButtonTitle;
			this.inputString1 = inputString1;
			this.inputString2 = inputString2;
		}
	}
}

#endif
