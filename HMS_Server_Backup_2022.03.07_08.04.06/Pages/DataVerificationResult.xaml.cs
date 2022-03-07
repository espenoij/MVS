using System;
using System.Windows.Controls;
using System.Windows.Threading;
using Telerik.Windows.Data;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for DataVerificationResult.xaml
    /// </summary>
    partial class DataVerificationResult : UserControl
    {
        private RadObservableCollection<VerificationData> verificationDataDisplayList = new RadObservableCollection<VerificationData>();

        public DataVerificationResult()
        {
            InitializeComponent();
        }

        public void Init(Verfication verification, Config config)
        {
            // Liste med HMS output data
            gvHMSVerification.ItemsSource = verificationDataDisplayList;

            // Dispatcher som oppdatere UI
            DispatcherTimer uiTimer = new DispatcherTimer();
            uiTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ServerUIUpdateFrequency, Constants.ServerUIUpdateFrequencyDefault));
            uiTimer.Tick += runUIInputUpdate;
            uiTimer.Start();

            void runUIInputUpdate(object sender, EventArgs e)
            {
                if (AdminMode.IsActive)
                {
                    // Overføre fra data lister til display lister
                    DisplayList.Transfer(verification.GetVerificationData(), verificationDataDisplayList);
                }
            }
        }
    }
}