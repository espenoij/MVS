using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVS.Views.Controls;
using MVSTests.TestInfrastructure;

namespace MVSTests.Helpers
{
    [TestClass]
    public class ProjectsWatermarkTextBoxTests
    {
        [TestMethod]
        public void ProjectWizardFields_UseNativeWatermarkTextBoxes()
        {
            StaTestHelper.Run(() =>
            {
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var control = new Projects();
                var operatorField = control.FindName("tbOperator") as WatermarkTextBox;
                var vesselField = control.FindName("tbVesselName") as WatermarkTextBox;
                var locationField = control.FindName("tbLocation") as WatermarkTextBox;
                var commentsField = control.FindName("tbDataSetComments") as WatermarkTextBox;

                Assert.IsNotNull(operatorField);
                Assert.IsNotNull(vesselField);
                Assert.IsNotNull(locationField);
                Assert.IsNotNull(commentsField);

                Assert.AreEqual("Required", operatorField.WatermarkContent);
                Assert.AreEqual("Required", vesselField.WatermarkContent);
                Assert.AreEqual("Required", locationField.WatermarkContent);
                Assert.AreEqual("Optional", commentsField.WatermarkContent);

                Assert.AreEqual(32d, operatorField.MinHeight);
                Assert.AreEqual(VerticalAlignment.Center, operatorField.VerticalContentAlignment);
                Assert.AreEqual(32d, vesselField.MinHeight);
                Assert.AreEqual(32d, locationField.MinHeight);
                Assert.IsTrue(commentsField.AcceptsReturn);
                Assert.AreEqual(TextWrapping.Wrap, commentsField.TextWrapping);
                Assert.AreEqual(ScrollBarVisibility.Auto, commentsField.VerticalScrollBarVisibility);
                Assert.AreEqual(60d, commentsField.Height);
                Assert.AreEqual(60d, commentsField.MinHeight);
                Assert.AreEqual(120d, commentsField.MaxHeight);
            });
        }
    }
}
