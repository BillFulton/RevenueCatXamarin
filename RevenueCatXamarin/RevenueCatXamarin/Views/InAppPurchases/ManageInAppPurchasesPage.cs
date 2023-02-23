using System;
using System.Threading.Tasks;
using System.Linq;

using Xamarin.Forms;

using RevenueCatXamarin.Resx;
using RevenueCatXamarin.Views.InAppPurchases;

namespace RevenueCatXamarin.Views.InAppPurchases
{
    public class ManageInAppPurchasesPage : ContentPage
    // Assumes Internet is available
    {
        public static ManageInAppPurchasesPage Current;

        public ManageInAppPurchasesPage ()
        {
            Current = this;
        }

        // * UI stuff and logic from my app removed here **

        public static async Task PurchaseProductCompletionDoneAsync ( string transactionState, int? errorCode, string errorDescription )
        // Performs actions required once Completion for PurchaseProduct is done
        // errorDescription is for developer eyes only
        // NOTE: On IOS, called with both transaction state and errors; on Android, called with either an error status or (there being no error) the transaction state
        {
            // Check errorCode
            if ( errorCode != null )
            {
                switch ( errorCode )
                {
                    case (int)InAppEnums.PurchaseErrorStatus.PurchaseCancelledError:
                        // Not an error - ignore
                        break;

                    case (int)InAppEnums.PurchaseErrorStatus.PaymentPendingError:
                        // Advise user they may need to restart app after completing payment
                        await Device.InvokeOnMainThreadAsync ( async () =>
                        {
                            await App.NavPage.DisplayAlert ( T.PaymentPending, T.RestartAfterPayment, T.ButtonOK );
                        });
                        break;

                    default:
                        // An error occurred
                        await Device.InvokeOnMainThreadAsync ( async () =>
                        {
                            string briefErrorText = Enums.EnumToTextValue ( typeof(InAppEnums.PurchaseErrorStatus), (int)errorCode );
                            await App.NavPage.DisplayAlert ( T.Error, briefErrorText, T.ButtonOK );
                        });
                        return;
                }
            }

            // Check status
            switch ( transactionState )
            {
                case "Purchased":           // iOS, Android
                    // Here when a purchase is complete - update the user type
                    if ( Utility.IsInternetAvailable () )
                    {
                        // Update user type ( App.UserType and nonvolatile setting)
                        string entitlement = await App.UpdateUserTypeAsync ();
                        await Current.Navigation.PopAsync ();
                    }
                    else
                        throw new Exception ( "Internet dropped out just after inApp purchase was initiated" );
                    break;

                case "Cancelled":
                    break;

                case "Failed":              // iOS
                case "UnspecifiedState":    // Android
                    await Device.InvokeOnMainThreadAsync ( async () =>
                    {
                        await App.NavPage.DisplayAlert ( T.Information, T.PurchaseNotCompleted, T.ButtonOK );
                        await Current.Navigation.PopAsync ();
                    });
                    break;

                case "Deferred":            // iOS
                case "Purchasing":          // iOS
                case "Pending":             // Android
                    // Presumably this is payment deferred
                    await Device.InvokeOnMainThreadAsync ( async () =>
                    {
                        // Advise user they may need to restart RevenueCatXamarin once payment is made
                        await App.NavPage.DisplayAlert ( T.PaymentPending, T.RestartAfterPayment, T.ButtonOK );
                    });
                    break;

                    case "Restored":        // iOS
                        break;

                default:
                    break;
            }
        }

        public static async Task RestorePurchasesCompletionDoneAsync ( int? errorCode, string errorDescription )
        // Performs actions required once Completion for RestorePurchases is done
        // errorDescription is string.Empty if no error, else error text (for developer eyes only)
        {
            if ( errorCode != null )
            {
                // An error occurred
                await Device.InvokeOnMainThreadAsync ( async () =>
                {
                    Current.activityIndicator.IsRunning = false;
                    string briefErrorText = Enums.EnumToTextValue ( typeof(InAppEnums.PurchaseErrorStatus), (int)errorCode );
                    await App.NavPage.DisplayAlert ( T.Error, briefErrorText, T.ButtonOK );
                });

                // Save what we can
                await App.UpdateUserTypeAsync ();
               
                return;
            }

            // Here if no error

            if ( Utility.IsInternetAvailable () )
            {
                // Set user type
                string entitlement = await App.UpdateUserTypeAsync ();

                if ( entitlement == "AllFeatures" || entitlement == "CitizenScientist" )
                {
                    await Device.InvokeOnMainThreadAsync ( async () =>
                    {
                        Current.activityIndicator.IsRunning = false;
                        await App.NavPage.DisplayAlert ( T.Information, T.PurchasesRestored + " (" + entitlement + ")", T.ButtonOK );
                    });
                }
                else
                {
                    // Should not happen unless there never was a purchase
                    await Device.InvokeOnMainThreadAsync ( async () =>
                    {
                        Current.activityIndicator.IsRunning = false;
                        string msg = T.PurchasesNotRestored;
                        if ( ! entitlement.IsNullEmptyOrWhitespace () )
                            msg += " (" + entitlement + ")";
                        await App.NavPage.DisplayAlert ( T.Information, msg, T.ButtonOK );
                    });
                }
            }
            else
            {
                await Device.InvokeOnMainThreadAsync ( async () =>
                {
                    Current.activityIndicator.IsRunning = false;
                    await App.NavPage.DisplayAlert ( T.InternetUnavailable, T.InternetRequired, T.ButtonOK );
                });

                return;
            }
        }   
    }
}



