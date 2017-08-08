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

namespace Cosas
{
    [Activity(Label = "Cosas", MainLauncher = true, Icon = "@drawable/icon", 
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)] // Without that, it crashes when orientation changed
    public class MainActivity : Activity
    {
        public static FirebaseApp app;
        public static FirebaseAuth auth;
        public static DatabaseReference databaseReference;

        protected override void OnCreate(Bundle bundle)
        {
            // Add this flag to see the status bar coloured with our dark primary color
            Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView (Resource.Layout.Main);

            // Init Firebase app, auth and database reference
            InitFirebaseAuth(this);

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

        public static void RefreshUI(Activity activity)
        {
            // If we have some query on search menu item, filter our list, otherwise show all
            if (!string.IsNullOrWhiteSpace(Things.currentQuery))
            {
                Things.thingsQuery = Things.thingsList.FindAll(x => x.name.ToLower().Contains(Things.currentQuery.ToLower()));
                Things.thingsQuery.AddRange(Things.thingsList.FindAll(x => !Things.thingsQuery.Contains(x) && x.place.ToLower().Contains(Things.currentQuery.ToLower())));
            }
            else
            {
                Things.thingsQuery = Things.thingsList;
            }

            // Create an adapter with things list
            var adapter = new ThingsAdapter(activity, Things.thingsQuery);

            activity.RunOnUiThread(() =>
            {
                // Connect list with the adapter
                ListView list = activity.FindViewById<ListView>(Resource.Id.list);
                list.Adapter = adapter;
            });
        }

        private void Search_QueryTextChange(object sender, SearchView.QueryTextChangeEventArgs e)
        {
            // Set current query text and refresh list view to only see items that match the filter
            Things.currentQuery = e.NewText;
            RefreshUI(this);
        }

        private void listView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            // Show dialog to add new item
            ShowAddThingDialog(Things.thingsQuery[e.Position]);
        }

        private void List_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            // Delete item when long press on it. An alert will be shown.
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetTitle("Delete thing");
            alert.SetMessage(string.Format("¿Remove {0}?", Things.thingsQuery[e.Position].name));

            // Delete selected thing
            alert.SetPositiveButton("Delete", (senderAlert, args) =>
            {
                Delete(Things.thingsQuery[e.Position].uid);
            });

            alert.SetNegativeButton("Cancel", (senderAlert, args) => { });

            // Run the alert
            alert.Show();
        }

        public void ShowAddThingDialog(Thing thing = null)
        {
            // Create transaction to show our add item dialog on this activity
            var transaction = FragmentManager.BeginTransaction();
            var dialogFragment = new AddThingDialog(thing);

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

        public static void InitFirebaseAuth(Activity activity)
        {
            try
            {
                // Initialize Firebase app
                if (app == null)
                {
                    var options = new FirebaseOptions.Builder()
                    .SetApplicationId("")
                    .SetApiKey("")
                    .SetDatabaseUrl("")
                    .Build();

                    app = FirebaseApp.InitializeApp(activity, options);
                }

                // Get authorization on our Firebase app
                if (auth == null)
                {
                    auth = FirebaseAuth.GetInstance(app);
                    auth.SignInWithEmailAndPassword("", "").AddOnCompleteListener(new OnCompleteAuthListener(activity));
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
                Load(activity);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Toast.MakeText(activity, ex.Message, ToastLength.Long).Show();
            }
        }

        public static void Load(Activity activity)
        {
            try
            {
                // Get all items inside the darabase reference
                Query myQuery = databaseReference.OrderByChild("place");

                // Add listener that raises when load finish
                myQuery.AddValueEventListener(new OnLoadListener(activity));
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
                // If not exists (if we don't have the thing id) create a new one, otherwise update it
                if (string.IsNullOrEmpty(thingUid))
                    databaseReference.Push().SetValue(th.ModelToMap()).AddOnCompleteListener(new OnCompleteListener(this)).AddOnFailureListener(new OnFailureListener());
                else
                    databaseReference.Child(thingUid).SetValue(th.ModelToMap()).AddOnCompleteListener(new OnCompleteListener(this)).AddOnFailureListener(new OnFailureListener());
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
                // Remove the item/child with the given id
                databaseReference.Child(thingUid).RemoveValue(new OnDeleteListener(this));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
            }
        }
    }

    public static class Things
    {
        public static List<Thing> thingsList = new List<Thing>();
        public static List<Thing> thingsQuery = new List<Thing>();
        public static string currentQuery = string.Empty;
    }

    public class OnLoadListener : Java.Lang.Object, Firebase.Database.IValueEventListener
    {
        private Activity activity;

        public OnLoadListener(Activity activity)
        {
            this.activity = activity;
        }

        public void OnCancelled(DatabaseError error)
        {
            throw new NotImplementedException();
        }

        public void OnDataChange(DataSnapshot snapshot)
        {
            Things.thingsList.Clear();

            if (snapshot.HasChildren)
            {
                Java.Util.IIterator iterator = snapshot.Children.Iterator();
                while (iterator.HasNext)
                {
                    Things.thingsList.Add(new Thing((DataSnapshot)iterator.Next()));
                }
            }
            else
            {
                Things.thingsList.Add(new Thing(snapshot));
            }

            Things.thingsQuery = Things.thingsList;
            MainActivity.RefreshUI(this.activity);
        }
    }


    public class OnDeleteListener : Java.Lang.Object, DatabaseReference.ICompletionListener
    {
        private Activity activity;

        public OnDeleteListener(Activity activity)
        {
            this.activity = activity;
        }

        public void OnComplete(DatabaseError error, DatabaseReference @ref)
        {
            if (error == null)
                MainActivity.Load(activity);
            else
                Toast.MakeText(Application.Context, error.Message, ToastLength.Long).Show();
        }
    }

    public class OnCompleteAuthListener : Java.Lang.Object, Android.Gms.Tasks.IOnCompleteListener
    {
        private Activity activity;

        public OnCompleteAuthListener(Activity activity)
        {
            this.activity = activity;
        }

        public void OnComplete(Android.Gms.Tasks.Task task)
        {
            Console.WriteLine("Authorized");
            MainActivity.InitFirebaseAuth(activity);
        }
    }

    public class OnCompleteListener : Java.Lang.Object, Android.Gms.Tasks.IOnCompleteListener
    {
        private Activity activity;

        public OnCompleteListener(Activity activity)
        {
            this.activity = activity;
        }

        public void OnComplete(Android.Gms.Tasks.Task task)
        {
            Console.WriteLine("Completed");
            MainActivity.Load(activity);
        }
    }

    public class OnFailureListener : Java.Lang.Object, Android.Gms.Tasks.IOnFailureListener
    {  
        public void OnFailure(Java.Lang.Exception e)
        {
            Console.WriteLine(e.Message);
            Toast.MakeText(Application.Context, e.Message, ToastLength.Long).Show();
        }
    }
}

