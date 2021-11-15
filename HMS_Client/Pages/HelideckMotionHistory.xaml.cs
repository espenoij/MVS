using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for HelideckMotionTrendZoom.xaml
    /// </summary>
    public partial class HelideckMotionHistory : UserControl
    {
        // View Model
        public HelideckMotionTrendVM helideckMotionTrendVM;

        public HelideckMotionHistory()
        {
            InitializeComponent();
        }

        public void Init(HelideckMotionTrendVM viewModel)
        {
            helideckMotionTrendVM = viewModel;

            // Context
            DataContext = this.helideckMotionTrendVM;

            // Init UI
            InitUI();
        }

        private void InitUI()
        {
            // Koble pitch chart til pitch data
            chartPitch20m.Series[0].ItemsSource = helideckMotionTrendVM.pitchData20mList;
            chartRoll20m.Series[0].ItemsSource = helideckMotionTrendVM.rollData20mList;
            chartSignificantHeaveRate20m.Series[0].ItemsSource = helideckMotionTrendVM.significantHeaveRateData20mList;
            chartInclination20m.Series[0].ItemsSource = helideckMotionTrendVM.inclinationData20mList;

            chartPitch3h.Series[0].ItemsSource = helideckMotionTrendVM.pitchData3hList;
            chartRoll3h.Series[0].ItemsSource = helideckMotionTrendVM.rollData3hList;
            chartSignificantHeaveRate3h.Series[0].ItemsSource = helideckMotionTrendVM.significantHeaveRateData3hList;
            chartInclination3h.Series[0].ItemsSource = helideckMotionTrendVM.inclinationData3hList;
        }
    }
}
