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
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

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
                var legacyGrid = CreateLegacyGrid(records);

                var sideBySide = new Grid();
                sideBySide.ColumnDefinitions.Add(new ColumnDefinition());
                sideBySide.ColumnDefinitions.Add(new ColumnDefinition());
                Grid.SetColumn(control, 0);
                sideBySide.Children.Add(control);
                Grid.SetColumn(legacyGrid, 1);
                sideBySide.Children.Add(legacyGrid);

                var window = new Window
                {
                    Content = sideBySide,
                    Width = 1200,
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
                    Assert.IsFalse(
                        grid.CanUserDeleteRows,
                        "WPF disables row deletion for this read-only grid despite the legacy markup setting CanUserDeleteRows=true.");
                    Assert.IsTrue(grid.IsReadOnly);
                    Assert.IsTrue(grid.IsManipulationEnabled);
                    Assert.AreEqual(DataGridSelectionMode.Single, grid.SelectionMode);
                    Assert.AreEqual(DataGridSelectionUnit.FullRow, grid.SelectionUnit);

                    Assert.AreEqual(records.Count, legacyGrid.Items.Count);
                    for (var index = 0; index < records.Count; index++)
                        Assert.AreSame(records[index], legacyGrid.Items[index]);

                    Assert.AreEqual(GetRowColor(legacyGrid, records[0]), GetRowColor(grid, records[0]));
                    Assert.AreEqual(GetRowColor(legacyGrid, records[1]), GetRowColor(grid, records[1]));
                    Assert.AreEqual(GetRowColor(legacyGrid, records[2]), GetRowColor(grid, records[2]));
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

        private static RadGridView CreateLegacyGrid(ObservableCollection<SensorData> records)
        {
            var grid = new RadGridView
            {
                ItemsSource = records,
                IsReadOnly = true,
                IsManipulationEnabled = true,
                CanUserSortColumns = true,
                CanUserReorderColumns = false,
                CanUserDeleteRows = true,
                SelectionMode = System.Windows.Controls.SelectionMode.Single,
                AutoGenerateColumns = false,
                ShowGroupPanel = false,
                RowStyle = CreateLegacyRowStyle()
            };
            grid.Columns.Add(new GridViewDataColumn { Header = "Data ID", DataMemberBinding = new Binding("id") });
            grid.Columns.Add(new GridViewDataColumn { Header = "Name", DataMemberBinding = new Binding("name") });
            grid.Columns.Add(new GridViewDataColumn { Header = "Data", DataMemberBinding = new Binding("data") });
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
            AddStatusTrigger(style, "NONE", "Transparent");
            return style;
        }

        private static void AddStatusTrigger(Style style, string status, string resourceKey)
        {
            var trigger = new DataTrigger
            {
                Binding = new Binding("statusString"),
                Value = status
            };
            var value = resourceKey == "Transparent"
                ? Brushes.Transparent
                : Application.Current.FindResource(resourceKey);
            trigger.Setters.Add(new Setter(Control.BackgroundProperty, value));
            style.Triggers.Add(trigger);
        }

        private static Color? GetRowColor(RadGridView grid, SensorData record)
        {
            grid.UpdateLayout();
            var row = grid.ItemContainerGenerator.ContainerFromItem(record) as GridViewRow;
            Assert.IsNotNull(row, "Expected a Telerik grid row for " + record.name + ".");
            return (row.Background as SolidColorBrush)?.Color;
        }

        private static Color? GetRowColor(DataGrid grid, SensorData record)
        {
            grid.ScrollIntoView(record);
            grid.UpdateLayout();
            var row = grid.ItemContainerGenerator.ContainerFromItem(record) as DataGridRow;
            Assert.IsNotNull(row, "Expected a DataGrid row for " + record.name + ".");
            return (row.Background as SolidColorBrush)?.Color;
        }
    }
}
