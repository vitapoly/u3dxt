using System;
using System.Collections;
using UnityEngine;
using U3DXT.Core;
using U3DXT.iOS.Social;
using U3DXT.iOS.Native.Foundation;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.MessageUI;
using MiniJSON;
using U3DXT.iOS.Native.Social;

public class SocialTest : MonoBehaviour {
	
	Texture2D _logo;
	Twitter _twitter;
	Facebook _facebook;
	SinaWeibo _sinaWeibo;
	
	void Start() {
	
		_logo = GameObject.Find("Logo").guiTexture.texture as Texture2D;

		if (CoreXT.IsDevice) {
			SubscribeEvents();
			CreateDirectServices();
		}
	}

	void OnGUI() {
		
		if (CoreXT.IsDevice) {
			
			GUILayout.BeginArea(new Rect(50, 50, Screen.width - 100, Screen.height/2 - 50));
				GUILayout.BeginHorizontal();
					OnGUIActivitySheet();
					OnGUIEmail();
					OnGUISMS();
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
					OnGUIPost();
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
					OnGUIDirectTwitter();
					OnGUIDirectFacebook();
					OnGUIDirectSinaWeibo();
				GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}
		
		OnGUILog();
	}
	
	void SubscribeEvents() {
					
		SocialXT.ShareCompleted += delegate(object sender, ShareCompletedEventArgs e) {
			Log("Share to " + e.ActivityType + " completed: " + e.Completed);
		};
		
		SocialXT.MailCompleted += delegate(object sender, MailCompletedEventArgs e) {
			Log("Mail result: " + e.Result + " error: " + e.Error);
		};
		
		SocialXT.SMSCompleted += delegate(object sender, SMSCompletedEventArgs e) {
			Log("SMS result: " + e.Result);
		};
		
		SocialXT.PostCompleted += delegate(object sender, PostCompletedEventArgs e) {
			Log("Post completed: " + e.Completed);
		};
	}
	
	void CreateDirectServices() {
		
		// create direct services to social networks
		_twitter = new Twitter();
		_sinaWeibo = new SinaWeibo();

		//TODO: init facebook with your app ID and an array of permissions
		_facebook = new Facebook();
		_facebook.Init("1234567890", new string[] {"read_stream", "email", "publish_stream"});
	}
	
	void OnGUIActivitySheet() {
		if (GUILayout.Button("Activity Sheet", GUILayout.ExpandHeight(true))) {
			SocialXT.Share(new object[] {
				"You can find the best Unity Plugins at U3DXT.com!",
				"http://www.U3DXT.com",
				_logo
			});
		}
	}
	
	void OnGUIEmail() {
		if (GUILayout.Button("Email", GUILayout.ExpandHeight(true))) {
			SocialXT.Mail(new string[] { "support@U3DXT.com" },
				"email subject",
				"email body",
				false,
				_logo
			);
		}
	}

	void OnGUISMS() {
		if (GUILayout.Button("SMS", GUILayout.ExpandHeight(true))) {
			SocialXT.SMS(new string[] { "15555555555" }, "hello");
		}
	}

	void OnGUIPost() {
		if (GUILayout.Button("Twitter Post", GUILayout.ExpandHeight(true))) {
			SocialXT.Post(SLRequest.SLServiceTypeTwitter,
				"@vitapoly Your Unity Plugins rock!",
				_logo,
				"http://www.U3DXT.com"
			);
		}
		
		if (GUILayout.Button("Facebook Post", GUILayout.ExpandHeight(true))) {
			SocialXT.Post(SLRequest.SLServiceTypeFacebook,
				"Super fun games from vitapoly!",
				null,
				"http://www.vitapoly.com"
			);
		}
		
		if (GUILayout.Button("Sina Weibo Post", GUILayout.ExpandHeight(true))) {
			SocialXT.Post(SLRequest.SLServiceTypeSinaWeibo,
				"My first weibo from U3DXT Social by vitapoly.",
				_logo,
				"http://www.U3DXT.com"
			);
		}
	}
	
	void OnGUIDirectTwitter() {
		if (GUILayout.Button("Twitter API", GUILayout.ExpandHeight(true))) {

			// get a list of tweets
			// when done, it calls the callback function with a JSON object
			_twitter.GetTweets(delegate(object obj) {
				Log("tweets: " + Json.Serialize(obj));
			});
			
			// update status
			_twitter.Update("@vitapoly I am using vitapoly's awesome SocialXT Unity Plugin.", delegate(object obj) {
				Log("update: " + Json.Serialize(obj));
			});
			
			// search for tweets
			_twitter.SearchTweet("vitapoly", delegate(object obj) {
				Log("tweets containing vitapoly: " + Json.Serialize(obj));
			});
			
			// you can also query twitter directly if a functionality is not built-in to the Twitter class
			// get a list of suggested users
			// supply a callback function with more arguments
			_twitter.GetFromURL("https://api.twitter.com/1.1/users/suggestions.json",
				delegate(object obj, NSHTTPURLResponse urlResponse, NSError error) {
					Log("suggested users: " + Json.Serialize(obj));
				}
			);
		}
	}
	
	void OnGUIDirectFacebook() {
		if (GUILayout.Button("Facebook API", GUILayout.ExpandHeight(true))) {
			
			// get the user's feed
			// when done, it calls the callback function with a JSON object
			_facebook.GetFeed(delegate(object obj) {
				Log("feed: " + Json.Serialize(obj));
			});

			// get info about current user
			_facebook.GetUser(delegate(object obj) {
				Log("user: " + Json.Serialize(obj));
			});

			// get the user's friends
			_facebook.GetFriends(delegate(object obj) {
				Log("friends: " + Json.Serialize(obj));
			});

			// post a link
			_facebook.PostLink(
				"You can find the best Unity Plugins at www.U3DXT.com!",
				"http://www.U3DXT.com",
				delegate(object obj) {
					Log("post: " + Json.Serialize(obj));
				}
			);

			// you can also query the Facebook Graph API directly if a functionality is not built-in to the Facebook class
			// get info about vitapoly
			// supply a callback function with more arguments
			_facebook.GetFromURL("https://graph.facebook.com/vitapoly",
				delegate(object obj, NSHTTPURLResponse urlResponse, NSError error) {
					Log("vitapoly: " + Json.Serialize(obj));
				}
			);
		}
	}
	
	void OnGUIDirectSinaWeibo() {
		if (GUILayout.Button("Sina Weibo API", GUILayout.ExpandHeight(true))) {
			// get the user's timeline
			// when done, it calls the callback function with a JSON object
			_sinaWeibo.GetHomeTimeline(delegate(object obj) {
				Log("timeline: " + Json.Serialize(obj));
			});

			// get a list of favorites
			_sinaWeibo.GetFavorites(delegate(object obj) {
				Log("favorites: " + Json.Serialize(obj));
			});
			
			// post a weibo
			_sinaWeibo.PostWeibo("Hello from SocialXT Unity Plugin by vitapoly", delegate(object obj) {
				Log("post: " + Json.Serialize(obj));
			});

			// you can also query the Weibo API directly if a functionality is not built-in to the Weibo class
			// get a list of hot users
			// supply a callback function with more arguments
			_sinaWeibo.GetFromURL("https://api.weibo.com/2/suggestions/users/hot.json",
				delegate(object obj, NSHTTPURLResponse urlResponse, NSError error) {
					Log("hot users: " + Json.Serialize(obj));
				}
			);
		}
	}

	string _log = "Debug log:";
	Vector2 _scrollPosition = Vector2.zero;
	
	void OnGUILog() {
		GUILayout.BeginArea(new Rect(50, Screen.height / 2, Screen.width - 100, Screen.height / 2 - 50));
		_scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
		GUI.skin.box.wordWrap = true;
		GUI.skin.box.alignment = TextAnchor.UpperLeft;
		GUILayout.Box(_log, GUILayout.ExpandHeight(true));
		GUILayout.EndScrollView();
		GUILayout.EndArea();
	}
	
	void Log(string str) {
		_log += "\n" + str;
		_scrollPosition.y = Mathf.Infinity;
	}
}


