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
    /// Interaction logic for HelideckWindHeadingTrend_CAP.xaml
    /// </summary>
    public partial class WindHeadingTrend_CAP : UserControl
    {
        // View Model
        public WindHeadingTrendVM viewModel;

        public WindHeadingTrend_CAP()
        {
            InitializeComponent();
        }

        public void Init(WindHeadingTrendVM viewModel)
        {
            this.viewModel = viewModel;

            DataContext = viewModel;

            // Init UI
            InitUI();
        }

        private void InitUI()
        {
            // Koble chart til data
            chartVesselHeading.Series[0].ItemsSource = viewModel.vesselHdg20mDataList;
            chartWindHeading.Series[0].ItemsSource = viewModel.windDir20mDataList;
        }
    }
}
