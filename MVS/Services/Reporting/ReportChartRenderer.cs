using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using MVS.Models;
using MVS.Services;

namespace MVS.Services.Reporting
{
    /// <summary>
    /// Renders verification result charts and designed panels to PNG byte arrays
    /// using GDI+ (System.Drawing). Plain bitmaps are embedded directly into the
    /// PDF so rendering is deterministic and runs without a visual tree.
    ///
    /// Palette follows the modern MRU report design specification:
    ///   Primary (Deep Navy)   #0D1F3C â€” authoritative backgrounds, cover
    ///   Secondary (Steel Blue) #1B4F8A â€” section headers, card borders
    ///   Accent (Teal)          #00A8A8 â€” highlights, icons, accent bars
    ///   Success                #2DB87A â€” good / verified indicators
    ///   Warning (Amber)        #F5A623 â€” near-threshold, acceptable
    ///   Failure (Red)          #E03A3A â€” poor quality, attention needed
    ///   Reference MRU          #1A6EBF â€” steel blue (existing on-screen token)
    ///   Vessel MRU             #E07B10 â€” burnt orange (existing on-screen token)
    /// </summary>
    public static class ReportChartRenderer
    {
        private static readonly CultureInfo Ci = CultureInfo.CurrentCulture;

        // SES Energy Brand Toolkit — Primary palette
        private static readonly Color ColorPrimary      = Color.FromArgb(0x33, 0x4A, 0x5C); // SES Dark Blue Grey
        private static readonly Color ColorSecondary    = Color.FromArgb(0x26, 0x37, 0x46); // SES Dark Blue Grey deep
        private static readonly Color ColorAccent       = Color.FromArgb(0x3D, 0xE6, 0xA9); // SES Energy Green
        private static readonly Color ColorSuccess      = Color.FromArgb(0x3D, 0xE6, 0xA9); // SES Energy Green (success)
        private static readonly Color ColorWarning      = Color.FromArgb(0xF5, 0xA6, 0x23); // Amber (functional)
        private static readonly Color ColorFailure      = Color.FromArgb(0xE0, 0x3A, 0x3A); // Signal Red (functional)
        private static readonly Color ColorNeutralLight = Color.FromArgb(0xF2, 0xF5, 0xF7); // Light surface
        private static readonly Color ColorNeutralMid   = Color.FromArgb(0x7F, 0x94, 0xA5); // Mid-tone grey
        private static readonly Color ColorNeutralDark  = Color.FromArgb(0x1A, 0x27, 0x32); // Near-black text
        private static readonly Color ColorDivider      = Color.FromArgb(0xD6, 0xDF, 0xE6); // Subtle divider
        private static readonly Color ColorWhite        = Color.White;
        private static readonly Color ColorBlack        = Color.FromArgb(0x0A, 0x0A, 0x0A); // Near-black

        // Signal colors — exact match to SesColors.xaml graph line palette
        private static readonly Color ColorReference = Color.FromArgb(0x1A, 0x6E, 0xBF); // Steel blue   (#1A6EBF)
        private static readonly Color ColorVessel    = Color.FromArgb(0xE0, 0x7B, 0x10); // Burnt orange (#E07B10)
        private static readonly Color ColorDeviation = Color.FromArgb(0x80, 0x40, 0xB0); // Violet       (#8040B0)
        // â”€â”€ Chart background / grid â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private static readonly Color ColorBackground = ColorWhite;
        private static readonly Color ColorAxis       = Color.FromArgb(100, 120, 135);
        private static readonly Color ColorGrid       = Color.FromArgb(225, 232, 237);
        private static readonly Color ColorText       = ColorNeutralDark;

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // PUBLIC API
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

		/// <summary>
		/// Renders a premium full-page SES Energy branded cover panel.
		/// Dark Blue Grey gradient background, geometric motif, large typography,
		/// project metadata, and Energy Green accents.
		/// Width=900; Height=760 fills the A4 content area below the logo.
		/// </summary>
		public static byte[] RenderCoverBanner(VerificationReportModel model, int width = 900, int height = 760)
		{
			if (model == null) throw new ArgumentNullException(nameof(model));

			using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
			using (var g   = Graphics.FromImage(bmp))
			{
				g.SmoothingMode     = SmoothingMode.AntiAlias;
				g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

				// Dark Blue Grey gradient background
				using (var bgBrush = new LinearGradientBrush(
					new Rectangle(0, 0, width, height),
					ColorPrimary, ColorSecondary,
					LinearGradientMode.Vertical))
				{
					g.FillRectangle(bgBrush, 0, 0, width, height);
				}

				// Geometric offshore motif: concentric circles
				using (var motifPen = new Pen(Color.FromArgb(18, 255, 255, 255), 1.5f))
				{
					g.DrawEllipse(motifPen, width - 380, height - 280, 520, 520);
					g.DrawEllipse(motifPen, width - 310, height - 210, 400, 400);
					g.DrawEllipse(motifPen, width - 240, height - 140, 280, 280);
				}

				// Subtle diagonal grid
				using (var gridPen = new Pen(Color.FromArgb(8, 255, 255, 255), 1f))
				{
					for (int x = -height; x < width + height; x += 80)
						g.DrawLine(gridPen, x, 0, x + height, height);
				}

				// SES Energy Green accent bars
				using (var ab = new SolidBrush(ColorAccent))
				{
					g.FillRectangle(ab, 0, 0, width, 5);
					g.FillRectangle(ab, 0, 0, 5, height);
				}

				int lp = 32;

				// Brand label strip
				using (var tagFont = new Font("Segoe UI", 9f, FontStyle.Bold))
				using (var greenBr = new SolidBrush(ColorAccent))
				using (var sf      = new StringFormat())
				{
					sf.Alignment     = StringAlignment.Near;
					sf.LineAlignment = StringAlignment.Center;
					g.DrawString("SES ENERGY  |  MOTION VERIFICATION", tagFont, greenBr,
						new RectangleF(lp, 14, width - lp * 2, 22), sf);
				}

				// Large title typography
				using (var t1Font  = new Font("Segoe UI", 36f, FontStyle.Bold))
				using (var t2Font  = new Font("Segoe UI", 28f, FontStyle.Regular))
				using (var whiteBr = new SolidBrush(ColorWhite))
				{
					g.DrawString("MOTION REFERENCE UNIT", t1Font, whiteBr, lp, 66);
					g.DrawString("VERIFICATION REPORT",   t2Font, whiteBr, lp, 116);
				}

				// Thick Energy Green rule under title
				using (var rulePen = new Pen(ColorAccent, 3f))
					g.DrawLine(rulePen, lp, 166, lp + 480, 166);

				// Vessel / project hero line
				string heroLine = !string.IsNullOrWhiteSpace(model.VesselName)
					? model.VesselName.ToUpper()
					: (!string.IsNullOrWhiteSpace(model.ProjectName) ? model.ProjectName.ToUpper() : "\u2014");

				using (var heroFont = new Font("Segoe UI", 20f, FontStyle.Bold))
				using (var greenBr  = new SolidBrush(ColorAccent))
					g.DrawString(TruncateStr(heroLine, 50), heroFont, greenBr, lp, 184);

				// Metadata grid — 2 columns x 2 rows
				int metaY    = 252;
				int metaColW = (width - lp * 2) / 2;
				string[] mLabels = { "PROJECT", "VESSEL", "OPERATOR", "LOCATION" };
				string[] mValues =
				{
					string.IsNullOrWhiteSpace(model.ProjectName) ? "\u2014" : model.ProjectName,
					string.IsNullOrWhiteSpace(model.VesselName)  ? "\u2014" : model.VesselName,
					string.IsNullOrWhiteSpace(model.Operator)    ? "\u2014" : model.Operator,
					string.IsNullOrWhiteSpace(model.Location)    ? "\u2014" : model.Location,
				};

				using (var labelFont = new Font("Segoe UI", 8f, FontStyle.Bold))
				using (var valueFont = new Font("Segoe UI", 10.5f, FontStyle.Regular))
				using (var greenBr   = new SolidBrush(ColorAccent))
				using (var whiteBr   = new SolidBrush(ColorWhite))
				using (var divPen    = new Pen(Color.FromArgb(50, 255, 255, 255), 0.8f))
				{
					for (int i = 0; i < mLabels.Length; i++)
					{
						int col = i % 2;
						int row = i / 2;
						int cx  = lp + col * metaColW;
						int cy  = metaY + row * 64;
						g.DrawLine(divPen, cx, cy, cx + metaColW - 12, cy);
						g.DrawString(mLabels[i], labelFont, greenBr, cx, cy + 4);
						g.DrawString(TruncateStr(mValues[i], 40), valueFont, whiteBr, cx, cy + 20);
					}
				}

				// Timing row — 4 columns
				int row2Y = metaY + 2 * 64 + 16;
				string[] tLabels = { "CAPTURE START", "CAPTURE END", "DURATION", "GENERATED" };
				string[] tValues =
				{
					string.IsNullOrWhiteSpace(model.StartTime) ? "\u2014" : model.StartTime,
					string.IsNullOrWhiteSpace(model.EndTime)   ? "\u2014" : model.EndTime,
					string.IsNullOrWhiteSpace(model.Duration)  ? "\u2014" : model.Duration,
					model.GeneratedUtc.ToLocalTime().ToString("yyyy-MM-dd  HH:mm") + " UTC",
				};

				using (var labelFont = new Font("Segoe UI", 8f, FontStyle.Bold))
				using (var valueFont = new Font("Segoe UI", 10.5f, FontStyle.Regular))
				using (var greenBr   = new SolidBrush(ColorAccent))
				using (var whiteBr   = new SolidBrush(ColorWhite))
				using (var divPen    = new Pen(Color.FromArgb(50, 255, 255, 255), 0.8f))
				{
					int tw = (width - lp * 2) / 4;
					for (int i = 0; i < tLabels.Length; i++)
					{
						int cx = lp + i * tw;
						g.DrawLine(divPen, cx, row2Y, cx + tw - 8, row2Y);
						g.DrawString(tLabels[i], labelFont, greenBr, cx, row2Y + 4);
						g.DrawString(TruncateStr(tValues[i], 25), valueFont, whiteBr, cx, row2Y + 20);
					}
				}

				// Bottom classification band in Energy Green
				int bandH = 48;
				using (var bandBrush = new SolidBrush(ColorAccent))
					g.FillRectangle(bandBrush, 0, height - bandH, width, bandH);

				VerificationStatus vstatus   = OverallStatus(model);
				string             vstatusLbl = StatusBadgeLabel(vstatus);
				string statusDesc = model.HasData
					? string.Format("{0:N0} SAMPLES  \u00B7  CORRECTIONS {1}  \u00B7  {2}",
						model.SampleCount,
						model.HasCorrectionApplied ? "APPLIED" : "PENDING",
						vstatusLbl)
					: "NO DATA CAPTURED";

				using (var docFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
				using (var darkBr  = new SolidBrush(ColorSecondary))
				using (var sf      = new StringFormat())
				{
					sf.Alignment     = StringAlignment.Near;
					sf.LineAlignment = StringAlignment.Center;
					g.DrawString(statusDesc, docFont, darkBr,
						new RectangleF(lp, height - bandH, width - 190, bandH), sf);
				}

				DrawStatusBadge(g, model, width - 172, height - bandH - 10, 156, 58);

				return ToPng(bmp);
			}
		}

		/// <summary>
		/// Renders the executive dashboard panel: two rows of three KPI cards showing
		/// Verification Status, Samples Averaged, Capture Duration (row 1) and
		/// Maximum Outlier, Corrections Applied, Verification Confidence (row 2).
		/// </summary>
		/// <summary>
		/// Renders the executive dashboard panel: a single row of four large KPI cards giving
		/// an instant status read: Verification Status | Samples Averaged | Capture Duration |
		/// Corrections Applied. Data quality detail lives in the dedicated Data Quality section.
		/// </summary>
        /// <summary>
        /// Renders the Executive Dashboard: 6 KPI cards in a 2x3 grid.
        /// Row 1: Verification Status | Samples | Capture Duration
        /// Row 2: Max Outlier Rate | Corrections | Confidence Score
        /// Width=900; Height=300 for A4 content area.
        /// </summary>
        public static byte[] RenderExecutiveDashboard(VerificationReportModel model, int width = 900, int height = 420)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (var g   = Graphics.FromImage(bmp))
            {
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(ColorNeutralLight);

                DrawSectionHeader(g, "EXECUTIVE DASHBOARD", 0, 0, width, 38);

                const int cols  = 3;
                const int rows  = 3;
                int gap         = 10;
                int headerH     = 46;
                int rowH        = (height - headerH - gap * (rows + 1)) / rows;
                int cardW       = (width  - gap * (cols + 1)) / cols;

                // ── Derive card data (calculations unchanged) ────────────────────────────
                VerificationStatus vstatus = OverallStatus(model);
                Color  statusColor = StatusBadgeColor(vstatus);
                string statusValue = !model.HasData ? "NO DATA" :
                    vstatus == VerificationStatus.Good       ? "\u2713 VERIFIED" :
                    vstatus == VerificationStatus.Acceptable ? "ACCEPTABLE" : "\u26A0 ATTENTION";

                string samplesValue = model.HasData ? model.SampleCount.ToString("N0") : "\u2014";
                Color  samplesColor = model.HasData ? SamplesColor(model.SampleCount) : ColorNeutralMid;

                double durationMin = 0;
                if (!string.IsNullOrWhiteSpace(model.Duration) &&
                    TimeSpan.TryParse(model.Duration, out TimeSpan ts))
                    durationMin = ts.TotalMinutes;
                string durValue = durationMin > 0
                    ? string.Format(Ci, "{0:F1} MIN", durationMin) : "\u2014";
                Color durColor = durationMin >= VerificationAssessment.MinDurationRecommendedMinutes ? ColorSuccess
                              : durationMin >= VerificationAssessment.MinDurationAcceptableMinutes  ? ColorWarning
                              : durationMin > 0 ? ColorFailure : ColorNeutralMid;

                double worst        = model.WorstOutlierPercent;
                string outlierValue = (double.IsNaN(worst) || !model.HasData)
                    ? "\u2014" : string.Format(Ci, "{0:F1} %", worst);
                Color outlierColor  = (double.IsNaN(worst) || !model.HasData) ? ColorNeutralMid
                    : worst <= VerificationAssessment.OutlierAcceptablePercent ? ColorSuccess
                    : worst <= VerificationAssessment.OutlierAttentionPercent  ? ColorWarning : ColorFailure;

                string corrValue = model.HasCorrectionApplied ? "\u2713 APPLIED"
                                 : model.HasData ? "PENDING" : "\u2014";
                Color corrColor  = model.HasCorrectionApplied ? ColorSuccess
                                 : model.HasData ? ColorWarning : ColorNeutralMid;

                // Confidence score (algorithm unchanged)
                double sampleScore  = model.HasData ? Math.Min(1.0, (double)model.SampleCount / VerificationAssessment.MinSamplesGood) : 0;
                double outlierScore = (double.IsNaN(worst) || worst <= 0) ? 1.0 : Math.Max(0, 1.0 - worst / 10.0);
                double durScore     = Math.Min(1.0, durationMin / Math.Max(1, VerificationAssessment.MinDurationRecommendedMinutes));
                int    confScore    = model.HasData ? (int)Math.Round((sampleScore * 0.4 + durScore * 0.35 + outlierScore * 0.25) * 100) : 0;
                string confValue    = model.HasData ? confScore + " / 100" : "\u2014";
                Color  confColor    = confScore >= 80 ? ColorSuccess : confScore >= 60 ? ColorWarning : ColorFailure;

                // Correction values per axis
                double pitchCorr = model.RecommendedCorrection(VerificationAxisKind.Pitch);
                double rollCorr  = model.RecommendedCorrection(VerificationAxisKind.Roll);
                double heaveCorr = model.RecommendedCorrection(VerificationAxisKind.Heave);
                string pitchUnit = model.Unit(VerificationAxisKind.Pitch);
                string rollUnit  = model.Unit(VerificationAxisKind.Roll);
                string heaveUnit = model.Unit(VerificationAxisKind.Heave);

                string FmtCorr(double v, string unit) =>
                    (model.HasData && !double.IsNaN(v))
                        ? string.Format(Ci, "{0:+0.000;-0.000;0.000} {1}", v, unit)
                        : "\u2014";

                // Row 0: Status | Samples | Duration
                // Row 1: Outliers | Corrections | Confidence
                // Row 2: Pitch Correction | Roll Correction | Heave Correction
                var cards = new[]
                {
                    (row: 0, col: 0, label: "VERIFICATION STATUS", value: statusValue,          color: statusColor),
                    (row: 0, col: 1, label: "SAMPLES",             value: samplesValue,          color: samplesColor),
                    (row: 0, col: 2, label: "CAPTURE DURATION",    value: durValue,              color: durColor),
                    (row: 1, col: 0, label: "MAX OUTLIER RATE",    value: outlierValue,          color: outlierColor),
                    (row: 1, col: 1, label: "CORRECTIONS",         value: corrValue,             color: corrColor),
                    (row: 1, col: 2, label: "CONFIDENCE",          value: confValue,             color: confColor),
                    (row: 2, col: 0, label: "PITCH CORRECTION",    value: FmtCorr(pitchCorr, pitchUnit),  color: ColorAccent),
                    (row: 2, col: 1, label: "ROLL CORRECTION",     value: FmtCorr(rollCorr,  rollUnit),   color: ColorAccent),
                    (row: 2, col: 2, label: "HEAVE CORRECTION",    value: FmtCorr(heaveCorr, heaveUnit),  color: ColorAccent),
                };

                foreach (var card in cards)
                {
                    int cx = gap + card.col * (cardW + gap);
                    int cy = headerH + gap + card.row * (rowH + gap);

                    // Card body + border
                    using (var bg = new SolidBrush(ColorWhite))
                        g.FillRectangle(bg, cx, cy, cardW, rowH);
                    using (var borderPen = new Pen(ColorDivider, 1f))
                        g.DrawRectangle(borderPen, cx, cy, cardW - 1, rowH - 1);

                    // Thick colored top accent bar
                    using (var accentBr = new SolidBrush(card.color))
                        g.FillRectangle(accentBr, cx, cy, cardW, 8);

                    // Primary value — scaled font, slightly tighter than before
                    float fs = card.value.Length <= 5  ? 26f
                             : card.value.Length <= 9  ? 21f
                             : card.value.Length <= 13 ? 17f : 13f;
                    using (var valFont  = new Font("Segoe UI", fs, FontStyle.Bold))
                    using (var valBrush = new SolidBrush(ColorNeutralDark))
                    using (var sf       = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        g.DrawString(card.value, valFont, valBrush,
                            new RectangleF(cx + 4, cy + 10, cardW - 8, rowH - 30), sf);

                    // Label pinned to bottom — ALL CAPS, muted
                    using (var lFont  = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                    using (var lBrush = new SolidBrush(ColorNeutralMid))
                    using (var sf     = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        g.DrawString(card.label, lFont, lBrush,
                            new RectangleF(cx, cy + rowH - 20, cardW, 17), sf);
                }

                return ToPng(bmp);
            }
        }


		/// <summary>
		/// Renders a three-row horizontal bullet chart panel (Pitch, Roll, Heave)
		/// showing the deviation magnitude relative to the reference scale, with
		/// green / amber / red zone bands and a clear threshold marker.
		/// </summary>
        public static byte[] RenderBulletChartsPanel(VerificationReportModel model, int width = 900, int height = 210)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (var g   = Graphics.FromImage(bmp))
            {
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(ColorWhite);

                DrawSectionHeader(g, "DEVIATION vs. REFERENCE SCALE", 0, 0, width, 32);

                int y = 36;
                int rowH = (height - 40) / 3;

                var rows = new[]
                {
                    ("Pitch",  VerificationAssessment.Unit(VerificationAxisKind.Pitch),
                     model.DevPitch?.Mean  ?? double.NaN,
                     model.MagnitudeScale(VerificationAxisKind.Pitch)),
                    ("Roll",   VerificationAssessment.Unit(VerificationAxisKind.Roll),
                     model.DevRoll?.Mean   ?? double.NaN,
                     model.MagnitudeScale(VerificationAxisKind.Roll)),
                    ("Heave",  VerificationAssessment.Unit(VerificationAxisKind.Heave),
                     model.DevHeave?.Mean  ?? double.NaN,
                     model.MagnitudeScale(VerificationAxisKind.Heave)),
                };

                foreach (var (label, unit, value, scale) in rows)
                {
                    DrawBulletRow(g, 0, y, width, rowH, label, unit, value, scale);
                    y += rowH;
                }

                return ToPng(bmp);
            }
        }

        /// <summary>
        /// Horizontal bar chart of the calculated mean deviation per axis.
        /// </summary>
        public static byte[] RenderDeviationChart(VerificationReportModel model, int width = 900, int height = 320)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var rows = new[]
            {
                new BarRow("Pitch deviation",  model.DevPitch?.Mean  ?? 0, "\u00B0", ColorDeviation),
                new BarRow("Roll deviation",   model.DevRoll?.Mean   ?? 0, "\u00B0", ColorDeviation),
                new BarRow("Heave deviation",  model.DevHeave?.Mean  ?? 0, "m",      ColorDeviation),
            };

            return RenderHorizontalBars("Calculated deviation  (Vessel \u2212 Reference)", rows, width, height, signed: true);
        }

        /// <summary>
        /// Grouped horizontal bars comparing reference and vessel mean per axis.
        /// </summary>
        public static byte[] RenderMeansChart(VerificationReportModel model, int width = 900, int height = 380)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var rows = new[]
            {
                new BarRow("Pitch \u2014 Reference", model.RefPitch?.Mean  ?? 0, "\u00B0", ColorReference),
                new BarRow("Pitch \u2014 Vessel",    model.TestPitch?.Mean ?? 0, "\u00B0", ColorVessel),
                new BarRow("Roll \u2014 Reference",  model.RefRoll?.Mean   ?? 0, "\u00B0", ColorReference),
                new BarRow("Roll \u2014 Vessel",     model.TestRoll?.Mean  ?? 0, "\u00B0", ColorVessel),
                new BarRow("Heave \u2014 Reference", model.RefHeave?.Mean  ?? 0, "m",      ColorReference),
                new BarRow("Heave \u2014 Vessel",    model.TestHeave?.Mean ?? 0, "m",      ColorVessel),
            };

			return RenderHorizontalBars("Reference vs. Vessel mean", rows, width, height, signed: true);
		}

		/// <summary>
		/// Three premium hero cards showing the recommended correction value for each axis.
		/// Corrections are the dominant visual element (large type, dark card, accent bar).
		/// </summary>
		public static byte[] RenderCorrectionCards(VerificationReportModel model, int width = 900, int height = 340)
		{
			if (model == null) throw new ArgumentNullException(nameof(model));

			using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
			using (var g   = Graphics.FromImage(bmp))
			{
				g.SmoothingMode     = SmoothingMode.AntiAlias;
				g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
				g.Clear(ColorNeutralLight);

				DrawSectionHeader(g, "RECOMMENDED CORRECTIONS", 0, 0, width, 38);

				int gap   = 12;
				int cardW = (width - gap * 4) / 3;
				int cardY = 46;
				int cardH = height - cardY - 8;

				var axes = new[]
				{
					("PITCH",
					 model.RecommendedCorrection(VerificationAxisKind.Pitch),
					 model.AppliedCorrection(VerificationAxisKind.Pitch),
					 model.Unit(VerificationAxisKind.Pitch)),
					("ROLL",
					 model.RecommendedCorrection(VerificationAxisKind.Roll),
					 model.AppliedCorrection(VerificationAxisKind.Roll),
					 model.Unit(VerificationAxisKind.Roll)),
					("HEAVE",
					 model.RecommendedCorrection(VerificationAxisKind.Heave),
					 model.AppliedCorrection(VerificationAxisKind.Heave),
					 model.Unit(VerificationAxisKind.Heave)),
				};

				for (int i = 0; i < axes.Length; i++)
				{
					var (label, recommended, applied, unit) = axes[i];
					int cx = gap + i * (cardW + gap);

					// Dark card body
					using (var cardBg = new SolidBrush(ColorPrimary))
						g.FillRectangle(cardBg, cx, cardY, cardW, cardH);

					// Energy Green top accent bar
					using (var accentBrush = new SolidBrush(ColorAccent))
						g.FillRectangle(accentBrush, cx, cardY, cardW, 5);

					// Axis label
					using (var labelFont = new Font("Segoe UI", 10f, FontStyle.Bold))
					using (var greenBr   = new SolidBrush(ColorAccent))
					using (var sf        = new StringFormat { Alignment = StringAlignment.Center })
						g.DrawString(label + " CORRECTION", labelFont, greenBr,
							new RectangleF(cx, cardY + 12, cardW, 22), sf);

					// Hero value
					bool   hasData = model.HasData && !double.IsNaN(recommended);
					string valStr  = hasData
						? string.Format(Ci, "{0:+0.000;-0.000;0.000} {1}", recommended, unit)
						: "\u2014";

					using (var valFont = new Font("Segoe UI", 22f, FontStyle.Bold))
					using (var whiteBr = new SolidBrush(ColorWhite))
					using (var sf      = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
						g.DrawString(valStr, valFont, whiteBr,
							new RectangleF(cx + 4, cardY + 38, cardW - 8, cardH - 92), sf);

					// Divider
					using (var divPen = new Pen(Color.FromArgb(50, 255, 255, 255), 1f))
						g.DrawLine(divPen, cx + 16, cardY + cardH - 52, cx + cardW - 16, cardY + cardH - 52);

					// Status badge (tinted)
					string statusStr = model.HasCorrectionApplied ? "\u2713  APPLIED" : "PENDING";
					Color  statusCol = model.HasCorrectionApplied ? ColorAccent : ColorWarning;
					DrawTintedBadge(g, cx + 16, cardY + cardH - 46, cardW - 32, 26, statusCol, statusStr);

					// Applied-vs-recommended footnote
					if (model.HasCorrectionApplied && hasData && Math.Abs(applied - recommended) > 1e-6)
					{
						string appliedStr = string.Format(Ci, "Applied: {0:+0.000;-0.000;0.000} {1}", applied, unit);
						using (var aFont   = new Font("Segoe UI", 7.5f))
						using (var mutedBr = new SolidBrush(ColorNeutralMid))
						using (var sf      = new StringFormat { Alignment = StringAlignment.Center })
							g.DrawString(appliedStr, aFont, mutedBr,
								new RectangleF(cx, cardY + cardH - 18, cardW, 16), sf);
					}
				}

				return ToPng(bmp);
			}
		}

		private static void DrawPillBadge(Graphics g, int x, int y, int w, int h, string text, Color bg)
        {
            using (var brush = new SolidBrush(bg))
                g.FillRectangle(brush, x, y, w, h);
            using (var font      = new Font("Segoe UI", 7f, FontStyle.Bold))
            using (var textBrush = new SolidBrush(ContrastText(bg)))
            using (var sf        = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(text, font, textBrush, new RectangleF(x, y, w, h), sf);
        }

        /// <summary>
        /// Draws a tinted status badge with spec-compliant SES brand background/text colors.
        /// SUCCESS: #DFF8EE bg / #1E7A54 text  WARNING: #FFF3D6 bg / #B87C00 text
        /// FAIL: #FDE0E0 bg / #B42318 text
        /// </summary>
        private static void DrawTintedBadge(Graphics g, int x, int y, int w, int h, Color accentColor, string text)
        {
            double luma = (accentColor.R * 0.299 + accentColor.G * 0.587 + accentColor.B * 0.114) / 255.0;
            bool isGreen  = accentColor.G > 180 && accentColor.R < 100;
            bool isAmber  = accentColor.R > 200 && accentColor.G > 100 && accentColor.B < 80;
            bool isRed    = accentColor.R > 180 && accentColor.G < 80;

            Color bgColor, fgColor;
            if (isGreen)
            {
                bgColor = Color.FromArgb(0xDF, 0xF8, 0xEE);
                fgColor = Color.FromArgb(0x1E, 0x7A, 0x54);
            }
            else if (isAmber)
            {
                bgColor = Color.FromArgb(0xFF, 0xF3, 0xD6);
                fgColor = Color.FromArgb(0xB8, 0x7C, 0x00);
            }
            else if (isRed)
            {
                bgColor = Color.FromArgb(0xFD, 0xE0, 0xE0);
                fgColor = Color.FromArgb(0xB4, 0x23, 0x18);
            }
            else
            {
                bgColor = Color.FromArgb(0xF2, 0xF5, 0xF7);
                fgColor = ColorNeutralDark;
            }

            using (var bgBrush = new SolidBrush(bgColor))
                g.FillRectangle(bgBrush, x, y, w, h);
            using (var font    = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (var fgBrush = new SolidBrush(fgColor))
            using (var sf     = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(text, font, fgBrush, new RectangleF(x, y, w, h), sf);
        }

		/// <summary>
		/// Renders a session overview panel: four info cards showing Project, Vessel,
		/// Session timing, and Operator in a single row.
		/// </summary>
		public static byte[] RenderSessionOverviewPanel(VerificationReportModel model, int width = 900, int height = 180)
		{
			if (model == null) throw new ArgumentNullException(nameof(model));

			using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
			using (var g   = Graphics.FromImage(bmp))
			{
				g.SmoothingMode     = SmoothingMode.AntiAlias;
				g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
				g.Clear(ColorNeutralLight);

				DrawSectionHeader(g, "SESSION OVERVIEW", 0, 0, width, 34);

				int gap   = 8;
				int cardW = (width - gap * 5) / 4;
				int cardY = 42;
				int cardH = height - cardY - 6;

				string startEnd = (string.IsNullOrWhiteSpace(model.StartTime) ? "\u2014" : model.StartTime)
								+ "\n" +
								(string.IsNullOrWhiteSpace(model.EndTime) ? "\u2014" : model.EndTime);

				var cards = new[]
				{
					("PROJECT",  string.IsNullOrWhiteSpace(model.ProjectName) ? "\u2014" : model.ProjectName),
					("VESSEL",   string.IsNullOrWhiteSpace(model.VesselName)  ? "\u2014" : model.VesselName),
					("SESSION",  startEnd),
					("OPERATOR", string.IsNullOrWhiteSpace(model.Operator)    ? "\u2014" : model.Operator),
				};

				for (int i = 0; i < cards.Length; i++)
				{
					var (title, value) = cards[i];
					int cx = gap + i * (cardW + gap);

					using (var bg = new SolidBrush(ColorWhite))
						g.FillRectangle(bg, cx, cardY, cardW, cardH);
					using (var border = new Pen(ColorDivider, 1f))
						g.DrawRectangle(border, cx, cardY, cardW - 1, cardH - 1);
					using (var accent = new SolidBrush(ColorAccent))
						g.FillRectangle(accent, cx, cardY, 4, cardH);

					using (var tFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
					using (var tBrush = new SolidBrush(ColorNeutralMid))
						g.DrawString(title, tFont, tBrush, cx + 12, cardY + 8);

					using (var vFont = new Font("Segoe UI", 10f, FontStyle.Bold))
					using (var vBrush = new SolidBrush(ColorNeutralDark))
					using (var sf    = new StringFormat { LineAlignment = StringAlignment.Near })
						g.DrawString(value, vFont, vBrush,
							new RectangleF(cx + 12, cardY + 24, cardW - 20, cardH - 30), sf);
				}

				return ToPng(bmp);
			}
		}

		/// <summary>
		/// Renders compliance assessment scorecards: three criterion cards (Samples,
		/// Duration, Outliers) with visible pass/fail icons and target values, plus a
		/// prominent overall VERIFICATION QUALITY verdict card.
		/// </summary>
		public static byte[] RenderComplianceScorecards(VerificationReportModel model, int width = 900, int height = 200)
		{
			if (model == null) throw new ArgumentNullException(nameof(model));

			using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
			using (var g   = Graphics.FromImage(bmp))
			{
				g.SmoothingMode     = SmoothingMode.AntiAlias;
				g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
				g.Clear(ColorNeutralLight);

				DrawSectionHeader(g, "COMPLIANCE ASSESSMENT", 0, 0, width, 34);

				int gap   = 8;
				int cardW = (width - gap * 5) / 4;
				int cardY = 42;
				int cardH = height - cardY - 6;

				// ── Samples ───────────────────────────────────────────────────────────
				int    samples       = model.SampleCount;
				string samplesVal    = samples > 0 ? samples.ToString("N0") : "\u2014";
				string samplesTarget = string.Format("\u2265 {0:N0}", VerificationAssessment.MinSamplesGood);
				string samplesStatus = samples >= VerificationAssessment.MinSamplesGood       ? "GOOD" :
									   samples >= VerificationAssessment.MinSamplesAcceptable ? "ACCEPTABLE" :
									   samples > 0 ? "INSUFFICIENT" : "NO DATA";
				Color samplesColor   = samples >= VerificationAssessment.MinSamplesGood       ? ColorSuccess :
									   samples >= VerificationAssessment.MinSamplesAcceptable ? ColorWarning : ColorFailure;

				// ── Duration ──────────────────────────────────────────────────────────
				double actualMin = 0;
				if (!string.IsNullOrWhiteSpace(model.Duration) &&
					TimeSpan.TryParse(model.Duration, out TimeSpan ts))
					actualMin = ts.TotalMinutes;
				string durVal    = actualMin > 0 ? string.Format(Ci, "{0:F1} min", actualMin) : "\u2014";
				string durTarget = string.Format("\u2265 {0:F0} min", VerificationAssessment.MinDurationRecommendedMinutes);
				string durStatus = actualMin >= VerificationAssessment.MinDurationRecommendedMinutes ? "GOOD" :
								   actualMin >= VerificationAssessment.MinDurationAcceptableMinutes  ? "ACCEPTABLE" :
								   actualMin > 0 ? "INSUFFICIENT" : "NO DATA";
				Color durColor   = actualMin >= VerificationAssessment.MinDurationRecommendedMinutes ? ColorSuccess :
								   actualMin >= VerificationAssessment.MinDurationAcceptableMinutes  ? ColorWarning : ColorFailure;

				// ── Outliers ──────────────────────────────────────────────────────────
				double worst        = model.WorstOutlierPercent;
				string outlierVal   = double.IsNaN(worst) || !model.HasData ? "\u2014"
									: string.Format(Ci, "{0:F1} %", worst);
				string outlierTarget = string.Format("\u2264 {0:F0} %", VerificationAssessment.OutlierAcceptablePercent);
				string outlierStatus = double.IsNaN(worst) || !model.HasData ? "NO DATA" :
									   worst <= VerificationAssessment.OutlierAcceptablePercent ? "GOOD" :
									   worst <= VerificationAssessment.OutlierAttentionPercent  ? "ACCEPTABLE" : "TOO NOISY";
				Color outlierColor   = double.IsNaN(worst) || !model.HasData ? ColorNeutralMid :
									   worst <= VerificationAssessment.OutlierAcceptablePercent ? ColorSuccess :
									   worst <= VerificationAssessment.OutlierAttentionPercent  ? ColorWarning : ColorFailure;

				// ── Overall ───────────────────────────────────────────────────────────
				bool allGood      = samplesStatus == "GOOD" && durStatus == "GOOD" && outlierStatus == "GOOD";
				bool anyFail      = samplesStatus == "INSUFFICIENT" || durStatus == "INSUFFICIENT" || outlierStatus == "TOO NOISY";
				string overallVal = !model.HasData ? "NO DATA" : allGood ? "EXCELLENT" : anyFail ? "ATTENTION" : "ACCEPTABLE";
				Color overallColor = !model.HasData ? ColorNeutralMid : allGood ? ColorSuccess : anyFail ? ColorFailure : ColorWarning;

				var cards = new[]
				{
					("SAMPLES",   samplesVal,   samplesTarget,  samplesStatus,  samplesColor,  true),
					("DURATION",  durVal,       durTarget,      durStatus,      durColor,      true),
					("OUTLIERS",  outlierVal,   outlierTarget,  outlierStatus,  outlierColor,  true),
					("OVERALL",   overallVal,   string.Empty,   string.Empty,   overallColor,  false),
				};

				for (int i = 0; i < cards.Length; i++)
				{
					var (title, value, target, status, accentCol, showIcon) = cards[i];
					int cx = gap + i * (cardW + gap);
					bool isOverall = i == cards.Length - 1;

					// Card: Overall card gets solid accent background; criteria cards white
					if (isOverall)
					{
						using (var bg = new SolidBrush(accentCol))
							g.FillRectangle(bg, cx, cardY, cardW, cardH);
						using (var borderPen = new Pen(accentCol, 1f))
							g.DrawRectangle(borderPen, cx, cardY, cardW - 1, cardH - 1);
					}
					else
					{
						using (var bg = new SolidBrush(ColorWhite))
							g.FillRectangle(bg, cx, cardY, cardW, cardH);
						using (var borderPen = new Pen(ColorDivider, 1f))
							g.DrawRectangle(borderPen, cx, cardY, cardW - 1, cardH - 1);
						using (var accentBr = new SolidBrush(accentCol))
							g.FillRectangle(accentBr, cx, cardY, cardW, 6);
					}

					Color contrastOnCard = ContrastText(accentCol);
					Color textColor  = isOverall ? contrastOnCard : ColorNeutralDark;
					Color labelColor = isOverall ? Color.FromArgb(180, contrastOnCard) : ColorNeutralMid;

					// Pass/fail icon (top-right corner for criteria cards)
					if (showIcon && model.HasData)
					{
						string icon = (status == "GOOD" || status == "ACCEPTABLE") ? "\u2713" : "\u2717";
						using (var iconFont  = new Font("Segoe UI Symbol", 18f, FontStyle.Bold))
						using (var iconBrush = new SolidBrush(StatusTextOnWhite(accentCol)))
							g.DrawString(icon, iconFont, iconBrush,
								new RectangleF(cx + cardW - 36, cardY + 8, 30, 26),
								new StringFormat { Alignment = StringAlignment.Far });
					}

					// Title
					using (var tFont  = new Font("Segoe UI", 8f, FontStyle.Bold))
					using (var tBrush = new SolidBrush(labelColor))
					using (var sf     = new StringFormat { Alignment = StringAlignment.Center })
						g.DrawString(title, tFont, tBrush,
							new RectangleF(cx, cardY + 10, cardW, 16), sf);

					// Large value
					float fs = value.Length <= 8 ? 24f : value.Length <= 12 ? 18f : 14f;
					using (var vFont  = new Font("Segoe UI", fs, FontStyle.Bold))
					using (var vBrush = new SolidBrush(textColor))
					using (var sf     = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
						g.DrawString(value, vFont, vBrush,
							new RectangleF(cx, cardY + 28, cardW, cardH - 72), sf);

					// Target label
					if (!string.IsNullOrEmpty(target))
					{
						using (var trFont  = new Font("Segoe UI", 7.5f, FontStyle.Regular))
						using (var trBrush = new SolidBrush(ColorNeutralMid))
						using (var sf      = new StringFormat { Alignment = StringAlignment.Center })
							g.DrawString("Target: " + target, trFont, trBrush,
								new RectangleF(cx, cardY + cardH - 44, cardW, 16), sf);
					}

					// Status badge
					if (!string.IsNullOrEmpty(status))
						DrawPillBadge(g, cx + cardW / 2 - 44, cardY + cardH - 26, 88, 20, status, accentCol);
				}

				return ToPng(bmp);
			}
		}
		/// <summary>
		/// Renders color-coded horizontal indicator bars for correlation and latency
		/// per axis. High correlation (>=0.95) = Energy Green; Moderate (>=0.80) = Amber;
		/// Low = Red. Latency shown on a centered timeline bar.
		/// </summary>
		public static byte[] RenderCorrelationBars(VerificationReportModel model, int width = 900, int height = 260)
		{
			if (model == null) throw new ArgumentNullException(nameof(model));

			using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
			using (var g   = Graphics.FromImage(bmp))
			{
				g.SmoothingMode     = SmoothingMode.AntiAlias;
				g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
				g.Clear(ColorNeutralLight);

				DrawSectionHeader(g, "SIGNAL CORRELATION & LATENCY", 0, 0, width, 34);

				int y0    = 40;
				int rowH  = (height - y0 - 4) / 3;
				int labelW = 80;
				int halfW  = (width - labelW * 2 - 32) / 2;

				var pairs = new[]
				{
					("PITCH", model.PitchPair),
					("ROLL",  model.RollPair),
					("HEAVE", model.HeavePair),
				};

				using (var labelFont  = new Font("Segoe UI", 8.5f, FontStyle.Bold))
				using (var valueFont  = new Font("Consolas", 9.5f, FontStyle.Bold))
				using (var captionFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
				using (var divPen     = new Pen(ColorDivider, 0.5f))
				{
					for (int i = 0; i < pairs.Length; i++)
					{
						var (label, pair) = pairs[i];
						int cy = y0 + i * rowH;

						// Row background (alternating)
						if (i % 2 == 1)
						{
							using (var altBrush = new SolidBrush(ColorWhite))
								g.FillRectangle(altBrush, 0, cy, width, rowH);
						}

						// Axis label
						using (var lb = new SolidBrush(ColorNeutralDark))
						using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
							g.DrawString(label, labelFont, lb, new RectangleF(8, cy, labelW, rowH), sf);

						int lx = 8 + labelW;

						// ── Correlation bar ──────────────────────────────────
						double corr = pair?.Correlation ?? double.NaN;
						bool   hasCorr = !double.IsNaN(corr);
						Color  corrCol = hasCorr
							? (corr >= 0.95 ? ColorSuccess : corr >= 0.80 ? ColorWarning : ColorFailure)
							: ColorNeutralMid;

						int trackX = lx + 4;
						int trackY = cy + (rowH - 14) / 2;
						int trackW = halfW - 60;
						int trackH = 14;

						// Track background
						using (var trackBg = new SolidBrush(ColorDivider))
							g.FillRectangle(trackBg, trackX, trackY, trackW, trackH);

						// Filled bar
						if (hasCorr)
						{
							int fillW = (int)(trackW * Math.Min(1.0, Math.Max(0, corr)));
							using (var fillBr = new SolidBrush(corrCol))
								g.FillRectangle(fillBr, trackX, trackY, fillW, trackH);
						}

						// Track border
						using (var tp = new Pen(ColorNeutralMid, 0.5f))
							g.DrawRectangle(tp, trackX, trackY, trackW, trackH);

						// Correlation value label
						string corrStr = hasCorr ? string.Format("{0:0.000}", corr) : "\u2014";
						using (var vBrush = new SolidBrush(corrCol))
						using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
							g.DrawString(corrStr, valueFont, vBrush,
								new RectangleF(trackX + trackW + 6, cy, 52, rowH), sf);

						// "CORRELATION" caption + quality interpretation
						string corrQuality = !hasCorr ? "" : corr >= 0.95 ? "EXCELLENT" : corr >= 0.80 ? "GOOD" : corr >= 0.60 ? "ACCEPTABLE" : "POOR";
						using (var capBr = new SolidBrush(ColorNeutralMid))
						using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Far })
							g.DrawString("CORRELATION", captionFont, capBr,
								new RectangleF(trackX, cy + 2, trackW, (rowH - 14) / 2), sf);

						// ── Latency timeline bar ─────────────────────────────
						int lx2 = lx + halfW;
double latMs = pair != null ? pair.EstimatedLatencySeconds * 1000.0 : double.NaN;


						bool hasLat = !double.IsNaN(latMs);

						int lat_trackX = lx2;
						int lat_trackW = halfW - 60;
						int lat_centerX = lat_trackX + lat_trackW / 2;

						// Timeline base
						using (var trackBg = new SolidBrush(ColorDivider))
							g.FillRectangle(trackBg, lat_trackX, trackY, lat_trackW, trackH);

						// Center tick
						using (var cPen = new Pen(ColorNeutralDark, 1.5f))
							g.DrawLine(cPen, lat_centerX, trackY - 3, lat_centerX, trackY + trackH + 3);

						if (hasLat)
						{
							double maxMs  = 500.0;
							double frac   = Math.Min(1.0, Math.Abs(latMs) / maxMs);
							int    barW   = (int)(lat_trackW / 2 * frac);
							Color  latCol = frac < 0.25 ? ColorSuccess : frac < 0.6 ? ColorWarning : ColorFailure;

							int barX = latMs >= 0 ? lat_centerX : lat_centerX - barW;
							using (var fb = new SolidBrush(latCol))
								g.FillRectangle(fb, barX, trackY, barW, trackH);
						}

						using (var tp = new Pen(ColorNeutralMid, 0.5f))
							g.DrawRectangle(tp, lat_trackX, trackY, lat_trackW, trackH);

						// Latency value
						string latStr = hasLat ? string.Format("{0:+0;-0;0} ms", latMs) : "\u2014";
						using (var vBrush = new SolidBrush(ColorNeutralDark))
						using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
							g.DrawString(latStr, valueFont, vBrush,
								new RectangleF(lat_trackX + lat_trackW + 6, cy, 60, rowH), sf);

						using (var capBr = new SolidBrush(ColorNeutralMid))
						using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Far })
							g.DrawString("LATENCY", captionFont, capBr,
								new RectangleF(lat_trackX, cy + 2, lat_trackW, (rowH - 14) / 2), sf);

						// Row divider
						g.DrawLine(divPen, 0, cy + rowH - 1, width, cy + rowH - 1);
					}
				}

					return ToPng(bmp);
					}
				}

				/// <summary>
				/// Renders a full-width per-axis summary banner (900×96 GDI) that sits above the
				/// detailed statistics table for that axis. Dark navy background; shows axis name,
				/// data quality status, applied correction, sample count, outlier rate, and
				/// confidence for instant at-a-glance assessment per axis.
				/// </summary>
				public static byte[] RenderAxisSummaryCard(VerificationReportModel model, VerificationAxisKind axis, int width = 900, int height = 130)
				{
					if (model == null) throw new ArgumentNullException(nameof(model));

					using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
					using (var g   = Graphics.FromImage(bmp))
					{
						g.SmoothingMode     = SmoothingMode.AntiAlias;
						g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

						// Dark navy gradient background
						using (var bgBr = new LinearGradientBrush(
							new Rectangle(0, 0, width, height),
							ColorPrimary, ColorSecondary, LinearGradientMode.Horizontal))
							g.FillRectangle(bgBr, 0, 0, width, height);

						// Energy Green left accent
						using (var accentBr = new SolidBrush(ColorAccent))
							g.FillRectangle(accentBr, 0, 0, 6, height);

						// Derive axis statistics
						AxisStatistics     refStat  = model.RefStats(axis);
						AxisStatistics     testStat = model.TestStats(axis);
						AxisStatistics     devStat  = model.DevStats(axis);
						PairedSeriesStatistics pair = model.PairStats(axis);
						string unit                 = model.Unit(axis);
						VerificationStatus vstatus  = VerificationAssessment.Classify(axis, refStat, testStat, devStat);

						// Confidence score for this axis
						double sampleScore = model.HasData && devStat?.SampleCount > 0
							? Math.Min(1.0, (double)devStat.SampleCount / VerificationAssessment.MinSamplesGood) : 0;
						double outlierPct  = devStat?.OutlierPercent ?? double.NaN;
						double outlierScore = double.IsNaN(outlierPct) || outlierPct <= 0
							? 1.0 : Math.Max(0, 1.0 - outlierPct / 10.0);
						int    axisConf    = model.HasData ? (int)Math.Round((sampleScore * 0.6 + outlierScore * 0.4) * 100) : 0;
						Color  confColor   = axisConf >= 80 ? ColorSuccess : axisConf >= 60 ? ColorWarning : ColorFailure;

						// Applied correction
						double correction = model.RecommendedCorrection(axis);
						string corrStr    = (model.HasData && !double.IsNaN(correction))
							? string.Format(Ci, "{0:+0.000;-0.000;0.000} {1}", correction, unit)
							: "\u2014";

						// Status color
						Color statusColor = vstatus == VerificationStatus.Good       ? ColorSuccess
										  : vstatus == VerificationStatus.Acceptable  ? ColorWarning
										  : vstatus == VerificationStatus.NeedsAttention ? ColorFailure
										  : ColorNeutralMid;
						string statusLabel = VerificationAssessment.StatusLabel(vstatus);

						// ── Layout: axis name | correction | samples | outliers | confidence ──
						int lp    = 18;
						int col1X = lp;          // Axis label
						int col2X = 200;         // Correction value
						int col3X = 400;         // Samples
						int col4X = 560;         // Outlier rate
						int col5X = 730;         // Confidence

						using (var whiteBr  = new SolidBrush(ColorWhite))
						using (var greenBr  = new SolidBrush(ColorAccent))
						using (var mutedBr  = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
						using (var statusBr = new SolidBrush(statusColor))
						{
							// Large axis name
							using (var axisFont = new Font("Segoe UI", 24f, FontStyle.Bold))
								g.DrawString(model.AxisTitle(axis).ToUpper(), axisFont, whiteBr, col1X, 10);

							// Status label below axis name
							using (var statFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
								g.DrawString(statusLabel, statFont, statusBr, col1X, 64);

							// Divider
							using (var divPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
								g.DrawLine(divPen, col2X - 12, 12, col2X - 12, height - 12);

							// Correction
							using (var capFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
							using (var valFont = new Font("Consolas", 20f, FontStyle.Bold))
							{
								g.DrawString("CORRECTION", capFont, mutedBr, col2X, 10);
								g.DrawString(corrStr, valFont, greenBr, col2X, 26);
								if (model.HasCorrectionApplied && model.HasData)
								{
									using (var appliedFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
										g.DrawString("\u2713 APPLIED", appliedFont, new SolidBrush(ColorSuccess), col2X, 68);
								}
							}

							// Divider
							using (var divPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
								g.DrawLine(divPen, col3X - 12, 12, col3X - 12, height - 12);

							// Samples
							using (var capFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
							using (var valFont = new Font("Segoe UI", 20f, FontStyle.Bold))
							{
								string sampleStr = (devStat?.SampleCount ?? 0) > 0
									? (devStat.SampleCount).ToString("N0", Ci) : "\u2014";
								g.DrawString("SAMPLES", capFont, mutedBr, col3X, 10);
								g.DrawString(sampleStr, valFont, whiteBr, col3X, 26);
							}

							// Divider
							using (var divPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
								g.DrawLine(divPen, col4X - 12, 12, col4X - 12, height - 12);

							// Outlier rate
							using (var capFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
							using (var valFont = new Font("Segoe UI", 20f, FontStyle.Bold))
							{
								string outlierStr = !double.IsNaN(outlierPct) && model.HasData
									? string.Format(Ci, "{0:0.0}%", outlierPct) : "\u2014";
								Color outlierCol = double.IsNaN(outlierPct) ? ColorNeutralMid
									: outlierPct <= VerificationAssessment.OutlierAcceptablePercent ? ColorSuccess
									: outlierPct <= VerificationAssessment.OutlierAttentionPercent  ? ColorWarning
									: ColorFailure;
								g.DrawString("OUTLIERS", capFont, mutedBr, col4X, 10);
								g.DrawString(outlierStr, valFont, new SolidBrush(outlierCol), col4X, 26);
							}

							// Divider
							using (var divPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
								g.DrawLine(divPen, col5X - 12, 12, col5X - 12, height - 12);

							// Confidence score
							using (var capFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
							using (var valFont = new Font("Segoe UI", 20f, FontStyle.Bold))
							{
								string confStr = model.HasData ? axisConf.ToString() + " / 100" : "\u2014";
								g.DrawString("CONFIDENCE", capFont, mutedBr, col5X, 10);
								g.DrawString(confStr, valFont, new SolidBrush(confColor), col5X, 26);
							}
						}

						return ToPng(bmp);
					}
				}

				/// <summary>
				/// Renders a Data Quality Dashboard panel: overall quality verdict, confidence
				/// score, and three progress bars (samples, duration, outliers vs. targets).
				/// Height 260 gives enough room for all three bars with clear target labels.
				/// </summary>
				public static byte[] RenderConfidencePanel(VerificationReportModel model, int width = 900, int height = 260)
				{
					if (model == null) throw new ArgumentNullException(nameof(model));

			using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
			using (var g   = Graphics.FromImage(bmp))
			{
				g.SmoothingMode     = SmoothingMode.AntiAlias;
				g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
				g.Clear(ColorNeutralLight);

				DrawSectionHeader(g, "DATA QUALITY DASHBOARD", 0, 0, width, 38);

				// ── Derive scores ─────────────────────────────────────────────────────
				double sampleScore = model.HasData
					? Math.Min(1.0, (double)model.SampleCount / VerificationAssessment.MinSamplesGood)
					: 0;

				double durationMin = 0;
				if (!string.IsNullOrWhiteSpace(model.Duration) && TimeSpan.TryParse(model.Duration, out TimeSpan ts))
					durationMin = ts.TotalMinutes;
				double durationScore = Math.Min(1.0, durationMin / VerificationAssessment.MinDurationRecommendedMinutes);

				double outlierPct   = model.WorstOutlierPercent;
				double outlierScore = double.IsNaN(outlierPct) || outlierPct <= 0
					? 1.0 : Math.Max(0, 1.0 - outlierPct / 10.0);

				double overallScore = (sampleScore * 0.4 + durationScore * 0.35 + outlierScore * 0.25) * 100.0;
				int    scoreInt     = model.HasData ? (int)Math.Round(overallScore) : 0;
				Color  scoreColor   = scoreInt >= 80 ? ColorSuccess : scoreInt >= 60 ? ColorWarning : ColorFailure;

				string qualityVerdict = !model.HasData ? "NO DATA"
					: scoreInt >= 80 ? "EXCELLENT"
					: scoreInt >= 60 ? "ACCEPTABLE"
					: "ATTENTION";

				// ── Left column: score card (width 220) ───────────────────────────────
				int scoreCardW = 220;
				int scoreCardY = 46;
				int scoreCardH = height - scoreCardY - 8;

				using (var bg = new SolidBrush(ColorWhite))
					g.FillRectangle(bg, 8, scoreCardY, scoreCardW, scoreCardH);
				using (var borderPen = new Pen(ColorDivider, 1f))
					g.DrawRectangle(borderPen, 8, scoreCardY, scoreCardW - 1, scoreCardH - 1);
				using (var accentBr = new SolidBrush(scoreColor))
					g.FillRectangle(accentBr, 8, scoreCardY, scoreCardW, 6);

				// Large confidence score
				// Large confidence score — readable on white card; status conveyed by top accent bar
				using (var bigFont = new Font("Segoe UI", 52f, FontStyle.Bold))
				using (var sb      = new SolidBrush(StatusTextOnWhite(scoreColor)))
					g.DrawString(model.HasData ? scoreInt.ToString() : "\u2014", bigFont, sb, 24, scoreCardY + 10);

				if (model.HasData)
				{
					using (var unitFont = new Font("Segoe UI", 16f, FontStyle.Regular))
					using (var ub = new SolidBrush(ColorNeutralMid))
						g.DrawString("/ 100", unitFont, ub, 24, scoreCardY + 72);
				}

				// "CONFIDENCE SCORE" label
				using (var capFont = new Font("Segoe UI", 8f, FontStyle.Bold))
				using (var capBr   = new SolidBrush(ColorNeutralMid))
					g.DrawString("CONFIDENCE SCORE", capFont, capBr, 24, scoreCardY + 96);

				// Quality verdict badge â€” tinted per SES brand spec
				int badgeY = scoreCardY + scoreCardH - 42;
				DrawTintedBadge(g, 18, badgeY, scoreCardW - 20, 32, scoreColor, "DATA QUALITY:  " + qualityVerdict);

				// ── Right column: three progress bars ─────────────────────────────────
				int barsX  = scoreCardW + 20;
				int barsW  = width - barsX - 14;
				int barH   = 20;
				int rowH   = (height - 46 - 8) / 3;
				int bar1Y  = 46;

				var bars = new[]
				{
					(
						"SAMPLES",
						sampleScore,
						model.HasData ? model.SampleCount.ToString("N0", Ci) : "\u2014",
						string.Format(Ci, "{0:N0}  target", VerificationAssessment.MinSamplesGood),
						sampleScore >= 1.0 ? ColorSuccess : sampleScore >= 0.2 ? ColorWarning : ColorFailure
					),
					(
						"CAPTURE DURATION",
						durationScore,
						durationMin > 0 ? string.Format(Ci, "{0:F1} min", durationMin) : "\u2014",
						string.Format(Ci, "{0:F0} min  target", VerificationAssessment.MinDurationRecommendedMinutes),
						durationScore >= 1.0 ? ColorSuccess : durationScore >= 0.5 ? ColorWarning : ColorFailure
					),
					(
						"WORST OUTLIER RATE",
						outlierScore,
						double.IsNaN(outlierPct) || !model.HasData ? "\u2014" : string.Format(Ci, "{0:0.0}%", outlierPct),
						string.Format(Ci, "\u2264 {0:0}%  target", VerificationAssessment.OutlierAcceptablePercent),
						outlierScore >= 0.95 ? ColorSuccess : outlierScore >= 0.5 ? ColorWarning : ColorFailure
					),
				};

				using (var labelFont  = new Font("Segoe UI", 8.5f, FontStyle.Bold))
				using (var valueFont  = new Font("Segoe UI", 18f, FontStyle.Bold))
				using (var targetFont = new Font("Segoe UI", 8f, FontStyle.Regular))
				using (var darkBr     = new SolidBrush(ColorNeutralDark))
				using (var mutedBr    = new SolidBrush(ColorNeutralMid))
				{
					for (int i = 0; i < bars.Length; i++)
					{
						var (bLabel, bFrac, bMeasured, bTarget, bColor) = bars[i];
						int rowY = bar1Y + i * rowH;

						// Row background (alternating)
						if (i % 2 == 0)
						{
							using (var altBr = new SolidBrush(ColorWhite))
								g.FillRectangle(altBr, barsX - 4, rowY, barsW + 4, rowH);
						}

						// Label
						g.DrawString(bLabel, labelFont, mutedBr, barsX, rowY + 6);

						// Large measured value
						g.DrawString(bMeasured, valueFont, new SolidBrush(bColor),
							new PointF(barsX, rowY + 22));

						// Progress track (fills the remaining width)
						int trackX = barsX + 120;
						int trackY2 = rowY + rowH / 2 + 2;
						int trackW  = barsW - 128;
						using (var trackBg = new SolidBrush(ColorDivider))
							g.FillRectangle(trackBg, trackX, trackY2, trackW, barH);
						using (var fillBr = new SolidBrush(bColor))
							g.FillRectangle(fillBr, trackX, trackY2, (int)(trackW * Math.Min(1.0, bFrac)), barH);
						using (var tp = new Pen(ColorNeutralMid, 0.5f))
							g.DrawRectangle(tp, trackX, trackY2, trackW, barH);

						// Target label inside/below the bar
						g.DrawString(bTarget, targetFont, mutedBr, trackX, trackY2 + barH + 2);
					}
				}

				return ToPng(bmp);
			}
		}


        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // DRAWING HELPERS â€” design elements
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <summary>Navy section header bar with white upper-case label.</summary>
        private static void DrawSectionHeader(Graphics g, string text, int x, int y, int w, int h)
        {
            using (var bg = new SolidBrush(ColorPrimary))
                g.FillRectangle(bg, x, y, w, h);
            using (var accent = new SolidBrush(ColorAccent))
                g.FillRectangle(accent, x, y, 4, h);
            using (var font  = new Font("Segoe UI", 10f, FontStyle.Bold))
            using (var brush = new SolidBrush(ColorWhite))
            using (var sf    = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                g.DrawString(text, font, brush, new RectangleF(x + 12, y, w - 12, h), sf);
        }

        /// <summary>Light sub-header with teal left accent.</summary>
        private static void DrawSubHeader(Graphics g, string text, int x, int y, int w)
        {
            using (var bg = new SolidBrush(Color.FromArgb(0xE8, 0xEF, 0xF8)))
                g.FillRectangle(bg, x, y, w, 22);
            using (var accent = new SolidBrush(ColorAccent))
                g.FillRectangle(accent, x, y, 3, 22);
            using (var font  = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var brush = new SolidBrush(ColorSecondary))
            using (var sf    = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                g.DrawString(text, font, brush, new RectangleF(x + 8, y, w - 8, 22), sf);
        }

        /// <summary>Draws a KPI card with a colored left accent, icon, title, and value.</summary>
        private static void DrawKpiCard(Graphics g, int x, int y, int w, int h,
                                        string icon, string title, string value, Color accentColor, string badge)
        {
            // Card background + border
            using (var bg = new SolidBrush(ColorWhite))
                g.FillRectangle(bg, x, y, w, h);
            using (var border = new Pen(ColorDivider, 1f))
                g.DrawRectangle(border, x, y, w - 1, h - 1);

            // Left accent bar
            using (var accent = new SolidBrush(accentColor))
                g.FillRectangle(accent, x, y, 4, h);

            // Icon
            using (var iconFont  = new Font("Segoe UI Symbol", 16f, FontStyle.Bold))
            using (var iconBrush = new SolidBrush(accentColor))
                g.DrawString(icon, iconFont, iconBrush, x + 10, y + 8);

            // Title
            using (var titleFont  = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (var titleBrush = new SolidBrush(ColorNeutralMid))
                g.DrawString(title, titleFont, titleBrush, x + 10, y + 30);

            // Value
            using (var valFont  = new Font("Segoe UI", 17f, FontStyle.Bold))
            using (var valBrush = new SolidBrush(ColorNeutralDark))
            using (var sf       = new StringFormat { LineAlignment = StringAlignment.Near })
                g.DrawString(value, valFont, valBrush, new RectangleF(x + 10, y + 44, w - 16, 32), sf);

            // Optional badge
            if (!string.IsNullOrEmpty(badge))
                DrawPillBadge(g, x + w - 62, y + 6, 56, 18, badge, accentColor);
        }

        /// <summary>
        /// Dashboard KPI card: top accent bar (5 px, full-width), large centered value,
        /// small label at bottom. Used for the 2x3 executive dashboard grid.
        /// </summary>
        private static void DrawDashboardCard(Graphics g, int x, int y, int w, int h,
                                              string value, string label, Color accentColor)
        {
            using (var bg = new SolidBrush(ColorWhite))
                g.FillRectangle(bg, x, y, w, h);
            using (var border = new Pen(ColorDivider, 1f))
                g.DrawRectangle(border, x, y, w - 1, h - 1);
            using (var accent = new SolidBrush(accentColor))
                g.FillRectangle(accent, x, y, w, 5);
            float fontSize = value.Length <= 4 ? 34f : value.Length <= 7 ? 28f : value.Length <= 11 ? 22f : 17f;
            using (var valFont  = new Font("Segoe UI", fontSize, FontStyle.Bold))
            using (var valBrush = new SolidBrush(ColorNeutralDark))
            using (var sf       = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(value, valFont, valBrush, new RectangleF(x, y + 6, w, h - 30), sf);
            using (var dot = new SolidBrush(accentColor))
                g.FillEllipse(dot, x + 10, y + h - 20, 8, 8);
            using (var lblFont  = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (var lblBrush = new SolidBrush(ColorNeutralMid))
            using (var sf       = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(label, lblFont, lblBrush, new RectangleF(x + 20, y + h - 22, w - 24, 18), sf);
        }
        /// <summary>Per-axis KPI card with deviation value and RMS sub-value.</summary>
        private static void DrawAxisKpiCard(Graphics g, int x, int y, int w, int h,
                                            string axisLabel, string unit, string devValue,
                                            Color statusColor, AxisStatistics stats, double scale)
        {
            // Card + accent bar
            using (var bg = new SolidBrush(ColorWhite))
                g.FillRectangle(bg, x, y, w, h);
            using (var border = new Pen(ColorDivider, 1f))
                g.DrawRectangle(border, x, y, w - 1, h - 1);
            using (var accent = new SolidBrush(statusColor))
                g.FillRectangle(accent, x, y, 4, h);

            // Axis header
            using (var hFont  = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var hBrush = new SolidBrush(ColorSecondary))
                g.DrawString(axisLabel, hFont, hBrush, x + 10, y + 8);

            // Mean deviation (primary value) — use readable color on white card background
            using (var valFont  = new Font("Segoe UI", 18f, FontStyle.Bold))
            using (var valBrush = new SolidBrush(StatusTextOnWhite(statusColor)))
            using (var sf       = new StringFormat { LineAlignment = StringAlignment.Near })
                g.DrawString(devValue, valFont, valBrush, new RectangleF(x + 10, y + 26, w - 14, 30), sf);

            // RMS sub-value
            if (stats != null && !double.IsNaN(stats.Rms))
            {
                string rmsStr = $"RMS {stats.Rms:0.000} {unit}";
                using (var rmsFont  = new Font("Segoe UI", 7.5f, FontStyle.Regular))
                using (var rmsBrush = new SolidBrush(ColorNeutralMid))
                    g.DrawString(rmsStr, rmsFont, rmsBrush, x + 10, y + 57);
            }

            // Mini progress bar (deviation / scale)
            if (stats != null && !double.IsNaN(stats.Mean) && scale > 0)
            {
                double frac = Math.Min(1.0, Math.Abs(stats.Mean) / scale);
                int barX    = x + 10;
                int barY    = y + h - 18;
                int barW    = w - 20;
                int barH    = 8;

                using (var trackBrush = new SolidBrush(ColorNeutralLight))
                    g.FillRectangle(trackBrush, barX, barY, barW, barH);
                using (var fillBrush = new SolidBrush(statusColor))
                    g.FillRectangle(fillBrush, barX, barY, (int)(barW * frac), barH);
                using (var trackPen = new Pen(ColorDivider, 0.5f))
                    g.DrawRectangle(trackPen, barX, barY, barW, barH);
            }
        }

        /// <summary>Full-width bullet chart row for a single axis.</summary>
        private static void DrawBulletRow(Graphics g, int x, int y, int w, int h,
                                          string label, string unit, double value, double scale)
        {
            const int labelW  = 80;
            const int valueW  = 100;
            const int padding = 12;

            int trackX = x + labelW + padding;
            int trackW = w - labelW - valueW - padding * 2;
            int trackY = y + h / 2 - 7;
            int trackH = 14;

            // Row background (alternate shade handled by caller)
            using (var rowBg = new SolidBrush(Color.FromArgb(0xFA, 0xFB, 0xFD)))
                g.FillRectangle(rowBg, x, y, w, h);

            // Axis label
            using (var lFont  = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var lBrush = new SolidBrush(ColorNeutralDark))
            using (var sf     = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
                g.DrawString(label, lFont, lBrush,
                    new RectangleF(x + 2, y, labelW - 4, h), sf);

            if (double.IsNaN(value) || scale <= 0)
            {
                using (var naFont  = new Font("Segoe UI", 8f, FontStyle.Regular))
                using (var naBrush = new SolidBrush(ColorNeutralMid))
                    g.DrawString("No data", naFont, naBrush, trackX + 4, y + h / 2 - 8);
                return;
            }

            double absValue = Math.Abs(value);
            double frac     = Math.Min(1.2, absValue / scale); // allow slight overshoot for visibility

            // Zone bands: 0-60% green, 60-80% amber, 80-100% red
            DrawZonedTrack(g, trackX, trackY, trackW, trackH);

            // Measure bar
            Color barColor = ZoneColor(frac);
            int   barW2    = Math.Max(3, (int)(trackW * Math.Min(frac, 1.0)));
            using (var barBrush = new SolidBrush(barColor))
                g.FillRectangle(barBrush, trackX, trackY, barW2, trackH);

            // Scale marker (= 100% threshold)
            using (var markerPen = new Pen(ColorPrimary, 2f))
                g.DrawLine(markerPen, trackX + trackW, trackY - 3, trackX + trackW, trackY + trackH + 3);

            // Value label
            string valStr = $"{value:+0.000;-0.000;0.000} {unit}";
            using (var vFont  = new Font("Consolas", 9f, FontStyle.Bold))
            using (var vBrush = new SolidBrush(barColor))
            using (var sf     = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                g.DrawString(valStr, vFont, vBrush,
                    new RectangleF(trackX + trackW + padding, y, valueW, h), sf);

            // Track border
            using (var borderPen = new Pen(ColorDivider, 0.8f))
                g.DrawRectangle(borderPen, trackX, trackY, trackW, trackH);

            // Divider line between rows
            using (var divPen = new Pen(ColorDivider, 0.5f))
                g.DrawLine(divPen, x, y + h - 1, x + w, y + h - 1);
        }

        /// <summary>Compact inline bullet row used inside the executive dashboard.</summary>
        private static void DrawInlineBulletRow(Graphics g, int x, int y, int w, int h,
                                                string label, string unit, double value, double scale)
        {
            const int labelW  = 60;
            const int valueW  = 120;
            const int padding = 8;

            int trackX = x + labelW + padding;
            int trackW = w - labelW - valueW - padding * 2;
            int trackY = y + h / 2 - 6;
            int trackH = 12;

            // Label
            using (var lFont  = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var lBrush = new SolidBrush(ColorNeutralDark))
            using (var sf     = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
                g.DrawString(label, lFont, lBrush, new RectangleF(x, y, labelW - 2, h), sf);

            if (double.IsNaN(value) || scale <= 0)
            {
                using (var naFont  = new Font("Segoe UI", 7.5f, FontStyle.Regular))
                using (var naBrush = new SolidBrush(ColorNeutralMid))
                    g.DrawString("â€”", naFont, naBrush, trackX, y + h / 2 - 7);
                return;
            }

            double absValue = Math.Abs(value);
            double frac     = Math.Min(1.2, absValue / scale);

            DrawZonedTrack(g, trackX, trackY, trackW, trackH);

            int   barW  = Math.Max(2, (int)(trackW * Math.Min(frac, 1.0)));
            Color barCol = ZoneColor(frac);
            using (var barBrush = new SolidBrush(barCol))
                g.FillRectangle(barBrush, trackX, trackY, barW, trackH);

            using (var markerPen = new Pen(ColorPrimary, 2f))
                g.DrawLine(markerPen, trackX + trackW, trackY - 2, trackX + trackW, trackY + trackH + 2);

            string valStr = $"{value:+0.000;-0.000;0.000} {unit}";
            using (var vFont  = new Font("Consolas", 8.5f, FontStyle.Bold))
            using (var vBrush = new SolidBrush(barCol))
            using (var sf     = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                g.DrawString(valStr, vFont, vBrush,
                    new RectangleF(trackX + trackW + padding, y, valueW, h), sf);

            using (var borderPen = new Pen(ColorDivider, 0.6f))
                g.DrawRectangle(borderPen, trackX, trackY, trackW, trackH);
        }

        /// <summary>Draws green/amber/red zone background bands for a bullet track.</summary>
        private static void DrawZonedTrack(Graphics g, int x, int y, int w, int h)
        {
            // Green zone 0-60%
            using (var b = new SolidBrush(Color.FromArgb(0xE6, 0xF7, 0xEF)))
                g.FillRectangle(b, x, y, (int)(w * 0.60), h);
            // Amber zone 60-80%
            using (var b = new SolidBrush(Color.FromArgb(0xFD, 0xF5, 0xE0)))
                g.FillRectangle(b, x + (int)(w * 0.60), y, (int)(w * 0.20), h);
            // Red zone 80-100%
            using (var b = new SolidBrush(Color.FromArgb(0xFD, 0xEC, 0xEC)))
                g.FillRectangle(b, x + (int)(w * 0.80), y, w - (int)(w * 0.80), h);
        }

        /// <summary>Draws the overall-status badge on the cover banner.</summary>
        private static void DrawStatusBadge(Graphics g, VerificationReportModel model, int x, int y, int w, int h)
        {
            VerificationStatus status = OverallStatus(model);
            Color bg  = StatusBadgeColor(status);
            string lbl = StatusBadgeLabel(status);

            // Badge background (rounded rectangle approximated with filled rect + ellipses)
            using (var brush = new SolidBrush(bg))
                g.FillRectangle(brush, x, y, w, h);

            using (var outlinePen = new Pen(Color.FromArgb(60, 255, 255, 255), 1.5f))
                g.DrawRectangle(outlinePen, x, y, w - 1, h - 1);

            // Status icon + label — text color must contrast against the badge background
            using (var iconFont  = new Font("Segoe UI Symbol", 18f, FontStyle.Bold))
            using (var lblFont   = new Font("Segoe UI",        11f, FontStyle.Bold))
            using (var textBrush = new SolidBrush(ContrastText(bg)))
            using (var sfCtr     = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                string icon = status == VerificationStatus.Good || status == VerificationStatus.Acceptable
                    ? "\u2713"   // check mark
                    : status == VerificationStatus.NeedsAttention ? "\u26A0" : "\u2014";
                g.DrawString(icon, iconFont, textBrush, new RectangleF(x, y + 4, w, 30), sfCtr);
                g.DrawString(lbl,  lblFont,  textBrush, new RectangleF(x, y + 36, w, 40), sfCtr);
            }
        }

        // 
        // ORIGINAL BAR CHART RENDERER (retained + updated palette)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private sealed class BarRow
        {
            public BarRow(string label, double value, string unit, Color color)
            {
                Label = label;
                Value = double.IsNaN(value) ? 0 : value;
                Unit  = unit;
                Color = color;
            }

            public string Label { get; }
            public double Value { get; }
            public string Unit  { get; }
            public Color  Color { get; }
        }

        private static byte[] RenderHorizontalBars(string title, BarRow[] rows, int width, int height, bool signed)
        {
            using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (var g   = Graphics.FromImage(bmp))
            {
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(ColorBackground);

                using (var titleFont = new Font("Segoe UI", 13f, FontStyle.Bold))
                using (var labelFont = new Font("Segoe UI",  9.5f))
                using (var valueFont = new Font("Segoe UI",  9.5f, FontStyle.Bold))
                using (var textBrush = new SolidBrush(ColorText))
                using (var axisPen   = new Pen(ColorAxis, 1.2f))
                using (var gridPen   = new Pen(ColorGrid, 1f))
                {
                    // Title strip
                    using (var stripBrush = new SolidBrush(ColorNeutralLight))
                        g.FillRectangle(stripBrush, 0, 0, width, 38);
                    using (var accentBrush = new SolidBrush(ColorAccent))
                        g.FillRectangle(accentBrush, 0, 0, 4, 38);
                    g.DrawString(title, titleFont, textBrush, 12, 10);

                    const int marginLeft   = 160;
                    const int marginRight  = 100;
                    const int marginTop    = 52;
                    const int marginBottom = 22;

                    var plot = new RectangleF(
                        marginLeft,
                        marginTop,
                        width - marginLeft - marginRight,
                        height - marginTop - marginBottom);

                    double maxAbs = 0;
                    foreach (var r in rows)
                        maxAbs = Math.Max(maxAbs, Math.Abs(r.Value));
                    if (maxAbs <= 0) maxAbs = 1;
                    double niceMax = NiceCeiling(maxAbs);

                    float zeroX     = signed ? plot.Left + plot.Width / 2f : plot.Left;
                    float fullWidth = signed ? plot.Width / 2f : plot.Width;

                    DrawGridlines(g, gridPen, axisPen, textBrush, labelFont, plot, zeroX, fullWidth, niceMax, signed);

                    float rowHeight = plot.Height / rows.Length;
                    float barHeight = Math.Min(26f, rowHeight * 0.55f);

                    for (int i = 0; i < rows.Length; i++)
                    {
                        BarRow r       = rows[i];
                        float  centerY = plot.Top + rowHeight * i + rowHeight / 2f;

                        // Alternating row shade
                        if (i % 2 == 1)
                        {
                            using (var rowBg = new SolidBrush(ColorNeutralLight))
                                g.FillRectangle(rowBg,
                                    plot.Left - marginLeft + marginLeft, centerY - rowHeight / 2f,
                                    plot.Width, rowHeight);
                        }

                        // Row label
                        var labelRect = new RectangleF(4, centerY - rowHeight / 2f, marginLeft - 10, rowHeight);
                        using (var sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
                            g.DrawString(r.Label, labelFont, textBrush, labelRect, sf);

                        float barLen = (float)(Math.Abs(r.Value) / niceMax) * fullWidth;
                        float barTop = centerY - barHeight / 2f;
                        float barX   = r.Value >= 0 ? zeroX : zeroX - barLen;

                        // Shadow / depth effect
                        using (var shadowBrush = new SolidBrush(Color.FromArgb(30, ColorPrimary)))
                            g.FillRectangle(shadowBrush, barX + 2, barTop + 2, Math.Max(1f, barLen), barHeight);

                        using (var barBrush = new SolidBrush(r.Color))
                            g.FillRectangle(barBrush, barX, barTop, Math.Max(1f, barLen), barHeight);

                        // Value label at bar end
                        string valueText = $"{r.Value:+0.000;-0.000;0.000} {r.Unit}";
                        float  valueX    = r.Value >= 0 ? barX + barLen + 6 : barX - 6;
                        using (var sf = new StringFormat
                        {
                            Alignment     = r.Value >= 0 ? StringAlignment.Near : StringAlignment.Far,
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
                    float  dx   = (float)(frac * fullWidth);

                    if (signed)
                    {
                        DrawTick(g, gridPen, textBrush, font, sf, plot, zeroX + dx,  niceMax * frac);
                        if (t != 0)
                            DrawTick(g, gridPen, textBrush, font, sf, plot, zeroX - dx, -niceMax * frac);
                    }
                    else
                    {
                        DrawTick(g, gridPen, textBrush, font, sf, plot, plot.Left + dx, niceMax * frac);
                    }
                }
            }

            g.DrawLine(axisPen, zeroX, plot.Top, zeroX, plot.Bottom);
        }

        private static void DrawTick(Graphics g, Pen gridPen, Brush textBrush, Font font, StringFormat sf,
                                     RectangleF plot, float x, double value)
        {
            g.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);
            string txt = value.ToString("0.###", Ci);
            g.DrawString(txt, font, textBrush, new RectangleF(x - 30, plot.Bottom + 2, 60, 16), sf);
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // LOGIC HELPERS
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static VerificationStatus OverallStatus(VerificationReportModel model)
        {
            if (!model.HasData)
                return VerificationStatus.Unknown;

            bool samplesOk  = model.SampleCount >= VerificationAssessment.MinSamplesAcceptable;
            bool samplesGood = model.SampleCount >= VerificationAssessment.MinSamplesGood;

            double outlier = model.WorstOutlierPercent;
            bool outlierOk   = double.IsNaN(outlier) || outlier <= VerificationAssessment.OutlierAttentionPercent;
            bool outlierGood = double.IsNaN(outlier) || outlier <= VerificationAssessment.OutlierAcceptablePercent;

            if (samplesGood && outlierGood)  return VerificationStatus.Good;
            if (samplesOk   && outlierOk)    return VerificationStatus.Acceptable;
            return VerificationStatus.NeedsAttention;
        }

        private static Color StatusBadgeColor(VerificationStatus status)
        {
            switch (status)
            {
                case VerificationStatus.Good:           return ColorSuccess;
                case VerificationStatus.Acceptable:     return ColorWarning;
                case VerificationStatus.NeedsAttention: return ColorFailure;
                default:                                return ColorNeutralMid;
            }
        }

        private static string StatusBadgeLabel(VerificationStatus status)
        {
            switch (status)
            {
                case VerificationStatus.Good:           return "VERIFIED";
                case VerificationStatus.Acceptable:     return "ACCEPTABLE";
                case VerificationStatus.NeedsAttention: return "REVIEW";
                default:                                return "NO DATA";
            }
        }

        private static string DataQualityLabel(VerificationReportModel model)
        {
            if (!model.HasData) return "No Data";
            return VerificationAssessment.StatusLabel(OverallStatus(model));
        }

        private static Color DataQualityColor(VerificationReportModel model)
        {
            if (!model.HasData) return ColorNeutralMid;
            return StatusBadgeColor(OverallStatus(model));
        }

        private static Color SamplesColor(int samples)
        {
            if (samples >= VerificationAssessment.MinSamplesGood)       return ColorSuccess;
            if (samples >= VerificationAssessment.MinSamplesAcceptable)  return ColorWarning;
            return ColorFailure;
        }

        private static Color AxisStatusColor(VerificationAxisKind axis, AxisStatistics dev)
        {
            if (dev == null || dev.SampleCount == 0 || double.IsNaN(dev.Mean))
                return ColorNeutralMid;
            double scale = axis == VerificationAxisKind.Heave ? 0.5 : 1.0;
            double frac  = Math.Abs(dev.Mean) / scale;
            return ZoneColor(frac);
        }

        private static Color ZoneColor(double frac)
        {
            if (frac < 0.60) return ColorSuccess;
            if (frac < 0.80) return ColorWarning;
            return ColorFailure;
        }

        private static double MaxDeviation(VerificationReportModel model)
        {
            double max = double.NaN;
            foreach (double v in new[]
            {
                model.DevPitch?.Mean  ?? double.NaN,
                model.DevRoll?.Mean   ?? double.NaN,
                model.DevHeave?.Mean  ?? double.NaN,
            })
            {
                if (double.IsNaN(v)) continue;
                double abs = Math.Abs(v);
                max = double.IsNaN(max) ? abs : Math.Max(max, abs);
            }
            return max;
        }

        private static string TruncateStr(string s, int maxLen)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= maxLen ? s : s[..maxLen] + "â€¦";
        }

        private static double NiceCeiling(double value)
        {
            if (value <= 0) return 1;
            double exp      = Math.Floor(Math.Log10(value));
            double pow      = Math.Pow(10, exp);
            double frac     = value / pow;
            double niceFrac = frac <= 1 ? 1 : frac <= 2 ? 2 : frac <= 5 ? 5 : 10;
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

        /// <summary>
        /// Returns white or Dark Blue Grey text for maximum contrast against <paramref name="bg"/>.
        /// Energy Green (#3DE6A9) and other light brand colors require dark text for accessibility.
        /// </summary>
        private static Color ContrastText(Color bg)
        {
            double luma = (bg.R * 0.299 + bg.G * 0.587 + bg.B * 0.114) / 255.0;
            return luma > 0.45 ? ColorSecondary : ColorWhite;
        }

        /// <summary>
        /// Returns a readable version of a status color for text drawn on white or light-grey
        /// card backgrounds. Energy Green and Amber are too light on white; they are replaced
        /// with the dark brand color to meet minimum contrast requirements.
        /// </summary>
        private static Color StatusTextOnWhite(Color statusColor)
        {
            double luma = (statusColor.R * 0.299 + statusColor.G * 0.587 + statusColor.B * 0.114) / 255.0;
            return luma > 0.45 ? ColorSecondary : statusColor;
        }
    }
}
