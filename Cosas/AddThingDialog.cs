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
    public class DialogEventArgs : EventArgs
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string Place { get; set; }
    }

    // Dialog event delegate
    public delegate void DialogEventHandler(object sender, DialogEventArgs args);

    [Activity(Label = "Add thing")]
    public class AddThingDialog : DialogFragment
    {
        public event DialogEventHandler Dismissed;

        private Thing thing;
        private IList placesList;

        public AddThingDialog(Thing thing, IList placesList)
        {
            // If we get the thing on the constructor means that we are editing an existing item
            this.thing = thing;
            this.placesList = placesList;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Full screen with material theme only available since Lollipop
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
            {
                // Make full screen
                SetStyle(DialogFragmentStyle.NoTitle, Android.Resource.Style.ThemeMaterialLightNoActionBarFullscreen);
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set dialog title
            this.Dialog.SetTitle(this.Tag);

            // Load view from xml
            var view = inflater.Inflate(Resource.Layout.AddThing, container, false);

            // get ui elements
            var textName = view.FindViewById<TextView>(Resource.Id.thingName);
            var textPlace = view.FindViewById<AutoCompleteTextView>(Resource.Id.thingPlace);
            var buttonSave = view.FindViewById<Button>(Resource.Id.saveThing);

            // Add autocomplete list with places (usefull to repeat places)
            ArrayAdapter dictionaryAdapter = new ArrayAdapter(this.Dialog.Context, Android.Resource.Layout.SimpleDropDownItem1Line, placesList);
            textPlace.Adapter = dictionaryAdapter;

            // Name text edit will grow as text typed
            textName.InputType = Android.Text.InputTypes.TextFlagCapSentences | Android.Text.InputTypes.ClassText | Android.Text.InputTypes.TextFlagMultiLine;
            // Start with capitals
            textPlace.InputType = Android.Text.InputTypes.TextFlagCapSentences;

            // If we have an existing item, show its values
            string CurrentUid = null;
            if (thing != null)
            {
                CurrentUid = thing.uid;
                textName.Text = thing.name;
                textPlace.Text = thing.place;
                buttonSave.Text = GetString(Resource.String.edit_thing);
            }
            else
            {
                // Set focus at the end and show keyboard
                textName.RequestFocus();
                this.Dialog.Window.SetSoftInputMode(SoftInput.StateVisible);
            }

            // When save button pressed
            buttonSave.Click += delegate
            {
                if (string.IsNullOrWhiteSpace(textPlace.Text) || string.IsNullOrWhiteSpace(textName.Text))
                    return;

                // Return values
                if (null != Dismissed)
                    Dismissed(this, new DialogEventArgs { Uid = CurrentUid, Name = textName.Text, Place = textPlace.Text });

                Dismiss();
            };

            return view;
        }
    }
}