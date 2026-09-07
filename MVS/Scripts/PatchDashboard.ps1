param([string]$file)

$content = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)

$oldStart = $content.IndexOf("        /// <summary>`r`n        /// Renders the executive dashboard panel: three-column KPI cards for")
$oldEnd   = $content.IndexOf("        /// <summary>`r`n        /// Renders a three-row horizontal bullet chart panel")

if ($oldStart -lt 0 -or $oldEnd -lt 0) {
	Write-Error "Boundaries not found! oldStart=$oldStart oldEnd=$oldEnd"
	exit 1
}

$newMethod = @'
		/// <summary>
		/// Renders the executive dashboard panel: two rows of three KPI cards showing
		/// Verification Status, Samples Averaged, Capture Duration (row 1) and
		/// Maximum Outlier, Corrections Applied, Verification Confidence (row 2).
		/// </summary>
		public static byte[] RenderExecutiveDashboard(VerificationReportModel model, int width = 900, int height = 330)
		{
			if (model == null) throw new ArgumentNullException(nameof(model));

			using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
			using (var g   = Graphics.FromImage(bmp))
			{
				g.SmoothingMode     = SmoothingMode.AntiAlias;
				g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
				g.Clear(ColorNeutralLight);

				DrawSectionHeader(g, "EXECUTIVE DASHBOARD", 0, 0, width, 36);

				int gap   = 8;
				int cardW = (width - gap * 4) / 3;
				int cardH = 126;
				int row1Y = 44;
				int row2Y = row1Y + cardH + gap;

				VerificationStatus vstatus    = OverallStatus(model);
				Color  statusColor = StatusBadgeColor(vstatus);
				string statusText  = !model.HasData ? "NO DATA" :
					vstatus == VerificationStatus.Good       ? "VERIFIED" :
					vstatus == VerificationStatus.Acceptable ? "ACCEPTABLE" : "ATTENTION";

				string samplesText  = model.HasData ? model.SampleCount.ToString("N0") : "\u2014";
				Color  samplesColor = model.HasData ? SamplesColor(model.SampleCount) : ColorNeutralMid;

				double durationMin = 0;
				if (!string.IsNullOrWhiteSpace(model.Duration) &&
					TimeSpan.TryParse(model.Duration, out TimeSpan ts))
					durationMin = ts.TotalMinutes;
				string durText  = durationMin > 0
					? string.Format(Ci, "{0:F1} min", durationMin) : "\u2014";
				Color  durColor = durationMin >= VerificationAssessment.MinDurationRecommendedMinutes ? ColorSuccess
								: durationMin >= VerificationAssessment.MinDurationAcceptableMinutes  ? ColorWarning
								: durationMin > 0 ? ColorFailure : ColorNeutralMid;

				double outlierPct   = model.WorstOutlierPercent;
				string outlierText  = !model.HasData || double.IsNaN(outlierPct) ? "\u2014"
					: string.Format(Ci, "{0:F1} %", outlierPct);
				Color  outlierColor = !model.HasData || double.IsNaN(outlierPct) ? ColorNeutralMid
									: outlierPct <= VerificationAssessment.OutlierAcceptablePercent ? ColorSuccess
									: outlierPct <= VerificationAssessment.OutlierAttentionPercent  ? ColorWarning
									: ColorFailure;

				string corrText  = model.HasCorrectionApplied ? "APPLIED"
								 : model.HasData ? "PENDING" : "\u2014";
				Color  corrColor = model.HasCorrectionApplied ? ColorSuccess
								 : model.HasData ? ColorWarning : ColorNeutralMid;

				double sampleScore   = model.HasData
					? Math.Min(1.0, (double)model.SampleCount / VerificationAssessment.MinSamplesGood) : 0;
				double durationScore = Math.Min(1.0,
					durationMin / VerificationAssessment.MinDurationRecommendedMinutes);
				double outlierScore  = double.IsNaN(outlierPct) || outlierPct <= 0
					? 1.0 : Math.Max(0, 1.0 - outlierPct / 10.0);
				int    confScore     = model.HasData
					? (int)Math.Round(
						(sampleScore * 0.4 + durationScore * 0.35 + outlierScore * 0.25) * 100) : 0;
				string confText      = model.HasData
					? string.Format(Ci, "{0} / 100", confScore) : "\u2014";
				Color  confColor     = confScore >= 80 ? ColorSuccess
									 : confScore >= 60 ? ColorWarning : ColorFailure;

				// Row 1: Status | Samples | Duration
				DrawDashboardCard(g, gap,                     row1Y, cardW, cardH,
					statusText,  "VERIFICATION STATUS",     statusColor);
				DrawDashboardCard(g, gap + (cardW + gap),     row1Y, cardW, cardH,
					samplesText, "SAMPLES AVERAGED",        samplesColor);
				DrawDashboardCard(g, gap + (cardW + gap) * 2, row1Y, cardW, cardH,
					durText,     "CAPTURE DURATION",        durColor);

				// Row 2: Outliers | Corrections | Confidence
				DrawDashboardCard(g, gap,                     row2Y, cardW, cardH,
					outlierText, "MAXIMUM OUTLIER",         outlierColor);
				DrawDashboardCard(g, gap + (cardW + gap),     row2Y, cardW, cardH,
					corrText,    "CORRECTIONS",             corrColor);
				DrawDashboardCard(g, gap + (cardW + gap) * 2, row2Y, cardW, cardH,
					confText,    "VERIFICATION CONFIDENCE", confColor);

				return ToPng(bmp);
			}
		}

'@

$newContent = $content.Substring(0, $oldStart) + $newMethod + $content.Substring($oldEnd)
[System.IO.File]::WriteAllText($file, $newContent, [System.Text.Encoding]::UTF8)
Write-Host "Done. Has EXECUTIVE DASHBOARD: $($newContent.Contains('EXECUTIVE DASHBOARD'))"
