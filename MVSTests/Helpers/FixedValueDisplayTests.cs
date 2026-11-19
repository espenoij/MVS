using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVSTests.TestInfrastructure;
using Telerik.Windows.Data;

namespace MVSTests.Helpers
{
    [TestClass]
    public class FixedValueDisplayTests
    {
        [TestMethod]
        public void FixedValueDisplay_MatchesTelerikUpdatesAndPreservesControls()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mvs-fixed-" + Guid.NewGuid().ToString("N") + ".config");
            try
            {
                StaTestHelper.Run(() =>
                {
                    if (Application.Current == null)
                    {
                        var app = new App();
                        app.InitializeComponent();
                    }

                    var mapped = ConfigurationManager.OpenMappedExeConfiguration(
                        new ExeConfigurationFileMap { ExeConfigFilename = configPath },
                        ConfigurationUserLevel.None);
                    mapped.Sections.Add(ConfigSection.FileReaderConfig, new AppSettingsSection());
                    var config = new Config();
                    typeof(Config).GetField("appConfig", BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(config, mapped);
                    typeof(Config).GetField("dataConfig", BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(config, mapped);

                    var sensor = new SensorData(SensorType.FixedValue);
                    sensor.fixedValue.frequency = 100;
                    sensor.fixedValue.value = "7";
                    var dialog = new FixedValueSetupWindow(sensor, config)
                    {
                        WindowState = WindowState.Normal,
                        Width = 900,
                        Height = 600
                    };
                    dialog.Show();
                    try
                    {
                        var grid = (DataGrid)dialog.FindName("lvFixedDataOutput");
                        var tab = (TabItem)dialog.FindName("tabDataSelection");
                        var start = (Button)dialog.FindName("btnFixedValueOutputStart");
                        var stop = (Button)dialog.FindName("btnFixedValueOutputStop");
                        var clear = (Button)dialog.FindName("chkClearAllData");
                        var limit = (TextBox)dialog.FindName("tbTotalDataLines");
                        var status = (TextBlock)dialog.FindName("statusBar");
                        var current = (INotifyCollectionChanged)grid.ItemsSource;
                        var legacy = new RadObservableCollection<FileDataLine>();
                        var append = typeof(FixedValueSetupWindow).GetMethod(
                            "DisplayDataLines", BindingFlags.Instance | BindingFlags.NonPublic);

                        limit.Text = "2";
                        Assert.AreEqual(dialog.FindResource("ColorHorizontalBarBackground"), tab.Background);
                        limit.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
                        start.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        Assert.IsFalse(start.IsEnabled);
                        Assert.IsTrue(stop.IsEnabled);
                        Assert.AreEqual("Output started...", status.Text);
                        var frame = new DispatcherFrame();
                        NotifyCollectionChangedEventHandler onFirstItem = (_, __) => frame.Continue = false;
                        current.CollectionChanged += onFirstItem;
                        var timeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                        timeout.Tick += (_, __) => frame.Continue = false;
                        timeout.Start();
                        Dispatcher.PushFrame(frame);
                        timeout.Stop();
                        current.CollectionChanged -= onFirstItem;
                        Assert.IsTrue(grid.Items.Count > 0, "The running timer should display the fixed value.");
                        Assert.AreEqual("7", ((FileDataLine)grid.Items[0]).data);

                        stop.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        Assert.IsTrue(start.IsEnabled);
                        Assert.IsFalse(stop.IsEnabled);
                        Assert.AreEqual("Output stopped.", status.Text);
                        clear.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        var currentEvents = CaptureEvents(current);
                        var legacyEvents = CaptureEvents(legacy);

                        foreach (var value in new[] { "1", "2" })
                        {
                            append.Invoke(dialog, new object[] { value });
                            legacy.Add(new FileDataLine { data = value });
                        }
                        grid.UpdateLayout();
                        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                        grid.SelectedItem = grid.Items[1];
                        var selected = grid.SelectedItem;

                        append.Invoke(dialog, new object[] { "3" });
                        legacy.Add(new FileDataLine { data = "3" });
                        legacy.RemoveAt(0);
                        grid.UpdateLayout();
                        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

                        CollectionAssert.AreEqual(
                            legacy.Select(x => x.data).ToArray(),
                            grid.Items.Cast<FileDataLine>().Select(x => x.data).ToArray());
                        CollectionAssert.AreEqual(legacyEvents, currentEvents);
                        Assert.AreSame(selected, grid.SelectedItem);
                        Assert.AreEqual("Outputing fixed value data...", status.Text);
                        Assert.IsTrue(grid.IsReadOnly);
                        Assert.IsFalse(grid.CanUserSortColumns);

                        clear.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        legacy.Clear();
                        CollectionAssert.AreEqual(legacyEvents, currentEvents);
                        Assert.AreEqual(0, grid.Items.Count);
                        Assert.IsNull(grid.SelectedItem);

                        Assert.IsTrue(start.IsEnabled);
                        Assert.IsFalse(stop.IsEnabled);
                        Assert.AreEqual("7", sensor.fixedValue.value);
                        Assert.AreEqual(100d, sensor.fixedValue.frequency);
                    }
                    finally
                    {
                        dialog.Close();
                    }
                });
            }
            finally
            {
                if (File.Exists(configPath))
                    File.Delete(configPath);
            }
        }

        private static List<string> CaptureEvents(INotifyCollectionChanged collection)
        {
            var events = new List<string>();
            ((INotifyPropertyChanged)collection).PropertyChanged += (_, e) => events.Add("P:" + e.PropertyName);
            collection.CollectionChanged += (_, e) =>
                events.Add(string.Format("C:{0}:{1}:{2}", e.Action, e.NewStartingIndex, e.OldStartingIndex));
            return events;
        }
    }
}
