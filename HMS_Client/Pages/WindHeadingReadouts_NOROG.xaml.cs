using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for WindHeadingReadouts_NOROG.xaml
    /// </summary>
    public partial class WindHeadingReadouts_NOROG : UserControl
    {
        public WindHeadingReadouts_NOROG()
        {
            InitializeComponent();
        }

        public void Init(WindHeadingVM viewModel)
        {
            DataContext = viewModel;
        }
    }
}
