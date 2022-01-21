using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for WindHeadingReadouts_CAP.xaml
    /// </summary>
    public partial class WindHeadingReadouts_CAP : UserControl
    {
        public WindHeadingReadouts_CAP()
        {
            InitializeComponent();
        }

        public void Init(WindHeadingVM viewModel)
        {
            DataContext = viewModel;
        }
    }
}
