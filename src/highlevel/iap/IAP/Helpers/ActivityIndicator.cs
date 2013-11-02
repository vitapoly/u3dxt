#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.UIKit;
using UnityEngine;

namespace U3DXT.iOS.IAP.Helpers {
	internal class ActivityIndicator {

		private UIView _activityView = null;
		private UIActivityIndicatorView _activityIndicator = null;
		private UILabel _activityLabel = null;

		public ActivityIndicator() {
		}

		public void Show(string text) {
			// figure out parent
			UIView parentView = null;
			if ((UIApplication.SharedApplication().keyWindow.rootViewController != null)
			    && (UIApplication.SharedApplication().keyWindow.rootViewController.view != null))
				parentView = UIApplication.SharedApplication().keyWindow.rootViewController.view;
			else
				parentView = UIApplication.SharedApplication().keyWindow;

			var frame = parentView.bounds;

			// create view
			if (_activityView == null) {
				_activityView = new UIView(frame);
				_activityView.alpha = 0.8f;
				_activityView.backgroundColor = UIColor.BlackColor();

				_activityIndicator = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.WhiteLarge);
				_activityIndicator.hidesWhenStopped = false;
				_activityView.AddSubview(_activityIndicator);

				_activityLabel = new UILabel(frame);
				_activityLabel.textAlignment = NSTextAlignment.Center;
				_activityLabel.backgroundColor = UIColor.ClearColor();
				_activityLabel.textColor = UIColor.WhiteColor();
				_activityLabel.numberOfLines = 2;
				_activityView.AddSubview(_activityLabel);					
			}

			// re-set frames
			_activityView.frame = frame;

			var indiFrame = _activityIndicator.frame;
			indiFrame.x = frame.width / 2 - indiFrame.width / 2;
			indiFrame.y = frame.height / 2 - indiFrame.height / 2;
			_activityIndicator.frame = indiFrame;

			var labelFrame = new Rect(0, indiFrame.yMax, frame.width, indiFrame.height * 2);
			_activityLabel.frame = labelFrame;

			_activityLabel.text = text;

			// add it to parent
			if (_activityView.superview == null) {
				_activityIndicator.StartAnimating();
				parentView.AddSubview(_activityView);
			}
		}

		public void Hide() {
			if ((_activityView != null) && (_activityView.superview != null)) {
				_activityView.RemoveFromSuperview();
				_activityIndicator.StopAnimating();
			}
		}
	}
}

#endif
