using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for GeneralInformation_CAP.xaml
    /// </summary>
    public partial class GeneralInformation_CAP : UserControl
    {
        public GeneralInformation_CAP()
        {
            InitializeComponent();
        }

        public void Init(GeneralInformationVM viewModel)
        {
            DataContext = viewModel;
        }
    }
}
