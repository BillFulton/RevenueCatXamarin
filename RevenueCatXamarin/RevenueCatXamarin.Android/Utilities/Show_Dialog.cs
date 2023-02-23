/* USAGE (no need to invoke main thread):
  
    private async Task A ()
    {
        RevenueCatXamarin.Droid.Utilities.Show_Dialog msg = new Utilities.Show_Dialog ();
        await msg.ShowDialogAsync ( "Error", "Message" );
    }

    OR

    private async Task B ()
    {
        RevenueCatXamarin.Droid.Utilities.Show_Dialog msg1 = new Utilities.Show_Dialog ();
        if ( await msg1.ShowDialogAsync ( "Error", "Message", true, false, Show_Dialog.MessageResult.YES, Show_Dialog.MessageResult.NO ) == Show_Dialog.MessageResult.YES )
        { 
            //Do anything
        }
    }
 
 */

using System;

using Android.App;
using System.Threading.Tasks;
using Plugin.CurrentActivity;

namespace RevenueCatXamarin.Droid.Utilities
{
    public class Show_Dialog
    {
        public enum MessageResult
        {
            NONE = 0,
            OK = 1,
            CANCEL = 2,
            ABORT = 3,
            RETRY = 4,
            IGNORE = 5,
            YES = 6,
            NO = 7
        }

        Activity mcontext;
        public Show_Dialog ()
        {
            this.mcontext = CrossCurrentActivity.Current.Activity;
        }

        public Task<MessageResult> ShowDialogAsync ( string Title, string Message, bool SetCancelable = false, bool SetInverseBackgroundForced = false, MessageResult PositiveButton = MessageResult.OK, MessageResult NegativeButton = MessageResult.NONE, MessageResult NeutralButton = MessageResult.NONE, int IconAttribute = Android.Resource.Attribute.AlertDialogIcon )
        {
            var tcs = new TaskCompletionSource<MessageResult>();

            var builder = new AlertDialog.Builder ( mcontext );
            builder.SetIconAttribute ( IconAttribute );
            builder.SetTitle ( Title );
            builder.SetMessage ( Message );
            builder.SetCancelable ( SetCancelable );

            builder.SetPositiveButton ( ( PositiveButton != MessageResult.NONE ) ? PositiveButton.ToString () : string.Empty, ( senderAlert, args ) =>
            {
                tcs.SetResult ( PositiveButton );
            });
            builder.SetNegativeButton ( ( NegativeButton != MessageResult.NONE ) ? NegativeButton.ToString () : string.Empty, delegate
            {
                tcs.SetResult ( NegativeButton );
            });
            builder.SetNeutralButton ( ( NeutralButton != MessageResult.NONE ) ? NeutralButton.ToString () : string.Empty, delegate
            {
                tcs.SetResult ( NeutralButton );
            });

            Xamarin.Forms.Device.BeginInvokeOnMainThread ( () =>
            {
                builder.Show ();
            });

            return tcs.Task;
        }
    }
}
