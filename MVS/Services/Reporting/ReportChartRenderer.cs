using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using MVS.Models;

namespace MVS.Services.Reporting
{
    /// <summary>
    /// Renders the verification result charts to PNG byte arrays using GDI+
    /// (System.Drawing, available because the app targets net10.0-windows with
    /// WinForms enabled).
    ///
    /// Plain bitmaps are used instead of scraping the live Telerik charts so the
    /// graphics are deterministic, run without a visual tree, and can be embedded
    /// directly into the PDF (and unit-tested).
    /// </summary>
    public static class ReportChartRenderer
    {
        private static readonly CultureInfo Ci = CultureInfo.CurrentCulture;

        // Brand-neutral palette consistent with the on-screen review cards.
        private static readonly Color ColorBackground = Color.White;
        private static readonly Color ColorAxis = Color.FromArgb(120, 120, 120);
        private static readonly Color ColorGrid = Color.FromArgb(230, 230, 230);
        private static readonly Color ColorText = Color.FromArgb(40, 40, 40);
        private static readonly Color ColorReference = Color.FromArgb(0, 122, 204);   // blue
        private static readonly Color ColorVessel = Color.FromArgb(0, 153, 102);      // green
        private static readonly Color ColorDeviation = Color.FromArgb(216, 120, 0);   // amber

        /// <summary>
        /// Horizontal bar chart of the calculated mean deviation per axis (the
        /// result the application produces). Degrees and metres are drawn on
        /// separate scales because they are different units.
        /// </summary>
        public static byte[] RenderDeviationChart(VerificationReportModel model, int width = 900, int height = 320)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var rows = new[]
            {
                new BarRow("Pitch deviation", model.DevPitch?.Mean ?? 0, "\u00B0", ColorDeviation),
                new BarRow("Roll deviation", model.DevRoll?.Mean ?? 0, "\u00B0", ColorDeviation),
                new BarRow("Heave deviation", model.DevHeave?.Mean ?? 0, "m", ColorDeviation),
            };

            return RenderHorizontalBars("Calculated deviation (Vessel - Reference)", rows, width, height, signed: true);
        }

        /// <summary>
        /// Grouped horizontal bars comparing the reference and vessel mean for
        /// each axis, so the reader can see how closely the unit tracks the
        /// trusted baseline.
        /// </summary>
        public static byte[] RenderMeansChart(VerificationReportModel model, int width = 900, int height = 380)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var rows = new[]
            {
                new BarRow("Pitch - Reference", model.RefPitch?.Mean ?? 0, "\u00B0", ColorReference),
                new BarRow("Pitch - Vessel", model.TestPitch?.Mean ?? 0, "\u00B0", ColorVessel),
                new BarRow("Roll - Reference", model.RefRoll?.Mean ?? 0, "\u00B0", ColorReference),
                new BarRow("Roll - Vessel", model.TestRoll?.Mean ?? 0, "\u00B0", ColorVessel),
                new BarRow("Heave - Reference", model.RefHeave?.Mean ?? 0, "m", ColorReference),
                new BarRow("Heave - Vessel", model.TestHeave?.Mean ?? 0, "m", ColorVessel),
            };

            return RenderHorizontalBars("Reference vs Vessel mean", rows, width, height, signed: true);
        }

        private sealed class BarRow
        {
            public BarRow(string label, double value, string unit, Color color)
            {
                Label = label;
                Value = double.IsNaN(value) ? 0 : value;
                Unit = unit;
                Color = color;
            }

            public string Label { get; }
            public double Value { get; }
            public string Unit { get; }
            public Color Color { get; }
        }

        private static byte[] RenderHorizontalBars(string title, BarRow[] rows, int width, int height, bool signed)
        {
            using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.Clear(ColorBackground);

                using (var titleFont = new Font("Segoe UI", 13f, FontStyle.Bold))
                using (var labelFont = new Font("Segoe UI", 9.5f, FontStyle.Regular))
                using (var valueFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                using (var textBrush = new SolidBrush(ColorText))
                using (var axisPen = new Pen(ColorAxis, 1.2f))
                using (var gridPen = new Pen(ColorGrid, 1f))
                {
                    const int marginLeft = 150;
                    const int marginRight = 90;
                    const int marginTop = 46;
                    const int marginBottom = 20;

                    g.DrawString(title, titleFont, textBrush, 16, 12);

                    var plot = new RectangleF(
                        marginLeft,
                        marginTop,
                        width - marginLeft - marginRight,
                        height - marginTop - marginBottom);

                    // Symmetric scale so positive/negative bars share a zero line.
                    double maxAbs = 0;
                    foreach (var r in rows)
                        maxAbs = Math.Max(maxAbs, Math.Abs(r.Value));
                    if (maxAbs <= 0) maxAbs = 1; // avoid divide-by-zero on all-zero data
                    double niceMax = NiceCeiling(maxAbs);

                    float zeroX = signed
                        ? plot.Left + plot.Width / 2f
                        : plot.Left;
                    float fullWidth = signed ? plot.Width / 2f : plot.Width;

                    // Gridlines + zero axis.
                    DrawGridlines(g, gridPen, axisPen, textBrush, labelFont, plot, zeroX, fullWidth, niceMax, signed);

                    float rowHeight = plot.Height / rows.Length;
                    float barHeight = Math.Min(26f, rowHeight * 0.55f);

                    for (int i = 0; i < rows.Length; i++)
                    {
                        BarRow r = rows[i];
                        float centerY = plot.Top + rowHeight * i + rowHeight / 2f;

                        // Row label (right-aligned into the left margin).
                        var labelRect = new RectangleF(4, centerY - rowHeight / 2f, marginLeft - 10, rowHeight);
                        using (var sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
                            g.DrawString(r.Label, labelFont, textBrush, labelRect, sf);

                        float barLen = (float)(Math.Abs(r.Value) / niceMax) * fullWidth;
                        float barTop = centerY - barHeight / 2f;
                        float barX = r.Value >= 0 ? zeroX : zeroX - barLen;

                        using (var barBrush = new SolidBrush(r.Color))
                            g.FillRectangle(barBrush, barX, barTop, Math.Max(1f, barLen), barHeight);

                        // Value label at the far end of the bar.
                        string valueText = string.Format(Ci, "{0:+0.000;-0.000;0.000} {1}", r.Value, r.Unit);
                        float valueX = r.Value >= 0 ? barX + barLen + 6 : barX - 6;
                        using (var sf = new StringFormat
                        {
                            Alignment = r.Value >= 0 ? StringAlignment.Near : StringAlignment.Far,
                            LineAlignment = StringAlignment.Center
                        })
                        {
                            var valueRect = new RectangleF(valueX - 80, centerY - rowHeight / 2f, 160, rowHeight);
                            g.DrawString(valueText, valueFont, textBrush, valueRect, sf);
                        }
                    }
                }

                return ToPng(bmp);
            }
        }

        private static void DrawGridlines(Graphics g, Pen gridPen, Pen axisPen, Brush textBrush, Font font,
                                          RectangleF plot, float zeroX, float fullWidth, double niceMax, bool signed)
        {
            const int ticks = 4;
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near })
            {
                for (int t = 0; t <= ticks; t++)
                {
                    double frac = (double)t / ticks;
                    float dx = (float)(frac * fullWidth);

                    if (signed)
                    {
                        DrawTick(g, gridPen, textBrush, font, sf, plot, zeroX + dx, niceMax * frac);
                        if (t != 0)
                            DrawTick(g, gridPen, textBrush, font, sf, plot, zeroX - dx, -niceMax * frac);
                    }
                    else
                    {
                        DrawTick(g, gridPen, textBrush, font, sf, plot, plot.Left + dx, niceMax * frac);
                    }
                }
            }

            // Emphasised zero / baseline axis.
            g.DrawLine(axisPen, zeroX, plot.Top, zeroX, plot.Bottom);
        }

        private static void DrawTick(Graphics g, Pen gridPen, Brush textBrush, Font font, StringFormat sf,
                                     RectangleF plot, float x, double value)
        {
            g.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);
            string txt = value.ToString("0.###", Ci);
            g.DrawString(txt, font, textBrush, new RectangleF(x - 30, plot.Bottom + 2, 60, 16), sf);
        }

        /// <summary>Rounds a positive value up to a clean 1/2/5 * 10^n boundary.</summary>
        private static double NiceCeiling(double value)
        {
            if (value <= 0) return 1;
            double exp = Math.Floor(Math.Log10(value));
            double pow = Math.Pow(10, exp);
            double frac = value / pow;
            double niceFrac;
            if (frac <= 1) niceFrac = 1;
            else if (frac <= 2) niceFrac = 2;
            else if (frac <= 5) niceFrac = 5;
            else niceFrac = 10;
            return niceFrac * pow;
        }

        private static byte[] ToPng(Bitmap bmp)
        {
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }
    }
}
