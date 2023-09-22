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
    public partial class HelideckMotionHistory_CAP : UserControl
    {
        public HelideckMotionHistory_CAP()
        {
            InitializeComponent();
        }

        public void Init(HelideckMotionTrendVM viewModel, LandingStatusTrendVM landingStatusTrendVM, Config config, RadTabItem tabHelideckMotionHistory)
        {
            // Context
            DataContext = viewModel;

            // Koble chart til data
            chartPitch20m.Series[0].ItemsSource = viewModel.pitchMaxUp20mList;
            chartPitch20m.Series[1].ItemsSource = viewModel.pitchMaxDown20mList;
            chartRoll20m.Series[0].ItemsSource = viewModel.rollMaxLeft20mList;
            chartRoll20m.Series[1].ItemsSource = viewModel.rollMaxRight20mList;
            
            chartSignificantHeaveRate20m.Series[0].ItemsSource = viewModel.significantHeaveRateData20mList;
            chartInclination20m.Series[0].ItemsSource = viewModel.inclinationData20mList;

            chartPitch3h.Series[0].ItemsSource = viewModel.pitchMaxUp3hList;
            chartPitch3h.Series[1].ItemsSource = viewModel.pitchMaxDown3hList;
            chartRoll3h.Series[0].ItemsSource = viewModel.rollMaxLeft3hList;
            chartRoll3h.Series[1].ItemsSource = viewModel.rollMaxRight3hList;

            chartSignificantHeaveRate3h.Series[0].ItemsSource = viewModel.significantHeaveRateData3hList;
            chartInclination3h.Series[0].ItemsSource = viewModel.inclinationData3hList;

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
                    RadObservableCollection<HelideckStatus> statusList;
                    string timeString;

                    // Sette status string: Finne data: Overføre til display data liste og overføre trend data fra data liste til display liste
                    if (tab20Minutes.IsSelected)
                    {
                        GraphBuffer.TransferDisplayData(landingStatusTrendVM.statusTrend20mList, viewModel.landingTrend20mDispList);
                        TrendLine.UpdateTrendData(viewModel.landingTrend20mDispList, statusTrendGrid20m, Application.Current);

                        statusList = landingStatusTrendVM.statusTrend20mList;
                        timeString = "20-minute";
                    }
                    else
                    {
                        GraphBuffer.TransferDisplayData(landingStatusTrendVM.statusTrend3hList, viewModel.statusTrend3hDispList);
                        TrendLine.UpdateTrendData(viewModel.statusTrend3hDispList, statusTrendGrid3h, Application.Current);

                        statusList = landingStatusTrendVM.statusTrend3hList;
                        timeString = "3-hour";
                    }

                    // Sette Status string: Sette tid
                    if (statusList.Count > 0)
                    {
                        viewModel.landingStatusTimeString = string.Format("{0} Trend ({1} - {2} UTC)",
                            timeString,
                            statusList[0].timestamp.ToString("HH:mm"),
                            statusList[statusList.Count - 1].timestamp.ToString("HH:mm"));
                    }
                    else
                    {
                        viewModel.landingStatusTimeString = string.Format("{0} Trend (--:-- - --:-- UTC)", timeString);
                    }

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
