#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using U3DXT.Core;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;
using U3DXT.Utils;
using U3DXT.iOS.Native.StoreKit;
using U3DXT.iOS.IAP.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace U3DXT.iOS.IAP {
	/// <summary>
	/// Main class for iOS IAP plugin. High-level API to in-app purchase.</summary>
	/// <remarks>
	/// At launch of your app, subscribe to InitializationCompleted and InitializationFailed events first.
	/// Then call Init() with an array of product IDs that you have configured on iTunes Connect.
	/// Only after InitializationCompleted event can you call other methods.
	/// InitializationFailed event contains an array of invalid product IDs which you can then investigate.
	/// </remarks>
	public class IAPXT {

#region events

		private static EventHandler<InitializationEventArgs> _initializationCompletedHandlers;
		/// <summary>
		/// Occurs when initialization has completed.
		/// </summary>
		public static event EventHandler<InitializationEventArgs> InitializationCompleted {
			add { _initializationCompletedHandlers += value; }
			remove { _initializationCompletedHandlers -= value; }
		}

		private static EventHandler<InitializationEventArgs> _initializationFailedHandlers;
		/// <summary>
		/// Occurs when initialization has failed.
		/// </summary>
		public static event EventHandler<InitializationEventArgs> InitializationFailed {
			add { _initializationFailedHandlers += value; }
			remove { _initializationFailedHandlers -= value; }
		}

		private static EventHandler<TransactionEventArgs> _transactionCompletedHandlers;
		/// <summary>
		/// Occurs when transaction has completed.
		/// </summary>
		public static event EventHandler<TransactionEventArgs> TransactionCompleted {
			add { _transactionCompletedHandlers += value; }
			remove { _transactionCompletedHandlers -= value; }
		}

		private static EventHandler<TransactionEventArgs> _transactionFailedHandlers;
		/// <summary>
		/// Occurs when transaction has failed.
		/// </summary>
		public static event EventHandler<TransactionEventArgs> TransactionFailed {
			add { _transactionFailedHandlers += value; }
			remove { _transactionFailedHandlers -= value; }
		}

		private static EventHandler<DownloadEventArgs> _downloadUpdatedHandlers;
		/// <summary>
		/// Occurs when a download progress has updated.
		/// </summary>
		public static event EventHandler<DownloadEventArgs> DownloadUpdated {
			add { _downloadUpdatedHandlers += value; }
			remove { _downloadUpdatedHandlers -= value; }
		}

		private static EventHandler<EventArgs> _restorationCompletedHandlers;
		/// <summary>
		/// Occurs when restoration has completed.
		/// </summary>
		public static event EventHandler<EventArgs> RestorationCompleted {
			add { _restorationCompletedHandlers += value; }
			remove { _restorationCompletedHandlers -= value; }
		}

		private static EventHandler<U3DXTErrorEventArgs> _restorationFailedHandlers;
		/// <summary>
		/// Occurs when restoration has failed.
		/// </summary>
		public static event EventHandler<U3DXTErrorEventArgs> RestorationFailed {
			add { _restorationFailedHandlers += value; }
			remove { _restorationFailedHandlers -= value; }
		}

		private static EventHandler<U3DXTErrorEventArgs> _storeViewLoadFailedHandlers;
		/// <summary>
		/// Occurs when initialization has completed.
		/// </summary>
		public static event EventHandler<U3DXTErrorEventArgs> StoreViewLoadFailed {
			add { _storeViewLoadFailedHandlers += value; }
			remove { _storeViewLoadFailedHandlers -= value; }
		}

#endregion

#region vars

		private static bool _initd = false;
		private static Dictionary<string, SKProduct> _products = new Dictionary<string, SKProduct>();
		private static HashSet<string> _boughtProducts = new HashSet<string>();
		private static ActivityIndicator _activityIndicator = null;
		private static SKProductsRequest _request = null;
		private static SKStoreProductViewController _storeViewController = null;

		/// <summary>
		/// Indicates whether to display native activity indicator when purchasing and downloading
		/// </summary>
		public static bool displayActivityIndicator = true;

		/// <summary>
		/// Indicates whether to use encryption to cache which products the user has bought on local install.
		/// This makes it more difficult for users to modify the PlayerPref to hack bought products.
		/// If you want to disable using encryption, set this to false.
		/// </summary>
		[Obsolete("This is not used anymore because it always uses simple encryption which does not violate export laws.")]
		public static bool useEncryptionForCache = true;

#endregion

#region methods

		static IAPXT() {
			_ReadCache();
		}

		private static string _encryptKey = "u3d";
		/// <summary>
		/// Gets or sets the encryption key.
		/// Default is "u3d" for a key size of 48 bits, and thus not violating export laws.
		/// </summary>
		/// <value>The encryption key.</value>
		public static string encryptionKey {
			get { return _encryptKey; }
			set {
				_encryptKey = value;
				_ReadCache();
			}
		}

		private static void _ReadCache() {
			try {
				var data = PlayerPrefs.GetString("U3DXT.iOS.IAP.BOUGHT_PRODUCTS");
				var json = data;
				var obj = Json.Deserialize(json);
				if (obj == null) {
					json = Encryption.SimpleDecryptToString(data, _encryptKey);
					obj = Json.Deserialize(json);
				}

				if (obj == null) {
					_boughtProducts = new HashSet<string>();
				} else {
					_boughtProducts = new HashSet<string>((obj as List<object>).Cast<string>().ToArray());
				}
			} catch (Exception) {
				_boughtProducts = new HashSet<string>();
			}
		}

		private static void _WriteCache() {
			var json = Json.Serialize(_boughtProducts.ToList());
			var data = Encryption.SimpleEncryptFromString(json, _encryptKey);

			PlayerPrefs.SetString("U3DXT.iOS.IAP.BOUGHT_PRODUCTS", data);
			PlayerPrefs.Save();
		}

		/// <summary>
		/// Initializes IAPXT with an array of product IDs.
		/// </summary>
		/// <remarks>Raises InitializationCompleted event when completed, or InitializationFailed event when failed.</remarks>
		/// <param name="productIDs">Product IDs.</param>
		public static void Init(string[] productIDs) {

			// init observer here so the observer is observing only if the user wants to use the high-level API
			// add observer only once
			if (!_initd) {
				SKPaymentQueue.DefaultQueue().AddTransactionObserver(PaymentTransactionObserver.instance);
				_initd = true;
			}

			_request = new SKProductsRequest(productIDs);
//			_request.Delegate = ProductsRequestDelegate.instance;
			_request.DidReceive += _OnProductRequestReceive;
			_request.Start();
		}

		internal static void _OnProductRequestReceive(object sender, SKProductsRequest.DidReceiveEventArgs e) {
			foreach (var productObj in e.response.products) {
				var product = productObj as SKProduct;
				_products[product.productIdentifier] = product;
			}

			// raise event
			if ((e.response.invalidProductIdentifiers == null) || (e.response.invalidProductIdentifiers.Length == 0)) {
				if (_initializationCompletedHandlers != null)
					_initializationCompletedHandlers(null, new InitializationEventArgs(e.response));
			} else {
				if (_initializationFailedHandlers != null)
					_initializationFailedHandlers(null, new InitializationEventArgs(e.response));
			}

			_request = null;
		}

		/// <summary>
		/// Indicates whether the platform can make payments. If the user has disabled in-app purchase, this would be false. 
		/// </summary>
		/// <value><c>true</c> if the platform can make payments; otherwise, <c>false</c>.</value>
		public static bool canMakePayments {
			get { return SKPaymentQueue.CanMakePayments(); }
		}

//		/// <summary>
//		/// Indicates whether the platform can make payments. If the user has disabled in-app purchase, this would be false. 
//		/// </summary>
//		/// <value><c>true</c> if the platform can make payments; otherwise, <c>false</c>.</value>
//		[Obsolete("Use the canMakePayments property.")]
//		public static bool CanMakePayments {
//			get { return SKPaymentQueue.CanMakePayments(); }
//		}

		/// <summary>
		/// A Dictionary of product IDs to SKProduct objects that have been successfully initialized.
		/// </summary>
		/// <value>The initialized products.</value>
		public static Dictionary<string, SKProduct> products {
			get { return _products; }
		}

//		/// <summary>
//		/// A Dictionary of product IDs to SKProduct objects that have been successfully initialized.
//		/// </summary>
//		/// <value>The initialized products.</value>
//		[Obsolete("Use the products property.")]
//		public static Dictionary<string, SKProduct> Products {
//			get { return _products; }
//		}

		/// <summary>
		/// Returns an initialized SKProduct object matching the specified product ID.
		/// </summary>
		/// <returns>The product.</returns>
		/// <param name="productID">Product ID</param>
		public static SKProduct GetProduct(string productID) {
			SKProduct product = null;
			_products.TryGetValue(productID, out product);
			return product;
		}

		/// <summary>
		/// Returns whether the product has been bought and stored in local cache.
		/// </summary>
		/// <remarks>
		/// This method only checks the local cache and not the server.
		/// So if it returns true, the user has definitely bought the product.
		/// However, if it returns false, it DOES NOT mean the user has not bought the product.
		/// It just means the user has not bought the product on this device with this installation.
		/// The user may have bought the same product on a different device or on a previous installation
		/// on the same device.
		/// </remarks>
		/// <returns><c>true</c> if has bought the specified productID; <c>false</c> if not bought locally.</returns>
		/// <param name="productID">the product ID to check whether it has been bought.</param>
		public static bool HasBought(string productID) {
			return _boughtProducts.Contains(productID);
		}

		/// <summary>
		/// Clears the local cache on whether products are bought.
		/// You should only use this during development for debugging purposes.
		/// </summary>
		public static void ClearCache() {
			_boughtProducts.Clear();
			_WriteCache();
		}

		/// <summary>
		/// Buy a product with specified productID and quantity.
		/// </summary>
		/// <remarks>
		/// This method starts a transaction with the server.
		/// If the DisplayActivityIndicator property is true, a semi-transparent native activity indicator view with label
		/// would cover the entire screen, blocking any user input from Unity.
		/// If you set the DisplayActivityIndicator property to false, you should provide a similar view blocking input
		/// during the purchase process so the user cannot start another purchase at the same time, for example.
		/// <p></p>
		/// On transaction complete, it raises either the TransactionCompleted or TransactionFailed event.
		/// On TransactionCompleted event, you should provide content to the user for what they have just bought.
		/// On TransactionFailed event, you can display an error message to the user.
		/// If the product has hosted download on Apple servers (available iOS 6.0 and later),
		/// the content would have been extracted and moved to app-storage:/downloads/[productID]/ by the time this event is raised.
		/// You can then move it somewhere else or use it as it is.
		/// </remarks>
		/// <returns><c>true</c> if a transaction has started; <c>false</c> otherwise.</returns>
		/// <param name="productID">The product to buy.</param>
		/// <param name="quantity">The quantity of the product to buy; it should be 1 for non-consumables.</param>
		public static bool Buy(string productID, int quantity = 1) {
			if (quantity <= 0)
				return false;

			var product = GetProduct(productID);
			if (product == null)
				return false;

			_ShowActivityIndicator("   Purchasing...");

			var payment = SKMutablePayment.Payment(product);
			payment.quantity = quantity;
			SKPaymentQueue.DefaultQueue().AddPayment(payment);

			return true;
		}

		internal static void _OnUpdatedTransactions(object[] transactions) {
			foreach (var transactionObj in transactions) {
				var transaction = transactionObj as SKPaymentTransaction;
				switch (transaction.transactionState) {
					case SKPaymentTransactionState.Purchased:
					case SKPaymentTransactionState.Restored:
						var hasDownloads = false;
						try {
							hasDownloads = (transaction.downloads != null);
						} catch (Exception) {
						}

						if (hasDownloads) {
							_Download(transaction.downloads);
						} else {
							_OnBought(transaction);
						}
						break;
					case SKPaymentTransactionState.Failed:
						_OnFailedBuy(transaction);
						break;
				}
			}
		}

		private static void _OnBought(SKPaymentTransaction transaction) {

			_boughtProducts.Add(transaction.payment.productIdentifier);
			_WriteCache();

			if (_transactionCompletedHandlers != null)
				_transactionCompletedHandlers(null, new TransactionEventArgs(transaction));

			_FinishTransaction(transaction);
		}

		private static void _OnFailedBuy(SKPaymentTransaction transaction, NSError error = null) {
			if (_transactionFailedHandlers != null)
				_transactionFailedHandlers(null, new TransactionEventArgs(transaction, error));

			_FinishTransaction(transaction);	
		}

		private static void _FinishTransaction(SKPaymentTransaction transaction) {
			SKPaymentQueue.DefaultQueue().FinishTransaction(transaction);

			_HideActivityIndicator();
		}

		/// <summary>
		/// The array of pending transactions.
		/// These are the transactions that have started but not yet called finished.
		/// </summary>
		/// <value>The pending transactions.</value>
		public static SKPaymentTransaction[] pendingTransactions {
			get {
				return SKPaymentQueue.DefaultQueue().transactions.Cast<SKPaymentTransaction>().ToArray();
			}
		}

//		/// <summary>
//		/// The array of pending transactions.
//		/// These are the transactions that have started but not yet called finished.
//		/// </summary>
//		/// <value>The pending transactions.</value>
//		[Obsolete("Use the pendingTransactions property.")]
//		public static SKPaymentTransaction[] PendingTransactions {
//			get {
//				return SKPaymentQueue.DefaultQueue().transactions.Cast<SKPaymentTransaction>().ToArray();
//			}
//		}

		private static void _Download(object[] downloads) {
			_ShowActivityIndicator("   Downloading...\n0%");
			SKPaymentQueue.DefaultQueue().StartDownloads(downloads);
		}

		internal static void _OnUpdatedDownload(object[] downloads) {
			foreach (var downloadObj in downloads) {
				var download = downloadObj as SKDownload;
				switch (download.downloadState) {
					case SKDownloadState.Active:
						_ShowActivityIndicator("   Downloading...\n" + Math.Round(download.progress * 100) + "%");
						break;
					case SKDownloadState.Finished:
						var srcPath = download.contentURL.Path() + "/Contents/";
						if (!Directory.Exists(srcPath)) {
//							Debug.Log("Starting download: " + srcPath);
							_Download(new object[]{download});
						} else {
							var dstPath = Application.persistentDataPath + "/downloads/";
							if (!Directory.Exists(dstPath))
								Directory.CreateDirectory(dstPath);
							dstPath += download.contentIdentifier + "/";
							if (Directory.Exists(dstPath))
								Directory.Delete(dstPath, true);
//							Debug.Log("Moving download from " + srcPath + " to " + dstPath);
							Directory.Move(srcPath, dstPath);
						}
						break;
				}
			}

			if (_downloadUpdatedHandlers != null)
				_downloadUpdatedHandlers(null, new DownloadEventArgs(downloads));

			// do this after because it needs to copy all before finishing anyone
			foreach (var downloadObj in downloads) {
				var download = downloadObj as SKDownload;
				if ((download.transaction != null) && _HasFinishedDownloads(download.transaction)) {

					if (download.downloadState == SKDownloadState.Finished)
						_OnBought(download.transaction);
					else
						_OnFailedBuy(download.transaction, download.error);
				}
			}
		}

		private static bool _HasFinishedDownloads(SKPaymentTransaction transaction) {
			foreach (var downloadObj in transaction.downloads) {
				var download = downloadObj as SKDownload;
				if ((download.downloadState == SKDownloadState.Active)
				    || (download.downloadState == SKDownloadState.Paused)
				    || (download.downloadState == SKDownloadState.Waiting)) {
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Restores all completed products that the user has bought.
		/// It's similar to Buy() in that it starts transactions. So it raises to the same events on transaction completion.
		/// Additionally, it raises RestorationCompleted and RestorationFailed events on completion.
		/// </summary>
		/// <seealso cref="Buy"/>
		public static void RestoreCompletedTransactions() {
			SKPaymentQueue.DefaultQueue().RestoreCompletedTransactions();
		}

		internal static void _OnRestoreCompleted() {
			if (_restorationCompletedHandlers != null)
				_restorationCompletedHandlers(null, EventArgs.Empty);
		}

		internal static void _OnRestoreFailed(NSError error) {
			if (_restorationFailedHandlers != null)
				_restorationFailedHandlers(null, new U3DXTErrorEventArgs(error));
		}

		/// <summary>
		/// Presents the native App Store interface with the product identified by its iTunes ID.
		/// <p></p>
		/// This is available in iOS 6.0 and later.
		/// </summary>
		/// <param name="iTunesID">The iTunes ID that identifies the product to show.</param>
		public static void ShowStore(string iTunesID) {
			_storeViewController = new SKStoreProductViewController();
//			vc.Delegate = StoreProductViewControllerDelegate.instance;

			var param = new Dictionary<object, object>();
			param[SKStoreProductViewController.SKStoreProductParameterITunesItemIdentifier] = iTunesID;

			_storeViewController.LoadProduct(param, delegate(bool result, NSError error) {
				if (result) {
					_storeViewController.DidFinish += _ShowStoreDidFinish;
					UIApplication.deviceRootViewController.PresentViewController(_storeViewController, true, null);
				} else {
					if (_storeViewLoadFailedHandlers != null)
						_storeViewLoadFailedHandlers(null, new U3DXTErrorEventArgs(error));
					_storeViewController = null;
				}
			});
		}

		private static void _ShowStoreDidFinish(object sender, EventArgs e) {
			_storeViewController.presentingViewController.DismissViewController(true, null);
			_storeViewController = null;
		}

		private static void _ShowActivityIndicator(string text) {
			if (displayActivityIndicator) {
				if (_activityIndicator == null)
					_activityIndicator = new ActivityIndicator();
				_activityIndicator.Show(text);
			}
		}

		private static void _HideActivityIndicator() {
			if (_activityIndicator != null) {
				_activityIndicator.Hide();
			}
		}

#endregion
	}
}

#endif
