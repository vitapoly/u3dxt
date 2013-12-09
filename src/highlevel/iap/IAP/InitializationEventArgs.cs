#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Native.StoreKit;
using System.Collections;
using System.Linq;
using U3DXT.iOS.Native.Foundation;

namespace U3DXT.iOS.IAP {
	/// <summary>
	/// Initialization event arguments.
	/// </summary>
	public class InitializationEventArgs : EventArgs {

		/// <summary>
		/// The invalid IDs.
		/// </summary>
		public readonly string[] invalidIDs;

//		/// <summary>
//		/// The invalid IDs.
//		/// </summary>
//		[Obsolete("Use the invalidIDs property.")]
//		public readonly string[] InvalidIDs;

		/// <summary>
		/// The products.
		/// </summary>
		public readonly SKProduct[] products;

		/// <summary>
		/// The error if exists.
		/// </summary>
		public readonly NSError error;

//		/// <summary>
//		/// The products.
//		/// </summary>
//		[Obsolete("Use the products property.")]
//		public readonly SKProduct[] Products;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.IAP.InitializationEventArgs"/> class.
		/// </summary>
		/// <param name="response">Response.</param>
		public InitializationEventArgs(SKProductsResponse response) {
			if (response.invalidProductIdentifiers == null)
				invalidIDs = new string[0];
			else
				invalidIDs = response.invalidProductIdentifiers.Cast<string>().ToArray();
			products = response.products.Cast<SKProduct>().ToArray();

			error = null;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.IAP.InitializationEventArgs"/> class.
		/// </summary>
		/// <param name="invalidIDs">Invalid IDs.</param>
		/// <param name="error">Error.</param>
		public InitializationEventArgs(string[] invalidIDs, NSError error) {
			this.invalidIDs = invalidIDs;
			this.error = error;
			products = new SKProduct[0];
		}
	}
}

#endif
