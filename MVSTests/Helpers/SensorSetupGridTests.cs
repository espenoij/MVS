using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVSTests.TestInfrastructure;

namespace MVSTests.Helpers
{
    [TestClass]
    public class SensorSetupGridTests
    {
        [TestMethod]
        public void SensorSetupGrid_PreservesColumnsSelectionAndReadOnlyBehavior()
        {
            StaTestHelper.Run(() =>
            {
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var control = new SensorSetupPage();
                var grid = control.FindName("gvProjects") as DataGrid;
                Assert.IsNotNull(grid, "Sensor list should use the native DataGrid.");

                CollectionAssert.AreEqual(
                    new[] { "id", "type", "source", "mruTypeString", "name" },
                    grid.Columns
                        .Cast<DataGridTextColumn>()
                        .Select(column => ((Binding)column.Binding).Path.Path)
                        .ToArray());

                CollectionAssert.AreEqual(
                    new[] { 80d, 120d, 200d, 160d, 400d },
                    grid.Columns.Select(column => column.Width.Value).ToArray());
                CollectionAssert.AreEqual(
                    new[] { "ID", "Type", "Source", "MRU Type", "Name" },
                    grid.Columns.Select(column => column.Header as string).ToArray());

                Assert.IsTrue(grid.IsReadOnly);
                Assert.IsTrue(grid.CanUserSortColumns);
                Assert.IsFalse(grid.CanUserReorderColumns);
                Assert.IsFalse(grid.CanUserDeleteRows, "Read-only grid should not allow row deletion.");
                Assert.AreEqual(DataGridSelectionMode.Single, grid.SelectionMode);
                Assert.AreEqual(DataGridSelectionUnit.FullRow, grid.SelectionUnit);
            });
        }
    }
}
