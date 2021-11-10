using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;

namespace SensorMonitorClient
{
    /// <summary>
    /// Interaction logic for DialogHelicopterLanded
    /// .xaml
    /// </summary>
    public partial class DialogHelicopterLanded : RadWindow
    {
        private UserInputsVM userInputsVM;
        private HelideckStabilityLimitsVM helideckStabilityLimitsVM;
        private HelideckStatusTrendVM helideckStatusTrendVM;

        public DialogHelicopterLanded(UserInputsVM userInputsVM, HelideckStabilityLimitsVM helideckStabilityLimitsVM, HelideckStatusTrendVM helideckStatusTrendVM)
        {
            InitializeComponent();

            DataContext = userInputsVM;

            this.userInputsVM = userInputsVM;
            this.helideckStabilityLimitsVM = helideckStabilityLimitsVM;
            this.helideckStatusTrendVM = helideckStatusTrendVM;
        }

        public void Heading(double heading)
        {
            userInputsVM.helicopterLandedHeading = heading;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            // Overføre heading verdi
            userInputsVM.onDeckHelicopterHeading = userInputsVM.helicopterLandedHeading;

            // Display Mode
            userInputsVM.displayMode = DisplayMode.OnDeck;

            // Resetter trend grafene til å vise 20 minutter data
            helideckStabilityLimitsVM.selectedGraphTime = GraphTime.Minutes20;
            helideckStatusTrendVM.selectedGraphTime = GraphTime.Minutes20;

            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}
