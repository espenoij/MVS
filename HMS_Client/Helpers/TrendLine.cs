﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HMS_Client
{
    static class TrendLine
    {
        public static void GenerateGridColumnDefinitions(Canvas canvas, double listMaxLength)
        {
            int width = (int)(canvas.Width / listMaxLength);

            for (int i = 0; i < listMaxLength; i++)
            {
                // Create the rectangle
                Rectangle rec = new Rectangle()
                {
                    Width = width,
                    Height = 10
                };

                // Add to a canvas for example
                canvas.Children.Add(rec);
                Canvas.SetTop(rec, 0);
                Canvas.SetLeft(rec, i * width);
            }
        }

        public static void UpdateTrendData(List<HelideckStatusType> statusTrendDispList, Canvas canvas, Application app)
        {
            Brush amberBrush = (Brush)app.FindResource("ColorAmber");
            Brush redBrush = (Brush)app.FindResource("ColorRed");
            Brush blueBrush = (Brush)app.FindResource("ColorBlue");
            Brush bakgroundBrush = (Brush)app.FindResource("ColorBackgroundSeparator");

            for (int i = 0; i < statusTrendDispList.Count; i++)
            {
                Rectangle rect = null;

                if (i < canvas.Children.Count)
                    rect = canvas.Children[i] as Rectangle;

                if (rect != null)
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
    }
}