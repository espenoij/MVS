using System;
using System.Linq;
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

        public void Init(RadObservableCollection<VerificationData> verificationData, Config config)
        {
            // Liste med HMS output data
            gvHMSVerification.ItemsSource = verificationData;

            // Dispatcher som oppdatere UI
            DispatcherTimer uiTimer = new DispatcherTimer();
            uiTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ServerUIUpdateFrequency, Constants.ServerUIUpdateFrequencyDefault));
            uiTimer.Tick += runUIInputUpdate;
            uiTimer.Start();

            void runUIInputUpdate(object sender, EventArgs e)
            {
                // Hente listen med prosesserte sensor data
                foreach (var item in verificationData)
                {
                    // Finne igjen sensor i display listen
                    var sensorDataList = verificationDataDisplayList.ToList().Where(x => x.id == item.id);

                    // Dersom vi fant sensor
                    if (sensorDataList.Count() > 0)
                    {
                        // Oppdater data
                        verificationDataDisplayList.First().Set(item);
                    }
                    // ...fant ikke sensor
                    else
                    {
                        // Legg den inn i listen
                        verificationDataDisplayList.Add(new VerificationData(item));
                    }
                }
            }
        }
    }
}