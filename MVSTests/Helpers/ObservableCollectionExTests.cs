using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;

namespace MVSTests.Helpers
{
    [TestClass]
    public class ObservableCollectionExTests
    {
        [TestMethod]
        public void ObservableCollectionEx_AddRange_EmitsExpectedNotifications()
        {
            var native = new ObservableCollectionEx<int>();
            var nativeEvents = CaptureNotifications(native, () => native.AddRange(new[] { 1, 2, 3 }));

            CollectionAssert.AreEqual(
                new[] { "P:Count", "P:Item[]", "C:Reset:-1:-1::" },
                nativeEvents);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, native.ToArray());
        }

        [TestMethod]
        public void ObservableCollectionEx_Add_EmitsExpectedNotifications()
        {
            var native = new ObservableCollectionEx<int>();
            var nativeEvents = CaptureNotifications(native, () => native.Add(1));

            CollectionAssert.AreEqual(
                new[] { "P:Count", "P:Item[]", "C:Add:0:-1:1:" },
                nativeEvents);
            CollectionAssert.AreEqual(new[] { 1 }, native.ToArray());
        }

        [TestMethod]
        public void ObservableCollectionEx_BoundedDisplayUpdates_EmitExpectedNotifications()
        {
            var native = new ObservableCollectionEx<int> { 1, 2 };

            var nativeEvents = CaptureNotifications(native, () =>
            {
                native.Add(3);
                native.RemoveAt(0);
                native.Clear();
            });

            CollectionAssert.AreEqual(
                new[]
                {
                    "P:Count", "P:Item[]", "C:Add:2:-1:3:",
                    "P:Count", "P:Item[]", "C:Remove:-1:0::1",
                    "P:Count", "P:Item[]", "C:Reset:-1:-1::"
                },
                nativeEvents);
            Assert.AreEqual(0, native.Count);
        }

        [TestMethod]
        public void ObservableCollectionEx_EmptyAddRange_EmitsNoNotifications()
        {
            var native = new ObservableCollectionEx<int>();

            var nativeEvents = CaptureNotifications(native, () => native.AddRange(Array.Empty<int>()));

            Assert.AreEqual(0, nativeEvents.Count);
            Assert.AreEqual(0, native.Count);
        }

        [TestMethod]
        public void ObservableCollectionEx_SuspendedRemovals_EmitOneReset()
        {
            var native = new ObservableCollectionEx<int> { 1, 2, 3, 4 };

            var nativeEvents = CaptureNotifications(native, () =>
            {
                native.SuspendNotifications();
                native.RemoveAt(0);
                native.RemoveAt(0);
                native.ResumeNotifications();
            });

            CollectionAssert.AreEqual(
                new[] { "P:Count", "P:Item[]", "C:Reset:-1:-1::" },
                nativeEvents);
            CollectionAssert.AreEqual(new[] { 3, 4 }, native.ToArray());
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
