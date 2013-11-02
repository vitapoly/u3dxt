#if (UNITY_EDITOR || UNITY_IPHONE)

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
	/// Provides methods to directly query the Facebook API.</summary>
	/// <remarks>
	/// All calls in this class works in the background without prompting the user.
	/// So be mindful of privacy issues.
	/// When a query is finished, it calls the callback delegate with a JSON object as the response.
	/// <p></p>
	/// his class does not have the complete Facebook API as direct methods,
	/// but for anything not built-in, you can use the GetFromURL() or PostToURL() methods.
	/// These two methods along with all the other direct methods
	/// have the system authenticated user access info filled in already.
	/// <p></p>
	/// See https://developers.facebook.com/docs/reference/api/ for more information about Facebook Graph API.
	/// </remarks>
	public class Facebook : DirectRequestService {
		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.Social.Facebook"/> class.
		/// </summary>
		public Facebook() : base(ACAccountType.IdentifierFacebook, SLRequest.SLServiceTypeFacebook) {
		}
		
		/// <summary>
		/// Initializes Facebook with a Facebook app ID and an array of permissions.
		/// Raises InitializationCompleted event when completed, or InitializationFailed event when failed.
		/// You can get the account information with the <c>account</c> property after successful initialization.
		/// </summary>
		/// <remarks>The Facebook app must enable iOS integration, and the bundle ID of the mobile app must be the same
		/// as registered on the Facebook app.
		/// </remarks>
		/// <param name='appID'>
		/// The Facebook app ID.
		/// </param>
		/// <param name='permissions'>
		/// An array of strings representing permissions to request.
		/// </param>
		public void Init(string appID, string[] permissions) {
			_options = new Dictionary<object, object>();
			_options[ACAccountStore.ACFacebookAppIdKey] = appID;
			_options[ACAccountStore.ACFacebookAudienceKey] = ACAccountStore.ACFacebookAudienceFriends;
			_options[ACAccountStore.ACFacebookPermissionsKey] = permissions;

			_Init();
		}
		
		/// <summary>
		/// Queries for user's feed.
		/// </summary>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetFeed(Action<object> callback) {
			GetFromURL("https://graph.facebook.com/me/feed",
				_MakeCallback(callback));
		}
		
		/// <summary>
		/// Posts a message on the user's feed.
		/// </summary>
		/// <param name='message'>
		/// The message to post.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void PostMessage(string message, Action<object> callback) {
			PostToURL("https://graph.facebook.com/me/feed",
				new Dictionary<object, object> {
					{"message", message}
				},
				_MakeCallback(callback));
		}
		
		/// <summary>
		/// Posts a message and link on the user's feed.
		/// </summary>
		/// <param name='message'>
		/// The message to post.
		/// </param>
		/// <param name='link'>
		/// An URL to post.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void PostLink(string message, string link, Action<object> callback) {
			PostToURL("https://graph.facebook.com/me/feed",
				new Dictionary<object, object> {
					{"link", link},
					{"message", message}
				},
				_MakeCallback(callback));
		}

		/// <summary>
		/// Posts a picture on the user's feed.
		/// </summary>
		/// <param name='message'>
		/// The message to post.
		/// </param>
		/// <param name='pictureURL'>
		/// The URL to the picture.
		/// </param>
		/// <param name='pictureName'>
		/// Name of the picture, or null.
		/// </param>
		/// <param name='pictureCaption'>
		/// Caption of the picture, or null.
		/// </param>
		/// <param name='pictureDescription'>
		/// Description of the picture, or null.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void PostPicture(string message, string pictureURL, string pictureName, string pictureCaption, string pictureDescription, Action<object> callback) {
			PostToURL("https://graph.facebook.com/me/feed",
				new Dictionary<object, object> {
					{"message", message},
					{"picture", pictureURL},
					{"name", pictureName},
					{"caption", pictureCaption},
					{"description", pictureDescription}
				},
				_MakeCallback(callback));
		}

		/// <summary>
		/// Searches for the query string for a specified type.
		/// </summary>
		/// <param name='query'>
		/// The string to search for.
		/// </param>
		/// <param name='type'>
		/// The type to search for.
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void Search(string query, string type, Action<object> callback) {
			GetFromURL("https://graph.facebook.com/search?q=" + Uri.EscapeUriString(query) + "&type=" + Uri.EscapeUriString(type),
				_MakeCallback(callback));
		}

		/// <summary>
		/// ueries for a list of the user's friends.
		/// </summary>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetFriends(Action<object> callback) {
			GetFromURL("https://graph.facebook.com/me/friends",
				_MakeCallback(callback));
		}

		/// <summary>
		/// Queries for info about the user.
		/// </summary>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetUser(Action<object> callback) {
			GetFromURL("https://graph.facebook.com/me",
				_MakeCallback(callback));
		}

		/// <summary>
		/// Queries for a list of the user's likes.
		/// </summary>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetLikes(Action<object> callback) {
			GetFromURL("https://graph.facebook.com/me/likes",
				_MakeCallback(callback));
		}
		
		/// <summary>
		/// Queries for info about an object specified by name. It can be a person, a company, a group, etc.
		/// </summary>
		/// <param name='name'>
		/// The name of the object to query for..
		/// </param>
		/// <param name='callback'>
		/// A callback delegate to be called on completion.
		/// </param>
		public void GetObject(string name, Action<object> callback) {
			GetFromURL("https://graph.facebook.com/" + name,
				_MakeCallback(callback));
		}
	}
}

#endif
