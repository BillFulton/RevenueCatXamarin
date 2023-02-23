using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RevenueCatXamarin
{
    public partial class App : Application
    {
        public static bool IsiOS = Xamarin.Forms.Device.RuntimePlatform == Xamarin.Forms.Device.iOS;            // Platform is iOS
        public static bool IsAndroid = Xamarin.Forms.Device.RuntimePlatform == Xamarin.Forms.Device.Android;    // Platform is Android
        public static NavigationPage NavPage;

        public App ()
        {
            InitializeComponent();

            MainPage = new NavigationPage ( new MainPage () );
            NavPage = (NavigationPage)MainPage;
        }

        protected override void OnStart ()
        {
        }

        protected override void OnSleep ()
        {
        }

        protected override void OnResume ()
        {
        }
    }
}

