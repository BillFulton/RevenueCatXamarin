using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RevenueCatXamarin;
using RevenueCatXamarin.Droid.Resx;
using RevenueCatXamarin.Droid.Utilities;
using Xamarin.RevenueCat;
using Xamarin.RevenueCat.Android;
using Xamarin.RevenueCat.Android.Extensions;

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

using Android.Content;
using Android.App;

using Xamarin.Forms;
using Plugin.CurrentActivity;
using static RevenueCatXamarin.InAppEnums;
using Android.Drm;

[assembly: Dependency (typeof (RevenueCatXamarin.Droid.InAppPurchases.InAppPurchases))]

namespace RevenueCatXamarin.Droid.InAppPurchases
{
    public class InAppPurchases : RevenueCatXamarin.IRevenueCat
    {
        private string localisedPriceMonthly, localisedPriceQuarterly, localisedPriceCitizenScientist, localisedPriceLifetime;
        private Package monthlyPackage;                     // Cached
        private Package quarterlyPackage;                   // Cached
        private Package citizenScientistPackage;            // Cached
        private Package lifetimePackage;                    // Cached
        private const int GRACE_PERIOD_DAYS = 16;           // Set by Apple
        private volatile bool initialised;                  // true => InitialiseRevenueCatAsync completed
        public static string ManagementUrl;                 // App Store / Play Store management URL (cache)

        private int subscriptionExpiry;                     // Subscription expiry, as days since 1 Jan 2000
        private Context currentContext;

        public InAppPurchases ()
        {
            initialised = false;
            currentContext = CrossCurrentActivity.Current.Activity;
        }
        
        public async Task InitialiseRevenueCatAsync ()
        // Initialises RevenueCat
        // Called from App.OnStart ()
        // Assumes Internet is available
        {
            try
            {
                #if ( DEBUG )
                    Purchases.DebugLogsEnabled = true;
                #else
                    Purchases.DebugLogsEnabled = false;
                #endif

                ManagementUrl = string.Empty;
                PurchasesConfiguration.Builder builder = new PurchasesConfiguration.Builder ( currentContext, "goog_xxxxxxxxxxxxxxxx" );    // Supply your own code here
                PurchasesConfiguration purchasesConfiguration = new PurchasesConfiguration ( builder );
                Purchases.Configure ( purchasesConfiguration );
			    Offerings offerings = await Purchases.SharedInstance.GetOfferingsAsync ();
                if ( offerings == null )
                    return;

			    Offering offering = offerings.Current;
                if ( offering == null )
                    return;

                monthlyPackage = offering.GetPackage ( "$rc_monthly" );
			    StoreProduct monthlyStoreProduct = monthlyPackage.Product;
			    localisedPriceMonthly = monthlyStoreProduct.Price;

                quarterlyPackage = offering.GetPackage ( "$rc_three_month" );
                StoreProduct quarterlyStoreProduct = quarterlyPackage.Product;
                localisedPriceQuarterly = quarterlyStoreProduct.Price;

                citizenScientistPackage = offering.GetPackage ( "citizen_scientist" );
                StoreProduct citizenScientistStoreProduct = citizenScientistPackage.Product;
                localisedPriceCitizenScientist = citizenScientistStoreProduct.Price;

                lifetimePackage = offering.GetPackage ( "$rc_lifetime" );
                StoreProduct lifetimeStoreProduct = lifetimePackage.Product;
                localisedPriceLifetime = lifetimeStoreProduct.Price;

                // Clean up
                monthlyStoreProduct.Dispose ();
                quarterlyStoreProduct.Dispose ();
                citizenScientistStoreProduct.Dispose ();
                lifetimeStoreProduct.Dispose ();
                offering.Dispose ();
                offerings.Dispose ();

                // Confirm done
                initialised = true;
            }
            catch ( Xamarin.RevenueCat.Android.Extensions.Exceptions.PurchasesErrorException ex )
            {
                string description = ex.PurchasesError.Code.Description;
                string diagnosis = ex.PurchasesError.UnderlyingErrorMessage;        // Debug - real reason for the problem
                RevenueCatXamarin.Droid.Utilities.Show_Dialog msg = new Utilities.Show_Dialog ();
                if ( description.Contains ( "problem with the store" ) )
                {
                    description = S.StoreProblemInDetail;       // Ask user to verify logged in to Google and re-start app
                    await msg.ShowDialogAsync ( S.Warning, description );

                    // Continuing is possible in some circumstances
                    return;
                }
                else if ( ex.PurchasesError.Code == PurchasesErrorCode.PurchaseNotAllowedError )
                {
                    // Results from Google Play status BILLING_UNAVAILABLE
                    // Likely causes:
                    //      The Play Store app on the user's device is out of date
                    //      Google Play is unable to charge the user's payment method
                    //      The user is an enterprise user and their enterprise admin has disabled users from making purchases
                    //      The user is in a country not supported by Google
                    // Advise user then allow user to continue

                    string message = S.BillingUnavailable1 + Environment.NewLine  + Environment.NewLine +
				     S.BillingUnavailable2 + Environment.NewLine  + Environment.NewLine +
				     S.BillingUnavailable3 + Environment.NewLine  +
				     S.BillingUnavailable4 + Environment.NewLine  +
				     S.BillingUnavailable5 + Environment.NewLine  +
				     S.BillingUnavailable6 + Environment.NewLine + Environment.NewLine +
				     S.BillingUnavailable7;
                                 
                    await msg.ShowDialogAsync ( S.Warning, message );
                    return;
                }
		else
                {
                    MyUtil.WriteLogFile ( S.Exception, ex.ToString () );
			throw new Exception ( "In InAppPurchases.InitialiseRevenueCatAsync #1" + ex.ToString () + " " +
                        description + " UnderlyingError: " + diagnosis, ex.InnerException );
                }
            }
            catch ( Exception ex )
            {
                MyUtil.WriteLogFile ( S.Exception, ex.ToString () );
		throw new Exception ( "In InAppPurchases.InitialiseRevenueCatAsync " + ex.ToString(), ex.InnerException );
            }
        }

        public bool IsInitialised ()
        // Returns true if InitialiseRevenueCatAsync() has completed
        {
            return initialised ? true : false;
        }

        // Immediately before calling the following methods, caller is expected to ensure InitialiseRevenueCatAsync() has completed (above method) and Internet is still available

        public string LocalisedPrice ( string productCode )
        // Returns localised price string for given product
        {
            try
            {
                switch ( productCode )
                {
                    case "monthly":
                        return localisedPriceMonthly;

                    case "quarterly":
                        return localisedPriceQuarterly;

                    case "citizenScientist":
                        return localisedPriceCitizenScientist;

                    case "lifetime":
                        return localisedPriceLifetime;

                    default:
                        return string.Empty;
                }
            }
            catch ( Exception ex )
            {
                MyUtil.WriteLogFile ( S.Exception, ex.ToString () );
				throw new Exception ( "In InAppPurchases.LocalisedPrice " + ex.ToString (), ex.InnerException );
            }
        }

        public string PurchaseProduct ( string productCode )
        // Initiates purchase transaction for given product
        // Returns transaction state string (string.Empty or "Cancelled" - completion code handles other status)
        // Assumes Internet is available
        {
            PurchaseSuccessInfo purchaseSuccessInfo = null;

            try
            {
                // Initiate purchase transaction

                PurchaseCallback listener = new PurchaseCallback ();

                switch ( productCode )
                {
                    case "monthly":
                        Purchases.SharedInstance.PurchasePackage ( (Activity)currentContext, monthlyPackage, listener );
                        break;

                    case "quarterly":
                        Purchases.SharedInstance.PurchasePackage ( (Activity)currentContext, quarterlyPackage, listener );
                        break;

                    case "citizenScientist":
                        Purchases.SharedInstance.PurchasePackage  ( (Activity)currentContext, citizenScientistPackage, listener );
                        break;

                    case "lifetime":
                        Purchases.SharedInstance.PurchasePackage  ( (Activity)currentContext, lifetimePackage, listener );
                        break;
                }
            }
            catch ( Exception ex )
            {
                MyUtil.WriteLogFile ( S.Exception, ex.ToString () );
				throw new Exception ( "In InAppPurchases.PurchaseProduct " + ex.ToString (), ex.InnerException );
            }

            if ( purchaseSuccessInfo == null )
                return string.Empty;

            // Return preliminary status (which we will likely ignore in favour of listener data)
            PurchaseState purchaseState = purchaseSuccessInfo.StoreTransaction.PurchaseState;
            if ( purchaseState == PurchaseState.Purchased )
                return "Purchased";
            else if ( purchaseState == PurchaseState.Pending )
                return "Pending";
            else if ( purchaseState == PurchaseState.UnspecifiedState )
                return "UnspecifiedState";
            return string.Empty;
        }

        public async Task<List<EntitlementInfo>> ActiveEntitlementsAsync ()
        // Returns details of current entitlements - elements can include "AllFeatures", "CitizenScientist"
        {
             try
             {
                List<RevenueCatXamarin.EntitlementInfo> retVal = new List<RevenueCatXamarin.EntitlementInfo> { };

                using (CustomerInfo customerInfo = await Purchases.SharedInstance.GetCustomerInfoAsync () )
                {
                    if ( customerInfo == null )
                        return retVal;      // Empty list
                    
                    ManagementUrl = customerInfo.ManagementURL == null ? string.Empty : customerInfo.ManagementURL.ToString ();

                    EntitlementInfos entitlementInfos = customerInfo.Entitlements;
                    if ( entitlementInfos == null )
                        return retVal;      // Empty list

                    IDictionary<string, Com.Revenuecat.Purchases.EntitlementInfo> activeEntitlementsDictionary = entitlementInfos.Active;

                    entitlementInfos.Dispose ();
                    entitlementInfos = null;

                    if ( activeEntitlementsDictionary.Count == 0 )
                    {
                        activeEntitlementsDictionary = null;
                        return retVal;      // Empty list
                    }

                    foreach ( KeyValuePair<string, Com.Revenuecat.Purchases.EntitlementInfo> kvp in activeEntitlementsDictionary )
                    {
                        Com.Revenuecat.Purchases.EntitlementInfo rcEntitlementInfo = kvp.Value;
                        RevenueCatXamarin.EntitlementInfo entitlementInfo = new RevenueCatXamarin.EntitlementInfo ();

                        entitlementInfo.Identifier = rcEntitlementInfo.Identifier;
                        entitlementInfo.ProductIdentifier = rcEntitlementInfo.ProductIdentifier;
                        entitlementInfo.WillRenew = rcEntitlementInfo.WillRenew;

                        if ( rcEntitlementInfo.PeriodType == PeriodType.Intro )
                            entitlementInfo.PeriodType = (int)RevenueCatXamarin.InAppEnums.PeriodId.Intro;
                        else if ( rcEntitlementInfo.PeriodType == PeriodType.Normal )
                            entitlementInfo.PeriodType = (int)RevenueCatXamarin.InAppEnums.PeriodId.Normal;
                        else if ( rcEntitlementInfo.PeriodType == PeriodType.Trial )
                            entitlementInfo.PeriodType = (int)RevenueCatXamarin.InAppEnums.PeriodId.Trial;

                        if ( rcEntitlementInfo.LatestPurchaseDate == null )
                            entitlementInfo.LatestPurchaseDate = default(DateTime);
                        else 
                            entitlementInfo.LatestPurchaseDate = FromJavaDate ( rcEntitlementInfo.LatestPurchaseDate );

                        if ( rcEntitlementInfo.OriginalPurchaseDate == null )
                            entitlementInfo.OriginalPurchaseDate = default(DateTime);
                        else
                            entitlementInfo.OriginalPurchaseDate = FromJavaDate ( rcEntitlementInfo.OriginalPurchaseDate );

                        if ( rcEntitlementInfo.ExpirationDate == null )
                            entitlementInfo.ExpirationDate = default(DateTime);
                        else
                            entitlementInfo.ExpirationDate = FromJavaDate ( rcEntitlementInfo.ExpirationDate );

                        //switch ( rcEntitlementInfo.Store )
                        if ( rcEntitlementInfo.Store == Store.Amazon )
                            entitlementInfo.Store = (int)RevenueCatXamarin.InAppEnums.StoreId.Amazon;
                        else if ( rcEntitlementInfo.Store == Store.AppStore )
                            entitlementInfo.Store = (int)RevenueCatXamarin.InAppEnums.StoreId.AppStore;
                        else if ( rcEntitlementInfo.Store == Store.MacAppStore )
                            entitlementInfo.Store = (int)RevenueCatXamarin.InAppEnums.StoreId.MacAppStore;
                        else if ( rcEntitlementInfo.Store == Store.PlayStore )
                            entitlementInfo.Store = (int)RevenueCatXamarin.InAppEnums.StoreId.PlayStore;
                        else if ( rcEntitlementInfo.Store == Store.Promotional )
                            entitlementInfo.Store = (int)RevenueCatXamarin.InAppEnums.StoreId.Promotional;
                        else if ( rcEntitlementInfo.Store == Store.Stripe )
                            entitlementInfo.Store = (int)RevenueCatXamarin.InAppEnums.StoreId.Stripe;
                        else
                             entitlementInfo.Store = (int)RevenueCatXamarin.InAppEnums.StoreId.UnknownStore;

                        entitlementInfo.IsSandbox = rcEntitlementInfo.IsSandbox;

                        if ( rcEntitlementInfo.UnsubscribeDetectedAt == null )
                            entitlementInfo.UnsubscribeDetectedAt = default(DateTime);
                        else
                            entitlementInfo.UnsubscribeDetectedAt = FromJavaDate ( rcEntitlementInfo.UnsubscribeDetectedAt );

                        if ( rcEntitlementInfo.BillingIssueDetectedAt == null )
                            entitlementInfo.BillingIssueDetectedAt = default(DateTime);
                        else
                            entitlementInfo.BillingIssueDetectedAt = FromJavaDate ( rcEntitlementInfo.BillingIssueDetectedAt );

                        retVal.Add ( entitlementInfo );
                    }

                    activeEntitlementsDictionary = null;
                }
               
                return retVal;
            }
            catch ( Exception ex )
            {
                MyUtil.WriteLogFile ( S.Exception, ex.ToString () );
				throw new Exception ( "In InAppPurchases.ActiveEntitlementsAsync " + ex.ToString (), ex.InnerException );
            }
        }

        public async Task<List<string>> NonConsumablePurchasesAsync ()
        // Returns list of product identifiers for all user's non-consumable product purchases
        {
            try
            {
                List<string> nonConsumablePurchases = new List<string> {};
                using ( CustomerInfo customerInfo = await Purchases.SharedInstance.GetCustomerInfoAsync () )
                {
                    if ( customerInfo == null )
                        return nonConsumablePurchases;                         // Empty list

                    IList<Transaction> nsNonConsumableTransactions = customerInfo.NonSubscriptionTransactions;  //  customerInfo.PurchasedNonSubscriptionSkus is obsolete
                    if ( nsNonConsumableTransactions == null )
                        return nonConsumablePurchases;                         // Empty list

                    foreach ( Transaction transaction in nsNonConsumableTransactions )
                    {
                        if ( transaction.ProductId.IsNullEmptyOrWhitespace () )
                            continue;

                        if ( transaction.PurchaseDate != null )
                        {
                            if ( ! nonConsumablePurchases.Contains ( transaction.ProductId ) )
                                nonConsumablePurchases.Add ( transaction.ProductId );
                        }
                    }    
                };

                return nonConsumablePurchases;
            }
            catch ( Exception ex )
            {
                MyUtil.WriteLogFile ( S.Exception, ex.ToString () );
				throw new Exception ( "In InAppPurchases.NonConsumablePurchasesAsync " + ex.ToString (), ex.InnerException );
            }
        }

        public async Task<List<string>> ActiveStoreSubscriptionsAsync ()
        // Returns active store subscriptions - NOTE: RevenueCat docs suggest this may be deprecated
        // Returns empty list if none
        {
            try
            {
                List<string> activeSubscriptions = new List<string> {};
                using ( CustomerInfo customerInfo = await Purchases.SharedInstance.GetCustomerInfoAsync () )
                {
                    if ( customerInfo == null )
                        return activeSubscriptions;                         // Empty list

                    ICollection<string> nsActiveSubscriptions = customerInfo.ActiveSubscriptions;
                    if ( customerInfo.ActiveSubscriptions == null )
                        return activeSubscriptions;                         // Empty list

                    foreach ( string subscription in nsActiveSubscriptions )
                        activeSubscriptions.Add ( subscription.ToString () );
                }

                return activeSubscriptions; 
            }
            catch ( Exception ex )
            {
                MyUtil.WriteLogFile ( S.Exception, ex.ToString () );
				throw new Exception ( "In InAppPurchases.ActiveSubscriptionsAsync " + ex.ToString (), ex.InnerException );
            }
        }

        public async Task<List<string>> AllPurchasedProductIdentifiersAsync ()
        // Returns list of all products purchased by the user, regardless of expiration
        // Returns empty list if none
        {
            try
            {
                List<string> allPurchasedProductIdentifiers = new List<string> {};
                using ( CustomerInfo customerInfo = await Purchases.SharedInstance.GetCustomerInfoAsync () )
                {
                    if ( customerInfo == null )
                        return allPurchasedProductIdentifiers;              // Empty list

                    ICollection<string> nsAllPurchasedProductIdentifiers = customerInfo.AllPurchasedSkus;
                    if ( customerInfo.AllPurchasedSkus == null )
                        return allPurchasedProductIdentifiers;              // Empty list

                    foreach ( string identifier in nsAllPurchasedProductIdentifiers )
                        allPurchasedProductIdentifiers.Add ( identifier.ToString () );
                }

                return allPurchasedProductIdentifiers; 
            }
            catch ( Exception ex )
            {
                MyUtil.WriteLogFile ( S.Exception, ex.ToString () );
				throw new Exception ( "In InAppPurchases.AllPurchasedProductIdentifiersAsync " + ex.ToString (), ex.InnerException );
            }
        }

        public async Task<DateTime> PurchaseDateForProductIdentifierAsync ( string productIdentifier )
        // Returns purchase date of given product identifier
        // Returns null if none
        {
            try
            {
                Java.Util.Date nsDate = null;
                using ( CustomerInfo customerInfo = await Purchases.SharedInstance.GetCustomerInfoAsync () )
                {
                    if ( customerInfo == null )
                        return default(DateTime);

                    nsDate = customerInfo.GetPurchaseDateForSku ( productIdentifier );
                }

                if ( nsDate == null )
                    return default(DateTime);

                return FromJavaDate ( nsDate );
            }
            catch ( Exception ex )
            {
                MyUtil.WriteLogFile ( S.Exception, ex.ToString () );
				throw new Exception ( "In InAppPurchases.PurchaseDateForProductIdentifierAsync " + ex.ToString (), ex.InnerException );
            }
        }

        public string RestorePurchases ()
        // Restores purchases
        // Returns any error indication, else string.Empty
        {
            try
            {
                // Restore purchases
                if ( Purchases.SharedInstance == null )
                    return S.NoPurchasesFound;

                Purchases.SharedInstance.RestorePurchases ( new ReceiveCustomerInfoCallback () );
                return string.Empty;
            }
            catch ( Exception ex )
            {
                MyUtil.WriteLogFile ( S.Exception, ex.ToString () );
				throw new Exception ( "In InAppPurchases.RestorePurchases " + ex.ToString (), ex.InnerException );
            }
        }

        public async Task<string> ManagementUrlAsync ( )
        // Returns url of store management page
        // Returns string.Empty if error
        {
            try
            {
                if ( ManagementUrl != null )
                    return ManagementUrl;       // Cached value

                string url = string.Empty;
                using ( CustomerInfo customerInfo = await Purchases.SharedInstance.GetCustomerInfoAsync () )
                {
                    if ( customerInfo != null )
                        url = customerInfo.ManagementURL.ToString ();
                }

                ManagementUrl = url;
                return url;
            }
            catch ( Exception ex )
            {
                MyUtil.WriteLogFile ( S.Exception, ex.ToString () );
				throw new Exception ( "In InAppPurchases.ManagementUrlAsync " + ex.ToString (), ex.InnerException );
            }
        }

        private DateTime FromJavaDate( Java.Util.Date javaDate )
        // Returns System date corresponding to given Java date
        {
            if ( javaDate == null )
                return default(DateTime);

            DateTime epoch = new DateTime ( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc );
            return epoch.AddMilliseconds ( javaDate.Time );
        }
    }
}

