using System;
using Firebase.Database;

namespace Cosas
{
    // Public arguments of our event delegate
    public class FirebaseEventArgs : EventArgs
    {
        /// <summary>
        /// OnValueEventListener.OnDataChange(DataSnapshot snapshot)
        /// </summary>
        public DataSnapshot snapshot { get; set; }
        /// <summary>
        /// OnValueEventListener.OnCancelled(DatabaseError error)
        /// OnDeleteListener.OnComplete(DatabaseError error, DatabaseReference @ref)
        /// </summary>
        public DatabaseError error { get; set; }
        /// <summary>
        /// OnDeleteListener.OnComplete(DatabaseError error, DatabaseReference @ref)
        /// </summary>
        public DatabaseReference @ref { get; set; }
        /// <summary>
        /// OnCompleteAuthListener.OnComplete(Android.Gms.Tasks.Task task)
        /// OnCompleteListener.OnComplete(Android.Gms.Tasks.Task task)
        /// </summary>
        public Android.Gms.Tasks.Task task { get; set; }
        /// <summary>
        /// OnFailureListener.OnFailure(Java.Lang.Exception e)
        /// </summary>
        public Java.Lang.Exception exception { get; set; }
    }

    // Firebase event delegate
    public delegate void FirebaseEventHandler(object sender, FirebaseEventArgs e);

    public class OnCompleteAuthListener : Java.Lang.Object, Android.Gms.Tasks.IOnCompleteListener
    {
        public event FirebaseEventHandler Raised;

        public void OnComplete(Android.Gms.Tasks.Task task)
        {
            Raised?.Invoke(this, new FirebaseEventArgs() { task = task });
        }
    }

    public class OnCompleteListener : Java.Lang.Object, Android.Gms.Tasks.IOnCompleteListener
    {
        public event FirebaseEventHandler Raised;

        public void OnComplete(Android.Gms.Tasks.Task task)
        {
            Raised?.Invoke(this, new FirebaseEventArgs() { task = task });
        }
    }
    
    public class OnFailureListener : Java.Lang.Object, Android.Gms.Tasks.IOnFailureListener
    {
        public event FirebaseEventHandler Raised;
        
        public void OnFailure(Java.Lang.Exception e)
        {
            Raised?.Invoke(this, new FirebaseEventArgs() { exception = e });
        }
    }

    public class OnValueEventListener : Java.Lang.Object, Firebase.Database.IValueEventListener
    {
        public event FirebaseEventHandler Raised;

        public void OnCancelled(DatabaseError error)
        {
            Raised?.Invoke(this, new FirebaseEventArgs() { error = error });
        }

        public void OnDataChange(DataSnapshot snapshot)
        {
            Raised?.Invoke(this, new FirebaseEventArgs() { snapshot = snapshot });
        }
    }

    public class OnDeleteListener : Java.Lang.Object, DatabaseReference.ICompletionListener
    {
        public event FirebaseEventHandler Raised;

        public void OnComplete(DatabaseError error, DatabaseReference @ref)
        {
            Raised?.Invoke(this, new FirebaseEventArgs() { error = error, @ref = @ref });
        }
    }
}