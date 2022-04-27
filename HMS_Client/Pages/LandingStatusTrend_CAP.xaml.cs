using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for LandingStatusTrend_CAP.xaml
    /// </summary>
    public partial class LandingStatusTrend_CAP : UserControl
    {
        private DispatcherTimer trendUpdateTimer = new DispatcherTimer();

        public List<HelideckStatusType> landingTrend20mDispList = new List<HelideckStatusType>();
        public List<HelideckStatusType> statusTrend3hDispList = new List<HelideckStatusType>();

        public LandingStatusTrend_CAP()
        {
            InitializeComponent();
        }

        public void Init(LandingStatusTrendVM landingStatusTrendVM, Config config)
        {
            DataContext = landingStatusTrendVM;

            for (int i = 0; i < Constants.landingTrendDisplayListMax; i++)
            {
                landingTrend20mDispList.Add(new HelideckStatusType());
                statusTrend3hDispList.Add(new HelideckStatusType());
            }

            // Generere grid til landing status trend display
            TrendLine.GenerateGridColumnDefinitions(statusTrendGrid20m, Constants.landingTrendDisplayListMax);
            TrendLine.GenerateGridColumnDefinitions(statusTrendGrid3h, Constants.landingTrendDisplayListMax);

            // Oppdatere UI
            trendUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            trendUpdateTimer.Tick += TrendUpdate;
            trendUpdateTimer.Start();

            void TrendUpdate(object sender, EventArgs e)
            {
                if (gLandingStatusTrend.Visibility == Visibility.Visible)
                {
                    // Overføre til display data liste og overføre trend data fra data liste til display liste
                    if (landingStatusTrendVM.selectedGraphTime == GraphTime.Minutes20)
                    {
                        GraphBuffer.TransferDisplayData(landingStatusTrendVM.statusTrend20mList, landingTrend20mDispList);
                        TrendLine.UpdateTrendData(landingTrend20mDispList, statusTrendGrid20m, Application.Current);
                    }
                    else
                    {
                        GraphBuffer.TransferDisplayData(landingStatusTrendVM.statusTrend3hList, statusTrend3hDispList);
                        TrendLine.UpdateTrendData(statusTrend3hDispList, statusTrendGrid3h, Application.Current);
                    }
                }
            }
        }
    }
}
