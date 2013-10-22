using System;
using U3DXT.iOS.Native.UIKit;
using System.Collections.Generic;
using U3DXT.iOS.GUI;

//namespace U3DXT.iOS.GUI.Helpers {
//	internal class ImagePickerControllerDelegate : UIImagePickerControllerDelegate {
//
//		private static ImagePickerControllerDelegate _instance;
//		public static ImagePickerControllerDelegate instance {
//			get {
//				if (_instance == null)
//					_instance = new ImagePickerControllerDelegate();
//				return _instance;
//			}
//		}
//
//		public ImagePickerControllerDelegate() {
//		}
//
//		public override void DidFinishPickingMediaWithInfo(UIImagePickerController picker, Dictionary<object, object> info) {
//			if (picker.parentViewController != null)
//				picker.parentViewController.DismissViewController(true, null);
//			else
//				UIApplication.SharedApplication().keyWindow.rootViewController.DismissViewController(true, null);
//
//			var image = info[UIImagePickerController.OriginalImage] as UIImage;
//			GUIXT._OnPickedMedia(image);
//		}
//
//		public override void DidCancel(UIImagePickerController picker) {
//			if (picker.parentViewController != null)
//				picker.parentViewController.DismissViewController(true, null);
//			else
//				UIApplication.SharedApplication().keyWindow.rootViewController.DismissViewController(true, null);
//
//			//TODO send cancel event
//		}
//	}
//}

