using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for HelideckMotionLimits_NOROG.xaml
    /// </summary>
    public partial class HelideckMotionLimits_NOROG : UserControl
    {
        // Configuration settings
        private Config config;

        // User Input Data
        private HelideckMotionLimitsVM viewModel;

        public HelideckMotionLimits_NOROG()
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
            timerUI.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUpdateFrequencyUIDefault));
            timerUI.Tick += UpdateUI;
            timerUI.Start();

            void UpdateUI(object sender, EventArgs e)
            {
                if (viewModel != null &&
                    tabHelicopterOps.IsSelected)
                {
                    // Lese data timeout fra config
                    double dataTimeout = config.ReadWithDefault(ConfigKey.DataTimeout, Constants.DataTimeoutDefault);

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
                            gridMaxPitch.Background = (Brush)FindResource("ColorRed");
                    }
                    else
                    {
                        // Grå bakgrunn
                        gridMaxPitch.Background = (Brush)FindResource("ColorNABackground");
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
                            gridMaxRoll.Background = (Brush)FindResource("ColorRed");
                    }
                    else
                    {
                        // Grå bakgrunn
                        gridMaxRoll.Background = (Brush)FindResource("ColorNABackground");
                    }

                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Helideck Motion: Max Inclination
                    ///////////////////////////////////////////////////////////////////////////////////////////
                    if (viewModel.inclinationMax20mData?.status == DataStatus.OK)
                    {
                        if (viewModel.inclinationMax20mData.limitStatus == LimitStatus.OK)
                            // Blank bakgrunn
                            gridMaxInclination.Background = (Brush)FindResource("ColorBackgroundSeparator");
                        else
                            // Rød bakgrunn
                            gridMaxInclination.Background = (Brush)FindResource("ColorRed");
                    }
                    else
                    {
                        // Grå bakgrunn
                        gridMaxInclination.Background = (Brush)FindResource("ColorNABackground");
                    }

                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Helideck Motion: Max Heave Amplitude
                    ///////////////////////////////////////////////////////////////////////////////////////////
                    if (viewModel.heaveAmplitudeMax20mData?.status == DataStatus.OK)
                    {
                        if (viewModel.heaveAmplitudeMax20mData.limitStatus == LimitStatus.OK)
                            // Blank bakgrunn
                            gridHeaveAmplitude.ClearValue(Grid.BackgroundProperty);
                        else
                            // Rød bakgrunn
                            gridHeaveAmplitude.Background = (Brush)FindResource("ColorRed");
                    }
                    else
                    {
                        // Grå bakgrunn
                        gridHeaveAmplitude.Background = (Brush)FindResource("ColorNABackground");
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
                            gridSignificantHeaveRate.Background = (Brush)FindResource("ColorRed");
                    }
                    else
                    {
                        // Grå bakgrunn
                        gridSignificantHeaveRate.Background = (Brush)FindResource("ColorNABackground");
                    }

                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Helideck Motion: Heave Period
                    ///////////////////////////////////////////////////////////////////////////////////////////
                    if (viewModel.heavePeriodData?.status == DataStatus.OK)
                    {
                        // Blank bakgrunn
                        gridHeavePeriod.ClearValue(Grid.BackgroundProperty);
                    }
                    else
                    {
                        // Grå bakgrunn
                        gridHeavePeriod.Background = (Brush)FindResource("ColorNABackground");
                    }
                }
            }
        }
    }
}
