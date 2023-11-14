using System;
using System.Windows.Controls;
using System.Windows.Threading;
using Telerik.Windows.Data;

namespace MVS
{
    /// <summary>
    /// Interaction logic for MVSOutput.xaml
    /// </summary>
    public partial class MVSOutput : UserControl
    {
        // Configuration settings
        private Config config;

        private RadObservableCollection<HMSData> hmsOutputDisplayList = new RadObservableCollection<HMSData>();

        DispatcherTimer uiTimer = new DispatcherTimer();

        public MVSOutput()
        {
            InitializeComponent();
        }

        public void Init(
            MVSDataCollection hmsOutputDataList,
            Config config)
        {
            this.config = config;

            // Liste med HMS output data
            gvMVSOutputData.ItemsSource = hmsOutputDisplayList;

            // Dispatcher som oppdatere UI
            uiTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ServerUIUpdateFrequency, Constants.ServerUIUpdateFrequencyDefault));
            uiTimer.Tick += runUIInputUpdate;

            void runUIInputUpdate(object sender, EventArgs e)
            {
                // Overføre fra data lister til display lister
                DisplayList.Transfer(hmsOutputDataList.GetDataList(), hmsOutputDisplayList);
            }
        }

        public void Start()
        {
            uiTimer.Start();
        }

        public void Stop()
        {
            uiTimer.Stop();
        }
    }
}