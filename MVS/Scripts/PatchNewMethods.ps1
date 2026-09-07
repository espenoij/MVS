param([string]$file)

$content = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)

# Insert two new methods before RenderCorrelationBars
$insertAt = $content.IndexOf("`t`t/// <summary>`r`n`t`t/// Renders color-coded horizontal indicator bars for correlation")
Write-Host "Insert at: $insertAt"
if ($insertAt -lt 0) { Write-Error "Marker not found"; exit 1 }

$newMethods = @'
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
		/// Renders compliance assessment scorecards: one card per criterion
		/// (Samples, Duration, Outliers) plus an Overall verdict card.
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

				// Samples
				int    samples      = model.SampleCount;
				string samplesVal   = samples > 0 ? samples.ToString("N0") : "\u2014";
				string samplesTarget = string.Format("\u2265 {0:N0}", VerificationAssessment.MinSamplesGood);
				string samplesStatus = samples >= VerificationAssessment.MinSamplesGood       ? "GOOD" :
									   samples >= VerificationAssessment.MinSamplesAcceptable ? "ACCEPTABLE" :
									   samples > 0 ? "INSUFFICIENT" : "NO DATA";
				Color samplesColor = samples >= VerificationAssessment.MinSamplesGood       ? ColorSuccess :
									 samples >= VerificationAssessment.MinSamplesAcceptable ? ColorWarning : ColorFailure;

				// Duration
				double actualMin = 0;
				if (!string.IsNullOrWhiteSpace(model.Duration) &&
					TimeSpan.TryParse(model.Duration, out TimeSpan ts))
					actualMin = ts.TotalMinutes;
				string durVal    = actualMin > 0 ? string.Format(Ci, "{0:F1} min", actualMin) : "\u2014";
				string durTarget = string.Format("\u2265 {0:F0} min", VerificationAssessment.MinDurationRecommendedMinutes);
				string durStatus = actualMin >= VerificationAssessment.MinDurationRecommendedMinutes ? "GOOD" :
								   actualMin >= VerificationAssessment.MinDurationAcceptableMinutes  ? "ACCEPTABLE" :
								   actualMin > 0 ? "INSUFFICIENT" : "NO DATA";
				Color durColor = actualMin >= VerificationAssessment.MinDurationRecommendedMinutes ? ColorSuccess :
								 actualMin >= VerificationAssessment.MinDurationAcceptableMinutes  ? ColorWarning : ColorFailure;

				// Outliers
				double worst       = model.WorstOutlierPercent;
				string outlierVal  = double.IsNaN(worst) || !model.HasData ? "\u2014"
								   : string.Format(Ci, "{0:F1} %", worst);
				string outlierTarget = string.Format("\u2264 {0:F0} %", VerificationAssessment.OutlierAcceptablePercent);
				string outlierStatus = double.IsNaN(worst) || !model.HasData ? "NO DATA" :
									   worst <= VerificationAssessment.OutlierAcceptablePercent ? "GOOD" :
									   worst <= VerificationAssessment.OutlierAttentionPercent  ? "ACCEPTABLE" : "TOO NOISY";
				Color outlierColor = double.IsNaN(worst) || !model.HasData ? ColorNeutralMid :
									 worst <= VerificationAssessment.OutlierAcceptablePercent ? ColorSuccess :
									 worst <= VerificationAssessment.OutlierAttentionPercent  ? ColorWarning : ColorFailure;

				// Overall
				bool allGood = samplesStatus == "GOOD" && durStatus == "GOOD" && outlierStatus == "GOOD";
				bool anyFail = samplesStatus == "INSUFFICIENT" || durStatus == "INSUFFICIENT" || outlierStatus == "TOO NOISY";
				string overallVal    = !model.HasData ? "NO DATA" : allGood ? "EXCELLENT" : anyFail ? "ATTENTION" : "ACCEPTABLE";
				Color  overallColor  = !model.HasData ? ColorNeutralMid : allGood ? ColorSuccess : anyFail ? ColorFailure : ColorWarning;

				var cards = new[]
				{
					("SAMPLES",  samplesVal,  samplesTarget,  samplesStatus,  samplesColor),
					("DURATION", durVal,      durTarget,      durStatus,      durColor),
					("OUTLIERS", outlierVal,  outlierTarget,  outlierStatus,  outlierColor),
					("OVERALL",  overallVal,  string.Empty,   string.Empty,   overallColor),
				};

				for (int i = 0; i < cards.Length; i++)
				{
					var (title, value, target, status, accentCol) = cards[i];
					int cx = gap + i * (cardW + gap);

					using (var bg = new SolidBrush(ColorWhite))
						g.FillRectangle(bg, cx, cardY, cardW, cardH);
					using (var border = new Pen(ColorDivider, 1f))
						g.DrawRectangle(border, cx, cardY, cardW - 1, cardH - 1);
					using (var accent = new SolidBrush(accentCol))
						g.FillRectangle(accent, cx, cardY, cardW, 5);

					// Title
					using (var tFont  = new Font("Segoe UI", 8f, FontStyle.Bold))
					using (var tBrush = new SolidBrush(ColorNeutralMid))
					using (var sf     = new StringFormat { Alignment = StringAlignment.Center })
						g.DrawString(title, tFont, tBrush,
							new RectangleF(cx, cardY + 10, cardW, 16), sf);

					// Large measured value
					float fs = value.Length <= 8 ? 24f : value.Length <= 12 ? 18f : 14f;
					using (var vFont  = new Font("Segoe UI", fs, FontStyle.Bold))
					using (var vBrush = new SolidBrush(ColorNeutralDark))
					using (var sf     = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
						g.DrawString(value, vFont, vBrush,
							new RectangleF(cx, cardY + 28, cardW, cardH - 72), sf);

					// Target
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
						DrawPillBadge(g, cx + cardW / 2 - 40, cardY + cardH - 26, 80, 20, status, accentCol);
				}

				return ToPng(bmp);
			}
		}

'@

$newContent = $content.Substring(0, $insertAt) + $newMethods + $content.Substring($insertAt)
[System.IO.File]::WriteAllText($file, $newContent, [System.Text.Encoding]::UTF8)
Write-Host "Done. Has RenderSessionOverviewPanel: $($newContent.Contains('RenderSessionOverviewPanel'))"
Write-Host "Has RenderComplianceScorecards: $($newContent.Contains('RenderComplianceScorecards'))"
