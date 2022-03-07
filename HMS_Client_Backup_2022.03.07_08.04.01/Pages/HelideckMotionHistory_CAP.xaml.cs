using System;
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
        public HelideckMotionHistory_CAP()
        {
            InitializeComponent();
        }

        public void Init(HelideckMotionTrendVM helideckMotionTrendVM, Config config, RadTabItem tabHelideckMotionHistory)
        {
            // Context
            DataContext = helideckMotionTrendVM;

            // Init UI
            // Koble pitch chart til pitch data
            chartPitch20m.Series[0].ItemsSource = helideckMotionTrendVM.pitchData20mList;
            chartRoll20m.Series[0].ItemsSource = helideckMotionTrendVM.rollData20mList;
            chartSignificantHeaveRate20m.Series[0].ItemsSource = helideckMotionTrendVM.significantHeaveRateData20mList;
            chartInclination20m.Series[0].ItemsSource = helideckMotionTrendVM.inclinationData20mList;

            chartPitch3h.Series[0].ItemsSource = helideckMotionTrendVM.pitchData3hList;
            chartRoll3h.Series[0].ItemsSource = helideckMotionTrendVM.rollData3hList;
            chartSignificantHeaveRate3h.Series[0].ItemsSource = helideckMotionTrendVM.significantHeaveRateData3hList;
            chartInclination3h.Series[0].ItemsSource = helideckMotionTrendVM.inclinationData3hList;


            DispatcherTimer timerUI = new DispatcherTimer();

            // Oppdatere resten av UI
            timerUI.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            timerUI.Tick += UpdateUI;
            timerUI.Start();

            void UpdateUI(object sender, EventArgs e)
            {
                if (helideckMotionTrendVM != null &&
                    tabHelideckMotionHistory.IsSelected)
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
