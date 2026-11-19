using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVSTests.TestInfrastructure;

namespace MVSTests.Helpers
{
    [TestClass]
    public class ErrorMessagesGridTests
    {
        [TestMethod]
        public void ErrorMessagesGrid_UsesReadOnlyNativeDataGridWithExpectedColumns()
        {
            StaTestHelper.Run(() =>
            {
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var page = new ErrorMessagesPage();
                var grid = page.FindName("lvErrorMessagesData") as DataGrid;

                Assert.IsNotNull(grid, "Error messages should use the native DataGrid.");
                Assert.AreEqual(4, grid.Columns.Count);
                Assert.AreEqual(DataGridHeadersVisibility.None, grid.HeadersVisibility);
                Assert.IsTrue(grid.IsReadOnly);
                Assert.IsFalse(grid.CanUserSortColumns);
                Assert.IsFalse(grid.CanUserReorderColumns);
                Assert.IsFalse(grid.CanUserDeleteRows);
                Assert.IsFalse(grid.CanUserAddRows);
                Assert.AreEqual(0, grid.RowHeaderWidth);
                Assert.AreEqual(25d, grid.RowHeight);
                Assert.AreEqual(DataGridSelectionMode.Extended, grid.SelectionMode);
                Assert.AreEqual(DataGridSelectionUnit.FullRow, grid.SelectionUnit);
            });
        }
    }
}
