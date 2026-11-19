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
    public class ProjectsGridTests
    {
        [TestMethod]
        public void ProjectsGrid_PreservesColumnsSelectionAndStatusTemplate()
        {
            StaTestHelper.Run(() =>
            {
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var control = new Projects();
                var grid = control.FindName("gvProjects") as DataGrid;
                Assert.IsNotNull(grid, "Projects list should use the native DataGrid.");

                CollectionAssert.AreEqual(
                    new[] { "Id", "Name", "DateString" },
                    grid.Columns
                        .Take(3)
                        .Cast<DataGridTextColumn>()
                        .Select(column => ((Binding)column.Binding).Path.Path)
                        .ToArray());

                Assert.AreEqual(DataGridLengthUnitType.Pixel, grid.Columns[0].Width.UnitType);
                Assert.AreEqual(40d, grid.Columns[0].Width.Value);
                Assert.AreEqual(DataGridLengthUnitType.Star, grid.Columns[1].Width.UnitType);
                Assert.AreEqual(2d, grid.Columns[1].Width.Value);
                Assert.AreEqual(DataGridLengthUnitType.Star, grid.Columns[2].Width.UnitType);
                Assert.AreEqual(1d, grid.Columns[2].Width.Value);

                Assert.IsTrue(grid.IsReadOnly);
                Assert.IsTrue(grid.CanUserSortColumns);
                Assert.IsFalse(grid.CanUserReorderColumns);
                Assert.IsFalse(grid.CanUserDeleteRows, "Read-only grid should not allow row deletion.");
                Assert.AreEqual(DataGridSelectionMode.Single, grid.SelectionMode);
                Assert.AreEqual(DataGridSelectionUnit.FullRow, grid.SelectionUnit);
                Assert.AreEqual(0d, grid.RowHeaderWidth);

                var statusColumn = grid.Columns[3] as DataGridTemplateColumn;
                Assert.IsNotNull(statusColumn);
                Assert.AreEqual("Status", statusColumn.Header);
                Assert.IsNotNull(statusColumn.CellTemplate);
            });
        }
    }
}
