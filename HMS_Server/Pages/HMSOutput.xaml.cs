using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;
using Telerik.Windows.Data;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for HMSOutput.xaml
    /// </summary>
    public partial class HMSOutput : UserControl
    {
        // Configuration settings
        private Config config;

        private RadObservableCollection<HMSData> hmsOutputDisplayList = new RadObservableCollection<HMSData>();

        public HMSOutput()
        {
            InitializeComponent();
        }

        public void Init(
            HMSDataCollection hmsOutputDataList,
            Config config)
        {
            this.config = config;

            // Liste med HMS output data
            gvHMSOutputData.ItemsSource = hmsOutputDisplayList;

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
                    DisplayList.Transfer(hmsOutputDataList.GetDataList(), hmsOutputDisplayList);
                }
            }
        }
    }
}