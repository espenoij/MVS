using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVSTests.TestInfrastructure;

namespace MVSTests.Dialogs
{
    [TestClass]
    public class DialogCaptureReadinessTests
    {
        [TestMethod]
        public void Init_DisablesStartOnlyForBlockingFailures()
        {
            StaTestHelper.Run(() =>
            {
                EnsureApplication();

                var dialog = new DialogCaptureReadiness();
                dialog.Init(new ReadinessReport(new List<ReadinessItem>
                {
                    new ReadinessItem("Blocking failure", false, ReadinessSeverity.Blocking),
                    new ReadinessItem("Advisory failure", false, ReadinessSeverity.Advisory)
                }));

                var start = (Button)dialog.FindName("btnStart");
                var cancel = (Button)dialog.FindName("btnCancel");

                Assert.IsFalse(start.IsEnabled);
                Assert.IsTrue(cancel.IsEnabled);
                Assert.AreEqual(2, ((ItemsControl)dialog.FindName("icItems")).Items.Count);
                Assert.AreSame(Application.Current.FindResource("SesNativePrimaryButtonStyle"), start.Style);
                Assert.AreSame(Application.Current.FindResource("SesNativeSecondaryButtonStyle"), cancel.Style);
                Assert.AreEqual(
                    ((SolidColorBrush)Application.Current.FindResource("SesEnergyGreenBrush")).Color,
                    ((SolidColorBrush)start.Background).Color);
            });
        }

        [TestMethod]
        public void Init_LeavesStartEnabledForAdvisoryFailures()
        {
            StaTestHelper.Run(() =>
            {
                EnsureApplication();

                var dialog = new DialogCaptureReadiness();
                dialog.Init(new ReadinessReport(new List<ReadinessItem>
                {
                    new ReadinessItem("Advisory failure", false, ReadinessSeverity.Advisory)
                }));

                Assert.IsTrue(((Button)dialog.FindName("btnStart")).IsEnabled);
            });
        }

        [TestMethod]
        public void StartAndCancel_KeepTheirConfirmationSemantics()
        {
            StaTestHelper.Run(() =>
            {
                EnsureApplication();

                var startDialog = new DialogCaptureReadiness();
                ((Button)startDialog.FindName("btnStart")).RaiseEvent(
                    new RoutedEventArgs(ButtonBase.ClickEvent));
                Assert.IsTrue(startDialog.UserConfirmed);

                var cancelDialog = new DialogCaptureReadiness();
                ((Button)cancelDialog.FindName("btnCancel")).RaiseEvent(
                    new RoutedEventArgs(ButtonBase.ClickEvent));
                Assert.IsFalse(cancelDialog.UserConfirmed);
            });
        }

        private static void EnsureApplication()
        {
            if (Application.Current == null)
            {
                var app = new App();
                app.InitializeComponent();
            }
        }
    }
}
