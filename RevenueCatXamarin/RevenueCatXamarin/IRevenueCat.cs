using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RevenueCatXamarin
{
    public interface IRevenueCat
    {
        // RevenueCat for iOS V4.9.0.2, V4.9.0.4, Android 5.3.0.3
        // Access these methods via DependencyService

        Task InitialiseRevenueCatAsync ();                          // Initiate the asynchronous initialisation of RevenueCat - check Internet is available first
                                                                    // To minimise app startup delay, this can be called in a separate thread that has an initial delay of several seconds
        bool IsInitialised ();                                      // Returns true if InitialiseRevenueCatAsync() has completed

        // Immediately before calling the following methods, caller is expected to ensure InitialiseRevenueCatAsync() has completed (above method) and Internet is still available

        string LocalisedPrice ( string productCode );               // Returns localised price string for given product
        string PurchaseProduct ( string productCode );              // Performs purchase transaction for given product, returns transactionStatus
                                                                    // Transaction status is: "Purchasing", "Purchased", "Cancelled", "Failed", "Restored", "Deferred"
                                                                    // Caller should first verify not already purchased
        Task<List<EntitlementInfo>> ActiveEntitlementsAsync ();     // Returns details of current entitlements - elements can include "AllFeatures", "CitizenScientist"
        Task<List<string>> NonConsumablePurchasesAsync ();          // Returns list of all non-consumable product identifiers purchased by the user
        Task<List<string>> ActiveStoreSubscriptionsAsync ();        // Returns list of active store subscriptions
        Task<DateTime> PurchaseDateForProductIdentifierAsync ( string productIdentifier ); // Returns purchase date for given product identifier
        Task<List<string>> AllPurchasedProductIdentifiersAsync ();  // Returns list of all purchased product identifiers, regardless of expiration
        string RestorePurchases ();                                 // Restores purchases - returns one class of errors (other errors handled in Completion code)
        Task<string>ManagementUrlAsync ();                          // Returns link to webpage where user can manage his or her subscriptions
    }
}
