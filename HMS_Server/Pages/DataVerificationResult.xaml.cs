using System.Windows.Controls;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for DataVerificationResult.xaml
    /// </summary>
    partial class DataVerificationResult : UserControl
    {
        // Configuration settings
        private Config config;

        public DataVerificationResult()
        {
            InitializeComponent();
        }

        public void Init(
            RadObservableCollectionEx<VerificationData> verificationData,
            Config config)
        {
            this.config = config;

            // Liste med HMS output data
            gvHMSVerification.ItemsSource = verificationData;
        }
    }
}