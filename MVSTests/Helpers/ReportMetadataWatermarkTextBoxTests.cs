using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVS.Views.Controls;
using MVSTests.TestInfrastructure;

namespace MVSTests.Helpers
{
    [TestClass]
    public class ReportMetadataWatermarkTextBoxTests
    {
        [TestMethod]
        public void ReportMetadataFields_UseNativeWatermarkTextBoxes()
        {
            StaTestHelper.Run(() =>
            {
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var editor = new ReportMetadataEditor();
                var window = new Window
                {
                    Content = editor,
                    Width = 700,
                    Height = 800,
                    ShowInTaskbar = false,
                    WindowStyle = WindowStyle.None
                };
                window.Show();
                try
                {
                    editor.UpdateLayout();
                    var fields = FindVisualChildren<WatermarkTextBox>(editor).ToArray();
                    Assert.AreEqual(32, fields.Length);
                    Assert.IsTrue(fields.All(field => !string.IsNullOrWhiteSpace(field.WatermarkContent)));

                    var objective = FindByBindingPath(fields, "TestObjective");
                    Assert.AreEqual("Mandatory", objective.WatermarkContent);
                    Assert.AreEqual(TextWrapping.Wrap, objective.TextWrapping);
                    Assert.IsTrue(objective.AcceptsReturn);
                    Assert.AreEqual(
                        UpdateSourceTrigger.PropertyChanged,
                        BindingOperations.GetBinding(objective, TextBox.TextProperty).UpdateSourceTrigger);

                    var additionalEquipment = FindByBindingPath(fields, "AdditionalEquipment");
                    Assert.AreEqual("Optional", additionalEquipment.WatermarkContent);
                    Assert.AreEqual(
                        UpdateSourceTrigger.LostFocus,
                        BindingOperations.GetBinding(additionalEquipment, TextBox.TextProperty).UpdateSourceTrigger);

                    objective.ApplyTemplate();
                    var watermark = objective.Template.FindName("PART_Watermark", objective) as TextBlock;
                    Assert.IsNotNull(watermark);
                    Assert.AreEqual(Visibility.Visible, watermark.Visibility);
                    objective.Text = "entered value";
                    Assert.AreEqual(Visibility.Collapsed, watermark.Visibility);
                    objective.Text = string.Empty;
                    Assert.AreEqual(Visibility.Visible, watermark.Visibility);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static WatermarkTextBox FindByBindingPath(
            WatermarkTextBox[] fields,
            string path)
        {
            return fields.Single(field =>
                BindingOperations.GetBinding(field, TextBox.TextProperty)?.Path.Path == path);
        }

        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(
            DependencyObject parent)
            where T : DependencyObject
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                var child = VisualTreeHelper.GetChild(parent, index);
                if (child is T typedChild)
                    yield return typedChild;

                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }
    }
}
