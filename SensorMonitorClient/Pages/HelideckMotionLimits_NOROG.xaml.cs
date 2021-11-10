using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace SensorMonitorClient
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

            //if (!viewModel.IsWithinLimits(ValueType.SignificantHeaveRate))
            //    SHRIsWithinLimits = false;

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
                    // Lese data timeout fra config
                    double dataTimeout = config.Read(ConfigKey.DataTimeout, Constants.DataTimeoutDefault);

                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Helideck Motion: Max Pitch
                    ///////////////////////////////////////////////////////////////////////////////////////////                  
                    if (viewModel.pitchMax20mData?.dataStatus == DataStatus.OK)
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
                        // Oransje bakgrunn
                        gridMaxPitch.Background = (Brush)FindResource("ColorAmber");
                    }

                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Helideck Motion: Roll Max
                    ///////////////////////////////////////////////////////////////////////////////////////////
                    if (viewModel.rollMax20mData?.dataStatus == DataStatus.OK)
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
                        // Oransje bakgrunn
                        gridMaxRoll.Background = (Brush)FindResource("ColorAmber");
                    }

                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Helideck Motion: Max Inclination
                    ///////////////////////////////////////////////////////////////////////////////////////////
                    if (viewModel.inclinationMax20mData?.dataStatus == DataStatus.OK)
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
                        // Oransje bakgrunn
                        gridMaxInclination.Background = (Brush)FindResource("ColorAmber");
                    }

                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Helideck Motion: Max Heave Amplitude
                    ///////////////////////////////////////////////////////////////////////////////////////////
                    if (viewModel.heaveAmplitudeMax20mData?.dataStatus == DataStatus.OK)
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
                        // Oransje bakgrunn
                        gridHeaveAmplitude.Background = (Brush)FindResource("ColorAmber");
                    }

                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Helideck Motion: Significant Heave Rate
                    ///////////////////////////////////////////////////////////////////////////////////////////
                    if (viewModel.significantHeaveRateData?.dataStatus == DataStatus.OK)
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
                        // Oransje bakgrunn
                        gridSignificantHeaveRate.Background = (Brush)FindResource("ColorAmber");
                    }

                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Helideck Motion: Heave Period
                    ///////////////////////////////////////////////////////////////////////////////////////////
                    if (viewModel.heavePeriodData?.dataStatus == DataStatus.OK)
                    {
                        // Blank bakgrunn
                        gridHeavePeriod.ClearValue(Grid.BackgroundProperty);
                    }
                    else
                    {
                        // Oransje bakgrunn
                        gridHeavePeriod.Background = (Brush)FindResource("ColorAmber");
                    }
                }
            }
        }
    }
}
