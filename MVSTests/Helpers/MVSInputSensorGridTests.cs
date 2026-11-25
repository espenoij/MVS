using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVSTests.TestInfrastructure;

namespace MVSTests.Helpers
{
    [TestClass]
    public class MVSInputSensorGridTests
    {
        [TestMethod]
        public void SensorInputGrid_PreservesColumnsSelectionAndStatusColors()
        {
            StaTestHelper.Run(() =>
            {
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var control = new MVSInputSetup();
                var grid = control.FindName("gvSensorInputData") as DataGrid;
                Assert.IsNotNull(grid, "Sensor input list should use the native DataGrid.");

                var records = new ObservableCollection<SensorData>
                {
                    CreateRecord(1, "Timeout", PortStatus.NoData),
                    CreateRecord(2, "Reading", PortStatus.Reading),
                    CreateRecord(3, "Idle", PortStatus.Closed)
                };
                grid.ItemsSource = records;
                var window = new Window
                {
                    Content = control,
                    Width = 600,
                    Height = 500,
                    ShowInTaskbar = false,
                    WindowStyle = WindowStyle.None
                };
                window.Show();
                try
                {
                    control.UpdateLayout();
                    grid.UpdateLayout();
                    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

                    Assert.AreEqual(3, grid.Columns.Count);
                    CollectionAssert.AreEqual(
                        new[] { "id", "name", "data" },
                        grid.Columns
                            .Cast<DataGridTextColumn>()
                            .Select(column => ((Binding)column.Binding).Path.Path)
                            .ToArray());
                    CollectionAssert.AreEqual(
                        new[] { "Data ID", "Name", "Data" },
                        grid.Columns.Select(column => column.Header as string).ToArray());
                    CollectionAssert.AreEqual(
                        new[] { 110d, 300d, 160d },
                        grid.Columns.Select(column => column.Width.Value).ToArray());
                    Assert.IsTrue(grid.CanUserSortColumns);
                    Assert.IsFalse(grid.CanUserReorderColumns);
                    Assert.IsFalse(grid.CanUserDeleteRows);
                    Assert.IsTrue(grid.IsReadOnly);
                    Assert.IsTrue(grid.IsManipulationEnabled);
                    Assert.AreEqual(DataGridSelectionMode.Single, grid.SelectionMode);
                    Assert.AreEqual(DataGridSelectionUnit.FullRow, grid.SelectionUnit);

                    AssertRowColor(grid, records[0], "ColorAmber");
                    AssertRowColor(grid, records[1], "ColorGreen");
                    AssertRowColor(grid, records[2], "Transparent");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static SensorData CreateRecord(int id, string name, PortStatus status)
        {
            return new SensorData
            {
                id = id,
                name = name,
                data = id * 1.5,
                portStatus = status
            };
        }

        private static void AssertRowColor(DataGrid grid, SensorData record, string resourceKey)
        {
            grid.ScrollIntoView(record);
            grid.UpdateLayout();
            var row = grid.ItemContainerGenerator.ContainerFromItem(record) as DataGridRow;
            Assert.IsNotNull(row, "Expected a DataGrid row for " + record.name + ".");
            var actual = (row.Background as SolidColorBrush)?.Color;
            var expected = resourceKey == "Transparent"
                ? Brushes.Transparent.Color
                : (Application.Current.FindResource(resourceKey) as SolidColorBrush)?.Color;
            Assert.AreEqual(expected, actual, "Unexpected row status color for " + record.statusString + ".");
        }
    }
}
