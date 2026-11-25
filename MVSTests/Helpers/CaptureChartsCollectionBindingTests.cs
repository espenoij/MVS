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
                    var sampleTime = projectVM.alignmentTime.AddMinutes(-10);
                    foreach (var collection in new[]
                    {
                        projectVM.refPitchList, projectVM.testPitchList,
                        projectVM.refRollList, projectVM.testRollList,
                        projectVM.refHeaveList, projectVM.testHeaveList
                    })
                    {
                        collection.AddRange(CreateSamples(sampleTime));
                    }

                    var populatedPixels = Render(view);
                    Assert.IsTrue(
                        CountDifferentPixels(emptyPixels, populatedPixels) > 0,
                        "Adding live chart samples should render both traces.");

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
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static List<HMSData> CreateSamples(DateTime timestamp)
        {
            return new List<HMSData>
            {
                CreateSample(timestamp, -1.5),
                CreateSample(timestamp.AddMinutes(2), 0.5),
                CreateSample(timestamp.AddMinutes(4), 1.5),
                CreateSample(timestamp.AddMinutes(6), -0.5),
                CreateSample(timestamp.AddMinutes(8), 1)
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
    }
}
