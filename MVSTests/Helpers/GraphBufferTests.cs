using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;

namespace MVSTests.Helpers
{
    [TestClass]
    public class GraphBufferTests
    {
        [TestMethod]
        public void Transfer_AppendsSnapshotAndRaisesOneReset()
        {
            var buffer = new ObservableCollectionEx<HMSData>
            {
                new HMSData { data = 1, timestamp = DateTime.UtcNow },
                new HMSData { data = 2, timestamp = DateTime.UtcNow.AddSeconds(1) },
            };
            var display = new ObservableCollectionEx<HMSData>();
            var events = new List<NotifyCollectionChangedEventArgs>();
            display.CollectionChanged += (_, e) => events.Add(e);

            GraphBuffer.Transfer(buffer, display);

            Assert.AreEqual(0, buffer.Count);
            Assert.AreEqual(2, display.Count);
            Assert.AreEqual(1d, display[0].data);
            Assert.AreEqual(2d, display[1].data);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(NotifyCollectionChangedAction.Reset, events[0].Action);
        }

        [TestMethod]
        public void RemoveOldData_RemovesExpiredPointsAndRaisesOneReset()
        {
            var expired = new HMSData { timestamp = DateTime.UtcNow.AddMinutes(-10) };
            var recent = new HMSData { timestamp = DateTime.UtcNow };
            var dataList = new ObservableCollectionEx<HMSData> { expired, recent };
            var events = new List<NotifyCollectionChangedEventArgs>();
            dataList.CollectionChanged += (_, e) => events.Add(e);

            GraphBuffer.RemoveOldData(dataList, 60);

            Assert.AreEqual(1, dataList.Count);
            Assert.AreSame(recent, dataList[0]);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(NotifyCollectionChangedAction.Reset, events[0].Action);
        }
    }
}
