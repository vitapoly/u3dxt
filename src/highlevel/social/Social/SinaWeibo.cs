using System;
using U3DXT;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;
using U3DXT.iOS.Native.Social;
using U3DXT.iOS.Native.MessageUI;
using U3DXT.iOS.Social.Helpers;
using U3DXT.iOS.Native.Accounts;
using U3DXT.Utils;
using System.Text;
using System.Collections.Generic;

namespace U3DXT.iOS.Social {
	/// <summary>
	/// Provides methods to directly query the Sina Weibo API.</summary>
	/// <remarks>
	/// All calls in this class works in the background without prompting the user.
	/// So be mindful of privacy issues.
	/// When a query is finished, it calls the callback delegate with a JSON object as the response.
	/// <p></p>
	/// This class does not implement the complete Weibo API as direct methods,
	/// but for anything not built-in, you can use the GetFromURL() or PostToURL() methods.
	/// These two methods along with all the other direct methods
	/// have the system authenticated user access info filled in already.
	/// <p></p>
	/// See http://open.weibo.com/wiki/API文档_V2/en for more information about Sina Weibo API.
	/// </remarks>
	public class SinaWeibo : DirectRequestService {
		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.Social.SinaWeibo"/> class.
		/// </summary>
		public SinaWeibo() : base(ACAccountType.IdentifierSinaWeibo, SLRequest.SLServiceTypeSinaWeibo) {
		}
		
		/// <summary>
		/// Queries for the user's public timeline with a list of weibos.
		/// </summary>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetPublicTimeline(Action<object> callback) {
			GetFromURL("https://api.weibo.com/2/statuses/public_timeline.json",
				_MakeCallback(callback));
		}

		/// <summary>
		/// Queries for the user's home timeline with a list of weibos.
		/// </summary>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetHomeTimeline(Action<object> callback) {
			GetFromURL("https://api.weibo.com/2/statuses/home_timeline.json",
				_MakeCallback(callback));
		}

		/// <summary>
		/// Queries for info about a weibo.
		/// </summary>
		/// <param name='id'>
		/// The weibo ID.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetWeibo(string id, Action<object> callback) {
			GetFromURL("https://api.weibo.com/2/statuses/show.json?id=" + id,
				_MakeCallback(callback));
		}
		
		/// <summary>
		/// Posts a weibo.
		/// </summary>
		/// <param name='status'>
		/// The status update.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void PostWeibo(string status, Action<object> callback) {
			PostToURL("https://api.weibo.com/2/statuses/update.json",
				new Dictionary<object, object> {{"status", status}},
				_MakeCallback(callback));
		}
		
		/// <summary>
		/// Posts a status update with a picture.
		/// </summary>
		/// <param name='status'>
		/// The status update.
		/// </param>
		/// <param name='pictureURL'>
		/// The URL to the picture.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void PostPicture(string status, string pictureURL, Action<object> callback) {
			PostToURL("https://api.weibo.com/2/statuses/upload_url_text.json",
				new Dictionary<object, object> {{"status", status}, {"url", pictureURL}},
				_MakeCallback(callback));
		}

		/// <summary>
		/// Queries for info about a user.
		/// </summary>
		/// <param name='user'>
		/// The user to query for.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetUser(string user, Action<object> callback) {
			GetFromURL("https://api.weibo.com/2/users/show.json?screen_name=" + Uri.EscapeUriString(user),
				_MakeCallback(callback));
		}

		/// <summary>
		/// Searches for a list of weibo containing the query string.
		/// </summary>
		/// <param name='query'>
		/// The string to search.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void SearchWeibo(string query, Action<object> callback) {
			GetFromURL("https://api.weibo.com/2/search/suggestions/statuses.json?q=" + Uri.EscapeUriString(query),
				_MakeCallback(callback));
		}

		/// <summary>
		/// Queries for the specified user's friends.
		/// </summary>
		/// <param name='user'>
		/// The user to query for.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetFriends(string user, Action<object> callback) {
			GetFromURL("https://api.weibo.com/2/friendships/friends.json?screen_name=" + Uri.EscapeUriString(user),
				_MakeCallback(callback));
		}

		/// <summary>
		/// Queries for the specified user's followers.
		/// </summary>
		/// <param name='user'>
		/// The user to query for.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetFollowers(string user, Action<object> callback) {
			GetFromURL("https://api.weibo.com/2/friendships/followers.json?screen_name=" + Uri.EscapeUriString(user),
				_MakeCallback(callback));
		}

		/// <summary>
		/// Searches for a list of users containing the query string.
		/// </summary>
		/// <param name='query'>
		/// The string to search for.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void SearchUser(string query, Action<object> callback) {
			GetFromURL("https://api.weibo.com/2/search/suggestions/users.json?q=" + Uri.EscapeUriString(query),
				_MakeCallback(callback));
		}

		/// <summary>
		/// Queries for the user's favorites.
		/// </summary>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetFavorites(Action<object> callback) {
			GetFromURL("https://api.weibo.com/2/favorites.json",
				_MakeCallback(callback));
		}

		/// <summary>
		/// Queries for the recent hour's trends.
		/// </summary>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetRecentHourTrends(Action<object> callback) {
			GetFromURL("https://api.weibo.com/2/trends/hourly.json",
				_MakeCallback(callback));
		}
	}
}

