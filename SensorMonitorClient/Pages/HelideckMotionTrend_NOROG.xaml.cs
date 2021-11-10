using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Telerik.Windows.Controls.ChartView;
using Telerik.Windows.Data;

namespace SensorMonitorClient
{
    /// <summary>
    /// Interaction logic for HelideckMotionTrend_NOROG.xaml
    /// </summary>
    public partial class HelideckMotionTrend_NOROG : UserControl
    {
        // Configuration settings
        private Config config;

        // View Model
        public HelideckMotionTrendVM viewModel;

        public HelideckMotionTrend_NOROG()
        {
            InitializeComponent();
        }

        public void Init(HelideckMotionTrendVM viewModel, Config config)
        {
            this.viewModel = viewModel;
            this.config = config;

            DataContext = viewModel;

            // Init UI
            InitUI();
        }

        private void InitUI()
        {
            // Koble chart til data
            chartPitch20m.Series[0].ItemsSource = viewModel.pitchData20mList;
            chartRoll20m.Series[0].ItemsSource = viewModel.rollData20mList;
            chartInclination20m.Series[0].ItemsSource = viewModel.inclinationData20mList;
            chartHeaveAmplitude20m.Series[0].ItemsSource = viewModel.heaveAmplitudeData20mList;
            chartSignificantHeaveRate20m.Series[0].ItemsSource = viewModel.significantHeaveRateData20mList;

            chartPitch3h.Series[0].ItemsSource = viewModel.pitchData3hList;
            chartRoll3h.Series[0].ItemsSource = viewModel.rollData3hList;
            chartInclination3h.Series[0].ItemsSource = viewModel.inclinationData3hList;
            chartHeaveAmplitude3h.Series[0].ItemsSource = viewModel.heaveAmplitudeData3hList;
            chartSignificantHeaveRate3h.Series[0].ItemsSource = viewModel.significantHeaveRateData3hList;
        }
    }
}
