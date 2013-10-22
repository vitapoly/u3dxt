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
	/// Base class for Twitter, Facebook, and SinaWeibo to conveniently use SLRequest
	/// to directly querying a service's API.</summary>
	/// <remarks>
	/// You don't have to use this class directly, but you may want to use its
	/// GetFromURL() and PostToURL() methods with the Twitter, Facebook, or SinaWeibo instances.
	/// </remarks>
	public class DirectRequestService {

		/// <summary> U3DXT internal. </summary>
		protected ACAccountStore _service;

		/// <summary> U3DXT internal. </summary>
		protected ACAccountType _accountType;
		
		/// <summary> U3DXT internal. </summary>
		protected string _serviceType;
		
		/// <summary> U3DXT internal. </summary>
		protected Dictionary<object, object> _options = null;
		
		/// <summary> U3DXT internal. </summary>
		protected DirectRequestService(string accountType, string serviceType) {
			_service = new ACAccountStore();
			_accountType = _service.FindAccountType(accountType);
			_serviceType = serviceType;
		}
		
		/// <summary> U3DXT internal. </summary>
		protected Action<object, NSHTTPURLResponse, NSError> _MakeCallback(Action<object> callback) {
			return delegate(object obj, NSHTTPURLResponse urlResponse, NSError error) {
				if (callback != null)
					callback(obj);
				callback = null;
			};
		}
		
		/// <summary>
		/// Queries an URL with a GET. The request will have the system authenticated user
		/// access info filled in. When the query is done, it calls the callback delegate
		/// with a JSON object, the HTTP response, and an error if an error occurred.
		/// </summary>
		/// <param name="url"> The URL to query.</param>
		/// <param name="callback"> A callback delegate to be called when done.</param>
		public void GetFromURL(string url, Action<object, NSHTTPURLResponse, NSError> callback) {
			string cleanUrl;
			Dictionary<object, object> parameters;
			HttpUtil.ParseQueryString(url, out cleanUrl, out parameters);

			_Request(cleanUrl, SLRequestMethod.GET, parameters, callback);
		}

		/// <summary>
		/// Queries an URL with a POST. The parameters is added to the body of the query.
		/// The request will have the system authenticated user
		/// access info filled in. When the query is done, it calls the callback delegate
		/// with a JSON object, the HTTP response, and an error if an error occurred.
		/// </summary>
		/// <param name="url"> The URL to query.</param>
		/// <param name="parameters"> A JSON object.</param>
		/// <param name="callback"> A callback delegate to be called when done.</param>
		public void PostToURL(string url, Dictionary<object, object> parameters, Action<object, NSHTTPURLResponse, NSError> callback) {
			Dictionary<object, object> param = null;
			if (parameters != null) {
				param = new Dictionary<object, object>();
				foreach (var key in parameters.Keys) {
					param[key] = parameters[key];
				}
			}
			_Request(url, SLRequestMethod.POST, param, callback);
		}
		
		private void _Request(string url, SLRequestMethod method, Dictionary<object, object> parameters, Action<object, NSHTTPURLResponse, NSError> callback) {
			_service.RequestAccessToAccounts(_accountType, _options, delegate(bool granted, NSError error) {
				if (granted) {
					var account = _service.FindAccounts(_accountType)[0] as ACAccount;
					var request = SLRequest.Request(
						_serviceType,
						method,
						new NSURL(url),
						parameters);
					request.account = account;
					
					request.PerformRequest(delegate(NSData responseData, NSHTTPURLResponse urlResponse, NSError error2) {
						object obj = null;
						var response = Encoding.UTF8.GetString(responseData.ToByteArray());
						try {
							obj = Json.Deserialize(response);
						} catch (Exception) {
							obj = response;
						}
						callback(obj, urlResponse, error2);
						callback = null;
					});
					
					parameters = null;
					account = null;
					request = null;
				} else {
					callback(null, null, error);
					parameters = null;
					callback = null;
				}
			});
		}
	}
}

