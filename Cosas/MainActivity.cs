﻿using Android.App;
using Android.Widget;
using Android.OS;
using System;
using Firebase;
using Firebase.Auth;
using Cosas.Model;
using Firebase.Database;
using System.Collections.Generic;
using Cosas.Adapters;
using Android.Views;
using System.Linq;

namespace Cosas
{
    [Activity(Label = "Cosas", MainLauncher = true, Icon = "@drawable/icon",
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)] // Without that, it crashes when orientation changed
    public class MainActivity : Activity
    {
        // Firebase constants
        private const string ApplicationId = "";
        private const string ApiKey = "";
        private const string DatabaseUrl = "";

        // Firebase Auth, very unsecure, only use for debug purposes
        private const string Email = "";
        private const string Password = "";

        // Firebase variables
        public static FirebaseApp app;
        public static FirebaseAuth auth;
        public static DatabaseReference databaseReference;

        // Thing lists variables
        public static List<Thing> thingsList = new List<Thing>();
        public static List<Thing> thingsQuery = new List<Thing>();
        public static string currentQuery = string.Empty;

        protected override void OnCreate(Bundle bundle)
        {
            // Add this flag to see the status bar coloured with our dark primary color
            Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Init Firebase app, auth and database reference
            InitFirebaseAuth();

            // Initialize interface events (of the list view, that is our unique element)
            InitUI();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            base.OnCreateOptionsMenu(menu);
            MenuInflater.Inflate(Resource.Layout.MainMenu, menu);

            // Search on main menu
            var searchItem = menu.FindItem(Resource.Id.search);
            SearchView search = searchItem.ActionView as SearchView;
            // Raise an event when text on search menu item changed
            search.QueryTextChange += Search_QueryTextChange;

            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            // Main menu option pressed
            switch (item.ItemId)
            {
                // Add a new thing item
                case Resource.Id.add_thing:
                    ShowAddThingDialog();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        private void InitUI()
        {
            ListView list = FindViewById<ListView>(Resource.Id.list);
            // Handle the list click
            list.ItemClick += listView_ItemClick;
            // Handle the list long click
            list.ItemLongClick += List_ItemLongClick;
        }

        private void RefreshUI()
        {
            // If we have some query on search menu item, filter our list, otherwise show all
            if (!string.IsNullOrWhiteSpace(currentQuery))
            {
                thingsQuery = thingsList.FindAll(x => x.name.ToLower().Contains(currentQuery.ToLower()));
                thingsQuery.AddRange(thingsList.FindAll(x => !thingsQuery.Contains(x) && x.place.ToLower().Contains(currentQuery.ToLower())));
            }
            else
            {
                thingsQuery = thingsList;
            }

            // Create an adapter with things list
            var adapter = new ThingsAdapter(this, thingsQuery);

            RunOnUiThread(() =>
            {
                // Connect list with the adapter
                ListView list = FindViewById<ListView>(Resource.Id.list);
                list.Adapter = adapter;
            });
        }

        private void Search_QueryTextChange(object sender, SearchView.QueryTextChangeEventArgs e)
        {
            // Set current query text and refresh list view to only see items that match the filter
            currentQuery = e.NewText;
            RefreshUI();
        }

        private void listView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            // Show dialog to add new item
            ShowAddThingDialog(thingsQuery[e.Position]);
        }

        private void List_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            // Delete item when long press on it. An alert will be shown.
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetTitle("Delete thing");
            alert.SetMessage(string.Format("¿Remove {0}?", thingsQuery[e.Position].name));

            // Delete selected thing
            alert.SetPositiveButton("Delete", (senderAlert, args) =>
            {
                Delete(thingsQuery[e.Position].uid);
            });

            alert.SetNegativeButton("Cancel", (senderAlert, args) => { });

            // Run the alert
            alert.Show();
        }

        private void ShowAddThingDialog(Thing thing = null)
        {
            // Create transaction to show our add item dialog on this activity
            var transaction = FragmentManager.BeginTransaction();
            var dialogFragment = new AddThingDialog(thing, thingsList.Select(x => x.place).Distinct().ToArray());


            // Do staff with new/existing item, when we press save on the dialog
            dialogFragment.Dismissed += (s, e) =>
            {
                CreateUpdate(e.Name, e.Place, e.Uid);
                Toast.MakeText(this, String.Format("{0} saved", e.Name), ToastLength.Short).Show();
            };

            // If we show dialog title, specify if we are adding or editing an existing item
            string title = GetString(Resource.String.add_thing);
            if (thing != null)
                title = GetString(Resource.String.edit_thing);

            // Show dialog
            dialogFragment.Show(transaction, title);
        }

        private void InitFirebaseAuth()
        {
            try
            {
                // Initialize Firebase app
                if (app == null)
                {
                    var options = new FirebaseOptions.Builder()
                    .SetApplicationId(ApplicationId)
                    .SetApiKey(ApiKey)
                    .SetDatabaseUrl(DatabaseUrl)
                    .Build();

                    app = FirebaseApp.InitializeApp(this, options);
                }

                // Get authorization on our Firebase app
                if (auth == null)
                {
                    auth = FirebaseAuth.GetInstance(app);

                    OnCompleteAuthListener OnCompleteAuth = new OnCompleteAuthListener();
                    OnCompleteAuth.Raised += OnCompleteAuth_Raised;
                    auth.SignInWithEmailAndPassword(Email, Password).AddOnCompleteListener(OnCompleteAuth);

                    return;
                }

                // Get a database reference
                if (databaseReference == null)
                {
                    var db = FirebaseDatabase.GetInstance(app);
                    db.SetPersistenceEnabled(true);

                    // All our operations will be done on "thing" child
                    databaseReference = db.GetReference("things");
                }

                // Load all our current items
                Load();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
            }
        }

        private void OnCompleteAuth_Raised(object sender, FirebaseEventArgs e)
        {
            InitFirebaseAuth();
        }

        private void Load()
        {
            try
            {
                // Get all items inside the darabase reference
                Query myQuery = databaseReference.OrderByChild("place");

                OnValueEventListener OnValueEvent = new OnValueEventListener();
                OnValueEvent.Raised += OnValueEvent_Raised;

                // Add listener that raises when load finish
                myQuery.AddValueEventListener(OnValueEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Toast.MakeText(Application.Context, ex.Message, ToastLength.Long).Show();
            }
        }

        private void OnValueEvent_Raised(object sender, FirebaseEventArgs e)
        {
            // Load finished, reload things lists and refresh UI
            thingsList.Clear();

            if (e.snapshot.HasChildren)
            {
                Java.Util.IIterator iterator = e.snapshot.Children.Iterator();
                while (iterator.HasNext)
                {
                    thingsList.Add(new Thing((DataSnapshot)iterator.Next()));
                }
            }
            else
            {
                thingsList.Add(new Thing(e.snapshot));
            }

            thingsQuery = thingsList;
            RefreshUI();
        }

        private void CreateUpdate(string thingName, string thingPlace, string thingUid = null)
        {
            // Set the thing
            Thing th = new Thing()
            {
                name = thingName,
                place = thingPlace,
                user = auth.CurrentUser.Uid
            };

            try
            {
                OnCompleteListener OnComplete = new OnCompleteListener();
                OnComplete.Raised += OnComplete_Raised;
                OnFailureListener OnFailure = new OnFailureListener();
                OnFailure.Raised += OnFailure_Raised;

                // If not exists (if we don't have the thing id) create a new one, otherwise update it
                if (string.IsNullOrEmpty(thingUid))
                    databaseReference.Push().SetValue(th.ModelToMap()).AddOnCompleteListener(OnComplete).AddOnFailureListener(OnFailure);
                else
                    databaseReference.Child(thingUid).SetValue(th.ModelToMap()).AddOnCompleteListener(OnComplete).AddOnFailureListener(OnFailure);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
            }
        }

        private void OnComplete_Raised(object sender, FirebaseEventArgs e)
        {
            // Add/update thing completed, so load all things again
            Load();
        }

        private void OnFailure_Raised(object sender, FirebaseEventArgs e)
        {
            // Add/update thing failed, alert with the exception message
            Console.WriteLine(e.exception.Message);
            Toast.MakeText(Application.Context, e.exception.Message, ToastLength.Long).Show();
        }

        private void Delete(string thingUid)
        {
            try
            {
                OnDeleteListener OnDelete = new OnDeleteListener();
                OnDelete.Raised += OnDelete_Raised;
                // Remove the item/child with the given id
                databaseReference.Child(thingUid).RemoveValue(OnDelete);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
            }
        }

        private void OnDelete_Raised(object sender, FirebaseEventArgs e)
        {
            // Check if delete thing has failed, if not, load all things again
            if (e.error == null)
                Load();
            else
                Toast.MakeText(Application.Context, e.error.Message, ToastLength.Long).Show();
        }
    }
}

