using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for LandingStatusTrend_CAP.xaml
    /// </summary>
    public partial class LandingStatusTrend_CAP : UserControl
    {
        private DispatcherTimer trendUpdateTimer = new DispatcherTimer();

        public LandingStatusTrend_CAP()
        {
            InitializeComponent();
        }

        public void Init(RelativeWindLimitsVM relativeWindLimitsVM, Config config)
        {
            DataContext = relativeWindLimitsVM;

            // Generere grid til status trend display
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
                    // Overføre trend data fra data liste til display liste
                    if (relativeWindLimitsVM.visibilityItems20m)
                        TrendLine.UpdateTrendData(relativeWindLimitsVM.landingTrend20mDispList, Constants.landingTrendDisplayListMax, statusTrendGrid20m, Application.Current);
                    else
                        TrendLine.UpdateTrendData(relativeWindLimitsVM.statusTrend3hDispList, Constants.landingTrendDisplayListMax, statusTrendGrid3h, Application.Current);
                }
            }
        }
    }
}
