using System;
namespace RevenueCatXamarin
{
    public struct EntitlementInfo
        // In app purchase entitlement details (active entitlements)
    {
        public string Identifier;               // The entitlement identifier configured in the RevenueCat dashboard
        public string ProductIdentifier;        // The underlying product identifier that unlocked this entitlement
        public bool WillRenew;                  // Whether or not the entitlement is set to renew at the end of the current period
        public int PeriodType;                  // enum PeriodId - Intro, Normal, Trial
        public DateTime LatestPurchaseDate;     // The latest purchase or renewal date for this entitlement
        public DateTime OriginalPurchaseDate;   // The first date this entitlement was purchased
        public DateTime ExpirationDate;         // The expiration date for this entitlement - can be null for lifetime access
                                                // If period type is Trial, this is the trial expiration date
        public int Store;                       // enum StoreId - the store that unlocked this entitlement (App Store, Play Store, ...)
        public bool IsSandbox;                  // Whether this entitlement was unlocked from a sandbox or production purchase
        public DateTime UnsubscribeDetectedAt;  // The date an unsubscribe was detected - does not mean the entitlement is inactive
        public DateTime BillingIssueDetectedAt; // The date a billing issue was detected - null once resolved - does not mean the entitlement is inactive
    }
}

