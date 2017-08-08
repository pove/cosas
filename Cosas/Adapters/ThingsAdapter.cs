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

namespace Cosas.Adapters
{
    public class ThingsAdapter : BaseAdapter<Thing>
    {
        private readonly List<Thing> _links;
        private readonly Activity _activity;

        public ThingsAdapter(Activity activity, IEnumerable<Thing> links)
        {
            _links = links.ToList(); //OrderByDescending(s => s.name).ToList(); // not need this order because we retrieve it already ordered
            _activity = activity;
        }

        public override Thing this[int position]
        {
            get
            {
                return _links[position];
            }
        }

        public override long GetItemId(int position)
        {
            // Our key/id is an string, so we cannot return it
            return 0;
        }

        public override int Count
        {
            get { return _links.Count; }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = convertView;

            // Show a simple list view with two lines
            if (view == null)
            {
                view = _activity.LayoutInflater.Inflate(Android.Resource.Layout.SimpleExpandableListItem2, null);
            }

            var link = _links[position];

            // Set lines text
            TextView text1 = view.FindViewById<TextView>(Android.Resource.Id.Text1);
            text1.Text = link.name;

            TextView text2 = view.FindViewById<TextView>(Android.Resource.Id.Text2);
            text2.Text = link.place;

            return view;
        }
    }
}