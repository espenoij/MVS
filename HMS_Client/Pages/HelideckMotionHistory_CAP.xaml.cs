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
        public List<HelideckStatusType> landingTrend20mDispList = new List<HelideckStatusType>();
        public List<HelideckStatusType> statusTrend3hDispList = new List<HelideckStatusType>();

        public HelideckMotionHistory_CAP()
        {
            InitializeComponent();
        }

        public void Init(HelideckMotionTrendVM helideckMotionTrendVM, LandingStatusTrendVM landingStatusTrendVM, Config config, RadTabItem tabHelideckMotionHistory)
        {
            // Context
            DataContext = helideckMotionTrendVM;

            // Init UI
            for (int i = 0; i < Constants.landingTrendHistoryDisplayListMax; i++)
            {
                landingTrend20mDispList.Add(new HelideckStatusType());
                statusTrend3hDispList.Add(new HelideckStatusType());
            }

            // Koble pitch chart til pitch data
            chartPitch20m.Series[0].ItemsSource = helideckMotionTrendVM.pitchData20mList;
            chartRoll20m.Series[0].ItemsSource = helideckMotionTrendVM.rollData20mList;
            chartSignificantHeaveRate20m.Series[0].ItemsSource = helideckMotionTrendVM.significantHeaveRateData20mList;
            chartInclination20m.Series[0].ItemsSource = helideckMotionTrendVM.inclinationData20mList;

            chartPitch3h.Series[0].ItemsSource = helideckMotionTrendVM.pitchData3hList;
            chartRoll3h.Series[0].ItemsSource = helideckMotionTrendVM.rollData3hList;
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
                    // Overføre til display data liste og overføre trend data fra data liste til display liste
                    if (tab20Minutes.IsSelected)
                    {
                        GraphBuffer.TransferDisplayData(landingStatusTrendVM.statusTrend20mList, landingTrend20mDispList);
                        TrendLine.UpdateTrendData(landingTrend20mDispList, statusTrendGrid20m, Application.Current);
                    }
                    else
                    if (tab3Hours.IsSelected)
                    {
                        GraphBuffer.TransferDisplayData(landingStatusTrendVM.statusTrend3hList, statusTrend3hDispList);
                        TrendLine.UpdateTrendData(statusTrend3hDispList, statusTrendGrid3h, Application.Current);
                    }

                    if (helideckMotionTrendVM != null)
                    {
                        ///////////////////////////////////////////////////////////////////////////////////////////
                        // Helideck Motion: Max Pitch Up
                        ///////////////////////////////////////////////////////////////////////////////////////////                  
                        if (helideckMotionTrendVM.pitchMaxUp20mData?.status == DataStatus.OK)
                        {
                            if (helideckMotionTrendVM.pitchMaxUp20mData.limitStatus == LimitStatus.OK)
                                // Blank bakgrunn
                                gridMaxPitchUp.ClearValue(Panel.BackgroundProperty);
                            else
                                // Rød bakgrunn
                                gridMaxPitchUp.Background = (Brush)FindResource("ColorRed");
                        }
                        else
                        {
                            // Blank bakgrunn
                            gridMaxPitchUp.ClearValue(Panel.BackgroundProperty);
                        }

                        ///////////////////////////////////////////////////////////////////////////////////////////
                        // Helideck Motion: Max Pitch Down
                        ///////////////////////////////////////////////////////////////////////////////////////////                  
                        if (helideckMotionTrendVM.pitchMaxDown20mData?.status == DataStatus.OK)
                        {
                            if (helideckMotionTrendVM.pitchMaxDown20mData.limitStatus == LimitStatus.OK)
                                // Blank bakgrunn
                                gridMaxPitchDown.ClearValue(Panel.BackgroundProperty);
                            else
                                // Rød bakgrunn
                                gridMaxPitchDown.Background = (Brush)FindResource("ColorRed");
                        }
                        else
                        {
                            // Blank bakgrunn
                            gridMaxPitchDown.ClearValue(Panel.BackgroundProperty);
                        }

                        ///////////////////////////////////////////////////////////////////////////////////////////
                        // Helideck Motion: Max Roll Right
                        ///////////////////////////////////////////////////////////////////////////////////////////                  
                        if (helideckMotionTrendVM.rollMaxRight20mData?.status == DataStatus.OK)
                        {
                            if (helideckMotionTrendVM.rollMaxRight20mData.limitStatus == LimitStatus.OK)
                                // Blank bakgrunn
                                gridMaxRollRight.ClearValue(Panel.BackgroundProperty);
                            else
                                // Rød bakgrunn
                                gridMaxRollRight.Background = (Brush)FindResource("ColorRed");
                        }
                        else
                        {
                            // Blank bakgrunn
                            gridMaxRollRight.ClearValue(Panel.BackgroundProperty);
                        }

                        ///////////////////////////////////////////////////////////////////////////////////////////
                        // Helideck Motion: Max Roll Left
                        ///////////////////////////////////////////////////////////////////////////////////////////                  
                        if (helideckMotionTrendVM.rollMaxLeft20mData?.status == DataStatus.OK)
                        {
                            if (helideckMotionTrendVM.rollMaxLeft20mData.limitStatus == LimitStatus.OK)
                                // Blank bakgrunn
                                gridMaxRollLeft.ClearValue(Panel.BackgroundProperty);
                            else
                                // Rød bakgrunn
                                gridMaxRollLeft.Background = (Brush)FindResource("ColorRed");
                        }
                        else
                        {
                            // Blank bakgrunn
                            gridMaxRollLeft.ClearValue(Panel.BackgroundProperty);
                        }

                        ///////////////////////////////////////////////////////////////////////////////////////////
                        // Helideck Motion: Inclination
                        ///////////////////////////////////////////////////////////////////////////////////////////                  
                        if (helideckMotionTrendVM.inclinationMax20mData?.status == DataStatus.OK)
                        {
                            if (helideckMotionTrendVM.inclinationMax20mData.limitStatus == LimitStatus.OK)
                                // Blank bakgrunn
                                gridMaxInclination.ClearValue(Panel.BackgroundProperty);
                            else
                                // Rød bakgrunn
                                gridMaxInclination.Background = (Brush)FindResource("ColorRed");
                        }
                        else
                        {
                            // Blank bakgrunn
                            gridMaxInclination.ClearValue(Panel.BackgroundProperty);
                        }

                        ///////////////////////////////////////////////////////////////////////////////////////////
                        // Helideck Motion: SHR
                        ///////////////////////////////////////////////////////////////////////////////////////////                  
                        if (helideckMotionTrendVM.significantHeaveRateData?.status == DataStatus.OK)
                        {
                            if (helideckMotionTrendVM.significantHeaveRateData.limitStatus == LimitStatus.OK)
                                // Blank bakgrunn
                                gridSHR.ClearValue(Panel.BackgroundProperty);
                            else
                                // Rød bakgrunn
                                gridSHR.Background = (Brush)FindResource("ColorRed");
                        }
                        else
                        {
                            // Blank bakgrunn
                            gridSHR.ClearValue(Panel.BackgroundProperty);
                        }
                    }
                }
            }
        }
    }
}
