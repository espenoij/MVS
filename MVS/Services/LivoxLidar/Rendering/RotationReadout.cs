using System;
using System.Windows.Controls;

namespace MVS
{
    /// <summary>
    /// Adapter that updates three TextBlocks with a formatted rotation
    /// readout. Encapsulates the formatting and null-checks so the page
    /// code-behind only has to wire it once.
    /// </summary>
    internal class RotationReadout
    {
        private readonly TextBlock _x;
        private readonly TextBlock _y;
        private readonly TextBlock _z;

        internal RotationReadout(TextBlock x, TextBlock y, TextBlock z)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        internal void Update(double rotX, double rotY, double rotZ)
        {
            if (_x != null) _x.Text = Format(rotX);
            if (_y != null) _y.Text = Format(rotY);
            if (_z != null) _z.Text = Format(rotZ);
        }

        private static string Format(double deg) => $"{deg:+0.0;-0.0;0.0}°";
    }
}
