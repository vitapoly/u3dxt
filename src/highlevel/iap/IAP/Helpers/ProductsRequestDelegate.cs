using System;
using U3DXT.iOS.Native.StoreKit;

namespace U3DXT.iOS.IAP.Helpers {
	/// <summary>
	/// Products request delegate.
	/// </summary>
	internal class ProductsRequestDelegate : SKProductsRequestDelegate {

		private static ProductsRequestDelegate _instance;
		public static ProductsRequestDelegate instance {
			get {
				if (_instance == null)
					_instance = new ProductsRequestDelegate();
				return _instance;
			}
		}

		public ProductsRequestDelegate() {
		}

		public override void DidReceive(SKProductsRequest request, SKProductsResponse response) {
//			IAPXT._OnProductResponse(response);
		}
	}
}

