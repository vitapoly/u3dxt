#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using U3DXT.Core;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;
using U3DXT.Utils;
using U3DXT.iOS.Native.MediaPlayer;

namespace U3DXT.iOS.MediaPlayer {
	/// <summary>
	/// Main class for iOS MediaPlayer plugin. High-level API.</summary>
	/// <remarks>
	/// </remarks>
	public class MediaPlayerXT {

#region events

//		private static EventHandler<InitializationEventArgs> _initializationCompletedHandlers;
//		/// <summary>
//		/// Occurs when initialization has completed.
//		/// </summary>
//		public static event EventHandler<InitializationEventArgs> InitializationCompleted {
//			add { _initializationCompletedHandlers += value; }
//			remove { _initializationCompletedHandlers -= value; }
//		}

#endregion

#region vars
		private static MPMoviePlayerViewController _playerVC = null;
//		private static MPMoviePlayerController _player = null;
		private static NSURL contentURL = null;
#endregion

#region methods


		/// <summary>
		/// Plays the streaming fullscreen movie.
		/// </summary>
		/// <param name="url">URL.</param>
		/// <param name="autoPlay">If set to <c>true</c> auto play.</param>
		/// <param name="style">Style.</param>
		public static void PlayStreamingFullscreenMovie(string url,
		                                                bool autoPlay = true, 
		                                                MPMovieControlStyle style=MPMovieControlStyle.Fullscreen)
		{
			contentURL = new NSURL(url);

			if (_playerVC == null) {
				_playerVC = new MPMoviePlayerViewController();

				_playerVC.moviePlayer.movieSourceType = MPMovieSourceType.Streaming;
				_playerVC.moviePlayer.contentURL = contentURL;
		//		_playerVC.moviePlayer.SetFullscreen(true, false);
				_playerVC.moviePlayer.shouldAutoplay = autoPlay;


				_playerVC.moviePlayer.controlStyle = style;


				_playerVC.moviePlayer.LoadStateDidChange += _defaultMoviePlayerNotificationHandler;
				_playerVC.moviePlayer.DidExitFullscreen += _defaultMoviePlayerExitFullscreen;
				_playerVC.moviePlayer.PlaybackDidFinish += _defaultMoviePlayerFinishHandler;


				_playerVC.moviePlayer.PrepareToPlay();

			} else {
				_playerVC.moviePlayer.contentURL = contentURL;
				_playerVC.moviePlayer.PrepareToPlay();
			}

//			UIApplication.deviceRootViewController.AddChildViewController(_playerVC);
			UIApplication.deviceRootViewController.PresentViewController(_playerVC, false, null);

		}

		/// <summary>
		/// Closes the movie player.
		/// </summary>
		public static void CloseMoviePlayer() {

			if (_playerVC != null){
				_playerVC.DismissViewController(false, null);
			}

		}

		private static void _defaultMoviePlayerNotificationHandler(object sender, NSNotificationEventArgs e) 
		{
			if (_playerVC.moviePlayer.loadState == MPMovieLoadState.Playable) {
				Debug.Log("PLAY MOVIE");
				_playerVC.moviePlayer.Play();
			}

		}

		private static void _defaultMoviePlayerFinishHandler(object sender, NSNotificationEventArgs e)
		{
				CloseMoviePlayer();

	
		}

		private static void _defaultMoviePlayerExitFullscreen(object sender, NSNotificationEventArgs e)
		{
			_playerVC.moviePlayer.Stop();
		}

#endregion
	}
}

#endif
