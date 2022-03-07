using System.Windows;
using Telerik.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for DialogHelicopterLanded
    /// .xaml
    /// </summary>
    public partial class DialogHelicopterLanded : RadWindow
    {
        private UserInputsVM userInputsVM;
        private OnDeckStabilityLimitsVM helideckStabilityLimitsVM;
        private RelativeWindLimitsVM relativeWindLimitsVM;

        public DialogHelicopterLanded(UserInputsVM userInputsVM, OnDeckStabilityLimitsVM helideckStabilityLimitsVM, RelativeWindLimitsVM helideckStatusTrendVM)
        {
            InitializeComponent();

            DataContext = userInputsVM;

            this.userInputsVM = userInputsVM;
            this.helideckStabilityLimitsVM = helideckStabilityLimitsVM;
            this.relativeWindLimitsVM = helideckStatusTrendVM;
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
            relativeWindLimitsVM.selectedGraphTime = GraphTime.Minutes20;

            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}
