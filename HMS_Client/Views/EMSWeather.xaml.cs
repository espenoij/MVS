using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for EMSWeather.xaml
    /// </summary>
    public partial class EMSWeather : UserControl
    {
        public EMSWeather()
        {
            InitializeComponent();
        }

        public void Init(MeteorologicalVM viewModel)
        {
            DataContext = viewModel;
        }
    }
}
