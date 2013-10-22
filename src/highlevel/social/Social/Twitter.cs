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
	/// Provides methods to directly query the Twitter API 1.1.</summary>
	/// <remarks>
	/// All calls in this class works in the background without prompting the user.
	/// So be mindful of privacy issues.
	/// When a query is finished, it calls the callback delegate with a JSON object as the response.
	/// <p></p>
	/// This class does not implement the complete Twitter API as direct methods,
	/// but for anything not built-in, you can use the GetFromURL() or PostToURL() methods.
	/// These two methods along with all the other direct methods
	/// have the system authenticated user access info filled in already.
	/// <p></p>
	/// See https://dev.twitter.com/docs/api/1.1 for more information about Twitter API.
	/// </remarks>
	public class Twitter : DirectRequestService {
		
		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.Social.Twitter"/> class.
		/// </summary>
		public Twitter() : base(ACAccountType.IdentifierTwitter, SLRequest.SLServiceTypeTwitter) {
		}
		
		/// <summary>
		/// Queries for a list of tweets.
		/// </summary>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetTweets(Action<object> callback) {
			GetFromURL("https://api.twitter.com/1.1/statuses/home_timeline.json",
				_MakeCallback(callback));
		}
		
		/// <summary>
		/// Queries for info about a tweet.
		/// </summary>
		/// <param name='id'>
		/// The tweet ID.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetTweet(string id, Action<object> callback) {
			GetFromURL("https://api.twitter.com/1.1/statuses/show.json?id=" + id,
				_MakeCallback(callback));
		}
		
		/// <summary>
		/// Posts a status update.
		/// </summary>
		/// <param name='status'>
		/// The status update.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void Update(string status, Action<object> callback) {
			PostToURL("https://api.twitter.com/1.1/statuses/update.json",
				new Dictionary<object, object> {{"status", status}},
				_MakeCallback(callback));
		}

		/// <summary>
		/// Queries for tweets containing the query string.
		/// </summary>
		/// <param name='query'>
		/// The string to search for.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void SearchTweet(string query, Action<object> callback) {
			GetFromURL("https://api.twitter.com/1.1/search/tweets.json?q=" + Uri.EscapeUriString(query),
				_MakeCallback(callback));
		}
		
		/// <summary>
		/// Queries for a list of friends for a specified user.
		/// </summary>
		/// <param name='user'>
		/// The user to query for his/her friends.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetFriends(string user, Action<object> callback) {
			GetFromURL("https://api.twitter.com/1.1/friends/list.json?screen_name=" + Uri.EscapeUriString(user),
				_MakeCallback(callback));
		}

		/// <summary>
		/// Queries for a list of followers for a specified user.
		/// </summary>
		/// <param name='user'>
		/// The user to query for his/her followers.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetFollowers(string user, Action<object> callback) {
			GetFromURL("https://api.twitter.com/1.1/followers/list.json?screen_name=" + Uri.EscapeUriString(user),
				_MakeCallback(callback));
		}
		
		/// <summary>
		/// Queries for information about a user.
		/// </summary>
		/// <param name='user'>
		/// The user to query for.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetUser(string user, Action<object> callback) {
			GetFromURL("https://api.twitter.com/1.1/users/show.json?screen_name=" + Uri.EscapeUriString(user),
				_MakeCallback(callback));
		}
		
		/// <summary>
		/// Queries for a list of users containing the query string.
		/// </summary>
		/// <param name='query'>
		/// The string to search for.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void SearchUser(string query, Action<object> callback) {
			GetFromURL("https://api.twitter.com/1.1/users/search.json?q=" + Uri.EscapeUriString(query),
				_MakeCallback(callback));
		}

		/// <summary>
		/// Queries for a list of the current user's favorites.
		/// </summary>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetFavorites(Action<object> callback) {
			GetFromURL("https://api.twitter.com/1.1/favorites/list.json",
				_MakeCallback(callback));
		}
	}
}

