using System;
using U3DXT.iOS.Native.Foundation;
using U3DXT.iOS.Native.StoreKit;

namespace U3DXT.iOS.IAP {
	/// <summary>
	/// Transaction event arguments.
	/// </summary>
	public class TransactionEventArgs : EventArgs {

		/// <summary>
		/// The native transaction.
		/// </summary>
		public readonly SKPaymentTransaction transaction;

		/// <summary>
		/// The product ID.
		/// </summary>
		public readonly string productID;

//		/// <summary>
//		/// The product ID.
//		/// </summary>
//		[Obsolete("Use the productID property.")]
//		public readonly string ProductID;

		/// <summary>
		/// The quantity.
		/// </summary>
		public readonly int quantity;

//		/// <summary>
//		/// The quantity.
//		/// </summary>
//		[Obsolete("Use the quantity property.")]
//		public readonly int Quantity;

		/// <summary>
		/// The error if occurred.
		/// </summary>
		public readonly NSError error;

//		/// <summary>
//		/// The error if occurred.
//		/// </summary>
//		[Obsolete("Use the error property.")]
//		public readonly NSError Error;

		/// <summary>
		/// Indicates whether this transaction has any downloads.
		/// </summary>
		public readonly bool hasDownloads = false;

//		/// <summary>
//		/// Indicates whether this transaction has any downloads.
//		/// </summary>
//		[Obsolete("Use the hasDownloads property.")]
//		public readonly bool HasDownloads = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.IAP.TransactionEventArgs"/> class.
		/// </summary>
		/// <param name="transaction">Transaction.</param>
		/// <param name="error">Error.</param>
		public TransactionEventArgs(SKPaymentTransaction transaction, NSError error = null) {

			this.transaction = transaction;

			try {
				hasDownloads = (transaction.downloads != null);
			} catch (Exception) {
			}

			if (error != null)
				this.error = error;
			else
				this.error = transaction.error;

			if ((transaction.transactionState == SKPaymentTransactionState.Restored)
			    && (transaction.originalTransaction != null)) {
				transaction = transaction.originalTransaction;
			}

			productID = transaction.payment.productIdentifier;
			quantity = transaction.payment.quantity;

//			this.HasDownloads = this.hasDownloads;
//			this.Error = this.error;
//			this.ProductID = this.productID;
//			this.Quantity = this.quantity;
		}
	}
}

