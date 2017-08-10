using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Cosas.Model;
using System.Collections;

namespace Cosas
{
    // Public arguments of our event delegate
    public class AuthEventArgs : EventArgs
    {
        public string User { get; set; }
        public string Password { get; set; }
    }

    // Dialog event delegate
    public delegate void AuthEventHandler(object sender, AuthEventArgs args);

    [Activity(Label = "Authentication")]
    public class AuthDialog : DialogFragment
    {
        public event AuthEventHandler Dismissed;

        private string user;
        private string password;

        // Contructor for debug email/password constants
        public AuthDialog(string user, string password)
        {
            this.user = user;
            this.password = password;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Full screen with material theme only available since Lollipop
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
            {
                // Make full screen
                SetStyle(DialogFragmentStyle.Normal, Android.Resource.Style.ThemeMaterialLightNoActionBarFullscreen);
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set dialog title
            this.Dialog.SetTitle(this.Tag);

            // Load view from xml
            var view = inflater.Inflate(Resource.Layout.Auth, container, false);

            // get ui elements
            var textEmail = view.FindViewById<TextView>(Resource.Id.email);
            var textPassword = view.FindViewById<TextView>(Resource.Id.password);
            var buttonAuth = view.FindViewById<Button>(Resource.Id.authenticate);

            textEmail.Text = user;
            textPassword.Text = password;

            // Set focus and show keyboard
            textEmail.RequestFocus();
            this.Dialog.Window.SetSoftInputMode(SoftInput.StateVisible);


            // When authenticate button pressed
            buttonAuth.Click += delegate
            {
                if (string.IsNullOrWhiteSpace(textEmail.Text) || string.IsNullOrWhiteSpace(textPassword.Text))
                    return;

                // Return values
                if (null != Dismissed)
                    Dismissed(this, new AuthEventArgs { User = textEmail.Text, Password = textPassword.Text });

                Dismiss();
            };

            return view;
        }
    }
}