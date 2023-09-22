using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for HelideckMotionTrendZoom.xaml
    /// </summary>
    public partial class HelideckMotionHistory_NOROG : UserControl
    {
        public HelideckMotionHistory_NOROG()
        {
            InitializeComponent();
        }

        public void Init(HelideckMotionTrendVM viewModel, LandingStatusTrendVM landingStatusTrendVM, Config config, RadTabItem tabHelideckMotionHistory)
        {
            // Context
            DataContext = viewModel;

            // Koble chart til data
            chartPitch20m.Series[0].ItemsSource = viewModel.pitch20mList;
            chartRoll20m.Series[0].ItemsSource = viewModel.roll20mList;
            chartInclination20m.Series[0].ItemsSource = viewModel.inclinationData20mList;
            chartHeaveHeight20m.Series[0].ItemsSource = viewModel.heaveHeightData20mList;
            chartSignificantHeaveRate20m.Series[0].ItemsSource = viewModel.significantHeaveRateData20mList;

            chartPitch3h.Series[0].ItemsSource = viewModel.pitch3hList;
            chartRoll3h.Series[0].ItemsSource = viewModel.rollData3hList;
            chartInclination3h.Series[0].ItemsSource = viewModel.inclinationData3hList;
            chartHeaveHeight3h.Series[0].ItemsSource = viewModel.heaveHeightData3hList;
            chartSignificantHeaveRate3h.Series[0].ItemsSource = viewModel.significantHeaveRateData3hList;

            DispatcherTimer timerUI = new DispatcherTimer();

            // Oppdatere resten av UI
            timerUI.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            timerUI.Tick += UpdateUI;
            timerUI.Start();

            void UpdateUI(object sender, EventArgs e)
            {
                if (tabHelideckMotionHistory.IsSelected)
                {
                    if (viewModel != null)
                    {
                        ///////////////////////////////////////////////////////////////////////////////////////////
                        // Helideck Motion: Max Pitch Up
                        ///////////////////////////////////////////////////////////////////////////////////////////                  
                        if (viewModel.pitchMaxUp20mData?.status == DataStatus.OK)
                        {
                            if (viewModel.pitchMaxUp20mData.limitStatus == LimitStatus.OK)
                            {
                                // Blank bakgrunn
                                gridMaxPitchUp20m.ClearValue(Panel.BackgroundProperty);
                                gridMaxPitchUp3h.ClearValue(Panel.BackgroundProperty);
                            }
                            else
                            {
                                // Rød bakgrunn
                                gridMaxPitchUp20m.Background = (Brush)FindResource("ColorRed");
                                gridMaxPitchUp3h.Background = (Brush)FindResource("ColorRed");
                            }
                        }
                        else
                        {
                            // Blank bakgrunn
                            gridMaxPitchUp20m.ClearValue(Panel.BackgroundProperty);
                            gridMaxPitchUp3h.ClearValue(Panel.BackgroundProperty);
                        }

                        ///////////////////////////////////////////////////////////////////////////////////////////
                        // Helideck Motion: Max Pitch Down
                        ///////////////////////////////////////////////////////////////////////////////////////////                  
                        if (viewModel.pitchMaxDown20mData?.status == DataStatus.OK)
                        {
                            if (viewModel.pitchMaxDown20mData.limitStatus == LimitStatus.OK)
                            {
                                // Blank bakgrunn
                                gridMaxPitchDown20m.ClearValue(Panel.BackgroundProperty);
                                gridMaxPitchDown3h.ClearValue(Panel.BackgroundProperty);
                            }
                            else
                            {
                                // Rød bakgrunn
                                gridMaxPitchDown20m.Background = (Brush)FindResource("ColorRed");
                                gridMaxPitchDown3h.Background = (Brush)FindResource("ColorRed");
                            }
                        }
                        else
                        {
                            // Blank bakgrunn
                            gridMaxPitchDown20m.ClearValue(Panel.BackgroundProperty);
                            gridMaxPitchDown3h.ClearValue(Panel.BackgroundProperty);
                        }

                        ///////////////////////////////////////////////////////////////////////////////////////////
                        // Helideck Motion: Max Roll Right
                        ///////////////////////////////////////////////////////////////////////////////////////////                  
                        if (viewModel.rollMaxRight20mData?.status == DataStatus.OK)
                        {
                            if (viewModel.rollMaxRight20mData.limitStatus == LimitStatus.OK)
                            {
                                // Blank bakgrunn
                                gridMaxRollRight20m.ClearValue(Panel.BackgroundProperty);
                                gridMaxRollRight3h.ClearValue(Panel.BackgroundProperty);
                            }
                            else
                            {
                                // Rød bakgrunn
                                gridMaxRollRight20m.Background = (Brush)FindResource("ColorRed");
                                gridMaxRollRight3h.Background = (Brush)FindResource("ColorRed");
                            }
                        }
                        else
                        {
                            // Blank bakgrunn
                            gridMaxRollRight20m.ClearValue(Panel.BackgroundProperty);
                            gridMaxRollRight3h.ClearValue(Panel.BackgroundProperty);
                        }

                        ///////////////////////////////////////////////////////////////////////////////////////////
                        // Helideck Motion: Max Roll Left
                        ///////////////////////////////////////////////////////////////////////////////////////////                  
                        if (viewModel.rollMaxLeft20mData?.status == DataStatus.OK)
                        {
                            if (viewModel.rollMaxLeft20mData.limitStatus == LimitStatus.OK)
                            {
                                // Blank bakgrunn
                                gridMaxRollLeft20m.ClearValue(Panel.BackgroundProperty);
                                gridMaxRollLeft3h.ClearValue(Panel.BackgroundProperty);
                            }
                            else
                            {
                                // Rød bakgrunn
                                gridMaxRollLeft20m.Background = (Brush)FindResource("ColorRed");
                                gridMaxRollLeft3h.Background = (Brush)FindResource("ColorRed");
                            }
                        }
                        else
                        {
                            // Blank bakgrunn
                            gridMaxRollLeft20m.ClearValue(Panel.BackgroundProperty);
                            gridMaxRollLeft3h.ClearValue(Panel.BackgroundProperty);
                        }

                        ///////////////////////////////////////////////////////////////////////////////////////////
                        // Helideck Motion: Inclination
                        ///////////////////////////////////////////////////////////////////////////////////////////                  
                        if (viewModel.inclinationMax20mData?.status == DataStatus.OK)
                        {
                            if (viewModel.inclinationMax20mData.limitStatus == LimitStatus.OK)
                            {
                                // Blank bakgrunn
                                gridMaxInclination20m.ClearValue(Panel.BackgroundProperty);
                                gridMaxInclination3h.ClearValue(Panel.BackgroundProperty);
                            }
                            else
                            {
                                // Rød bakgrunn
                                gridMaxInclination20m.Background = (Brush)FindResource("ColorRed");
                                gridMaxInclination3h.Background = (Brush)FindResource("ColorRed");
                            }
                        }
                        else
                        {
                            // Blank bakgrunn
                            gridMaxInclination20m.ClearValue(Panel.BackgroundProperty);
                            gridMaxInclination3h.ClearValue(Panel.BackgroundProperty);
                        }

                        ///////////////////////////////////////////////////////////////////////////////////////////
                        // Helideck Motion: Heave Height
                        ///////////////////////////////////////////////////////////////////////////////////////////                  
                        if (viewModel.heaveHeightMax20mData?.status == DataStatus.OK)
                        {
                            if (viewModel.heaveHeightMax20mData.limitStatus == LimitStatus.OK)
                            {
                                // Blank bakgrunn
                                gridMaxHeaveHeight20m.ClearValue(Panel.BackgroundProperty);
                                gridMaxHeaveHeight3h.ClearValue(Panel.BackgroundProperty);
                            }
                            else
                            {
                                // Rød bakgrunn
                                gridMaxHeaveHeight20m.Background = (Brush)FindResource("ColorRed");
                                gridMaxHeaveHeight3h.Background = (Brush)FindResource("ColorRed");
                            }
                        }
                        else
                        {
                            // Blank bakgrunn
                            gridMaxHeaveHeight20m.ClearValue(Panel.BackgroundProperty);
                            gridMaxHeaveHeight3h.ClearValue(Panel.BackgroundProperty);
                        }

                        ///////////////////////////////////////////////////////////////////////////////////////////
                        // Helideck Motion: SHR
                        ///////////////////////////////////////////////////////////////////////////////////////////                  
                        if (viewModel.significantHeaveRateData?.status == DataStatus.OK)
                        {
                            if (viewModel.significantHeaveRateData.limitStatus == LimitStatus.OK)
                            {
                                // Blank bakgrunn
                                gridSHR20m.ClearValue(Panel.BackgroundProperty);
                                gridSHR3h.ClearValue(Panel.BackgroundProperty);
                            }
                            else
                            {
                                // Rød bakgrunn
                                gridSHR20m.Background = (Brush)FindResource("ColorRed");
                                gridSHR3h.Background = (Brush)FindResource("ColorRed");
                            }
                        }
                        else
                        {
                            // Blank bakgrunn
                            gridSHR20m.ClearValue(Panel.BackgroundProperty);
                            gridSHR3h.ClearValue(Panel.BackgroundProperty);
                        }
                    }
                }
            }
        }
    }
}
