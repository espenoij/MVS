using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MVS
{
    public class ObservableCollectionEx<T> : ObservableCollection<T>
    {
        private int notificationSuspensionCount;
        private bool collectionChangedWhileSuspended;
        private readonly List<string> propertyChangedWhileSuspended = new List<string>();

        public void AddRange(IEnumerable<T> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            SuspendNotifications();
            try
            {
                foreach (var item in items)
                    Add(item);
            }
            finally
            {
                ResumeNotifications();
            }
        }

        public void SuspendNotifications()
        {
            notificationSuspensionCount++;
        }

        public void ResumeNotifications()
        {
            if (notificationSuspensionCount == 0)
                throw new InvalidOperationException("Collection notifications are not suspended.");

            notificationSuspensionCount--;
            if (notificationSuspensionCount == 0)
            {
                var propertyNames = propertyChangedWhileSuspended.ToArray();
                propertyChangedWhileSuspended.Clear();
                bool collectionChanged = collectionChangedWhileSuspended;
                collectionChangedWhileSuspended = false;

                foreach (var propertyName in propertyNames)
                    base.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

                if (collectionChanged)
                    base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (notificationSuspensionCount > 0)
            {
                if (!propertyChangedWhileSuspended.Contains(e.PropertyName))
                    propertyChangedWhileSuspended.Add(e.PropertyName);
                return;
            }

            base.OnPropertyChanged(e);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (notificationSuspensionCount > 0)
            {
                collectionChangedWhileSuspended = true;
                return;
            }

            base.OnCollectionChanged(e);
        }
    }
}