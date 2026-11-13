using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Telerik.Windows.Data;

namespace MVSTests.Helpers
{
    [TestClass]
    public class DisplayListTests
    {
        private static IList<T> CreateDisplay<T>(bool useTelerik, params T[] items)
        {
            IList<T> display = useTelerik ? new RadObservableCollection<T>() : new ObservableCollection<T>();
            foreach (var item in items)
                display.Add(item);
            return display;
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Transfer_HmsData_UpdatesFirstMatchAndAppendsCopiesWithoutRemovingStaleItems(bool useTelerik)
        {
            var existing = new HMSData { id = 1, data = 1 };
            var duplicate = new HMSData { id = 1, data = 2 };
            var stale = new HMSData { id = 9, data = 9 };
            var added = new HMSData { id = 2, data = 20 };
            var display = CreateDisplay(useTelerik, existing, duplicate, stale);
            int additions = 0;
            bool dataChanged = false;
            ((INotifyCollectionChanged)display).CollectionChanged += (_, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                    additions++;
            };
            existing.PropertyChanged += (_, e) => dataChanged |= e.PropertyName == nameof(HMSData.data);

            DisplayList.Transfer(new[] { new HMSData { id = 1, data = 10 }, added }, display);

            Assert.AreEqual(4, display.Count);
            Assert.AreSame(existing, display[0]);
            Assert.AreEqual(10d, existing.data);
            Assert.AreEqual(2d, duplicate.data);
            Assert.AreSame(stale, display[2]);
            Assert.AreNotSame(added, display[3]);
            Assert.AreEqual(20d, display[3].data);
            Assert.AreEqual(1, additions);
            Assert.IsTrue(dataChanged);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void TransferDirect_ReplacesDisplayWithIndependentCopiesInSourceOrder(bool useTelerik)
        {
            var first = new HMSData { id = 2, data = 20 };
            var second = new HMSData { id = 1, data = 10 };
            var display = CreateDisplay(useTelerik, new HMSData { id = 9 });

            DisplayList.TransferDirect(new[] { first, second }, display);

            Assert.AreEqual(2, display.Count);
            Assert.AreEqual(2, display[0].id);
            Assert.AreEqual(1, display[1].id);
            Assert.AreNotSame(first, display[0]);
            Assert.AreNotSame(second, display[1]);
            first.data = 99;
            Assert.AreEqual(20d, display[0].data);

            DisplayList.TransferDirect(Array.Empty<HMSData>(), display);
            Assert.AreEqual(0, display.Count);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Transfer_SensorData_UpdatesByIdAndAppendsCopies(bool useTelerik)
        {
            var existing = new SensorData { id = 1, data = 1 };
            var added = new SensorData { id = 2, data = 20 };
            var display = CreateDisplay(useTelerik, existing);

            DisplayList.Transfer(new[] { new SensorData { id = 1, data = 10 }, added }, display);

            Assert.AreEqual(2, display.Count);
            Assert.AreSame(existing, display[0]);
            Assert.AreEqual(10d, existing.data);
            Assert.AreNotSame(added, display[1]);
            Assert.AreEqual(20d, display[1].data);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Transfer_SerialPortData_UpdatesByPortNameAndAppendsCopies(bool useTelerik)
        {
            var existing = new SerialPortData { portName = "COM1", buffer_text = "old" };
            var added = new SerialPortData { portName = "COM2", buffer_text = "second" };
            var display = CreateDisplay(useTelerik, existing);

            DisplayList.Transfer(new[]
            {
                new SerialPortData { portName = "COM1", buffer_text = "new", portStatus = PortStatus.Reading },
                added
            }, display);

            Assert.AreEqual(2, display.Count);
            Assert.AreSame(existing, display[0]);
            Assert.AreEqual("new", existing.buffer_text);
            Assert.AreEqual(PortStatus.Reading, existing.portStatus);
            Assert.AreNotSame(added, display[1]);
            Assert.AreEqual("COM2", display[1].portName);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Transfer_FileReaderData_MatchesFolderAndNameAndPreservesExistingSetup(bool useTelerik)
        {
            var existing = new FileReaderSetup
            {
                fileFolder = "first", fileName = "data.csv", dataLine = "old", readFrequency = 123
            };
            var added = new FileReaderSetup { fileFolder = "second", fileName = "data.csv", dataLine = "second" };
            var timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var display = CreateDisplay(useTelerik, existing);

            DisplayList.Transfer(new[]
            {
                new FileReaderSetup
                {
                    fileFolder = "first", fileName = "data.csv", dataLine = "new",
                    timestamp = timestamp, portStatus = PortStatus.Reading, readFrequency = 456
                },
                added
            }, display);

            Assert.AreEqual(2, display.Count);
            Assert.AreSame(existing, display[0]);
            Assert.AreEqual("new", existing.dataLine);
            Assert.AreEqual(timestamp, existing.timestamp);
            Assert.AreEqual(PortStatus.Reading, existing.portStatus);
            Assert.AreEqual(123d, existing.readFrequency);
            Assert.AreNotSame(added, display[1]);
            Assert.AreEqual("second", display[1].fileFolder);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Transfer_FixedValueData_UpdatesByIdAndAppendsCopies(bool useTelerik)
        {
            var existing = new FixedValueSetup { id = 1, value = "old" };
            var added = new FixedValueSetup { id = 2, value = "second" };
            var display = CreateDisplay(useTelerik, existing);

            DisplayList.Transfer(new[]
            {
                new FixedValueSetup { id = 1, value = "new", portStatus = PortStatus.Reading },
                added
            }, display);

            Assert.AreEqual(2, display.Count);
            Assert.AreSame(existing, display[0]);
            Assert.AreEqual("new", existing.value);
            Assert.AreEqual(PortStatus.Reading, existing.portStatus);
            Assert.AreNotSame(added, display[1]);
            Assert.AreEqual("second", display[1].value);
        }
    }
}
