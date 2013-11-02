#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
//using U3DXT.iOS.GUI.Helpers;
using U3DXT.Core;

namespace U3DXT.iOS.GUI {
	/// <summary>
	/// High-level API for GUI.
	/// </summary>
	public class GUIXT {

		private static EventHandler<MediaPickedEventArgs> _mediaPickedHandlers;
		/// <summary>
		/// Occurs when an image picker operation has completed.
		/// </summary>
		public static event EventHandler<MediaPickedEventArgs> MediaPicked {
			add { _mediaPickedHandlers += value; }
			remove { _mediaPickedHandlers -= value; }
		}

		private static EventHandler<EventArgs> _mediaPickCancelledHandlers;
		/// <summary>
		/// Occurs when an image picker operation has cancelled.
		/// </summary>
		public static event EventHandler<EventArgs> MediaPickCancelled {
			add { _mediaPickCancelledHandlers += value; }
			remove { _mediaPickCancelledHandlers -= value; }
		}

		private static UIImagePickerController _picker;
		private static UIPopoverController _popover;

		/// <summary>
		/// Shows the image picker.
		/// </summary>
		public static void ShowImagePicker(UIImagePickerControllerSourceType source = UIImagePickerControllerSourceType.PhotoLibrary) {
			_picker = new UIImagePickerController();
//			_picker.AllowsEditing = true;
			_picker.sourceType = source;
//			_picker.Delegate = ImagePickerControllerDelegate.instance;
			_picker.DidFinishPickingMediaWithInfo += _OnPickedMedia;
			_picker.DidCancel += _OnCancelledPick;

			var rootVc = UIApplication.SharedApplication().keyWindow.rootViewController;
			if (CoreXT.IsiPad && (source != UIImagePickerControllerSourceType.Camera)) {
				if (_popover == null)
					_popover = new UIPopoverController(_picker);
				else
					_popover.contentViewController = _picker;

				var rect = rootVc.view.bounds;
				rect.x = rect.width / 2;
				rect.y = rect.height;
				rect.width = 1;
				rect.height = 1;
				_popover.PresentPopover(
					rect,
					rootVc.view,
					UIPopoverArrowDirection.Down,
					true);
			} else {
				rootVc.PresentViewController(_picker, true, null);
			}
		}

		private static void _OnPickedMedia(object sender, UIImagePickerController.DidFinishPickingMediaWithInfoEventArgs e) {
			if (_popover != null)
				_popover.DismissPopover(true);
			else if (_picker.parentViewController != null)
				_picker.parentViewController.DismissViewController(true, null);
			else
				UIApplication.SharedApplication().keyWindow.rootViewController.DismissViewController(true, null);

			_picker = null;
			_popover = null;

			var image = e.info[UIImagePickerController.OriginalImage] as UIImage;

			if (_mediaPickedHandlers != null)
				_mediaPickedHandlers(null, new MediaPickedEventArgs(image));
		}

		private static void _OnCancelledPick(object sender, EventArgs e) {
			if (_picker.parentViewController != null)
				_picker.parentViewController.DismissViewController(true, null);
			else
				UIApplication.SharedApplication().keyWindow.rootViewController.DismissViewController(true, null);
			_picker = null;
			_popover = null;

			if (_mediaPickCancelledHandlers != null)
				_mediaPickCancelledHandlers(null, EventArgs.Empty);
		}

		/// <summary>
		/// Returns the localized string.
		/// </summary>
		/// <returns>The localized string.</returns>
		/// <param name="key">Key.</param>
		/// <param name="defaultValue">Default value if key does not exist.</param>
		/// <param name="file">File.</param>
		public static string GetLocalizedString(string key, string defaultValue, string file) {
			return NSBundle.MainBundle().LocalizedString(key, defaultValue, file);
		}

		/// <summary>
		/// Returns the localized string.
		/// </summary>
		/// <returns>The localized string.</returns>
		/// <param name="key">Key.</param>
		public static string GetLocalizedString(string key) {
			return GetLocalizedString(key, null, null);
		}


		private static EventHandler<AlertViewDismissedEventArgs> _alertViewDismissedHandlers;
		/// <summary>
		/// Occurs when an alert view is dismissed.
		/// </summary>
		public static event EventHandler<AlertViewDismissedEventArgs> AlertDismissed {
			add { _alertViewDismissedHandlers += value; }
			remove { _alertViewDismissedHandlers -= value; }
		}

		private static UIAlertView _alertView;

		/// <summary>
		/// Shows a native alert view.
		/// </summary>
		/// <param name="title">Title.</param>
		/// <param name="message">Message.</param>
		/// <param name="cancelButton">Title of the cancel button or null if no cancel button.</param>
		/// <param name="otherButtons">Titles of other buttons or null if no other buttons.</param>
		public static void ShowAlert(string title, string message, string cancelButton, string[] otherButtons) {
			_alertView = new UIAlertView(title, message, null, cancelButton, null);
			if (otherButtons != null) {
				foreach (var button in otherButtons) {
					_alertView.AddButton(button);
				}
			}

			_alertView.DidDismissWithButtonIndex += _AlertViewDismissed;
			_alertView.Show();
		}

		/// <summary>
		/// Shows a native alert view.
		/// </summary>
		/// <param name="title">Title.</param>
		/// <param name="message">Message.</param>
		/// <param name="cancelButton">Title of the cancel button or null if no cancel button.</param>
		/// <param name="otherButtons">Titles of other buttons or null if no other buttons.</param>
		/// <param name="style">Style of the alert.</param>
		public static void ShowAlert(string title, string message, string cancelButton, string[] otherButtons, UIAlertViewStyle style) {
			_alertView = new UIAlertView(title, message, null, cancelButton, null);
			_alertView.alertViewStyle = style;
			if (otherButtons != null) {
				foreach (var button in otherButtons) {
					_alertView.AddButton(button);
				}
			}

			_alertView.DidDismissWithButtonIndex += _AlertViewDismissed;
			_alertView.Show();
		}

		private static void _AlertViewDismissed(object sender, UIAlertView.DidDismissWithButtonIndexEventArgs e) {

			if (_alertViewDismissedHandlers != null) {
				UIAlertView alertView = sender as UIAlertView;
				string input1 = null;
				string input2 = null;
				if ((alertView.alertViewStyle == UIAlertViewStyle.PlainTextInput) || (alertView.alertViewStyle == UIAlertViewStyle.SecureTextInput)) {
					input1 = alertView.TextFieldAtIndex(0).text;
				} else if (alertView.alertViewStyle == UIAlertViewStyle.LoginAndPasswordInput) {
					input1 = alertView.TextFieldAtIndex(0).text;
					input2 = alertView.TextFieldAtIndex(1).text;
				}

				_alertViewDismissedHandlers(null, new AlertViewDismissedEventArgs(e.buttonIndex, alertView.ButtonTitleAtIndex(e.buttonIndex), input1, input2));
			}

			_alertView = null;
		}

		private GUIXT() {
		}
	}
}

#endif
