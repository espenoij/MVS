using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for WindHeadingCompassRose.xaml
    /// </summary>
    public partial class WindHeadingCompassRose : UserControl
    {
        public WindHeadingCompassRose()
        {
            InitializeComponent();
        }

        public void Init(WindHeadingVM viewModel)
        {
            DataContext = viewModel;
        }
    }
}
