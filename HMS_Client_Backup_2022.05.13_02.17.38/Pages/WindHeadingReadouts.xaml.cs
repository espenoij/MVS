using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for WindHeadingReadouts.xaml
    /// </summary>
    public partial class WindHeadingReadouts : UserControl
    {
        public WindHeadingReadouts()
        {
            InitializeComponent();
        }

        public void Init(WindHeadingVM viewModel)
        {
            DataContext = viewModel;
        }
    }
}
