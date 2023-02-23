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
	public class ReceiveCustomerInfoCallback: Java.Lang.Object, IReceiveCustomerInfoCallback
	{
		private bool errorOccurred;

		public ReceiveCustomerInfoCallback ()
		{
			errorOccurred = false;
		}

		public async void OnError ( PurchasesError error )
		{
			// We assume OnReceived is not called before OnError

			try
			{
				errorOccurred = true;

				// Get error name
				string errorName = error.Code.Name ();

				// Get corresponding enum numeric value
				int errorNumber = Int32.Parse ( error.Code.ToString () );
				// If above line does not work, use the following:					// *****************************************
				// int errorNumber = RevenueCatXamarin.Models.Enums.EnumNameToNumericValue ( typeof(RevenueCatXamarin.InAppEnums), errorName );

				// Call back to cross-platform code with the error status
                await RevenueCatXamarin.Views.InAppPurchases.ManageInAppPurchasesPage.RestorePurchasesCompletionDoneAsync ( errorNumber, errorName );
			}
			catch ( System.Exception ex )
			{
				Utilities.MyUtil.WriteLogFile ( Resx.S.Exception, ex.ToString() ) ;
				throw new System.Exception ( "In ReceiveCustomerInfoCallback.OnError " + ex.ToString (), ex.InnerException );
			}
		}

		public async void OnReceived ( CustomerInfo customerInfo )
		{
			try
			{
				// Bypass if error occurred
				if ( errorOccurred )
					return;

				// Call back to cross-platform code with null errorCode to indicate success
				await RevenueCatXamarin.Views.InAppPurchases.ManageInAppPurchasesPage.RestorePurchasesCompletionDoneAsync ( null, string.Empty );
			}
			catch ( System.Exception ex )
			{
				Utilities.MyUtil.WriteLogFile ( Resx.S.Exception, ex.ToString() ) ;
				throw new System.Exception ( "In ReceiveCustomerInfoCallback.OnReceived " + ex.ToString (), ex.InnerException );
			}
		}
	}
}

