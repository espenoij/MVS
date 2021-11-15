using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for NOROG_GeneralInformation.xaml
    /// </summary>
    public partial class GeneralInformation_NOROG : UserControl
    {
        // NOROG Client Data
        private GeneralInformationVM viewModel;

        public GeneralInformation_NOROG()
        {
            InitializeComponent();
        }

        public void Init(GeneralInformationVM viewModel)
        {
            DataContext = viewModel;

            this.viewModel = viewModel;

            // Init av UI
            InitUI();
        }

        private void InitUI()
        {
            // Vessel Name
            // Settes i admin general settings

            // HelideckCategory category
            // Settes i admin general settings

            // Software Version
            viewModel.softwareVersion = Constants.SoftwareVersion;
        }
    }
}
