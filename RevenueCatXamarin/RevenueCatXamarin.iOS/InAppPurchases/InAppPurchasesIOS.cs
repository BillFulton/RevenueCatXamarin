using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Foundation;
using RevenueCat;
using Xamarin.RevenueCat.iOS.Extensions;
using Xamarin.Forms;
using RevenueCatXamarin.iOS.Resx;
using RevenueCatXamarin.iOS.Utilities;

[assembly: Dependency (typeof (RevenueCatXamarin.iOS.InAppPurchases.InAppPurchases))]

namespace RevenueCatXamarin.iOS.InAppPurchases
{
    public class InAppPurchases : RevenueCatXamarin.IRevenueCat
    {
        private string localisedPriceMonthly, localisedPriceQuarterly, localisedPriceCitizenScientist, localisedPriceLifetime;

        private RCPackage monthlyPackage;                   // Cached
        private RCPackage quarterlyPackage;                 // Cached
        private RCPackage citizenScientistPackage;          // Cached
        private RCPackage lifetimePackage;                  // Cached
        private const int GRACE_PERIOD_DAYS = 16;           // Set by Apple
        private volatile bool initialised;                  // true => InitialiseRevenueCatAsync completed
        public static string ManagementUrl;                 // App Store / Play Store management URL (cache)

        private int subscriptionExpiry;                     // Subscription expiry, as days since 1 Jan 2000

        public InAppPurchases ()
        {
            initialised = false;
        }
        
        public async Task InitialiseRevenueCatAsync ()
        // Initialises RevenueCat and caches packages and prices
        // Called from App.OnStart ()
        // Assumes Internet is available
        {
            try
            {
                #if ( DEBUG )
                    RCPurchases.DebugLogsEnabled = true;
                #else
                    RCPurchases.DebugLogsEnabled = false;
                #endif

                ManagementUrl = string.Empty;

			    RCPurchases.ConfigureWithAPIKey ( "appl_xxxxxxxxxxxxxxxxxxxxxxxxx" );   // Substitute your own code
			    RCOfferings offerings = await RCPurchases.SharedPurchases.GetOfferingsAsync ();
                if ( offerings == null )
                    return;

			    RCOffering offering = offerings.Current;
                if ( offering == null )
                    return;

                monthlyPackage = offering.PackageWithIdentifier ( "$rc_monthly" );
			    RCStoreProduct monthlyStoreProduct = monthlyPackage.StoreProduct;
			    localisedPriceMonthly = monthlyStoreProduct.LocalizedPriceString;

                quarterlyPackage = offering.PackageWithIdentifier ( "$rc_three_month" );
                RCStoreProduct quarterlyStoreProduct = quarterlyPackage.StoreProduct;
                localisedPriceQuarterly = quarterlyStoreProduct.LocalizedPriceString;

                citizenScientistPackage = offering.PackageWithIdentifier ( "citizen_scientist" );
                RCStoreProduct citizenScientistStoreProduct = citizenScientistPackage.StoreProduct;
                localisedPriceCitizenScientist = citizenScientistStoreProduct.LocalizedPriceString;

                lifetimePackage = offering.PackageWithIdentifier ( "$rc_lifetime" );
                RCStoreProduct lifetimeStoreProduct = lifetimePackage.StoreProduct;
                localisedPriceLifetime = lifetimeStoreProduct.LocalizedPriceString;

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
            catch ( Xamarin.RevenueCat.iOS.Extensions.PurchasesErrorException ex )
            {
                Device.BeginInvokeOnMainThread ( () =>
                {
                    string description = ex.PurchasesError.Description;
                    NSError [] diagnosis = ex.PurchasesError.UnderlyingErrors;      // Debug - the real reason for the problem
                    if ( description.Contains ( "problem with the store" ) )
                        description = S.StoreProblemInDetail;                       // Ask user to restart app
                    MyUtil.ShowAlert ( S.Warning, description, S.OK );
                });

                // Continuing is possible in some circumstances
                return;
            }
            catch ( Exception ex )
            {
                MyUtil.WriteLogFile ( S.Exception, ex.ToString () );
				throw new Exception ( "In InAppPurchases.InitialiseRevenueCatAsync " + ex.ToString (), ex.InnerException );
            }
        }

        public bool IsInitialised ()
        // Returns true if InitialiseRevenueCatAsync() has completed
        {
            return initialised ? true: false;
        }

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
        // Returns string.Empty
        // Assumes Internet is available
        {
            string transactionState = String.Empty;

            try
            {
                // Define completion action

                Action<RCStoreTransaction, RCCustomerInfo, NSError, bool> purchaseCompletion =
                    new Action<RCStoreTransaction, RCCustomerInfo, NSError, bool> ( async ( transaction, customerInfo, error, status ) =>
                {
                    // Here when product purchase is complete

                    // Get transaction status
                    if ( transaction == null )
                        return;

                    if ( transaction.Sk1Transaction == null )
                        return;

                    StoreKit.SKPaymentTransactionState skTransactionState = transaction.Sk1Transaction.TransactionState;

                    transactionState = TransactionStateToString ( skTransactionState );          //  "Purchased", "Deferred", "Failed", ...

                    // Check for error
                    int? errorCode;
                    string errorDescription;
                    if ( error == null )
                    {
                        errorCode = null;
                        errorDescription = String.Empty;
                    }
                    else
                    {
                        errorCode = (int)error.Code;
                        errorDescription = (string)error.Description;
                    }

                    // Call back to platform-independent code with the status
                    await RevenueCatXamarin.Views.InAppPurchases.ManageInAppPurchasesPage.PurchaseProductCompletionDoneAsync ( transactionState, errorCode, errorDescription );
                });

                // Initiate purchase transaction
                switch ( productCode )
                {
                    case "monthly":
                        RCPurchases.SharedPurchases.PurchasePackage ( monthlyPackage, purchaseCompletion ); // Note: There is an overload available for a promotional package
                        break;

                    case "quarterly":
                        RCPurchases.SharedPurchases.PurchasePackage ( quarterlyPackage, purchaseCompletion );
                        break;

                    case "citizenScientist":
                        RCPurchases.SharedPurchases.PurchasePackage  ( citizenScientistPackage, purchaseCompletion );
                        break;

                    case "lifetime":
                        RCPurchases.SharedPurchases.PurchasePackage  ( lifetimePackage, purchaseCompletion );
                        break;
                }

            }
            catch ( Exception ex )
            {
                MyUtil.WriteLogFile ( S.Exception, ex.ToString () );
				throw new Exception ( "In InAppPurchases.PurchaseProduct " + ex.ToString (), ex.InnerException );
            }

            return string.Empty;        // No worthwhile preliminary status to report (unlike Android)
        }

        public async Task<List<RevenueCatXamarin.EntitlementInfo>> ActiveEntitlementsAsync ()
        // Returns user's active entitlements
        // Returns empty list if none
        {
            try
            {
                List<RevenueCatXamarin.EntitlementInfo> retVal = new List<RevenueCatXamarin.EntitlementInfo> { };

                using ( RCCustomerInfo customerInfo = await RCPurchases.SharedPurchases.GetCustomerInfoAsync () )
                {
                    if ( customerInfo == null )
                        return retVal;      // Empty list
                    
                    ManagementUrl = customerInfo.ManagementURL == null ? string.Empty : customerInfo.ManagementURL.ToString ();

                    RCEntitlementInfos entitlementInfos = customerInfo.Entitlements;
                    if ( entitlementInfos == null )
                        return retVal;      // Empty list

                    NSDictionary<NSString, RCEntitlementInfo> activeEntitlementsDictionary = entitlementInfos.Active;

                    entitlementInfos.Dispose ();
                    entitlementInfos = null;

                    if ( activeEntitlementsDictionary.Count == 0 )
                    {
                        activeEntitlementsDictionary.Dispose ();
                        activeEntitlementsDictionary = null;

                        return retVal;      // Empty list
                    }

                    foreach ( KeyValuePair<NSObject, NSObject> kvp in activeEntitlementsDictionary )
                    {
                        RCEntitlementInfo rcEntitlementInfo = (RCEntitlementInfo)kvp.Value;
                        RevenueCatXamarin.EntitlementInfo entitlementInfo = new RevenueCatXamarin.EntitlementInfo ();

                        entitlementInfo.Identifier = rcEntitlementInfo.Identifier;
                        entitlementInfo.ProductIdentifier = rcEntitlementInfo.ProductIdentifier;
                        entitlementInfo.WillRenew = rcEntitlementInfo.WillRenew;

                        switch ( rcEntitlementInfo.PeriodType )
                        {
                            case RCPeriodType.Intro:
                                entitlementInfo.PeriodType = (int)RevenueCatXamarin.InAppEnums.PeriodId.Intro;
                                break;

                            case RCPeriodType.Normal:
                                entitlementInfo.PeriodType = (int)RevenueCatXamarin.InAppEnums.PeriodId.Normal;
                                break;

                            case RCPeriodType.Trial:
                                entitlementInfo.PeriodType = (int)RevenueCatXamarin.InAppEnums.PeriodId.Trial;
                                break;
                            
                            default:
                                break;
                        }

                        if ( rcEntitlementInfo.LatestPurchaseDate == null )
                            entitlementInfo.LatestPurchaseDate = default(DateTime);
                        else
                            entitlementInfo.LatestPurchaseDate = (DateTime)rcEntitlementInfo.LatestPurchaseDate;

                        if ( rcEntitlementInfo.OriginalPurchaseDate == null )
                            entitlementInfo.OriginalPurchaseDate = default(DateTime);
                        else
                            entitlementInfo.OriginalPurchaseDate = (DateTime)rcEntitlementInfo.OriginalPurchaseDate;

                        if ( rcEntitlementInfo.ExpirationDate == null )
                            entitlementInfo.ExpirationDate = default(DateTime);
                        else
                            entitlementInfo.ExpirationDate = (DateTime)rcEntitlementInfo.ExpirationDate;

                        switch ( rcEntitlementInfo.Store )
                        {
                            case RCStore.Amazon:
                                entitlementInfo.Store = (int)RevenueCatXamarin.InAppEnums.StoreId.Amazon;
                                break;

                            case RCStore.AppStore:
                                entitlementInfo.Store = (int)RevenueCatXamarin.InAppEnums.StoreId.AppStore;
                                break;

                            case RCStore.MacAppStore:
                                entitlementInfo.Store = (int)RevenueCatXamarin.InAppEnums.StoreId.MacAppStore;
                                break;

                            case RCStore.PlayStore:
                                entitlementInfo.Store = (int)RevenueCatXamarin.InAppEnums.StoreId.PlayStore;
                                break;

                            case RCStore.Promotional:
                                entitlementInfo.Store = (int)RevenueCatXamarin.InAppEnums.StoreId.Promotional;
                                break;

                            case RCStore.Stripe:
                                entitlementInfo.Store = (int)RevenueCatXamarin.InAppEnums.StoreId.Stripe;
                                break;

                            case RCStore.UnknownStore:
                                entitlementInfo.Store = (int)RevenueCatXamarin.InAppEnums.StoreId.UnknownStore;
                                break;

                            default:
                                break;
                        }

                        entitlementInfo.IsSandbox = rcEntitlementInfo.IsSandbox;

                        if ( rcEntitlementInfo.UnsubscribeDetectedAt == null )
                            entitlementInfo.UnsubscribeDetectedAt = default(DateTime);
                        else
                            entitlementInfo.UnsubscribeDetectedAt = (DateTime)rcEntitlementInfo.UnsubscribeDetectedAt;

                        if ( rcEntitlementInfo.BillingIssueDetectedAt == null )
                            entitlementInfo.BillingIssueDetectedAt = default(DateTime);
                        else
                            entitlementInfo.BillingIssueDetectedAt = (DateTime)rcEntitlementInfo.BillingIssueDetectedAt;

                        rcEntitlementInfo.Dispose ();
                        rcEntitlementInfo = null;

                        retVal.Add ( entitlementInfo );
                    }

                    activeEntitlementsDictionary.Dispose ();
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
                using ( RCCustomerInfo customerInfo = await RCPurchases.SharedPurchases.GetCustomerInfoAsync () )
                {
                    if ( customerInfo == null )
                        return nonConsumablePurchases;                         // Empty list

                    NSSet<NSString> nsNonConsumablePurchases = customerInfo.NonConsumablePurchases;
                    if ( nsNonConsumablePurchases == null )
                        return nonConsumablePurchases;                         // Empty list

                    foreach ( NSString purchase in nsNonConsumablePurchases )
                        nonConsumablePurchases.Add ( purchase.ToString () );
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
        // Returns user's current store subscriptions - NOTE: RevenueCat docs suggest this might be deprecated
        // Returns empty list if none
        {
            try
            {
                List<string> activeSubscriptions = new List<string> {};
                using ( RCCustomerInfo customerInfo = await RCPurchases.SharedPurchases.GetCustomerInfoAsync () )
                {
                    if ( customerInfo == null )
                        return activeSubscriptions;                         // Empty list

                    NSSet<NSString> nsActiveSubscriptions = customerInfo.ActiveSubscriptions;
                    if ( nsActiveSubscriptions == null )
                        return activeSubscriptions;                         // Empty list

                    foreach ( NSString subscription in nsActiveSubscriptions )
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
                using ( RCCustomerInfo customerInfo = await RCPurchases.SharedPurchases.GetCustomerInfoAsync () )
                {
                    if ( customerInfo == null )
                        return allPurchasedProductIdentifiers;              // Empty list

                    NSSet<NSString> nsAllPurchasedProductIdentifiers = customerInfo.AllPurchasedProductIdentifiers;
                    if ( customerInfo.AllPurchasedProductIdentifiers == null )
                        return allPurchasedProductIdentifiers;              // Empty list

                    foreach ( NSString identifier in nsAllPurchasedProductIdentifiers )
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
                NSDate nsDate = null;
                using ( RCCustomerInfo customerInfo = await RCPurchases.SharedPurchases.GetCustomerInfoAsync () )
                {
                    if ( customerInfo == null )
                        return default(DateTime);

                    nsDate = customerInfo.PurchaseDateForProductIdentifier ( productIdentifier );
                }

                return nsDate == null ? default(DateTime) : (DateTime)nsDate; 
            }
            catch ( Exception ex )
            {
                MyUtil.WriteLogFile ( S.Exception, ex.ToString () );
				throw new Exception ( "In InAppPurchases.PurchaseDateForProductIdentifierAsync " + ex.ToString (), ex.InnerException );
            }
        }

        public string RestorePurchases ()
        // Restores purchases
        // Returns one class of errors - other errors are handled in Completion code
        // Async here because Android requires async
        {
            try
            {
                // Define completion action

                int? errorCode;

                Action<RCCustomerInfo, NSError>  restorePurchasesCompletion = new Action<RCCustomerInfo, NSError> ( async ( customerInfo, error ) =>
                {
                    // Here when purchases restored

                    string errorDescription;
                    if ( error == null )                    // RCPurchasesErrorCode
                    {
                        errorCode = null;
                        errorDescription = String.Empty;
                    }
                    else
                    {
                        errorCode = (int)error.Code;
                        errorDescription = (string)error.Description;
                    }

                    // Call back to cross-platform code with the status
                    await RevenueCatXamarin.Views.InAppPurchases.ManageInAppPurchasesPage.RestorePurchasesCompletionDoneAsync ( errorCode, errorDescription );
                });


                // Restore purchases
                if ( RCPurchases.SharedPurchases == null )
                    return S.NoPurchasesFound;

                RCPurchases.SharedPurchases.RestorePurchasesWithCompletion ( restorePurchasesCompletion );

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
                using ( RCCustomerInfo customerInfo = await RCPurchases.SharedPurchases.GetCustomerInfoAsync () )
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

        private string TransactionStateToString ( StoreKit.SKPaymentTransactionState transactionState )
        // Returns string corresponding to given transactionState
        {
            string transactionStatus;

            switch ( transactionState )
            {
                case StoreKit.SKPaymentTransactionState.Purchasing:
                    transactionStatus = "Purchasing";
                    break;

                case StoreKit.SKPaymentTransactionState.Purchased:
                    transactionStatus = "Purchased";
                    break;

                case StoreKit.SKPaymentTransactionState.Failed:
                    transactionStatus = "Failed";
                    break;

                case StoreKit.SKPaymentTransactionState.Restored:
                    transactionStatus = "Restored";
                    break;


                case StoreKit.SKPaymentTransactionState.Deferred:
                    transactionStatus = "Deferred";
                    break;

                default:
                    transactionStatus = "Failed";
                    break;
            }

            return transactionStatus;
        }
    }
}

