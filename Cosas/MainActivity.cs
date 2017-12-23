using Android.App;
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
using Android.Content;
using Android.Preferences;

namespace Cosas
{
    [Activity(Label = "Cosas", MainLauncher = true, Icon = "@drawable/icon",
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)] // Without that, it crashes when orientation changed
    public class MainActivity : Activity
    {
        // Firebase variables, you can set them up by code
        private string ApplicationId = "";
        private string ApiKey = "";
        private string DatabaseUrl = "";

        // Firebase Auth, very unsecure, only use for debug purposes
        private string Email = "";
        private const string Password = "";

        // Firebase variables
        public static FirebaseApp app;
        public static FirebaseAuth auth;
        public static DatabaseReference databaseReference;

        // Thing lists variables
        public static List<Thing> thingsList = new List<Thing>();
        public static List<Thing> thingsQuery = new List<Thing>();
        public static string currentQuery = string.Empty;

        // UI controls
        public TextView loading;
        public ListView list;
        public Button authButton;

        protected override void OnCreate(Bundle bundle)
        {
            // Add this flag to see the status bar coloured with our dark primary color
            Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

            base.OnCreate(bundle);

            // Get Firebase variables from local preferences
            ISharedPreferences getprefs = PreferenceManager.GetDefaultSharedPreferences(this);
            if (string.IsNullOrWhiteSpace(ApplicationId))
                ApplicationId = getprefs.GetString("ApplicationId", string.Empty);
            if (string.IsNullOrWhiteSpace(ApiKey))
                ApiKey = getprefs.GetString("ApiKey", string.Empty);
            if (string.IsNullOrWhiteSpace(DatabaseUrl))
                DatabaseUrl = getprefs.GetString("DatabaseUrl", string.Empty);
            if (string.IsNullOrWhiteSpace(Email))
                Email = getprefs.GetString("Email", string.Empty);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Initialize interface controls and events
            InitUI();

            // Init Firebase app, auth and database reference
            InitFirebase();
        }

        protected override void OnDestroy()
        {
            Firebase.FirebaseApp.Instance.Dispose();
            base.OnDestroy();
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
            // Loading text control
            loading = FindViewById<TextView>(Resource.Id.loading);
            
            // Auth button control
            authButton = FindViewById<Button>(Resource.Id.authenticate);
            authButton.Click += delegate { ShowAuthDialog(); };

            // List view for the things
            list = FindViewById<ListView>(Resource.Id.list);
            // Handle the list click
            list.ItemClick += listView_ItemClick;
            // Handle the list long click
            list.ItemLongClick += List_ItemLongClick;
        }

        private void RefreshUI()
        {
            thingsQuery.Clear();

            // If we have some query on search menu item, filter our list, otherwise show all
            if (!string.IsNullOrWhiteSpace(currentQuery))
            {
                // Find by phrase
                List<Thing> thingsQueried = thingsList.FindAll(x => x.searchfield.IndexOf(currentQuery, StringComparison.InvariantCultureIgnoreCase) > -1);

                // Find with words not together
                string[] queryWords = currentQuery.Split(' ');
                foreach (var item in queryWords)
                {
                    if (string.IsNullOrWhiteSpace(item))
                        continue;

                    thingsQueried.AddRange(thingsList.FindAll(x => x.searchfield.IndexOf(item, StringComparison.InvariantCultureIgnoreCase) > -1));
                }

                // Get most relevant first and discard duplicates
                thingsQuery.AddRange(thingsQueried.GroupBy(x => x.uid).OrderByDescending(g => g.Count()).SelectMany(x => x).Distinct().ToList());
            }
            else
            {
                thingsQuery.AddRange(thingsList);
            }

            // Create an adapter with things list
            var adapter = new ThingsAdapter(this, thingsQuery);

            RunOnUiThread(() =>
            {
                // Connect list with the adapter
                list.Adapter = adapter;

                // Hide loading
                if (loading.Visibility == ViewStates.Visible)
                    loading.Visibility = ViewStates.Gone;
            });
        }

        private void Search_QueryTextChange(object sender, SearchView.QueryTextChangeEventArgs e)
        {
            // Set current query text and refresh list view to only see items that match the filter. Remove accents.
            currentQuery = SearchHelper.RemoveDiacritics(e.NewText);
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
            var dialogFragment = new AddThingDialog(thing, thingsList.Select(x => x.place).OrderBy(x => x.Count()).Distinct().ToArray());


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

        private void ShowAuthDialog()
        {
            // Create transaction to show our add item dialog on this activity
            var transaction = FragmentManager.BeginTransaction();
            var dialogFragment = new AuthDialog(ApplicationId, ApiKey, DatabaseUrl, Email, Password);

            // Do staff with email/password, when we press authenticate on the dialog
            dialogFragment.Dismissed += (s, e) =>
            {
                // Hide auth button
                authButton.Visibility = ViewStates.Gone;
                // Show loading again
                loading.Visibility = ViewStates.Visible;

                // Set Firebase variables
                ApplicationId = e.ApplicationId;
                ApiKey = e.ApiKey;
                DatabaseUrl = e.DatabaseUrl;

                // Save Firebase variables on local preferences
                ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
                ISharedPreferencesEditor editor = prefs.Edit();
                editor.PutString("ApplicationId", ApplicationId);
                editor.PutString("ApiKey", ApiKey);
                editor.PutString("DatabaseUrl", DatabaseUrl);
                editor.PutString("Email", Email);
                editor.Apply();

                if (app == null)
                {
                    // Needs to initialize Firebase app first
                    InitFirebase();
                    return;
                }

                OnCompleteAuthListener OnCompleteAuth = new OnCompleteAuthListener();
                OnCompleteAuth.Raised += (se, ev) =>
                {
                    if (ev.task.IsSuccessful)
                    {
                        Toast.MakeText(this, "User authenticated", ToastLength.Short).Show();

                        // Load things
                        Load();
                    }
                    else
                    {
                        Toast.MakeText(this, ev.task.Exception.Message, ToastLength.Short).Show();
                        // Show again auth dialog
                        ShowAuthDialog();
                    }
                };

                auth.SignInWithEmailAndPassword(e.User, e.Password).AddOnCompleteListener(OnCompleteAuth);
            };

            // If we show dialog title, specify if we are adding or editing an existing item
            string title = GetString(Resource.String.authentication);

            // Show dialog
            dialogFragment.Show(transaction, title);


            // Hide loading
            if (loading != null)
                loading.Visibility = ViewStates.Gone;
            // Show auth button (user can press back)
            if (authButton != null)
                authButton.Visibility = ViewStates.Visible;
        }

        private void InitFirebase()
        {
            try
            {
                // Initialize Firebase app
                if (app == null)
                {
                    if (string.IsNullOrWhiteSpace(ApplicationId) || string.IsNullOrWhiteSpace(ApiKey) || string.IsNullOrWhiteSpace(DatabaseUrl))
                    {
                        // Show auth dialog to set firebase variables and authentication
                        ShowAuthDialog();
                        return;
                    }

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

                    auth.AuthState += Auth_AuthState;                    
                }

                // Get a database reference
                if (databaseReference == null)
                {
                    var db = FirebaseDatabase.GetInstance(app);
                    db.SetPersistenceEnabled(true);

                    // All our operations will be done on "thing" child
                    databaseReference = db.GetReference("things");

                    // Load all our current items
                    Load();
                }
                else
                {
                    // Refresh UI list view
                    RefreshUI();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
            }
        }

        private void Auth_AuthState(object sender, FirebaseAuth.AuthStateEventArgs e)
        {
            if (e.Auth == null || e.Auth.CurrentUser == null)
            {
                // Show auth dialog
                ShowAuthDialog();                
            }
        }

        private void Load()
        {
            try
            {
                // Get all items inside the database reference
                Query myQuery = databaseReference.OrderByChild("place");

                OnValueEventListener OnValueEvent = new OnValueEventListener();
                OnValueEvent.Raised += (s, e) =>
                {
                    if (e.snapshot == null)
                        return;

                    // Load finished, reload things lists and refresh UI
                    thingsList.Clear();
                    thingsQuery.Clear();

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

                    thingsQuery.AddRange(thingsList);

                    // Refresh UI list view
                    RefreshUI();
                };

                // Add listener that raises when load finish
                myQuery.AddValueEventListener(OnValueEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Toast.MakeText(Application.Context, ex.Message, ToastLength.Long).Show();
            }
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
                OnComplete.Raised += (s, e) =>
                {
                    // Add/update thing completed, so load all things again
                    Load();
                };

                OnFailureListener OnFailure = new OnFailureListener();
                OnFailure.Raised += (s, e) =>
                {
                    // Add/update thing failed, alert with the exception message
                    Console.WriteLine(e.exception.Message);
                    Toast.MakeText(Application.Context, e.exception.Message, ToastLength.Long).Show();
                };

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

        private void Delete(string thingUid)
        {
            try
            {
                OnDeleteListener OnDelete = new OnDeleteListener();
                OnDelete.Raised += (s, e) =>
                {
                    // Check if delete thing has failed, if not, load all things again
                    if (e.error == null)
                        Load();
                    else
                        Toast.MakeText(Application.Context, e.error.Message, ToastLength.Long).Show();
                };

                // Remove the item/child with the given id
                databaseReference.Child(thingUid).RemoveValue(OnDelete);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
            }
        }
    }
}

