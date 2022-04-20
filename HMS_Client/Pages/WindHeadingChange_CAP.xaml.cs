using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Telerik.Windows.Controls.ChartView;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for WindHeadingChange_CAP.xaml
    /// </summary>
    public partial class WindHeadingChange_CAP : UserControl
    {
        private DispatcherTimer trendUpdateTimer = new DispatcherTimer();

        public WindHeadingChange_CAP()
        {
            InitializeComponent();
        }

        public void Init(WindHeadingChangeVM windHeadingChangeVM, Config config)
        {
            DataContext = windHeadingChangeVM;

            // TEST
            DateTime lastDate = DateTime.UtcNow;
            double[] lastVal = new double[] { 25, 50, 45, 16, 45 };

            List<ChartDataObject> dataSouce = new List<ChartDataObject>();
            for (int i = 0; i < 5; ++i)
            {
                ChartDataObject obj = new ChartDataObject { Date = lastDate.AddMinutes(1), Value = lastVal[i] };
                dataSouce.Add(obj);
                lastDate = obj.Date;
            }

            LineSeries series = (LineSeries)this.chartVesselHeading.Series[0];
            series.CategoryBinding = new PropertyNameDataPointBinding() { PropertyName = "Date" };
            series.ValueBinding = new PropertyNameDataPointBinding() { PropertyName = "Value" };

            series.ItemsSource = dataSouce;
            // TEST


            // Koble chart til data
            //chartVesselHeading.Series[0].ItemsSource = windHeadingChangeVM.vesselHdg30mDataList;
            chartWindHeading.Series[0].ItemsSource = windHeadingChangeVM.windDir30mDataList;

            // Generere grid til status trend display
            TrendLine.GenerateGridColumnDefinitions(statusTrendCanvas, Constants.rwdTrendDisplayListMax);

            // Oppdatere UI
            trendUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            trendUpdateTimer.Tick += TrendUpdate;
            trendUpdateTimer.Start();

            void TrendUpdate(object sender, EventArgs e)
            {
                if (gbWindHeadingChange.Visibility == Visibility.Visible)
                {
                    TrendLine.UpdateTrendData(windHeadingChangeVM.rwdTrend30mDispList, statusTrendCanvas, Application.Current);
                }
            }
        }

        public class ChartDataObject
        {
            public DateTime Date
            {
                get;
                set;
            }
            public double Value
            {
                get;
                set;
            }
        }
    }
}
