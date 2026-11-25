using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVSTests.TestInfrastructure;

namespace MVSTests.Helpers
{
    [TestClass]
    public class CaptureChartsCollectionBindingTests
    {
        [TestMethod]
        public void CaptureCharts_RenderUpdatesFromNativeCollections()
        {
            StaTestHelper.Run(() =>
            {
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var projectVM = new ProjectVM();
                var view = new CaptureCharts();
                view.Init(projectVM);

                var alignmentTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
                view.DataContext = new ChartDataContext(alignmentTime);
                var collections = new List<ObservableCollectionEx<HMSData>>
                {
                    projectVM.refPitchList,
                    projectVM.testPitchList,
                    projectVM.refRollList,
                    projectVM.testRollList,
                    projectVM.refHeaveList,
                    projectVM.testHeaveList
                };
                var window = ShowOffscreen(view);
                try
                {
                    var previousPixels = Render(view);
                    for (var index = 0; index < collections.Count; index++)
                    {
                        var valueOffset = (index - (collections.Count - 1) / 2d) * 0.1;
                        collections[index].AddRange(CreateSamples(alignmentTime, valueOffset));
                        var pixels = Render(view);
                        Assert.IsTrue(
                            CountDifferentPixels(previousPixels, pixels) > 0,
                            "Adding samples should change the chart rendering for series " + index + ".");
                        previousPixels = pixels;
                    }

                    for (var index = 0; index < collections.Count; index++)
                    {
                        var valueOffset = (index - (collections.Count - 1) / 2d) * 0.1;
                        collections[index].Add(CreateSample(alignmentTime.AddMinutes(-1), -0.5 + valueOffset));
                    }
                    var pixelsAfterSingleItems = Render(view);
                    Assert.IsTrue(
                        CountDifferentPixels(previousPixels, pixelsAfterSingleItems) > 0,
                        "Adding individual samples should change the chart rendering.");

                    foreach (var collection in collections)
                        RemoveFirstTwoWithSuspendedNotifications(collection);
                    var pixelsAfterRemovals = Render(view);
                    Assert.IsTrue(
                        CountDifferentPixels(pixelsAfterSingleItems, pixelsAfterRemovals) > 0,
                        "Suspended removals should change the chart rendering.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static List<HMSData> CreateSamples(DateTime alignmentTime, double valueOffset)
        {
            return new List<HMSData>
            {
                CreateSample(alignmentTime.AddMinutes(-10), -1.5 + valueOffset),
                CreateSample(alignmentTime.AddMinutes(-8), 0.5 + valueOffset),
                CreateSample(alignmentTime.AddMinutes(-6), 1.5 + valueOffset),
                CreateSample(alignmentTime.AddMinutes(-4), -0.5 + valueOffset),
                CreateSample(alignmentTime.AddMinutes(-2), 1 + valueOffset)
            };
        }

        private static HMSData CreateSample(DateTime timestamp, double value)
        {
            return new HMSData
            {
                timestamp = timestamp,
                data = value,
                status = DataStatus.OK
            };
        }

        private static void RemoveFirstTwoWithSuspendedNotifications<T>(ObservableCollectionEx<T> collection)
        {
            collection.SuspendNotifications();
            try
            {
                collection.RemoveAt(0);
                collection.RemoveAt(0);
            }
            finally
            {
                collection.ResumeNotifications();
            }
        }

        private static int CountDifferentPixels(byte[] first, byte[] second)
        {
            Assert.AreEqual(first.Length, second.Length, "Rendered images must have the same dimensions.");

            var differentPixels = 0;
            for (var index = 0; index < first.Length; index += 4)
            {
                if (first[index] != second[index] ||
                    first[index + 1] != second[index + 1] ||
                    first[index + 2] != second[index + 2] ||
                    first[index + 3] != second[index + 3])
                {
                    differentPixels++;
                }
            }

            return differentPixels;
        }

        private static byte[] Render(FrameworkElement element)
        {
            const int width = 900;
            const int height = 700;
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            element.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(element);
            var pixels = new byte[width * height * 4];
            bitmap.CopyPixels(pixels, width * 4, 0);
            return pixels;
        }

        private static Window ShowOffscreen(FrameworkElement content)
        {
            var window = new Window
            {
                Content = content,
                Width = 900,
                Height = 700,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                ShowActivated = false,
                Left = -10000,
                Top = -10000
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            return window;
        }

        private sealed class ChartDataContext
        {
            public ChartDataContext(DateTime alignmentTime)
            {
                this.alignmentTime = alignmentTime;
            }

            public DateTime alignmentTime { get; }
            public DateTime alignmentTimeMin => alignmentTime.AddMinutes(-20);
            public DateTime alignmentTimeMax => alignmentTime;
            public double pitchChartAxisMax => 3;
            public double pitchChartAxisMin => -3;
            public double rollChartAxisMax => 3;
            public double rollChartAxisMin => -3;
            public double heaveChartAxisMax => 3;
            public double heaveChartAxisMin => -3;
            public RecordingContext MainWindowVM { get; } = new RecordingContext();
        }

        private sealed class RecordingContext
        {
            public bool IsRecording => false;
        }
    }
}
