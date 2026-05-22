using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MVS
{
    /// <summary>
    /// Converts a <see cref="FitRmseQuality"/> value (or its string label) into
    /// a Brush for display next to the Fit RMSE readout. Returns a neutral
    /// grey for Unknown / unrecognised input.
    /// </summary>
    public class FitRmseQualityBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush GoodBrush       = MakeFrozen(0x2E, 0x7D, 0x32); // green
        private static readonly SolidColorBrush AcceptableBrush = MakeFrozen(0x55, 0x8B, 0x2F); // olive-green
        private static readonly SolidColorBrush MarginalBrush   = MakeFrozen(0xE6, 0x8A, 0x00); // amber
        private static readonly SolidColorBrush BadBrush        = MakeFrozen(0xC6, 0x28, 0x28); // red
        private static readonly SolidColorBrush UnknownBrush    = MakeFrozen(0x55, 0x55, 0x55); // grey

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            FitRmseQuality q = FitRmseQuality.Unknown;
            if (value is FitRmseQuality qe) q = qe;
            else if (value is string s)
            {
                if (!Enum.TryParse(s, out q)) q = FitRmseQuality.Unknown;
            }

            switch (q)
            {
                case FitRmseQuality.Good:       return GoodBrush;
                case FitRmseQuality.Acceptable: return AcceptableBrush;
                case FitRmseQuality.Marginal:   return MarginalBrush;
                case FitRmseQuality.Bad:        return BadBrush;
                default:                        return UnknownBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        private static SolidColorBrush MakeFrozen(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }
}
