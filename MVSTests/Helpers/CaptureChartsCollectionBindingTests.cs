using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVS.Views.Controls;
using MVSTests.TestInfrastructure;

namespace MVSTests.Helpers
{
    [TestClass]
    public class CaptureChartsCollectionBindingTests
    {
        [TestMethod]
        public void CaptureCharts_RenderLiveCollectionsAndRefreshAfterBatchChanges()
        {
            StaTestHelper.Run(() =>
            {
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var projectVM = new ProjectVM();
                projectVM.alignmentTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
                var view = new CaptureCharts();
                view.Init(projectVM);
                view.DataContext = new ChartDataContext(projectVM.alignmentTime);

                var charts = new[]
                {
                    (LiveLineChart)view.FindName("chartPitch"),
                    (LiveLineChart)view.FindName("chartRoll"),
                    (LiveLineChart)view.FindName("chartHeave")
                };
                Assert.AreSame(projectVM.refPitchList, charts[0].ReferenceItemsSource);
                Assert.AreSame(projectVM.testPitchList, charts[0].TestItemsSource);
                Assert.AreSame(projectVM.refRollList, charts[1].ReferenceItemsSource);
                Assert.AreSame(projectVM.testRollList, charts[1].TestItemsSource);
                Assert.AreSame(projectVM.refHeaveList, charts[2].ReferenceItemsSource);
                Assert.AreSame(projectVM.testHeaveList, charts[2].TestItemsSource);

                var window = ShowOffscreen(view);
                try
                {
                    var emptyPixels = Render(view);
                    var sampleTime = projectVM.alignmentTime.AddMinutes(-20);
                    var collections = new[]
                    {
                        projectVM.refPitchList, projectVM.testPitchList,
                        projectVM.refRollList, projectVM.testRollList,
                        projectVM.refHeaveList, projectVM.testHeaveList
                    };
                    for (var index = 0; index < collections.Length; index++)
                    {
                        collections[index].AddRange(CreateSamples(sampleTime, index % 2 == 0 ? -0.5 : 0.5));
                    }

                    var populatedPixels = Render(view);
                    Assert.IsTrue(
                        CountDifferentPixels(emptyPixels, populatedPixels) > 0,
                        "Adding live chart samples should render the chart.");
                    AssertTraceAtVisibleBoundaries(
                        view,
                        charts[0],
                        populatedPixels,
                        ((SolidColorBrush)Application.Current.FindResource("ColorGraphLineRef")).Color,
                        "Reference");
                    AssertTraceAtVisibleBoundaries(
                        view,
                        charts[0],
                        populatedPixels,
                        ((SolidColorBrush)Application.Current.FindResource("ColorGraphLineTest")).Color,
                        "Vessel");

                    foreach (var collection in new[]
                    {
                        projectVM.refPitchList, projectVM.testPitchList,
                        projectVM.refRollList, projectVM.testRollList,
                        projectVM.refHeaveList, projectVM.testHeaveList
                    })
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

                    var trimmedPixels = Render(view);
                    Assert.IsTrue(
                        CountDifferentPixels(populatedPixels, trimmedPixels) > 0,
                        "Removing buffered samples should refresh the live charts.");
                    Assert.IsTrue(
                        CountPixelsNearColor(trimmedPixels, ((SolidColorBrush)Application.Current.FindResource("ColorGraphLineRef")).Color) > 0,
                        "The reference trace should remain visible after batched removals.");
                    Assert.IsTrue(
                        CountPixelsNearColor(trimmedPixels, ((SolidColorBrush)Application.Current.FindResource("ColorGraphLineTest")).Color) > 0,
                        "The vessel trace should remain visible after batched removals.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void DataAnalysis_UsesNativeChartsForAllSeriesAndRendersSamples()
        {
            StaTestHelper.Run(() =>
            {
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var projectVM = new ProjectVM();
                projectVM.alignmentTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
                var view = new DataAnalysis();
                view.Init(projectVM);
                view.DataContext = new ChartDataContext(projectVM.alignmentTime);

                var charts = new[]
                {
                    (LiveLineChart)view.FindName("chartPitch1"),
                    (LiveLineChart)view.FindName("chartPitch2"),
                    (LiveLineChart)view.FindName("chartPitch3"),
                    (LiveLineChart)view.FindName("chartRoll1"),
                    (LiveLineChart)view.FindName("chartRoll2"),
                    (LiveLineChart)view.FindName("chartRoll3"),
                    (LiveLineChart)view.FindName("chartHeave1"),
                    (LiveLineChart)view.FindName("chartHeave2"),
                    (LiveLineChart)view.FindName("chartHeave3")
                };
                Assert.AreEqual(9, charts.Length);

                Assert.AreSame(projectVM.refPitchList, charts[0].ReferenceItemsSource);
                Assert.AreSame(projectVM.testPitchList, charts[0].TestItemsSource);
                Assert.AreSame(projectVM.refPitchMeanList, charts[1].ReferenceItemsSource);
                Assert.AreSame(projectVM.testPitchMeanList, charts[1].TestItemsSource);
                Assert.AreSame(projectVM.devPitchMeanList, charts[2].TestItemsSource);
                Assert.AreSame(projectVM.refRollList, charts[3].ReferenceItemsSource);
                Assert.AreSame(projectVM.testRollList, charts[3].TestItemsSource);
                Assert.AreSame(projectVM.refRollMeanList, charts[4].ReferenceItemsSource);
                Assert.AreSame(projectVM.testRollMeanList, charts[4].TestItemsSource);
                Assert.AreSame(projectVM.devRollMeanList, charts[5].TestItemsSource);
                Assert.AreSame(projectVM.refHeaveList, charts[6].ReferenceItemsSource);
                Assert.AreSame(projectVM.testHeaveList, charts[6].TestItemsSource);
                Assert.AreSame(projectVM.refHeaveMeanList, charts[7].ReferenceItemsSource);
                Assert.AreSame(projectVM.testHeaveMeanList, charts[7].TestItemsSource);
                Assert.AreSame(projectVM.devHeaveMeanList, charts[8].TestItemsSource);
                Assert.AreSame(Application.Current.FindResource("ColorGraphLineDev"), charts[2].TestStroke);
                Assert.AreSame(Application.Current.FindResource("ColorGraphLineDev"), charts[5].TestStroke);
                Assert.AreSame(Application.Current.FindResource("ColorGraphLineDev"), charts[8].TestStroke);
                Assert.IsTrue(charts[0].ShowAbsoluteAxisLabels);
                Assert.IsTrue(charts[1].ShowAbsoluteAxisLabels);
                Assert.IsFalse(charts[6].ShowAbsoluteAxisLabels);
                Assert.IsFalse(charts[7].ShowAbsoluteAxisLabels);
                Assert.IsFalse(charts[8].ShowAbsoluteAxisLabels);

                var window = ShowOffscreen(view);
                try
                {
                    var emptyPixels = Render(view);
                    var sampleTime = projectVM.alignmentTime.AddMinutes(-20);
                    projectVM.refPitchList.AddRange(CreateSamples(sampleTime, -0.5));
                    projectVM.testPitchList.AddRange(CreateSamples(sampleTime, 0.5));
                    var populatedPixels = Render(view);
                    Assert.IsTrue(
                        CountDifferentPixels(emptyPixels, populatedPixels) > 0,
                        "Adding analysis samples should render the native chart traces.");
                    AssertTraceAtVisibleBoundaries(
                        view,
                        charts[0],
                        populatedPixels,
                        ((SolidColorBrush)Application.Current.FindResource("ColorGraphLineRef")).Color,
                        "Analysis reference");
                    AssertTraceAtVisibleBoundaries(
                        view,
                        charts[0],
                        populatedPixels,
                        ((SolidColorBrush)Application.Current.FindResource("ColorGraphLineTest")).Color,
                        "Analysis vessel");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static List<HMSData> CreateSamples(DateTime timestamp, double valueOffset)
        {
            return new List<HMSData>
            {
                CreateSample(timestamp, -1.5 + valueOffset),
                CreateSample(timestamp.AddMinutes(5), 0.5 + valueOffset),
                CreateSample(timestamp.AddMinutes(10), 1.5 + valueOffset),
                CreateSample(timestamp.AddMinutes(15), -0.5 + valueOffset),
                CreateSample(timestamp.AddMinutes(20), 1 + valueOffset)
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

        private static int CountPixelsNearColor(byte[] pixels, Color target)
        {
            var matches = 0;
            for (var index = 0; index < pixels.Length; index += 4)
            {
                if (pixels[index + 3] < 220)
                    continue;

                if (Math.Abs(pixels[index + 2] - target.R) <= 35 &&
                    Math.Abs(pixels[index + 1] - target.G) <= 35 &&
                    Math.Abs(pixels[index] - target.B) <= 35)
                {
                    matches++;
                }
            }

            return matches;
        }

        private static void AssertTraceAtVisibleBoundaries(
            FrameworkElement root,
            LiveLineChart chart,
            byte[] pixels,
            Color target,
            string seriesName)
        {
            const int plotLeft = 42;
            var transform = chart.TransformToAncestor(root);
            var screenPlotLeft = (int)Math.Round(transform.Transform(new Point(plotLeft, 0)).X);
            var screenPlotRight = (int)Math.Round(transform.Transform(new Point(chart.ActualWidth - 10, 0)).X);
            Assert.IsTrue(
                CountPixelsNearColorInColumns(pixels, target, screenPlotLeft - 3, screenPlotLeft + 3) > 0,
                seriesName + " trace should render at the visible time-window start. Plot column: " + screenPlotLeft);
            Assert.IsTrue(
                CountPixelsNearColorInColumns(pixels, target, screenPlotRight - 3, screenPlotRight + 3) > 0,
                seriesName + " trace should render at the visible time-window end. Plot column: " + screenPlotRight);
        }

        private static int CountPixelsNearColorInColumns(byte[] pixels, Color target, int minimumX, int maximumX)
        {
            const int stride = 900;
            var matches = 0;
            for (var y = 0; y < 700; y++)
            {
                for (var x = Math.Max(0, minimumX); x <= Math.Min(stride - 1, maximumX); x++)
                {
                    var index = (y * stride + x) * 4;
                    if (pixels[index + 3] < 220)
                        continue;

                    if (Math.Abs(pixels[index + 2] - target.R) <= 35 &&
                        Math.Abs(pixels[index + 1] - target.G) <= 35 &&
                        Math.Abs(pixels[index] - target.B) <= 35)
                    {
                        matches++;
                    }
                }
            }

            return matches;
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
            public double meanPitchChartAxisMax => 3;
            public double meanPitchChartAxisMin => -3;
            public double devPitchChartAxisMax => 3;
            public double devPitchChartAxisMin => -3;
            public double rollChartAxisMax => 3;
            public double rollChartAxisMin => -3;
            public double meanRollChartAxisMax => 3;
            public double meanRollChartAxisMin => -3;
            public double devRollChartAxisMax => 3;
            public double devRollChartAxisMin => -3;
            public double heaveChartAxisMax => 3;
            public double heaveChartAxisMin => -3;
            public double meanHeaveChartAxisMax => 3;
            public double meanHeaveChartAxisMin => -3;
            public double devHeaveChartAxisMax => 3;
            public double devHeaveChartAxisMin => -3;
            public RecordingContext MainWindowVM { get; } = new RecordingContext();
        }

        private sealed class RecordingContext
        {
            public bool IsRecording => false;
        }
    }
}
