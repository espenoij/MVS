using System;
using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for WindHeading.xaml
    /// </summary>
    public partial class WindHeading : UserControl
    {
        WindHeadingVM windHeadingVM;

        public WindHeading()
        {
            InitializeComponent();
        }

        public void Init(WindHeadingVM viewModel, Config config)
        {
            windHeadingVM = viewModel;
            DataContext = viewModel;

            // Compass init
            ucCompass.Init(viewModel);

            // Wind & Heading readouts init
            ucReadouts_CAP.Init(viewModel);
            ucReadouts_NOROG.Init(viewModel);

            // Wind Measurement
            foreach (WindMeasurement value in Enum.GetValues(typeof(WindMeasurement)))
                cboWindMeasurement.Items.Add(value.GetDescription());

            windHeadingVM.windMeasurement = (WindMeasurement)Enum.Parse(typeof(WindMeasurement), config.ReadWithDefault(ConfigKey.WindMeasurement, WindMeasurement.TwoMinuteMean.ToString()));

            cboWindMeasurement.SelectedIndex = (int)windHeadingVM.windMeasurement;
        }

        private void cboWindMeasurement_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            windHeadingVM.windMeasurement = (WindMeasurement)cboWindMeasurement.SelectedIndex;
        }

        public void ResetWindDisplay()
        {
            // Sette Wind & Heading til å vise 2-min mean vind
            windHeadingVM.windMeasurement = WindMeasurement.TwoMinuteMean;
        }
    }
}
