using System;
using System.Windows.Controls;

namespace SensorMonitorClient
{
    /// <summary>
    /// Interaction logic for GeneralInformation_CAP.xaml
    /// </summary>
    public partial class GeneralInformation_CAP : UserControl
    {
        // CAP Client Data
        private GeneralInformationVM viewModel;

        public GeneralInformation_CAP()
        {
            InitializeComponent();
        }

        public void Init(GeneralInformationVM viewModel, Config config)
        {
            DataContext = viewModel;

            this.viewModel = viewModel;

            // Init av UI
            InitUI(config);
        }

        private void InitUI(Config config)
        {
            // Software Version
            viewModel.softwareVersion = Constants.SoftwareVersion;
        }
    }
}
