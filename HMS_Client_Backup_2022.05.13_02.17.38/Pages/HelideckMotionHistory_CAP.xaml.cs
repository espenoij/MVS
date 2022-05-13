using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for HelideckMotionTrendZoom.xaml
    /// </summary>
    public partial class HelideckMotionHistory_CAP : UserControl
    {
        private HelideckMotionTrendVM helideckMotionTrendVM;

        public HelideckMotionHistory_CAP()
        {
            InitializeComponent();
        }

        public void Init(HelideckMotionTrendVM helideckMotionTrendVM, LandingStatusTrendVM landingStatusTrendVM, Config config, RadTabItem tabHelideckMotionHistory)
        {
            // Context
            DataContext = helideckMotionTrendVM;

            this.helideckMotionTrendVM = helideckMotionTrendVM;

            // Koble chart til data
            chartPitch20m.Series[0].ItemsSource = helideckMotionTrendVM.pitchMaxUp20mList;
            chartPitch20m.Series[1].ItemsSource = helideckMotionTrendVM.pitchMaxDown20mList;
            chartRoll20m.Series[0].ItemsSource = helideckMotionTrendVM.rollMaxLeft20mList;
            chartRoll20m.Series[1].ItemsSource = helideckMotionTrendVM.rollMaxRight20mList;
            
            chartSignificantHeaveRate20m.Series[0].ItemsSource = helideckMotionTrendVM.significantHeaveRateData20mList;
            chartInclination20m.Series[0].ItemsSource = helideckMotionTrendVM.inclinationData20mList;

            chartPitch3h.Series[0].ItemsSource = helideckMotionTrendVM.pitchMaxUp3hList;
            chartPitch3h.Series[1].ItemsSource = helideckMotionTrendVM.pitchMaxDown3hList;
            chartRoll3h.Series[0].ItemsSource = helideckMotionTrendVM.rollMaxLeft3hList;
            chartRoll3h.Series[1].ItemsSource = helideckMotionTrendVM.rollMaxRight3hList;

            chartSignificantHeaveRate3h.Series[0].ItemsSource = helideckMotionTrendVM.significantHeaveRateData3hList;
            chartInclination3h.Series[0].ItemsSource = helideckMotionTrendVM.inclinationData3hList;

            // Generere grid til landing status trend display
            TrendLine.GenerateGridColumnDefinitions(statusTrendGrid20m, Constants.landingTrendHistoryDisplayListMax);
            TrendLine.GenerateGridColumnDefinitions(statusTrendGrid3h, Constants.landingTrendHistoryDisplayListMax);

            DispatcherTimer timerUI = new DispatcherTimer();

            // Oppdatere resten av UI
            timerUI.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            timerUI.Tick += UpdateUI;
            timerUI.Start();

            void UpdateUI(object sender, EventArgs e)
            {
                if (tabHelideckMotionHistory.IsSelected)
                {
                    // Status string variabler
                    RadObservableCollectionEx<HelideckStatus> statusList;
                    string timeString;

                    // Overføre til display data liste og overføre trend data fra data liste til display liste
                    if (tab20Minutes.IsSelected)
                    {
                        GraphBuffer.TransferDisplayData(landingStatusTrendVM.statusTrend20mList, helideckMotionTrendVM.landingTrend20mDispList);
                        TrendLine.UpdateTrendData(helideckMotionTrendVM.landingTrend20mDispList, statusTrendGrid20m, Application.Current);

                        statusList = landingStatusTrendVM.statusTrend20mList;
                        timeString = "20-minute";
                    }
                    else
                    {
                        GraphBuffer.TransferDisplayData(landingStatusTrendVM.statusTrend3hList, helideckMotionTrendVM.statusTrend3hDispList);
                        TrendLine.UpdateTrendData(helideckMotionTrendVM.statusTrend3hDispList, statusTrendGrid3h, Application.Current);

                        statusList = landingStatusTrendVM.statusTrend3hList;
                        timeString = "3-hour";
                    }

                    // Status string
                    if (statusList.Count > 0)
                    {
                        helideckMotionTrendVM.landingStatusTimeString = string.Format("{0} Trend ({1} - {2} UTC)",
                            timeString,
                            statusList[0].timestamp.ToShortTimeString(),
                            statusList[statusList.Count - 1].timestamp.ToShortTimeString());
                    }
                    else
                    {
                        helideckMotionTrendVM.landingStatusTimeString = string.Format("{0} Trend (--:-- - --:-- UTC)", timeString);
                    }

                    if (helideckMotionTrendVM != null)
                    {
                        ///////////////////////////////////////////////////////////////////////////////////////////
                        // Helideck Motion: Max Pitch Up
                        ///////////////////////////////////////////////////////////////////////////////////////////                  
                        if (helideckMotionTrendVM.pitchMaxUp20mData?.status == DataStatus.OK)
                        {
                            if (helideckMotionTrendVM.pitchMaxUp20mData.limitStatus == LimitStatus.OK)
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
                        if (helideckMotionTrendVM.pitchMaxDown20mData?.status == DataStatus.OK)
                        {
                            if (helideckMotionTrendVM.pitchMaxDown20mData.limitStatus == LimitStatus.OK)
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
                        if (helideckMotionTrendVM.rollMaxRight20mData?.status == DataStatus.OK)
                        {
                            if (helideckMotionTrendVM.rollMaxRight20mData.limitStatus == LimitStatus.OK)
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
                        if (helideckMotionTrendVM.rollMaxLeft20mData?.status == DataStatus.OK)
                        {
                            if (helideckMotionTrendVM.rollMaxLeft20mData.limitStatus == LimitStatus.OK)
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
                        if (helideckMotionTrendVM.inclinationMax20mData?.status == DataStatus.OK)
                        {
                            if (helideckMotionTrendVM.inclinationMax20mData.limitStatus == LimitStatus.OK)
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
                        // Helideck Motion: SHR
                        ///////////////////////////////////////////////////////////////////////////////////////////                  
                        if (helideckMotionTrendVM.significantHeaveRateData?.status == DataStatus.OK)
                        {
                            if (helideckMotionTrendVM.significantHeaveRateData.limitStatus == LimitStatus.OK)
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
