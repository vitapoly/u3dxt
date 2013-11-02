#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.StoreKit;
using U3DXT.iOS.Native.Foundation;

namespace U3DXT.iOS.IAP.Helpers {
	/// <summary>
	/// Payment transaction observer.
	/// </summary>
	internal class PaymentTransactionObserver : SKPaymentTransactionObserver {

		private static PaymentTransactionObserver _instance;
		public static PaymentTransactionObserver instance {
			get {
				if (_instance == null)
					_instance = new PaymentTransactionObserver();
				return _instance;
			}
		}

		public PaymentTransactionObserver() {
		}

		public override void UpdatedTransactions(SKPaymentQueue queue, object[] transactions) {
			IAPXT._OnUpdatedTransactions(transactions);
		}

		public override void UpdatedDownloads(SKPaymentQueue queue, object[] downloads) {
			IAPXT._OnUpdatedDownload(downloads);
		}

		public override void RestoreCompletedTransactionsFinished(SKPaymentQueue queue) {
			IAPXT._OnRestoreCompleted();
		}

		public override void RestoreCompletedTransactionsFailed(SKPaymentQueue queue, NSError error) {
			IAPXT._OnRestoreFailed(error);
		}
	}
}

#endif
