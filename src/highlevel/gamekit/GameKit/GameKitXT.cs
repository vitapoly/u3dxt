#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using U3DXT.Core;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;
using U3DXT.Utils;
using U3DXT.iOS.Native.GameKit;
using U3DXT.iOS.Internals;
using U3DXT.iOS.GameKit.Helpers;

namespace U3DXT.iOS.GameKit {
	/// <summary>
	/// Main class for iOS GameKit plugin. High-level API.</summary>
	/// <remarks>
	/// </remarks>
	public class GameKitXT {

#region events

		private static EventHandler<EventArgs> _localPlayerAuthenticatedHandlers;
		/// <summary>
		/// Occurs when the local player has successfully authenticated into Game Center.
		/// </summary>
		public static event EventHandler<EventArgs> LocalPlayerAuthenticated {
			add { _localPlayerAuthenticatedHandlers += value; }
			remove { _localPlayerAuthenticatedHandlers -= value; }
		}

		private static EventHandler<U3DXTErrorEventArgs> _localPlayerAuthenticationFailedHandlers;
		/// <summary>
		/// Occurs when the local player has cancelled authentication into Game Center.
		/// </summary>
		public static event EventHandler<U3DXTErrorEventArgs> LocalPlayerAuthenticationFailed {
			add { _localPlayerAuthenticationFailedHandlers += value; }
			remove { _localPlayerAuthenticationFailedHandlers -= value; }
		}

		private static EventHandler<EventArgs> _scoreReportedHandlers;
		/// <summary>
		/// Occurs when the a score has successfully been reported.
		/// </summary>
		public static event EventHandler<EventArgs> ScoreReported {
			add { _scoreReportedHandlers += value; }
			remove { _scoreReportedHandlers -= value; }
		}

		private static EventHandler<U3DXTErrorEventArgs> _scoreReportFailedHandlers;
		/// <summary>
		/// Occurs when the a score failed to report.
		/// </summary>
		public static event EventHandler<U3DXTErrorEventArgs> ScoreReportFailed {
			add { _scoreReportFailedHandlers += value; }
			remove { _scoreReportFailedHandlers -= value; }
		}

		private static EventHandler<EventArgs> _achievementReportedHandlers;
		/// <summary>
		/// Occurs when the an achievement has successfully been reported.
		/// </summary>
		public static event EventHandler<EventArgs> AchievementReported {
			add { _achievementReportedHandlers += value; }
			remove { _achievementReportedHandlers -= value; }
		}

		private static EventHandler<U3DXTErrorEventArgs> _achievementReportFailedHandlers;
		/// <summary>
		/// Occurs when the an achievement failed to report.
		/// </summary>
		public static event EventHandler<U3DXTErrorEventArgs> AchievementReportFailed {
			add { _achievementReportFailedHandlers += value; }
			remove { _achievementReportFailedHandlers -= value; }
		}

#endregion

#region vars

		private static LocalPlayer _localPlayer;

#endregion

#region methods

		/// <summary>
		/// Authenticates the Game Center local player. Call this as soon as the game is initialized.
		/// <br></br>
		/// Raises the LocalPlayerAuthenticated and LocalPlayerAuthencationFailed events on completion.
		/// <br></br>
		/// Once the local player is authenticated, you can get the <c>LocalPlayer</c> property.
		/// </summary>
		public static void AuthenticateLocalPlayer() {
			var gkLocalPlayer = GKLocalPlayer.LocalPlayer();

			if (gkLocalPlayer.RespondsToSelector("authenticateHandler")) {
				// ios 6.0 new call
				gkLocalPlayer.authenticateHandler = delegate(UIViewController viewController, NSError error) {
					if (viewController != null) {
						_localPlayer = null;
						UIApplication.SharedApplication().keyWindow.rootViewController.PresentViewController(viewController, true, null);
					} else {
						_CheckLocalPlayer(error);
						error = null;
					}
				};
			} else {
				// deprecated in ios 6.0
				gkLocalPlayer.Authenticate(delegate(NSError error) {
					_CheckLocalPlayer(error);
					error = null;
				});
			}
		}

		private static void _CheckLocalPlayer(NSError error) {
			var gkLocalPlayer = GKLocalPlayer.LocalPlayer();

			if (gkLocalPlayer.authenticated) {
				// create wrapper and dispatch event
				_localPlayer = NSObjectWrapper.CreateWrapper(typeof(LocalPlayer), gkLocalPlayer, gkLocalPlayer.playerID) as LocalPlayer;
				if (_localPlayerAuthenticatedHandlers != null)
					_localPlayerAuthenticatedHandlers(null, EventArgs.Empty);

			} else {
				// set it to null and dispatch event
				_localPlayer = null;
				if (_localPlayerAuthenticationFailedHandlers != null)
					_localPlayerAuthenticationFailedHandlers(null, new U3DXTErrorEventArgs(error));
			}
		}

		/// <summary>
		/// The local player that is authenticated on this device, or null if not authenticated.
		/// </summary>
		/// <value>The local player.</value>
		public static LocalPlayer localPlayer {
			get { return _localPlayer; }
		}

		/// <summary>
		/// Brings up the default Game Center interface.
		/// Available in iOS 6.0 and later.
		/// </summary>
		public static void ShowGameCenter() {
			var vc = new GKGameCenterViewController();

			vc.gameCenterDelegate = GameCenterControllerDelegate.instance;

			UIApplication.SharedApplication().keyWindow.rootViewController.PresentViewController(vc, true, null);
		}

		/// <summary>
		/// Displays a Game Center banner on the top of the screen.
		/// The banner goes away in a few seconds.
		/// </summary>
		/// <param name="title">The title of the banner.</param>
		/// <param name="message">The message of the banner.</param>
		public static void ShowBanner(string title, string message) {
			GKNotificationBanner.ShowBanner(title, message, null);
		}

		/// <summary>
		/// Reports a high score to Game Center.
		/// The returned GKScore can be used for more advanced functionalities such as sending challenges.
		/// Raises ScoreReported and ScoreReportFailed events on completion.
		/// </summary>
		/// <returns>The score.</returns>
		/// <param name="leaderboardID">Leaderboard I.</param>
		/// <param name="score">Score.</param>
		public static GKScore ReportScore(string leaderboardID, long score) {
			var scoreReporter = new GKScore(leaderboardID);

			scoreReporter.value = score;
//			scoreReporter.context = 0;

			scoreReporter.ReportScore(_ScoreReported);

			return scoreReporter;
		}

		private static void _ScoreReported(NSError error) {
			if (error != null) {
				if (_scoreReportFailedHandlers != null)
					_scoreReportFailedHandlers(null, new U3DXTErrorEventArgs(error));
			} else {
				if (_scoreReportedHandlers != null)
					_scoreReportedHandlers(null, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Displays the Game Center Leaderboard interface.
		/// </summary>
		/// <param name="leaderboardID">The ID of the leaderboard to be displayed.</param>
		public static void ShowLeaderboard(string leaderboardID) {

			if (GKGameCenterViewController.ClassExists) {
				// ios 6 preferred method
				var gcvc = new GKGameCenterViewController();
				gcvc.gameCenterDelegate = GameCenterControllerDelegate.instance;
				gcvc.viewState = GKGameCenterViewControllerState.Leaderboards;
				gcvc.leaderboardTimeScope = GKLeaderboardTimeScope.Today;
				gcvc.leaderboardCategory = leaderboardID;

				UIApplication.SharedApplication().keyWindow.rootViewController.PresentViewController(gcvc, true, null);

			} else {
				var lbvc = new GKLeaderboardViewController();
				lbvc.leaderboardDelegate = LeaderboardViewControllerDelegate.instance;
				lbvc.timeScope = GKLeaderboardTimeScope.Today;
				lbvc.category = leaderboardID;

				UIApplication.SharedApplication().keyWindow.rootViewController.PresentViewController(lbvc, true, null);
			}
		}

		/// <summary>
		/// Reports the progress of an achivement to Game Center.
		/// The returned GKAchievement can be used for more advanced functionalities such as sending challenges.
		/// Raises AchievementReported and AchievementReportFailed events on completion.
		/// </summary>
		/// <returns>The native achievement.</returns>
		/// <param name="achievementId">The ID of the achievementId to report to.</param>
		/// <param name="percentComplete">The progress from 0.0 to 100.0.</param>
		public static GKAchievement ReportAchievement(string achievementId, double percentComplete) {
			var achievement = new GKAchievement(achievementId); 
			if (achievement.IsNil)
				return null;

			achievement.percentComplete = percentComplete;
			achievement.ReportAchievement(_AchievementReported);
			return achievement;
		}

		private static void _AchievementReported(NSError error) {
			if (error != null) {
				if (_achievementReportFailedHandlers != null)
					_achievementReportFailedHandlers(null, new U3DXTErrorEventArgs(error));
			} else {
				if (_achievementReportedHandlers != null)
					_achievementReportedHandlers(null, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Displays the Game Center Achievements interface.
		/// </summary>
		public static void ShowAchievements() {

			if (GKGameCenterViewController.ClassExists) {
				// ios 6 preferred method
				var gcvc = new GKGameCenterViewController();
				gcvc.gameCenterDelegate = GameCenterControllerDelegate.instance;
				gcvc.viewState = GKGameCenterViewControllerState.Achievements;

				UIApplication.SharedApplication().keyWindow.rootViewController.PresentViewController(gcvc, true, null);

			} else {
				var avc = new GKAchievementViewController();
				avc.achievementDelegate = AchievementViewControllerDelegate.instance;

				UIApplication.SharedApplication().keyWindow.rootViewController.PresentViewController(avc, true, null);
			}
		}

#endregion
	}
}

#endif
