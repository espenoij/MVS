using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for HelideckMotionLimits_Touchdown_CAP.xaml
    /// </summary>
    public partial class HelideckMotionLimits_Touchdown_CAP : UserControl
    {
        // Configuration settings
        private Config config;

        // User Input Data
        private HelideckMotionLimitsVM viewModel;

        public HelideckMotionLimits_Touchdown_CAP()
        {
            InitializeComponent();
        }

        public void Init(HelideckMotionLimitsVM viewModel, Config config, RadTabItem tabHelicopterOps)
        {
            this.viewModel = viewModel;
            this.config = config;

            DataContext = this.viewModel;

            InitUIUpdate(tabHelicopterOps);
        }

        private void InitUIUpdate(RadTabItem tabHelicopterOps)
        {
            DispatcherTimer timerUI = new DispatcherTimer();

            // Oppdatere resten av UI
            timerUI.Interval = TimeSpan.FromMilliseconds(config.Read(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUpdateFrequencyUIDefault));
            timerUI.Tick += UpdateUI;
            timerUI.Start();

            void UpdateUI(object sender, EventArgs e)
            {
                if (viewModel != null &&
                    tabHelicopterOps.IsSelected)
                {
                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Helideck Motion: Max Pitch
                    ///////////////////////////////////////////////////////////////////////////////////////////                  
                    if (viewModel.pitchMax20mData?.status == DataStatus.OK)
                    {
                        if (viewModel.pitchMax20mData.limitStatus == LimitStatus.OK)
                            // Blank bakgrunn
                            gridMaxPitch.Background = (Brush)FindResource("ColorBackgroundSeparator");
                        else
                            // Rød bakgrunn
                            gridMaxPitch.Background = (Brush)this.FindResource("ColorRed");
                    }
                    else
                    {
                        // Oransje bakgrunn
                        gridMaxPitch.Background = (Brush)this.FindResource("ColorAmber");
                    }

                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Helideck Motion: Roll Max
                    ///////////////////////////////////////////////////////////////////////////////////////////
                    if (viewModel.rollMax20mData?.status == DataStatus.OK)
                    {
                        if (viewModel.rollMax20mData.limitStatus == LimitStatus.OK)
                            // Blank bakgrunn
                            gridMaxRoll.ClearValue(Grid.BackgroundProperty);
                        else
                            // Rød bakgrunn
                            gridMaxRoll.Background = (Brush)this.FindResource("ColorRed");
                    }
                    else
                    {
                        // Oransje bakgrunn
                        gridMaxRoll.Background = (Brush)this.FindResource("ColorAmber");
                    }

                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Helideck Motion: Significant Heave Rate
                    ///////////////////////////////////////////////////////////////////////////////////////////
                    if (viewModel.significantHeaveRateData?.status == DataStatus.OK)
                    {
                        if (viewModel.significantHeaveRateData.limitStatus == LimitStatus.OK)
                            // Blank bakgrunn
                            gridSignificantHeaveRate.Background = (Brush)FindResource("ColorBackgroundSeparator");
                        else
                            // Rød bakgrunn
                            gridSignificantHeaveRate.Background = (Brush)this.FindResource("ColorRed");
                    }
                    else
                    {
                        // Oransje bakgrunn
                        gridSignificantHeaveRate.Background = (Brush)this.FindResource("ColorAmber");
                    }

                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Helideck Motion: Max Inclination
                    ///////////////////////////////////////////////////////////////////////////////////////////
                    if (viewModel.inclinationMax20mData?.status == DataStatus.OK)
                    {
                        if (viewModel.inclinationMax20mData.limitStatus == LimitStatus.OK)
                            // Blank bakgrunn
                            gridMaxInclination.ClearValue(Grid.BackgroundProperty);
                        else
                            // Rød bakgrunn
                            gridMaxInclination.Background = (Brush)this.FindResource("ColorRed");
                    }
                    else
                    {
                        // Oransje bakgrunn
                        gridMaxInclination.Background = (Brush)this.FindResource("ColorAmber");
                    }
                }
            }
        }
    }
}
