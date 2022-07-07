using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for Meteorological.xaml
    /// </summary>
    public partial class Meteorological : UserControl
    {
        public Meteorological()
        {
            InitializeComponent();
        }

        public void Init(MeteorologicalVM viewModel)
        {
            DataContext = viewModel;
        }
    }
}
