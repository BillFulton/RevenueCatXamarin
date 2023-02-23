using System;

using Com.Revenuecat.Purchases;
using Com.Revenuecat.Purchases.Api;
using Com.Revenuecat.Purchases.Common;
using Com.Revenuecat.Purchases.Google;
using Com.Revenuecat.Purchases.Identity;
using Com.Revenuecat.Purchases.Interfaces;
using Com.Revenuecat.Purchases.Models;
using Com.Revenuecat.Purchases.Strings;
using Com.Revenuecat.Purchases.Subscriberattributes;
using Com.Revenuecat.Purchases.Utils;

namespace RevenueCatXamarin.Droid.InAppPurchases
{
	public class PurchaseCallback : Java.Lang.Object, IPurchaseCallback
	{
		private bool errorOrCancellation;
	
		public PurchaseCallback ()
		{
			errorOrCancellation = false;
		}

		public async void OnError ( PurchasesError error, bool userCancelled )
		{
			// We assume OnReceived is not called before OnError

			try
			{
				// Bypass future OnCompleted
				errorOrCancellation = true;

				// Get error name
				string errorName = error.Code.ToString ();

				// Get corresponding enum numeric value
				int errorNumber = error.Code.Code;

				// Cancellation
				if ( userCancelled || error.Code == PurchasesErrorCode.PurchaseCancelledError )
				{
					// Call back to platform-independent code with the status
					await RevenueCatXamarin.Views.InAppPurchases.ManageInAppPurchasesPage.PurchaseProductCompletionDoneAsync ( transactionState: string.Empty,
																													 errorCode : (int)InAppEnums.PurchaseErrorStatus.PurchaseCancelledError,
																													 errorDescription: errorName );
					return;
				}

				// Handle other errors - call back to platform-independent code with the status
				await RevenueCatXamarin.Views.InAppPurchases.ManageInAppPurchasesPage.PurchaseProductCompletionDoneAsync ( transactionState: string.Empty,
																												 errorCode : errorNumber,
																												 errorDescription: errorName );
			}
			catch ( System.Exception ex )
			{
				Utilities.MyUtil.WriteLogFile ( Resx.S.Exception, ex.ToString() ) ;
				throw new System.Exception ( "In PurchaseCallback.OnError " + ex.ToString (), ex.InnerException );
			}
		}

		public async void OnCompleted ( StoreTransaction storeTransaction, CustomerInfo customerInfo )
		// Here when product purchase is complete
		{
			try
			{ 
				// Skip if there was an error or cancellation
				if ( errorOrCancellation )
					return;

				// Get transaction state
				string transactionState = storeTransaction.PurchaseState.Name ();		// "Purchased", "Pending" or "UnspecifiedState"

                // Call back to platform-independent code with the status
                await RevenueCatXamarin.Views.InAppPurchases.ManageInAppPurchasesPage.PurchaseProductCompletionDoneAsync ( transactionState, null, string.Empty );
			}
			catch ( System.Exception ex )
			{
				Utilities.MyUtil.WriteLogFile ( Resx.S.Exception, ex.ToString() ) ;
				throw new System.Exception ( "In PurchaseCallback.OnCompleted " + ex.ToString (), ex.InnerException );
			}
		}
	}
}

