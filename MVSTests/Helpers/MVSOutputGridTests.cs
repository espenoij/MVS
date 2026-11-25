using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVSTests.TestInfrastructure;

namespace MVSTests.Helpers
{
    [TestClass]
    public class MVSOutputGridTests
    {
        [TestMethod]
        public void MVSOutputGrid_PreservesColumnsSelectionAndStatusColors()
        {
            StaTestHelper.Run(() =>
            {
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var control = new MVSOutput();
                var grid = control.FindName("gvMVSOutputData") as DataGrid;
                Assert.IsNotNull(grid, "MVS output should use the native DataGrid.");

                var records = new ObservableCollection<HMSData>
                {
                    CreateRecord(1, "Timeout", DataStatus.TIMEOUT_ERROR),
                    CreateRecord(2, "Valid", DataStatus.OK),
                    CreateRecord(3, "Not available", DataStatus.OK_NA)
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

                    Assert.AreEqual(4, grid.Columns.Count);
                    var expectedStarWidths = new[] { 2d, 1d, 1d, 1d };
                    CollectionAssert.AreEqual(
                        new[] { "name", "dataString", "id", "statusString" },
                        grid.Columns
                            .Cast<DataGridTextColumn>()
                            .Select(column => ((System.Windows.Data.Binding)column.Binding).Path.Path)
                            .ToArray());
                    for (var index = 0; index < grid.Columns.Count; index++)
                    {
                        Assert.AreEqual(DataGridLengthUnitType.Star, grid.Columns[index].Width.UnitType);
                        Assert.AreEqual(expectedStarWidths[index], grid.Columns[index].Width.Value);
                    }
                    Assert.IsTrue(grid.CanUserSortColumns);
                    Assert.IsFalse(grid.CanUserReorderColumns);
                    Assert.IsFalse(grid.CanUserDeleteRows);
                    Assert.IsTrue(grid.IsReadOnly);
                    Assert.AreEqual(DataGridSelectionMode.Single, grid.SelectionMode);
                    Assert.AreEqual(DataGridSelectionUnit.FullRow, grid.SelectionUnit);
                    Assert.AreEqual(DataGridHeadersVisibility.Column, grid.HeadersVisibility);

                    grid.SelectedItem = records[1];
                    Assert.AreSame(records[1], grid.SelectedItem);

                    AssertRowColor(grid, records[0], "ColorAmber");
                    AssertRowColor(grid, records[1], "ColorGreen");
                    AssertRowColor(grid, records[2], "ColorGreen");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static HMSData CreateRecord(int id, string name, DataStatus status)
        {
            return new HMSData
            {
                id = id,
                name = name,
                data = id * 1.5,
                status = status
            };
        }

        private static void AssertRowColor(DataGrid grid, HMSData record, string resourceKey)
        {
            grid.ScrollIntoView(record);
            grid.UpdateLayout();
            var row = grid.ItemContainerGenerator.ContainerFromItem(record) as DataGridRow;
            Assert.IsNotNull(row, "Expected a realized DataGrid row for " + record.name + ".");
            var actual = (row.Background as SolidColorBrush)?.Color;
            var expected = (Application.Current.FindResource(resourceKey) as SolidColorBrush)?.Color;
            Assert.AreEqual(expected, actual, "Unexpected row status color for " + record.statusString + ".");
        }

    }
}
