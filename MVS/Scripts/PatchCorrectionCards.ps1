param([string]$file)

$content = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)

# Find RenderCorrectionCards start and end
$oldStart = $content.IndexOf("`t`tpublic static byte[] RenderCorrectionCards(VerificationReportModel model, int width = 900, int height = 200)")
$nextMethod = $content.IndexOf("`t`t/// <summary>`r`n`t`t/// Renders color-coded horizontal indicator bars")
Write-Host "Correction cards: start=$oldStart end=$nextMethod"

if ($oldStart -lt 0 -or $nextMethod -lt 0) {
	Write-Error "Boundaries not found"
	exit 1
}

$newMethod = @'
		/// <summary>
		/// Renders large hero correction cards: one card per axis (Pitch, Roll, Heave)
		/// with the correction value as the dominant typographic element.
		/// </summary>
		public static byte[] RenderCorrectionCards(VerificationReportModel model, int width = 900, int height = 280)
		{
			if (model == null) throw new ArgumentNullException(nameof(model));

			using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
			using (var g   = Graphics.FromImage(bmp))
			{
				g.SmoothingMode     = SmoothingMode.AntiAlias;
				g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
				g.Clear(ColorNeutralLight);

				DrawSectionHeader(g, "RECOMMENDED CORRECTIONS", 0, 0, width, 36);

				int cardY = 44;
				int gap   = 8;
				int cardW = (width - gap * 4) / 3;
				int cardH = height - cardY - 8;

				var axes = new[]
				{
					("PITCH", model.RecommendedCorrectionPitch, model.AppliedCorrectionPitch,
					 VerificationAssessment.Unit(VerificationAxisKind.Pitch)),
					("ROLL",  model.RecommendedCorrectionRoll,  model.AppliedCorrectionRoll,
					 VerificationAssessment.Unit(VerificationAxisKind.Roll)),
					("HEAVE", model.RecommendedCorrectionHeave, model.AppliedCorrectionHeave,
					 VerificationAssessment.Unit(VerificationAxisKind.Heave)),
				};

				for (int i = 0; i < axes.Length; i++)
				{
					var (label, recommended, applied, unit) = axes[i];
					int cx = gap + i * (cardW + gap);

					// Card body
					using (var cardBg = new SolidBrush(ColorPrimary))
						g.FillRectangle(cardBg, cx, cardY, cardW, cardH);

					// Energy Green top accent bar
					using (var accentBrush = new SolidBrush(ColorAccent))
						g.FillRectangle(accentBrush, cx, cardY, cardW, 5);

					// Card border
					using (var borderPen = new Pen(ColorAccent, 1f))
						g.DrawRectangle(borderPen, cx, cardY, cardW - 1, cardH - 1);

					// "AXIS CORRECTION" label (top)
					using (var labelFont = new Font("Segoe UI", 10f, FontStyle.Bold))
					using (var greenBr   = new SolidBrush(ColorAccent))
					using (var sf        = new StringFormat { Alignment = StringAlignment.Center })
						g.DrawString(label + " CORRECTION", labelFont, greenBr,
							new RectangleF(cx, cardY + 12, cardW, 20), sf);

					// Large correction value (hero element)
					bool   hasData = model.HasData && !double.IsNaN(recommended);
					string valStr  = hasData
						? string.Format("{0:+0.000;-0.000;0.000} {1}", recommended, unit)
						: "\u2014";

					using (var valFont  = new Font("Segoe UI", 36f, FontStyle.Bold))
					using (var whiteBr  = new SolidBrush(ColorWhite))
					using (var sf       = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
						g.DrawString(valStr, valFont, whiteBr,
							new RectangleF(cx + 4, cardY + 38, cardW - 8, cardH - 90), sf);

					// Divider line
					using (var divPen = new Pen(Color.FromArgb(60, 255, 255, 255), 1f))
						g.DrawLine(divPen, cx + 16, cardY + cardH - 48, cx + cardW - 16, cardY + cardH - 48);

					// Status (applied / pending)
					string statusStr = model.HasCorrectionApplied ? "\u2713  APPLIED" : "PENDING";
					Color  statusCol = model.HasCorrectionApplied ? ColorAccent : ColorWarning;
					using (var sFont = new Font("Segoe UI", 10f, FontStyle.Bold))
					using (var sBr   = new SolidBrush(statusCol))
					using (var sf    = new StringFormat { Alignment = StringAlignment.Center })
						g.DrawString(statusStr, sFont, sBr,
							new RectangleF(cx, cardY + cardH - 42, cardW, 24), sf);

					// Applied value footnote (if adjusted)
					if (model.HasCorrectionApplied && hasData && Math.Abs(applied - recommended) > 1e-6)
					{
						string appliedStr = string.Format("Applied: {0:+0.000;-0.000;0.000} {1}", applied, unit);
						using (var aFont   = new Font("Segoe UI", 7.5f, FontStyle.Regular))
						using (var mutedBr = new SolidBrush(ColorNeutralMid))
						using (var sf      = new StringFormat { Alignment = StringAlignment.Center })
							g.DrawString(appliedStr, aFont, mutedBr,
								new RectangleF(cx, cardY + cardH - 20, cardW, 16), sf);
					}
				}

				return ToPng(bmp);
			}
		}

'@

$newContent = $content.Substring(0, $oldStart) + $newMethod + $content.Substring($nextMethod)
[System.IO.File]::WriteAllText($file, $newContent, [System.Text.Encoding]::UTF8)
Write-Host "Done. File length: $($newContent.Length)"
