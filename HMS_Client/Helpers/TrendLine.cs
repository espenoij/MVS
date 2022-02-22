using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HMS_Client
{
    static class TrendLine
    {
        public static void GenerateGridColumnDefinitions(Grid grid, double listMaxLength)
        {
            for (int i = 0; i < listMaxLength; i++)
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

        public static void UpdateTrendData(List<HelideckStatusType> statusTrendDispList, double listLength, Grid grid, Application app)
        {
            Brush amberBrush = (Brush)app.FindResource("ColorAmber");
            Brush redBrush = (Brush)app.FindResource("ColorRed");
            Brush blueBrush = (Brush)app.FindResource("ColorBlue");
            Brush bakgroundBrush = (Brush)app.FindResource("ColorBackgroundSeparator");

            for (int i = 0; i < listLength; i++)
            {
                var rect = GetChildren(grid, 0, i) as Rectangle;

                if (i < statusTrendDispList.Count)
                {
                    switch (statusTrendDispList[i])
                    {
                        case HelideckStatusType.OFF:
                            rect.Fill = bakgroundBrush;
                            break;

                        case HelideckStatusType.BLUE:
                            rect.Fill = blueBrush;
                            break;

                        case HelideckStatusType.AMBER:
                            rect.Fill = amberBrush;
                            break;

                        case HelideckStatusType.RED:
                            rect.Fill = redBrush;
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
