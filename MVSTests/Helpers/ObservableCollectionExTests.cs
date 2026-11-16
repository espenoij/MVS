using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using Telerik.Windows.Data;

namespace MVSTests.Helpers
{
    [TestClass]
    public class ObservableCollectionExTests
    {
        [TestMethod]
        public void ObservableCollectionEx_AddRange_MatchesTelerikNotifications()
        {
            var telerik = new RadObservableCollection<int>();
            var native = new ObservableCollectionEx<int>();

            var telerikEvents = CaptureNotifications(telerik, () => telerik.AddRange(new[] { 1, 2, 3 }));
            var nativeEvents = CaptureNotifications(native, () => native.AddRange(new[] { 1, 2, 3 }));

            CollectionAssert.AreEqual(
                telerikEvents,
                nativeEvents,
                string.Format("Telerik: {0}; native: {1}", string.Join("|", telerikEvents), string.Join("|", nativeEvents)));
            CollectionAssert.AreEqual(telerik.ToArray(), native.ToArray());
        }

        [TestMethod]
        public void ObservableCollectionEx_Add_MatchesTelerikNotifications()
        {
            var telerik = new RadObservableCollection<int>();
            var native = new ObservableCollectionEx<int>();

            var telerikEvents = CaptureNotifications(telerik, () => telerik.Add(1));
            var nativeEvents = CaptureNotifications(native, () => native.Add(1));

            CollectionAssert.AreEqual(telerikEvents, nativeEvents);
            CollectionAssert.AreEqual(telerik.ToArray(), native.ToArray());
        }

        [TestMethod]
        public void ObservableCollectionEx_BoundedDisplayUpdates_MatchTelerikNotifications()
        {
            var telerik = new RadObservableCollection<int> { 1, 2 };
            var native = new ObservableCollectionEx<int> { 1, 2 };

            var telerikEvents = CaptureNotifications(telerik, () =>
            {
                telerik.Add(3);
                telerik.RemoveAt(0);
                telerik.Clear();
            });
            var nativeEvents = CaptureNotifications(native, () =>
            {
                native.Add(3);
                native.RemoveAt(0);
                native.Clear();
            });

            CollectionAssert.AreEqual(
                telerikEvents,
                nativeEvents,
                string.Format("Telerik: {0}; native: {1}", string.Join("|", telerikEvents), string.Join("|", nativeEvents)));
            CollectionAssert.AreEqual(telerik.ToArray(), native.ToArray());
        }

        [TestMethod]
        public void ObservableCollectionEx_EmptyAddRange_MatchesTelerikNotifications()
        {
            var telerik = new RadObservableCollection<int>();
            var native = new ObservableCollectionEx<int>();

            var telerikEvents = CaptureNotifications(telerik, () => telerik.AddRange(Array.Empty<int>()));
            var nativeEvents = CaptureNotifications(native, () => native.AddRange(Array.Empty<int>()));

            CollectionAssert.AreEqual(telerikEvents, nativeEvents);
            Assert.AreEqual(0, native.Count);
        }

        [TestMethod]
        public void ObservableCollectionEx_SuspendedRemovals_MatchTelerikNotifications()
        {
            var telerik = new RadObservableCollection<int> { 1, 2, 3, 4 };
            var native = new ObservableCollectionEx<int> { 1, 2, 3, 4 };

            var telerikEvents = CaptureNotifications(telerik, () =>
            {
                telerik.SuspendNotifications();
                telerik.RemoveAt(0);
                telerik.RemoveAt(0);
                telerik.ResumeNotifications();
            });
            var nativeEvents = CaptureNotifications(native, () =>
            {
                native.SuspendNotifications();
                native.RemoveAt(0);
                native.RemoveAt(0);
                native.ResumeNotifications();
            });

            CollectionAssert.AreEqual(
                telerikEvents,
                nativeEvents,
                string.Format("Telerik: {0}; native: {1}", string.Join("|", telerikEvents), string.Join("|", nativeEvents)));
            CollectionAssert.AreEqual(telerik.ToArray(), native.ToArray());
        }

        [TestMethod]
        public void ObservableCollectionEx_NestedSuspension_DefersNotificationsUntilOutermostResume()
        {
            var native = new ObservableCollectionEx<int> { 1, 2, 3, 4 };
            var nativeEvents = new List<string>();
            SubscribeToNotifications(native, nativeEvents);

            native.SuspendNotifications();
            native.SuspendNotifications();
            native.RemoveAt(0);
            native.RemoveAt(0);
            native.ResumeNotifications();
            Assert.AreEqual(0, nativeEvents.Count, "The inner resume must not publish notifications.");
            native.ResumeNotifications();

            CollectionAssert.AreEqual(
                new[] { "P:Count", "P:Item[]", "C:Reset:-1:-1::" },
                nativeEvents);
            CollectionAssert.AreEqual(new[] { 3, 4 }, native.ToArray());
        }

        [TestMethod]
        public void RadObservableCollection_AddRange_EmitsOneReset()
        {
            var collection = new RadObservableCollection<int>();
            var events = new List<NotifyCollectionChangedEventArgs>();
            collection.CollectionChanged += (_, e) => events.Add(e);

            collection.AddRange(new[] { 1, 2, 3 });

            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(NotifyCollectionChangedAction.Reset, events[0].Action);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, collection);
        }

        [TestMethod]
        public void RadObservableCollection_SuspendedRemovals_EmitOneReset()
        {
            var collection = new RadObservableCollection<int> { 1, 2, 3, 4 };
            var events = new List<NotifyCollectionChangedEventArgs>();
            collection.CollectionChanged += (_, e) => events.Add(e);

            collection.SuspendNotifications();
            collection.RemoveAt(0);
            collection.RemoveAt(0);
            collection.ResumeNotifications();

            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(NotifyCollectionChangedAction.Reset, events[0].Action);
            CollectionAssert.AreEqual(new[] { 3, 4 }, collection);
        }

        private static List<string> CaptureNotifications(
            INotifyCollectionChanged collection,
            Action mutation)
        {
            var events = new List<string>();
            SubscribeToNotifications(collection, events);

            mutation();

            return events;
        }

        private static void SubscribeToNotifications(
            INotifyCollectionChanged collection,
            List<string> events)
        {
            ((INotifyPropertyChanged)collection).PropertyChanged += (_, e) => events.Add("P:" + e.PropertyName);
            collection.CollectionChanged += (_, e) =>
            {
                events.Add(string.Format(
                    "C:{0}:{1}:{2}:{3}:{4}",
                    e.Action,
                    e.NewStartingIndex,
                    e.OldStartingIndex,
                    FormatItems(e.NewItems),
                    FormatItems(e.OldItems)));
            };
        }

        private static string FormatItems(IList items)
        {
            return items == null ? string.Empty : string.Join(",", items.Cast<object>());
        }
    }
}
