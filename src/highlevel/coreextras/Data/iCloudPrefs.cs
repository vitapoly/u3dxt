#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.Core;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;
using U3DXT.iOS.Native.AssetsLibrary;
using System.Collections.Generic;
using System.Linq;

namespace U3DXT.iOS.Data {
	/// <summary>
	/// High-level class for iCloud Key-Value Store to resemble PlayerPrefs.
	/// </summary>
	public static class iCloudPrefs {

#region events
		private static EventHandler<EventArgs> _accountChangedHandlers;
		/// <summary>
		/// Occurs when the iCloud account changes (logs in, logs out, or different account).
		/// </summary>
		public static event EventHandler<EventArgs> AccountChanged {
			add { _accountChangedHandlers += value; }
			remove { _accountChangedHandlers -= value; }
		}

		private static EventHandler<iCloudPrefsChangedEventArgs> _valuesChangedHandlers;
		/// <summary>
		/// Occurs when key values are changed externally from another device with the same iCloud account.
		/// </summary>
		public static event EventHandler<iCloudPrefsChangedEventArgs> ValuesChangedExternally {
			add { _valuesChangedHandlers += value; }
			remove { _valuesChangedHandlers -= value; }
		}
#endregion

#region vars
		/// <summary>
		/// The low-level NSUbiquitousKeyValueStore.
		/// </summary>
		/// <value>The low-level NSUbiquitousKeyValueStore.</value>
		public static NSUbiquitousKeyValueStore store {
			get;
			private set;
		}

		/// <summary>
		/// The iCloud ID, which is meaningless, just a way to identify between accounts.
		/// </summary>
		/// <value>The iCloud ID.</value>
		public static string iCloudID {
			get;
			private set;
		}

		private const string USER_DEFAULTS_PREFIX = "_iCloudPrefs_";
		private const string ID_KEY = "U3DXT.UbiquityIdentityToken";
		private static NSUserDefaults _userDefaults;

#endregion

		static iCloudPrefs() {
			if (CoreXT.IsDevice) {

				// get id from user defaults
				_userDefaults = NSUserDefaults.StandardUserDefaults();
				iCloudID = _userDefaults.Object(ID_KEY) as string;

				store = NSUbiquitousKeyValueStore.DefaultStore();
				store.DidChangeExternally += _OnStoreChanged;

				NSFileManager.DefaultManager().UbiquityIdentityDidChange += _Init;
				CoreXT.ApplicationResumed += _Init;

				_Init();
			}
		}

		private static void _Init(object sender = null, EventArgs e = null) {

			string oldID = iCloudID;

			object token = NSFileManager.DefaultManager().UbiquityIdentityToken();
			if (token == null) {
				iCloudID = null;
				_userDefaults.RemoveObject(ID_KEY);
			} else {
				NSData data = NSKeyedArchiver.ArchivedData(token);
				iCloudID = Convert.ToBase64String(data.ToByteArray());
				_userDefaults.SetObject(iCloudID, ID_KEY);
			}

			Synchronize();

			if (iCloudID != oldID) {
				if (_accountChangedHandlers != null)
					_accountChangedHandlers(null, EventArgs.Empty);
			}
		}


		/// <summary>
		/// Explicitly synchronizes with iCloud server.
		/// You don't have to call this manually because it synchronizes automatically at appropriate times.
		/// </summary>
		public static void Synchronize() {
			store.Synchronize();
		}

		private static void _OnStoreChanged(object sender, NSNotificationEventArgs e) {

			if (_valuesChangedHandlers != null) {
				var reason = (NSUbiquitousKeyValueStoreChangeReasonValues)Enum.ToObject(
					typeof(NSUbiquitousKeyValueStoreChangeReasonValues),
					Convert.ToInt32(e.userInfo[NSUbiquitousKeyValueStore.ChangeReasonKey]));

				object[] keys = e.userInfo[NSUbiquitousKeyValueStore.ChangedKeysKey] as object[];
				iCloudPrefsChange[] changes = new iCloudPrefsChange[keys.Length];

				for (int i=0; i<keys.Length; i++) {
					string key = keys[i] as string;

					// get old value
					object oldValue = _userDefaults.Object(USER_DEFAULTS_PREFIX + key);
					if ((oldValue is int) || (oldValue is long)) {
						oldValue = Convert.ToInt32(oldValue);
					} else if ((oldValue is double) || (oldValue is float)) {
						oldValue = (float)Convert.ToDouble(oldValue);
					}

					// clean new value, and set it in cache
					object newValue = store.Object(key);
					if (newValue == null) {
						_userDefaults.RemoveObject(USER_DEFAULTS_PREFIX + key);

					} else if (newValue is string) {
						_userDefaults.SetObject(newValue, USER_DEFAULTS_PREFIX + key);
					
					} else if ((newValue is int) || (newValue is long)) {
						// in case of long, convert it to int first
						newValue = Convert.ToInt32(newValue);
						_userDefaults.SetInteger((int)newValue, USER_DEFAULTS_PREFIX + key);

					} else if ((newValue is double) || (newValue is float)) {
						// in case of double, convert it to float first
						newValue = (float)Convert.ToDouble(newValue);
						_userDefaults.SetDouble((float)newValue, USER_DEFAULTS_PREFIX + key);
					}

					changes[i] = new iCloudPrefsChange(key, newValue, oldValue);
				}

				_valuesChangedHandlers(null, new iCloudPrefsChangedEventArgs(reason, changes));
			}
		}

		/// <summary>
		/// Returns the value associated with key.
		/// </summary>
		/// <returns>The int value.</returns>
		/// <param name="key">Key.</param>
		/// <param name="defaultValue">Default value.</param>
		public static int GetInt(string key, int defaultValue) {
			object value = store.Object(key);
			if ((value == null) || (!((value is int) || (value is long))))
				return defaultValue;
			return Convert.ToInt32(value);
		}

		/// <summary>
		/// Returns the value associated with key.
		/// </summary>
		/// <returns>The int value.</returns>
		/// <param name="key">Key.</param>
		public static int GetInt(string key) {
			return GetInt(key, 0);
		}

		/// <summary>
		/// Sets the value associated with key.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="value">Int value.</param>
		public static void SetInt(string key, int value) {

			_userDefaults.SetInteger(value, USER_DEFAULTS_PREFIX + key);

			store.SetLongLong(value, key);
		}

		/// <summary>
		/// Returns the value associated with key.
		/// </summary>
		/// <returns>The float value.</returns>
		/// <param name="key">Key.</param>
		/// <param name="defaultValue">Default value.</param>
		public static float GetFloat(string key, float defaultValue) {
			object value = store.Object(key);
			if ((value == null) || (!((value is float) || (value is double))))
				return defaultValue;
			return (float)Convert.ToDouble(value);
		}

		/// <summary>
		/// Returns the value associated with key.
		/// </summary>
		/// <returns>The float value.</returns>
		/// <param name="key">Key.</param>
		public static float GetFloat(string key) {
			return GetFloat(key, 0f);
		}

		/// <summary>
		/// Sets the value associated with key.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="value">Float value.</param>
		public static void SetFloat(string key, float value) {
			_userDefaults.SetDouble(value, USER_DEFAULTS_PREFIX + key);

			store.SetDouble(value, key);
		}

		/// <summary>
		/// Returns the value associated with key.
		/// </summary>
		/// <returns>The string value.</returns>
		/// <param name="key">Key.</param>
		/// <param name="defaultValue">Default value.</param>
		public static string GetString(string key, string defaultValue) {
			object value = store.Object(key);
			if ((value == null) || (!(value is string)))
				return defaultValue;
			return value as string;
		}

		/// <summary>
		/// Returns the value associated with key.
		/// </summary>
		/// <returns>The string value.</returns>
		/// <param name="key">Key.</param>
		public static string GetString(string key) {
			return GetString(key, string.Empty);
		}

		/// <summary>
		/// Sets the value associated with key.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="value">String value.</param>
		public static void SetString(string key, string value) {
			_userDefaults.SetObject(value, USER_DEFAULTS_PREFIX + key);

			store.SetString(value, key);
		}

		/// <summary>
		/// Determines if has key the specified key.
		/// </summary>
		/// <returns><c>true</c> if has key the specified key; otherwise, <c>false</c>.</returns>
		/// <param name="key">Key.</param>
		public static bool HasKey(string key) {
			return (store.Object(key) != null);
		}

		/// <summary>
		/// All keys in iCloud store.
		/// </summary>
		/// <value>The keys.</value>
		public static string[] keys {
			get {
				var keyValues = store.DictionaryRepresentation();
				return keyValues.Keys.Cast<string>().ToArray();
			}
		}

		/// <summary>
		/// Deletes all iCloudPrefs data.
		/// </summary>
		public static void DeleteAll() {

			var keyValues = store.DictionaryRepresentation();
			foreach (var key in keyValues.Keys) {
				DeleteKey(key as string);
			}
		}

		/// <summary>
		/// Deletes the value associated with the key.
		/// </summary>
		/// <param name="key">Key.</param>
		public static void DeleteKey(string key) {

			_userDefaults.RemoveObject(USER_DEFAULTS_PREFIX + key);

			store.RemoveObject(key);
		}

	}
}

#endif
