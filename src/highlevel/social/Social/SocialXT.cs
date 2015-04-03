#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using U3DXT.Core;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;
using U3DXT.iOS.Native.Social;
using U3DXT.iOS.Native.MessageUI;
using U3DXT.iOS.Social.Helpers;
using U3DXT.Utils;
using System.Collections.Generic;

namespace U3DXT.iOS.Social {
	/// <summary>
	/// Main class for iOS Social plugin. High-level API to share.</summary>
	/// <remarks>
	/// In iOS 6.0 and later, it is recommended to use the simple Share() method to
	/// show the native interface to share messages, images, and URLs
	/// via Facebook, Twitter, Sina Weibo, email, SMS, print, copy, save to camera roll, or
	/// assign to contact.
	/// <p></p>
	/// You can also use the Post() method to post to a specific social networking service.
	/// Or use Mail() and SMS() methods to send email and SMS.
	/// </remarks>
	public class SocialXT {

#region events
		private static EventHandler<ShareCompletedEventArgs> _shareCompletedHandlers;
		/// <summary>
		/// Occurs when a share operation has completed.
		/// </summary>
		public static event EventHandler<ShareCompletedEventArgs> ShareCompleted {
			add { _shareCompletedHandlers += value; }
			remove { _shareCompletedHandlers -= value; }
		}

		private static EventHandler<PostCompletedEventArgs> _postCompletedHandlers;
		/// <summary>
		/// Occurs when a post operation has completed.
		/// </summary>
		public static event EventHandler<PostCompletedEventArgs> PostCompleted {
			add { _postCompletedHandlers += value; }
			remove { _postCompletedHandlers -= value; }
		}

		private static EventHandler<MailCompletedEventArgs> _mailCompletedHandlers;
		/// <summary>
		/// Occurs when a mail operation has completed.
		/// </summary>
		public static event EventHandler<MailCompletedEventArgs> MailCompleted {
			add { _mailCompletedHandlers += value; }
			remove { _mailCompletedHandlers -= value; }
		}

		private static EventHandler<SMSCompletedEventArgs> _smsCompletedHandlers;
		/// <summary>
		/// Occurs when a SMS operation has completed.
		/// </summary>
		public static event EventHandler<SMSCompletedEventArgs> SMSCompleted {
			add { _smsCompletedHandlers += value; }
			remove { _smsCompletedHandlers -= value; }
		}
#endregion

#region private vars

		private static UIPopoverController _popover;

		private static UIDocumentInteractionController _documentIC;

#endregion

#region methods
		/// <summary>
		/// Shows the native UIActivityViewController to share message, images, and URLs
		/// via Facebook, Twitter, Weibo, email, SMS, print, copy, save to camera roll, or
		/// assign to contact.
		/// Raises ShareCompleted event when completed.
		/// </summary>
		/// <remarks>
		/// This is available in iOS 6.0 and later.</remarks>
		/// 
		/// <param name="items"> An array of items to share. Each item can be a string, NSURL, Texture2D, or UIImage.
		/// 			Strings starting with http:// or https:// will be automatically converted to URLs.</param>
		/// <param name="excludedActivityTypes"> An array of strings representing the activity types to exclude from sharing.
		/// 			See <see cref="UIActivity">Constants in UIActivity</see>.</param>
		public static void Share(object[] items, string[] excludedActivityTypes = null) {
			var nativeItems = new object[items.Length];
			for (int i=0; i<items.Length; i++) {
				var item = items[i];
				if (item is string) {
					string str = item as string;
					if (str.StartsWith("http://") || str.StartsWith("https://"))
						nativeItems[i] = new NSURL(str);
					else
						nativeItems[i] = str;
				}
				else if (item is Texture2D)
					nativeItems[i] = UIImage.FromTexture2D(item as Texture2D);
				else if (item is UIImage)
					nativeItems[i] = item;
				else if (item is NSURL)
					nativeItems[i] = item;
				else if (item is NSData)
					nativeItems[i] = item;
				else
					throw new U3DXTException("Unexpected item type: " + item.GetType());
			}
			
			var vc = new UIActivityViewController(nativeItems, null);
			if (vc.IsNil)
				return;

			vc.completionHandler = _activityViewCompleted;
			if (excludedActivityTypes != null)
				vc.excludedActivityTypes = excludedActivityTypes;
			
			var rootVc = UIApplication.deviceRootViewController;
			if (CoreXT.IsiPad) {
				_popover = new UIPopoverController(vc);
				
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
				rootVc.PresentViewController(vc, true, null);
			}
		}
		
		private static void _activityViewCompleted(string activityType, bool completed) {
			if (_shareCompletedHandlers != null)
				_shareCompletedHandlers(null, new ShareCompletedEventArgs(activityType, completed));
		}
		
		/// <summary>
		/// Shows the native SLComposeViewController to post a message with image and/or URL on Facebook, Twitter, or Weibo.
		/// Raises PostCompleted event when completed.</summary>
		/// <remarks>
		/// This is available in iOS 6.0 and later.
		/// </remarks>
		/// <param name="serviceType"> The service to post to. See <see cref="SLRequest">Constants in SLRequest</see>.</param>
		/// <param name="message"> The message to post or can be null.</param>
		/// <param name="image"> The image to post or can be null.</param>
		/// <param name="url"> The URL to post or can be null.</param>
		/// <param name="checkServiceAvailable"> Whether to check if the service is available first.</param>
		/// <returns> True if it is able to show the native view controller; false if the service type is not available.</returns>
		public static bool Post(string serviceType, string message, Texture2D image, String url, bool checkServiceAvailable = false) {
			UIImage uiimage = null;
			if (image != null)
				uiimage = UIImage.FromTexture2D(image);
			return Post(serviceType, message, uiimage, url, checkServiceAvailable);
		}

		/// <summary>
		/// Shows the native SLComposeViewController to post a message with image and/or URL on Facebook, Twitter, or Weibo.
		/// Raises PostCompleted event when completed.</summary>
		/// <remarks>
		/// This is available in iOS 6.0 and later.
		/// </remarks>
		/// <param name="serviceType"> The service to post to. See <see cref="SLRequest">Constants in SLRequest</see>.</param>
		/// <param name="message"> The message to post or can be null.</param>
		/// <param name="image"> The image to post or can be null.</param>
		/// <param name="url"> The URL to post or can be null.</param>
		/// <param name="checkServiceAvailable"> Whether to check if the service is available first.</param>
		/// <returns> True if it is able to show the native view controller; false if the service type is not available.</returns>
		public static bool Post(string serviceType, string message, UIImage image, String url, bool checkServiceAvailable) {
			if (checkServiceAvailable && !SLComposeViewController.IsAvailable(serviceType))
				return false;

			var vc = SLComposeViewController.ComposeViewController(serviceType);
			if (vc.IsNil)
				return false;

			vc.completionHandler = _composeViewCompleted;

			if (message != null)
				vc.SetInitialText(message);

			if (image != null)
				vc.AddImage(image);

			if (url != null)
				vc.AddURL(new NSURL(url));

			UIApplication.deviceRootViewController.PresentViewController(vc, true, null);
			return true;
		}
		
		private static void _composeViewCompleted(SLComposeViewControllerResult result) {
			if (UIApplication.deviceRootViewController != null)
				UIApplication.deviceRootViewController.DismissViewController(true, null);

			if (_postCompletedHandlers != null)
				_postCompletedHandlers(null, new PostCompletedEventArgs(result == SLComposeViewControllerResult.Done));
		}
		
		/// <summary>
		/// Shows the native MFMailComposeViewController to send an email.
		/// Raises MailCompleted event when completed.</summary>
		/// 
		/// <param name="recipients"> An array of strings representing the email addresses of the recipients.</param>
		/// <param name="subject"> The subject of the email.</param>
		/// <param name="body"> The body of the email.</param>
		/// <param name="bodyIsHTML"> True if the body is HTML; false otherwise.</param>
		/// <param name="image"> The image to attach to the email.</param>
		/// <param name="checkServiceAvailable"> Whether to check if the service is available first.</param>
		/// <returns> True if it is able to show the native view controller; false if it cannot send email.</returns>
		public static bool Mail(string[] recipients, string subject, string body, bool bodyIsHTML, Texture2D image = null, bool checkServiceAvailable = true) {
			if (checkServiceAvailable && !MFMailComposeViewController.CanSendMail())
				return false;

			var vc = new MFMailComposeViewController();
			if (vc.IsNil)
				return false;

			vc.mailComposeDelegate = MailComposeViewControllerDelegate.instance;
			vc.SetToRecipients(recipients);
			vc.SetSubject(subject);
			vc.SetMessageBody(body, bodyIsHTML);
			
			if (image != null) {
				var nsdata = NSData.FromByteArray(image.EncodeToPNG());
				vc.AddAttachmentData(nsdata, "image/png", "image.png");
			}
			
			UIApplication.deviceRootViewController.PresentViewController(vc, true, null);
			return true;
		}

		/// <summary>
		/// Shows the native MFMailComposeViewController to send an email.
		/// Raises MailCompleted event when completed.</summary>
		/// 
		/// <param name="recipients"> An array of strings representing the email addresses of the recipients.</param>
		/// <param name="subject"> The subject of the email.</param>
		/// <param name="body"> The body of the email.</param>
		/// <param name="bodyIsHTML"> True if the body is HTML; false otherwise.</param>
		/// <param name="image"> The image to attach to the email.</param>
		/// <param name="checkServiceAvailable"> Whether to check if the service is available first.</param>
		/// <returns> True if it is able to show the native view controller; false if it cannot send email.</returns>
		public static bool Mail(string[] recipients, string subject, string body, bool bodyIsHTML, UIImage image, bool checkServiceAvailable) {
			if (checkServiceAvailable && !MFMailComposeViewController.CanSendMail())
				return false;

			var vc = new MFMailComposeViewController();
			if (vc.IsNil)
				return false;

			vc.mailComposeDelegate = MailComposeViewControllerDelegate.instance;
			vc.SetToRecipients(recipients);
			vc.SetSubject(subject);
			vc.SetMessageBody(body, bodyIsHTML);

			if (image != null) {
				var nsdata = image.PNGRepresentation();
				vc.AddAttachmentData(nsdata, "image/png", "image.png");
			}

			UIApplication.deviceRootViewController.PresentViewController(vc, true, null);
			return true;
		}
		
		internal static void OnMailCompleted(MFMailComposeResult result, NSError error) {
			if (_mailCompletedHandlers != null)
				_mailCompletedHandlers(null, new MailCompletedEventArgs(result, error));
		}
		
		/// <summary>
		/// Shows the native MFMessageComposeViewController to send a SMS.
		/// Raises SMSCompleted event when completed.</summary>
		/// 
		/// <param name="recipients"> An array of strings representing the phone numbers of the recipients.</param>
		/// <param name="body"> The body of the SMS.</param>
		/// <param name="checkServiceAvailable"> Whether to check if the service is available first.</param>
		/// <returns> True if it is able to show the native view controller; false if it cannot send SMS.</returns>
		public static bool SMS(string[] recipients, string body, bool checkServiceAvailable = true) {
			if (checkServiceAvailable && !MFMessageComposeViewController.CanSendText())
				return false;
			
			var vc = new MFMessageComposeViewController();
			if (vc.IsNil)
				return false;

			vc.messageComposeDelegate = MessageComposeViewControllerDelegate.instance;
			vc.recipients = recipients;
			vc.body = body;
			
			UIApplication.deviceRootViewController.PresentViewController(vc, true, null);
			return true;
		}
		
		internal static void OnSMSCompleted(MessageComposeResult result) {
			if (_smsCompletedHandlers != null)
				_smsCompletedHandlers(null, new SMSCompletedEventArgs(result));
		}

		/// <summary>
		/// Open Instagram with the specified texture and caption.
		/// </summary>
		/// <returns><c>true</c> if Instagram is installed, <c>false</c> otherwise.</returns>
		/// <param name="texture">Texture.</param>
		/// <param name="caption">Caption.</param>
		public static bool Instagram(Texture2D texture, string caption = null) {
			UIImage image = UIImage.FromTexture2D(texture);
			return Instagram(image, caption);
		}

		/// <summary>
		/// Open Instagram with the specified image and caption.
		/// </summary>
		/// <returns><c>true</c> if Instagram is installed, <c>false</c> otherwise.</returns>
		/// <param name="image">Image.</param>
		/// <param name="caption">Caption.</param>
		public static bool Instagram(UIImage image, string caption = null) {
			if (!UIApplication.SharedApplication().CanOpenURL(new NSURL("instagram://app")))
				return false;

			// write image to tmp folder
			NSData data = image.JPEGRepresentation(1f);
			string filePath = Application.temporaryCachePath + "/" + UUID.Generate() + ".igo";
			data.WriteToFile(filePath, true);

			_documentIC = UIDocumentInteractionController.InteractionController(new NSURL(filePath, false));
			_documentIC.UTI = "com.instagram.exclusivegram";
			if (caption != null) {
				var annotation = new Dictionary<object, object>();
				annotation["InstagramCaption"] = caption;
				_documentIC.annotation = annotation;
			}

			var rootView = UIApplication.deviceRootViewController.view;
			_documentIC.PresentOpenInMenu(new Rect(0, 0, 1, 1), rootView, true);
			return true;
		}

#endregion
	}
}

#endif
