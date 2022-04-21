using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for EMSWindHeadingCompassRose.xaml
    /// </summary>
    public partial class EMSWindHeadingCompassRose : UserControl
    {
        public EMSWindHeadingCompassRose()
        {
            InitializeComponent();
        }

        public void Init(WindHeadingVM viewModel)
        {
            DataContext = viewModel;
        }
    }
}
