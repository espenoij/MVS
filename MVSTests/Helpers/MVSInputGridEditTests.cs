using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVSTests.TestInfrastructure;

namespace MVSTests.Helpers
{
    [TestClass]
    public class MVSInputGridEditTests
    {
        [TestMethod]
        public void InputGrid_CommitsValidDataIdAndRejectsCancelledOrInvalidEdits()
        {
            string path = Path.Combine(Path.GetTempPath(), "mvs-input-" + Guid.NewGuid().ToString("N") + ".config");
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
                        new ExeConfigurationFileMap { ExeConfigFilename = path }, ConfigurationUserLevel.None);
                    var section = new MVSDataConfigSection();
                    section.ClientDataItems.Add(new MVSDataConfig
                    {
                        id = 1, name = "Input", dataId = "10", dbColumn = "input"
                    });
                    mapped.Sections.Add(ConfigKey.MVSData, section);
                    mapped.AppSettings.Settings.Add(ConfigKey.ServerUIUpdateFrequency, "25");
                    mapped.Save(ConfigurationSaveMode.Full);

                    var config = new Config();
                    typeof(Config).GetField("dataConfig", BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(config, mapped);
                    typeof(Config).GetField("appConfig", BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(config, mapped);
                    var input = new MVSDataCollection();
                    input.GetDataList().Add(new HMSData { id = 1, name = "Input", dataId = 10, dbColumn = "input" });
                    var control = new MVSInputSetup();
                    control.Init(new ObservableCollectionEx<SensorData>(), input, config);
                    control.UpdateSensorDataList();
                    var window = new Window { Content = control, Width = 1000, Height = 400, ShowInTaskbar = false };
                    window.Show();
                    try
                    {
                        var grid = (DataGrid)control.FindName("gvHMSInputData");
                        grid.UpdateLayout();
                        Assert.AreEqual(3, grid.Columns.Count);
                        Assert.IsTrue(grid.Columns[0].IsReadOnly);
                        Assert.IsFalse(grid.Columns[1].IsReadOnly);
                        Assert.IsTrue(grid.Columns[2].IsReadOnly);
                        Assert.AreEqual("dataId", ((Binding)((DataGridTextColumn)grid.Columns[1]).Binding).Path.Path);
                        Assert.IsFalse(grid.CanUserAddRows);
                        Assert.IsFalse(grid.CanUserDeleteRows);

                        var row = (HMSData)grid.Items[0];
                        int rowCommits = 0;
                        grid.RowEditEnding += (_, e) => { if (e.EditAction == DataGridEditAction.Commit) rowCommits++; };
                        Edit(grid, row, "25");
                        input.GetDataList()[0].data = 42;
                        control.Start();
                        var frame = new DispatcherFrame();
                        var delay = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
                        delay.Tick += (_, __) => { delay.Stop(); frame.Continue = false; };
                        delay.Start();
                        Dispatcher.PushFrame(frame);
                        control.Stop();
                        Assert.AreEqual("25", ((TextBox)grid.Columns[1].GetCellContent(row)).Text);
                        Assert.AreEqual(42d, row.data);
                        Assert.IsTrue(grid.CommitEdit(DataGridEditingUnit.Cell, true));
                        Assert.IsTrue(grid.CommitEdit(DataGridEditingUnit.Row, true));
                        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                        Assert.AreEqual(1, rowCommits, "A row commit should be raised.");
                        Assert.AreEqual(25, row.dataId, "Row commit must update the edited row.");
                        Assert.AreEqual(25, input.GetDataList()[0].dataId);
                        Assert.AreEqual(42d, input.GetDataList()[0].data);
                        Assert.AreEqual("25", ReadSavedDataId(path));

                        Edit(grid, row, "30");
                        Assert.IsTrue(grid.CommitEdit(DataGridEditingUnit.Cell, true));
                        Assert.IsTrue(grid.CancelEdit(DataGridEditingUnit.Row));
                        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                        Assert.AreEqual(25, row.dataId);
                        Assert.AreEqual(25, input.GetDataList()[0].dataId);
                        Assert.AreEqual("25", ReadSavedDataId(path));

                        Edit(grid, row, "40");
                        Assert.IsTrue(grid.CancelEdit(DataGridEditingUnit.Cell));
                        Assert.IsTrue(grid.CommitEdit(DataGridEditingUnit.Row, true));
                        Assert.AreEqual(25, row.dataId);
                        Assert.AreEqual(25, input.GetDataList()[0].dataId);
                        Assert.AreEqual("25", ReadSavedDataId(path));

                        Edit(grid, row, "not a number");
                        Assert.IsFalse(grid.CommitEdit(DataGridEditingUnit.Cell, true));
                        Assert.AreEqual(25, input.GetDataList()[0].dataId);
                        Assert.AreEqual("25", ReadSavedDataId(path));
                        grid.CancelEdit(DataGridEditingUnit.Cell);
                        grid.CancelEdit(DataGridEditingUnit.Row);
                    }
                    finally
                    {
                        window.Close();
                    }
                });
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private static void Edit(DataGrid grid, HMSData row, string value)
        {
            grid.SelectedItem = row;
            grid.CurrentCell = new DataGridCellInfo(row, grid.Columns[1]);
            grid.Focus();
            Assert.IsTrue(grid.BeginEdit());
            grid.UpdateLayout();
            var editor = (TextBox)grid.Columns[1].GetCellContent(row);
            editor.Text = value;
        }

        private static string ReadSavedDataId(string path)
        {
            var mapped = ConfigurationManager.OpenMappedExeConfiguration(
                new ExeConfigurationFileMap { ExeConfigFilename = path }, ConfigurationUserLevel.None);
            return ((MVSDataConfigSection)mapped.GetSection(ConfigKey.MVSData)).ClientDataItems[0].dataId;
        }
    }
}
