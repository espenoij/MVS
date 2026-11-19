using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVSTests.TestInfrastructure;

namespace MVSTests.Helpers
{
    [TestClass]
    public class ProjectsWizardStepIndicatorTests
    {
        [TestMethod]
        public void StepIndicator_PreservesOrderStatusAndReachability()
        {
            StaTestHelper.Run(() =>
            {
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var control = new Projects();
                var indicator = control.FindName("wizardStepBar") as UniformGrid;
                Assert.IsNotNull(indicator, "Wizard steps should use the native WPF step indicator.");

                var steps = indicator.Children.OfType<Button>().ToArray();
                CollectionAssert.AreEqual(
                    new[] { "Setup", "LiDAR", "Capture", "Review", "Report" },
                    steps.Select(step => step.Content as string).ToArray());

                var updateSteps = typeof(Projects).GetMethod(
                    "UpdateStepProgressBar",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(updateSteps);
                updateSteps.Invoke(control, new object[] { 3 });

                CollectionAssert.AreEqual(
                    new[] { "Completed", "Completed", "Current", "Upcoming", "Upcoming" },
                    steps.Select(step => step.Tag as string).ToArray());
                CollectionAssert.AreEqual(
                    new[] { true, false, false, false, false },
                    steps.Select(step => step.IsEnabled).ToArray());
            });
        }
    }
}
