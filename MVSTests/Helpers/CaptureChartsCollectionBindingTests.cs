using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVSTests.TestInfrastructure;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace MVSTests.Helpers
{
    [TestClass]
    public class CaptureChartsCollectionBindingTests
    {
        [TestMethod]
        public void CaptureCharts_RenderIdenticallyWithTelerikAndNativeCollections()
        {
            StaTestHelper.Run(() =>
            {
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var projectVM = new ProjectVM();
                var nativeView = new CaptureCharts();
                var telerikView = new CaptureCharts();
                nativeView.Init(projectVM);
                telerikView.Init(projectVM);

                var alignmentTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
                nativeView.DataContext = new ChartDataContext(alignmentTime);
                telerikView.DataContext = new ChartDataContext(alignmentTime);

                var chartNames = new[] { "chartPitch", "chartRoll", "chartHeave" };
                var nativeCollections = new List<ObservableCollectionEx<HMSData>>();
                var telerikCollections = new List<RadObservableCollection<HMSData>>();
                foreach (var chartName in chartNames)
                {
                    var nativeChart = (RadCartesianChart)nativeView.FindName(chartName);
                    var telerikChart = (RadCartesianChart)telerikView.FindName(chartName);
                    Assert.AreEqual(2, nativeChart.Series.Count, chartName + " should have two series.");
                    Assert.AreEqual(2, telerikChart.Series.Count, chartName + " should have two series.");
                    for (var seriesIndex = 0; seriesIndex < nativeChart.Series.Count; seriesIndex++)
                    {
                        var nativeCollection = new ObservableCollectionEx<HMSData>();
                        var telerikCollection = new RadObservableCollection<HMSData>();
                        nativeChart.Series[seriesIndex].ItemsSource = nativeCollection;
                        telerikChart.Series[seriesIndex].ItemsSource = telerikCollection;
                        nativeCollections.Add(nativeCollection);
                        telerikCollections.Add(telerikCollection);
                    }
                }
                Assert.AreEqual(6, nativeCollections.Count);
                Assert.AreEqual(6, telerikCollections.Count);

                var nativeWindow = ShowOffscreen(nativeView);
                var telerikWindow = ShowOffscreen(telerikView);
                try
                {
                    var emptyNativePixels = Render(nativeView);
                    var emptyTelerikPixels = Render(telerikView);
                    AssertPixelsEqual(emptyTelerikPixels, emptyNativePixels, "Empty chart rendering");

                    var previousNativePixels = emptyNativePixels;
                    for (var index = 0; index < nativeCollections.Count; index++)
                    {
                        var valueOffset = (index - (nativeCollections.Count - 1) / 2d) * 0.1;
                        nativeCollections[index].AddRange(CreateSamples(alignmentTime, valueOffset));
                        telerikCollections[index].AddRange(CreateSamples(alignmentTime, valueOffset));

                        var telerikPixels = Render(telerikView);
                        var nativePixels = Render(nativeView);
                        var seriesName = chartNames[index / 2] + " series " + (index % 2);
                        AssertPixelsEqual(telerikPixels, nativePixels, "AddRange " + seriesName + " rendering");
                        Assert.IsTrue(
                            CountDifferentPixels(previousNativePixels, nativePixels) > 0,
                            "AddRange should render data from " + seriesName + ".");
                        previousNativePixels = nativePixels;
                    }

                    for (var index = 0; index < nativeCollections.Count; index++)
                    {
                        var valueOffset = (index - (nativeCollections.Count - 1) / 2d) * 0.1;
                        nativeCollections[index].Add(CreateSample(alignmentTime.AddMinutes(-1), -0.5 + valueOffset));
                        telerikCollections[index].Add(CreateSample(alignmentTime.AddMinutes(-1), -0.5 + valueOffset));
                    }
                    AssertRenderedCollectionsEqual(telerikView, nativeView, "Single-item update");

                    for (var index = 0; index < nativeCollections.Count; index++)
                    {
                        RemoveFirstTwoWithSuspendedNotifications(nativeCollections[index]);
                        RemoveFirstTwoWithSuspendedNotifications(telerikCollections[index]);
                    }
                    AssertRenderedCollectionsEqual(telerikView, nativeView, "Suspended removals");
                }
                finally
                {
                    nativeWindow.Close();
                    telerikWindow.Close();
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

        private static void RemoveFirstTwoWithSuspendedNotifications<T>(RadObservableCollection<T> collection)
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

        private static void AssertRenderedCollectionsEqual(CaptureCharts telerikView, CaptureCharts nativeView, string operation)
        {
            AssertPixelsEqual(Render(telerikView), Render(nativeView), operation + " chart rendering");
        }

        private static void AssertPixelsEqual(byte[] expected, byte[] actual, string operation)
        {
            Assert.AreEqual(
                0,
                CountDifferentPixels(expected, actual),
                operation + " should match pixel-for-pixel.");
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
