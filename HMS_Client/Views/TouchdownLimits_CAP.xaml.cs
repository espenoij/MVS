using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for TouchdownLimits_CAP.xaml
    /// </summary>
    public partial class TouchdownLimits_CAP : UserControl
    {
        // Configuration settings
        private Config config;

        // User Input Data
        private HelideckMotionLimitsVM helideckMotionLimitsVM;

        private RadTabControl tabControl;

        public TouchdownLimits_CAP()
        {
            InitializeComponent();
        }

        public void Init(HelideckMotionLimitsVM helideckMotionLimitsVM, Config config, RadTabItem tabHelicopterOps, RadTabControl tabControl)
        {
            this.helideckMotionLimitsVM = helideckMotionLimitsVM;
            this.config = config;
            this.tabControl = tabControl;

            DataContext = this.helideckMotionLimitsVM;

            InitUIUpdate(tabHelicopterOps);
        }

        private void InitUIUpdate(RadTabItem tabHelicopterOps)
        {
            DispatcherTimer timerUI = new DispatcherTimer();

            // Oppdatere resten av UI
            timerUI.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            timerUI.Tick += UpdateUI;
            timerUI.Start();

            void UpdateUI(object sender, EventArgs e)
            {
                if (helideckMotionLimitsVM != null &&
                    tabHelicopterOps.IsSelected)
                {
                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Helideck Motion: Max Pitch
                    ///////////////////////////////////////////////////////////////////////////////////////////                  
                    if (helideckMotionLimitsVM.pitchMax20mData?.status == DataStatus.OK)
                    {
                        if (helideckMotionLimitsVM.pitchMax20mData.limitStatus == LimitStatus.OK)
                            // Blank bakgrunn
                            gridMaxPitch.Background = (Brush)FindResource("ColorBackgroundSeparator");
                        else
                            // Rød bakgrunn
                            gridMaxPitch.Background = (Brush)this.FindResource("ColorRed");
                    }
                    else
                    {
                        // Blank bakgrunn
                        gridMaxPitch.Background = (Brush)FindResource("ColorBackgroundSeparator");
                    }

                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Helideck Motion: Roll Max
                    ///////////////////////////////////////////////////////////////////////////////////////////
                    if (helideckMotionLimitsVM.rollMax20mData?.status == DataStatus.OK)
                    {
                        if (helideckMotionLimitsVM.rollMax20mData.limitStatus == LimitStatus.OK)
                            // Blank bakgrunn
                            gridMaxRoll.ClearValue(Grid.BackgroundProperty);
                        else
                            // Rød bakgrunn
                            gridMaxRoll.Background = (Brush)this.FindResource("ColorRed");
                    }
                    else
                    {
                        // Blank bakgrunn
                        gridMaxRoll.ClearValue(Grid.BackgroundProperty);
                    }

                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Helideck Motion: Significant Heave Rate
                    ///////////////////////////////////////////////////////////////////////////////////////////
                    if (helideckMotionLimitsVM.significantHeaveRateData?.status == DataStatus.OK)
                    {
                        if (helideckMotionLimitsVM.significantHeaveRateData.limitStatus == LimitStatus.OK)
                            // Blank bakgrunn
                            gridSignificantHeaveRate.Background = (Brush)FindResource("ColorBackgroundSeparator");
                        else
                            // Rød bakgrunn
                            gridSignificantHeaveRate.Background = (Brush)this.FindResource("ColorRed");
                    }
                    else
                    {
                        // Blank bakgrunn
                        gridSignificantHeaveRate.Background = (Brush)FindResource("ColorBackgroundSeparator");
                    }

                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Helideck Motion: Max Inclination
                    ///////////////////////////////////////////////////////////////////////////////////////////
                    if (helideckMotionLimitsVM.inclinationMax20mData?.status == DataStatus.OK)
                    {
                        if (helideckMotionLimitsVM.inclinationMax20mData.limitStatus == LimitStatus.OK)
                            // Blank bakgrunn
                            gridMaxInclination.ClearValue(Grid.BackgroundProperty);
                        else
                            // Rød bakgrunn
                            gridMaxInclination.Background = (Brush)this.FindResource("ColorRed");
                    }
                    else
                    {
                        // Blank bakgrunn
                        gridMaxInclination.ClearValue(Grid.BackgroundProperty);
                    }
                }
            }
        }

        private void btnHelideckMotionHistory_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            tabControl.SelectedIndex = 1;
        }
    }
}
