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
using Java.Util;
using Firebase.Database;

namespace Cosas.Model
{
    public class Thing
    {
        // Our thing properties
        public string uid { get; set; }
        public string name { get; set; }
        public string place { get; set; }
        public string user { get; set; }

        public Thing()
        {
            // General constructor
        }

        public Thing(DataSnapshot snapShot)
        {
            // Get our model from the Firebase snapshot

            if (snapShot.GetValue(true) == null) return; // key, but no value, recently deleted. Return null.            

            uid = snapShot.Key;
            name = snapShot.Child("name")?.GetValue(true)?.ToString();
            place = snapShot.Child("place")?.GetValue(true)?.ToString();
            user = snapShot.Child("user")?.GetValue(true)?.ToString();
        }

        public HashMap ModelToMap()
        {
            // Convert our model to Firebase map
            HashMap map = new HashMap();
            //map.Put("uid", uid); commented because our id is the key of the child, which is given by Firebase
            map.Put("name", name);
            map.Put("place", place);
            map.Put("user", user);

            return map;
        }
    }
}