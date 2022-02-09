using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for LandingStatusTrend_CAP.xaml
    /// </summary>
    public partial class LandingStatusTrend_CAP : UserControl
    {
        private DispatcherTimer TrendUpdateTimer20m = new DispatcherTimer();
        private DispatcherTimer TrendUpdateTimer3h = new DispatcherTimer();

        public LandingStatusTrend_CAP()
        {
            InitializeComponent();
        }

        public void Init(HelideckStatusTrendVM viewModel, Config config)
        {
            DataContext = viewModel;

            // Generere grid til status trend display
            GenerateGridColumnDefinitions(statusTrendGrid20m);
            GenerateGridColumnDefinitions(statusTrendGrid3h);

            // Oppdatere UI
            TrendUpdateTimer20m.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ChartDataUpdateFrequency20m, Constants.ChartUpdateFrequencyUI20mDefault));
            TrendUpdateTimer20m.Tick += TrendUpdate20m;
            TrendUpdateTimer20m.Start();

            void TrendUpdate20m(object sender, EventArgs e)
            {
                UpdateTrendData(viewModel.statusTrend20mDispList, statusTrendGrid20m);
            }

            // Oppdatere UI
            TrendUpdateTimer3h.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ChartDataUpdateFrequency3h, Constants.ChartUpdateFrequencyUI3hDefault));
            TrendUpdateTimer3h.Tick += TrendUpdate3h;
            TrendUpdateTimer3h.Start();

            void TrendUpdate3h(object sender, EventArgs e)
            {
                UpdateTrendData(viewModel.statusTrend3hDispList, statusTrendGrid3h);
            }
        }

        private void GenerateGridColumnDefinitions(Grid grid)
        {
            for (int i = 0; i < Constants.statusTrendDisplayListMax; i++)
            {
                // Ny kolonnedefinisjon
                ColumnDefinition newColumn = new ColumnDefinition();

                // Kolonne bredde
                newColumn.Width = new GridLength(1, GridUnitType.Star);

                // Legge til ny kolonnedefinisjon i grid
                grid.ColumnDefinitions.Add(newColumn);

                // Nytt rektangel som bakgrunn i en grid celle
                Rectangle rect = new Rectangle();

                // Grid posisjon
                Grid.SetColumn(rect, i);

                // Legge inn i grid
                grid.Children.Add(rect);
            }
        }

        private void UpdateTrendData(List<HelideckStatusType> statusTrendDispList, Grid grid)
        {
            var converter = new BrushConverter();
            var amberBrush = (Brush)this.FindResource("ColorAmber");
            var redBrush = (Brush)this.FindResource("ColorRed");
            var blueBrush = (Brush)this.FindResource("ColorBlue");

            for (int i = 0; i < Constants.statusTrendDisplayListMax; i++)
            {
                var rec = GetChildren(grid, 0, i) as Rectangle;

                if (i < statusTrendDispList.Count)
                {
                    switch (statusTrendDispList[i])
                    {
                        case HelideckStatusType.OFF:
                            rec.Fill = (Brush)FindResource("ColorBackgroundSeparator");
                            break;

                        case HelideckStatusType.BLUE:
                            rec.Fill = blueBrush;
                            break;

                        case HelideckStatusType.AMBER:
                            rec.Fill = amberBrush;
                            break;

                        case HelideckStatusType.RED:
                            rec.Fill = redBrush;
                            break;
                    }
                }
            }
        }

        private static UIElement GetChildren(Grid grid, int row, int column)
        {
            UIElement foundChild = null;

            foreach (UIElement child in grid.Children)
            {
                if (Grid.GetRow(child) == row && Grid.GetColumn(child) == column)
                {
                    foundChild = child;
                }
            }

            return foundChild;
        }

    }
}
