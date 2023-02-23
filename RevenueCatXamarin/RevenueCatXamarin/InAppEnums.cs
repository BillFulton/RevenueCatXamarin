using System;
namespace RevenueCatXamarin
{
    public struct InAppEnums
    {
        // Entitlements (EntitlementInfo.PeriodType)
        public enum PeriodId { Intro, Normal, Trial }                                                           // Must match RCPeriodType
        public enum StoreId { Amazon, AppStore, MacAppStore, PlayStore, Promotional, Stripe, UnknownStore }     // Must match RCStore

        // Purchase error status (RCPurchasesErrorCode)
        // These codes for iOS are a superset of the codes for Android
        public enum PurchaseErrorStatus
        {   APIEndpointBlocked = 33,
            BeginRefundRequestError = 31,
            ConfigurationError = 23,
            CustomerInfoError = 29,
            EmptySubscriberAttributesError = 25,
            IneligibleError = 18,
            InsufficientPermissionsError = 19,
            InvalidAppleSubscriptionKeyError = 17,
            InvalidAppUserIdError = 14,
            InvalidCredentialsError = 11,
            InvalidPromotionalOfferError = 34,
            InvalidReceiptError = 8,
            InvalidSubscriberAttributesError = 21,
            LogOutAnonymousUserError = 22,
            MissingReceiptFileError = 9,
            NetworkError = 10,
            OfflineConnectionError = 35,
            OperationAlreadyInProgressForProductError = 15,
            PaymentPendingError = 20,
            ProductAlreadyPurchasedError = 6,
            ProductDiscountMissingIdentifierError = 26,
            ProductDiscountMissingSubscriptionGroupIdentifierError = 28,
            ProductNotAvailableForPurchaseError = 5,
            ProductRequestTimedOut = 32,
            PurchaseCancelledError = 1,
            PurchaseInvalidError = 4,
            PurchaseNotAllowedError = 3,
            ReceiptAlreadyInUseError = 7,
            ReceiptInUseByOtherSubscriberError = 13,
            StoreProblemError = 2,
            SystemInfoError = 30,
            UnexpectedBackendResponseError = 12,
            UnknownBackendError = 16,
            UnknownError = 0,
            UnsupportedError = 24
        }

        // Refund request status (RCRefundRequestStatus) - unused
        public enum RefundRequestStatus { Error, Success, UserCancelled }

        // IntroElegibilityStatus (RCIntroElegibilityStatus) - unused
        public enum IntroElegibilityStatus { Eligible, Ineligible, NoIntroOfferExists, Unknown }
    }
}

