using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SensorMonitorClient
{
    /// <summary>
    /// Interaction logic for UserInputs_NOROG.xaml
    /// </summary>
    public partial class UserInputs_NOROG : UserControl
    {
        // Configuration settings
        public Config config;

        // User Inputs View Model
        public UserInputsVM viewModel;

        public UserInputs_NOROG()
        {
            InitializeComponent();
        }

        public void Init(UserInputsVM viewModel, Config config, AdminSettingsVM adminSettingsVM)
        {
            DataContext = viewModel;

            this.config = config;
            this.viewModel = viewModel;

            // Helicopter Type
            foreach (HelicopterType value in Enum.GetValues(typeof(HelicopterType)))
                cboHelicopterType.Items.Add(value.ToString());

            cboHelicopterType.SelectedIndex = (int)viewModel.helicopterType;

            // Day / Night
            foreach (DayNight value in Enum.GetValues(typeof(DayNight)))
                cboDayNight.Items.Add(value.ToString());

            cboDayNight.SelectedIndex = (int)viewModel.dayNight;

            if (viewModel.dayNight.ToString().CompareTo("Day") == 0)
            {
                imgDayNightIcon.Source = new BitmapImage(new Uri("../Icons/outline_wb_sunny_black_48dp.png", UriKind.Relative));
            }
            else
            {
                imgDayNightIcon.Source = new BitmapImage(new Uri("../Icons/outline_nightlight_black_48dp.png", UriKind.Relative));
            }

            // Input fra bruker tillates ikke i Observer mode
            if (!adminSettingsVM.clientIsMaster)
            {
                cboHelicopterType.IsEnabled = false;
                cboDayNight.IsEnabled = false;
            }
        }

        private void cboHelicopterType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            viewModel.helicopterType = (HelicopterType)cboHelicopterType.SelectedIndex;
        }

        private void cboDayNight_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            viewModel.dayNight = (DayNight)cboDayNight.SelectedIndex;

            switch (viewModel.dayNight)
            {
                case DayNight.Day:
                    imgDayNightIcon.Source = new BitmapImage(new Uri("../Icons/outline_wb_sunny_black_48dp.png", UriKind.Relative));
                    break;

                case DayNight.Night:
                    imgDayNightIcon.Source = new BitmapImage(new Uri("../Icons/outline_nightlight_black_48dp.png", UriKind.Relative));
                    break;
            }
        }
    }
}
