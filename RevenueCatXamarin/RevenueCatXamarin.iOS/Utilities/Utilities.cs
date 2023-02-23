using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

using Foundation;
using UIKit;

using RevenueCatXamarin.iOS.Resx;
using RevenueCatXamarin.iOS;

namespace RevenueCatXamarin.iOS.Utilities
{
	public class MyUtil
	{
		public static UIViewController CurrentViewController ()
		// Returns the current view controller
		{
			UIWindow window = UIApplication.SharedApplication.KeyWindow;
			UIViewController vc = window.RootViewController;
			while (vc.PresentedViewController != null)
				vc = vc.PresentedViewController;
			return vc;
		}

		public static void ShowAlert ( string title, string message, string ok, Action onCompletedAction = null )
        // Displays an alert
        // Caller must ensure to be on main thread, e.g. InvokeOnMainThread ( () => { MyUtil.ShowAlert ( ... ); });
        // e.g. MyUtil.ShowAlert ( S.MyTitle, S.MyMessage, S.OK );
		// completedAction will be run when user has pressed ok
        {
            UIAlertController alertController = 
				UIAlertController.Create ( title, message, UIAlertControllerStyle.Alert );

			if ( onCompletedAction != null )
				alertController.AddAction ( UIAlertAction.Create ( S.OK, UIAlertActionStyle.Default, action => { onCompletedAction.Invoke (); } ) );

			CurrentViewController ().PresentViewController ( alertController, true, null );
		}
	}
}