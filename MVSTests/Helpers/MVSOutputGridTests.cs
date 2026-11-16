using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVSTests.TestInfrastructure;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

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
                var legacyGrid = CreateLegacyGrid(records);
                var sideBySide = new Grid();
                sideBySide.ColumnDefinitions.Add(new ColumnDefinition());
                sideBySide.ColumnDefinitions.Add(new ColumnDefinition());
                sideBySide.Children.Add(control);
                Grid.SetColumn(legacyGrid, 1);
                sideBySide.Children.Add(legacyGrid);

                var window = new Window
                {
                    Content = sideBySide,
                    Width = 900,
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

                    Assert.AreEqual(grid.Items.Count, legacyGrid.Items.Count);
                    Assert.AreEqual(grid.Columns.Count, legacyGrid.Columns.Count);
                    for (var index = 0; index < grid.Items.Count; index++)
                        Assert.AreSame(grid.Items[index], legacyGrid.Items[index]);

                    grid.SelectedItem = records[1];
                    legacyGrid.SelectedItem = records[1];
                    Assert.AreSame(records[1], grid.SelectedItem);
                    Assert.AreSame(records[1], legacyGrid.SelectedItem);
                    Assert.AreEqual(System.Windows.Controls.SelectionMode.Single, legacyGrid.SelectionMode);

                    AssertRowColor(grid, records[0], "ColorAmber");
                    AssertRowColor(grid, records[1], "ColorGreen");
                    AssertRowColor(grid, records[2], "ColorGreen");
                    Assert.AreEqual(
                        GetRowColor(legacyGrid, records[0]),
                        GetRowColor(grid, records[0]),
                        "Timeout row status color should match the Telerik grid.");
                    Assert.AreEqual(
                        GetRowColor(legacyGrid, records[1]),
                        GetRowColor(grid, records[1]),
                        "Valid row status color should match the Telerik grid.");
                    Assert.AreEqual(
                        GetRowColor(legacyGrid, records[2]),
                        GetRowColor(grid, records[2]),
                        "Not-available row status color should match the Telerik grid.");
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

        private static Color? GetRowColor(RadGridView grid, HMSData record)
        {
            grid.UpdateLayout();
            var row = grid.ItemContainerGenerator.ContainerFromItem(record) as GridViewRow;
            Assert.IsNotNull(row, "Expected a Telerik grid row for " + record.name + ".");
            return (row.Background as SolidColorBrush)?.Color;
        }

        private static Color? GetRowColor(DataGrid grid, HMSData record)
        {
            grid.UpdateLayout();
            var row = grid.ItemContainerGenerator.ContainerFromItem(record) as DataGridRow;
            Assert.IsNotNull(row, "Expected a DataGrid row for " + record.name + ".");
            return (row.Background as SolidColorBrush)?.Color;
        }

        private static RadGridView CreateLegacyGrid(ObservableCollection<HMSData> records)
        {
            var grid = new RadGridView
            {
                ItemsSource = records,
                IsReadOnly = true,
                IsManipulationEnabled = false,
                CanUserSortColumns = true,
                CanUserReorderColumns = false,
                CanUserDeleteRows = false,
                SelectionMode = System.Windows.Controls.SelectionMode.Single,
                AutoGenerateColumns = false,
                ShowGroupPanel = false,
                RowStyle = CreateLegacyRowStyle()
            };
            grid.Columns.Add(new GridViewDataColumn { Header = "Name", DataMemberBinding = new System.Windows.Data.Binding("name"), IsReadOnly = true });
            grid.Columns.Add(new GridViewDataColumn { Header = "Data", DataMemberBinding = new System.Windows.Data.Binding("dataString"), IsReadOnly = true });
            grid.Columns.Add(new GridViewDataColumn { Header = "ID", DataMemberBinding = new System.Windows.Data.Binding("id"), IsReadOnly = true });
            grid.Columns.Add(new GridViewDataColumn { Header = "Status", DataMemberBinding = new System.Windows.Data.Binding("statusString"), IsReadOnly = true });
            return grid;
        }

        private static Style CreateLegacyRowStyle()
        {
            var style = new Style(typeof(GridViewRow))
            {
                BasedOn = (Style)Application.Current.FindResource("GridViewRowStyle")
            };
            AddStatusTrigger(style, "TIMEOUT_ERROR", "ColorAmber");
            AddStatusTrigger(style, "OK", "ColorGreen");
            AddStatusTrigger(style, "OK_NA", "ColorGreen");
            return style;
        }

        private static void AddStatusTrigger(Style style, string status, string resourceKey)
        {
            var trigger = new DataTrigger
            {
                Binding = new System.Windows.Data.Binding("statusString"),
                Value = status
            };
            trigger.Setters.Add(new Setter(
                Control.BackgroundProperty,
                Application.Current.FindResource(resourceKey)));
            style.Triggers.Add(trigger);
        }
    }
}
