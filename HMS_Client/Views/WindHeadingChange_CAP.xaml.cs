using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Telerik.Windows.Controls.ChartView;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for WindHeadingChange_CAP.xaml
    /// </summary>
    public partial class WindHeadingChange_CAP : UserControl
    {
        private DispatcherTimer trendUpdateTimer = new DispatcherTimer();

        public WindHeadingChange_CAP()
        {
            InitializeComponent();
        }

        public void Init(WindHeadingChangeVM windHeadingChangeVM, Config config)
        {
            DataContext = windHeadingChangeVM;

            // Koble chart til data
            chartVesselHeading.Series[0].ItemsSource = windHeadingChangeVM.vesselHdg30mDataList;
            chartWindHeading.Series[0].ItemsSource = windHeadingChangeVM.windDir30mDataList;

            // Generere grid til status trend display
            TrendLine.GenerateGridColumnDefinitions(statusTrendCanvas, Constants.rwdTrendDisplayListMax);

            // Oppdatere UI
            trendUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            trendUpdateTimer.Tick += TrendUpdate;
            trendUpdateTimer.Start();

            void TrendUpdate(object sender, EventArgs e)
            {
                if (gbWindHeadingChange.Visibility == Visibility.Visible)
                {
                    TrendLine.UpdateTrendData(windHeadingChangeVM.rwdTrend30mDispList, statusTrendCanvas, Application.Current);
                }
            }
        }
    }
}
