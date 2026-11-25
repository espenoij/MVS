using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace MVS.Views.Controls
{
    public sealed class LiveLineChart : FrameworkElement
    {
        public static readonly DependencyProperty ReferenceItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ReferenceItemsSource),
                typeof(ObservableCollectionEx<HMSData>),
                typeof(LiveLineChart),
                new FrameworkPropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty TestItemsSourceProperty =
            DependencyProperty.Register(
                nameof(TestItemsSource),
                typeof(ObservableCollectionEx<HMSData>),
                typeof(LiveLineChart),
                new FrameworkPropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty TimeMinimumProperty =
            DependencyProperty.Register(nameof(TimeMinimum), typeof(DateTime), typeof(LiveLineChart),
                new FrameworkPropertyMetadata(DateTime.MinValue, OnChartPropertyChanged));

        public static readonly DependencyProperty TimeMaximumProperty =
            DependencyProperty.Register(nameof(TimeMaximum), typeof(DateTime), typeof(LiveLineChart),
                new FrameworkPropertyMetadata(DateTime.MinValue, OnChartPropertyChanged));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(LiveLineChart),
                new FrameworkPropertyMetadata(-1d, OnChartPropertyChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(LiveLineChart),
                new FrameworkPropertyMetadata(1d, OnChartPropertyChanged));

        public static readonly DependencyProperty ReferenceStrokeProperty =
            DependencyProperty.Register(nameof(ReferenceStroke), typeof(Brush), typeof(LiveLineChart),
                new FrameworkPropertyMetadata(null, OnChartPropertyChanged));

        public static readonly DependencyProperty TestStrokeProperty =
            DependencyProperty.Register(nameof(TestStroke), typeof(Brush), typeof(LiveLineChart),
                new FrameworkPropertyMetadata(null, OnChartPropertyChanged));

        public static readonly DependencyProperty ShowAbsoluteAxisLabelsProperty =
            DependencyProperty.Register(nameof(ShowAbsoluteAxisLabels), typeof(bool), typeof(LiveLineChart),
                new FrameworkPropertyMetadata(true, OnChartPropertyChanged));

        public ObservableCollectionEx<HMSData> ReferenceItemsSource
        {
            get { return (ObservableCollectionEx<HMSData>)GetValue(ReferenceItemsSourceProperty); }
            set { SetValue(ReferenceItemsSourceProperty, value); }
        }

        public ObservableCollectionEx<HMSData> TestItemsSource
        {
            get { return (ObservableCollectionEx<HMSData>)GetValue(TestItemsSourceProperty); }
            set { SetValue(TestItemsSourceProperty, value); }
        }

        public DateTime TimeMinimum
        {
            get { return (DateTime)GetValue(TimeMinimumProperty); }
            set { SetValue(TimeMinimumProperty, value); }
        }

        public DateTime TimeMaximum
        {
            get { return (DateTime)GetValue(TimeMaximumProperty); }
            set { SetValue(TimeMaximumProperty, value); }
        }

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public Brush ReferenceStroke
        {
            get { return (Brush)GetValue(ReferenceStrokeProperty); }
            set { SetValue(ReferenceStrokeProperty, value); }
        }

        public Brush TestStroke
        {
            get { return (Brush)GetValue(TestStrokeProperty); }
            set { SetValue(TestStrokeProperty, value); }
        }

        public bool ShowAbsoluteAxisLabels
        {
            get { return (bool)GetValue(ShowAbsoluteAxisLabelsProperty); }
            set { SetValue(ShowAbsoluteAxisLabelsProperty, value); }
        }

        private static void OnItemsSourceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var chart = (LiveLineChart)dependencyObject;
            if (e.OldValue is INotifyCollectionChanged oldCollection)
                oldCollection.CollectionChanged -= chart.OnCollectionChanged;
            if (e.NewValue is INotifyCollectionChanged newCollection)
                newCollection.CollectionChanged += chart.OnCollectionChanged;
            chart.InvalidateVisual();
        }

        private static void OnChartPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((LiveLineChart)dependencyObject).InvalidateVisual();
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Dispatcher.CheckAccess())
                InvalidateVisual();
            else
                Dispatcher.BeginInvoke(new Action(InvalidateVisual), DispatcherPriority.Render);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var width = ActualWidth;
            var height = ActualHeight;
            if (width <= 44 || height <= 16)
                return;

            var plot = new Rect(42, 8, width - 52, height - 16);
            var minimum = Minimum;
            var maximum = Maximum;
            if (!IsFinite(minimum) || !IsFinite(maximum) || maximum <= minimum)
            {
                minimum = -1;
                maximum = 1;
            }

            var timeMinimum = TimeMinimum;
            var timeMaximum = TimeMaximum;
            if (timeMinimum == DateTime.MinValue || timeMaximum <= timeMinimum)
            {
                timeMaximum = DateTime.UtcNow;
                timeMinimum = timeMaximum.AddMinutes(-20);
            }

            var textBrush = SystemColors.ControlTextBrush;
            var gridPen = new Pen(new SolidColorBrush(Color.FromRgb(218, 222, 227)), 1);
            var zeroPen = new Pen(Brushes.Black, 2);
            gridPen.Freeze();
            zeroPen.Freeze();

            for (var index = 0; index <= 4; index++)
            {
                var value = minimum + (maximum - minimum) * index / 4d;
                var y = plot.Bottom - plot.Height * index / 4d;
                if (Math.Abs(value) < Math.Max(Math.Abs(maximum - minimum) * 0.000001, 0.000001))
                    drawingContext.DrawLine(zeroPen, new Point(plot.Left, y), new Point(plot.Right, y));
                else
                    drawingContext.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));

                var labelValue = ShowAbsoluteAxisLabels ? Math.Abs(value) : value;
                var label = labelValue.ToString("0.##", CultureInfo.CurrentCulture);
                var formattedText = CreateText(label, textBrush);
                drawingContext.DrawText(formattedText, new Point(36 - formattedText.Width, y - formattedText.Height / 2));
            }

            var timeSpan = timeMaximum - timeMinimum;
            for (var minute = 0; minute <= 20; minute += 5)
            {
                var x = plot.Left + plot.Width * minute / 20d;
                drawingContext.DrawLine(gridPen, new Point(x, plot.Top), new Point(x, plot.Bottom));
            }

            drawingContext.PushClip(new RectangleGeometry(plot));
            DrawSeries(drawingContext, ReferenceItemsSource, timeMinimum, timeSpan, minimum, maximum, plot, ReferenceStroke ?? GetSeriesBrush("ColorGraphLineRef", Color.FromRgb(26, 110, 191)));
            DrawSeries(drawingContext, TestItemsSource, timeMinimum, timeSpan, minimum, maximum, plot, TestStroke ?? GetSeriesBrush("ColorGraphLineTest", Color.FromRgb(224, 123, 16)));
            drawingContext.Pop();
        }

        private static void DrawSeries(
            DrawingContext drawingContext,
            ObservableCollectionEx<HMSData> items,
            DateTime timeMinimum,
            TimeSpan timeSpan,
            double minimum,
            double maximum,
            Rect plot,
            Brush brush)
        {
            if (items == null || items.Count == 0)
                return;

            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                var isFigureOpen = false;
                foreach (var item in items)
                {
                    if (item == null || !IsFinite(item.graphData) ||
                        item.timestamp < timeMinimum || item.timestamp > timeMinimum + timeSpan)
                    {
                        isFigureOpen = false;
                        continue;
                    }

                    var x = plot.Left + plot.Width * (item.timestamp - timeMinimum).Ticks / (double)timeSpan.Ticks;
                    var y = plot.Bottom - plot.Height * (item.graphData - minimum) / (maximum - minimum);
                    var point = new Point(x, y);
                    if (!isFigureOpen)
                    {
                        context.BeginFigure(point, false, false);
                        isFigureOpen = true;
                    }
                    else
                    {
                        context.LineTo(point, true, false);
                    }
                }
            }

            geometry.Freeze();
            var pen = new Pen(brush, 1.5);
            pen.Freeze();
            drawingContext.DrawGeometry(null, pen, geometry);
        }

        private FormattedText CreateText(string text, Brush brush)
        {
            return new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                10,
                brush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        private Brush GetSeriesBrush(string resourceKey, Color fallback)
        {
            return TryFindResource(resourceKey) as Brush ?? new SolidColorBrush(fallback);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
