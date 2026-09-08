param([string]$file)
$c = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)

# ── 1. Change RenderAxisSummaryCard height 96 -> 130 ────────────────────────
$c = $c -replace 'public static byte\[\] RenderAxisSummaryCard\(VerificationReportModel model, VerificationAxisKind axis,\r\n\t\t\t\t\tint width = 900, int height = 96\)', 'public static byte[] RenderAxisSummaryCard(VerificationReportModel model, VerificationAxisKind axis, int width = 900, int height = 130)'
Write-Host "AxisSummary height patched."

# ── 2. Add quality interpretation after correlation value ───────────────────
# Find the "CORRELATION caption" drawing block and add quality label after it
$oldSnippet = '// "CORRELATION" caption
						using (var capBr = new SolidBrush(ColorNeutralMid))
						using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Far })
							g.DrawString("CORRELATION", captionFont, capBr,
								new RectangleF(trackX, cy + 2, trackW, (rowH - 14) / 2), sf);'
$newSnippet = '// "CORRELATION" caption + quality interpretation
						string corrQuality = !hasCorr ? "" : corr >= 0.95 ? "EXCELLENT" : corr >= 0.80 ? "GOOD" : corr >= 0.60 ? "ACCEPTABLE" : "POOR";
						using (var capBr = new SolidBrush(ColorNeutralMid))
						using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Far })
							g.DrawString("CORRELATION", captionFont, capBr,
								new RectangleF(trackX, cy + 2, trackW, (rowH - 14) / 2), sf);
						// Quality interpretation label below correlation numeric value
						if (hasCorr)
							using (var qBrush = new SolidBrush(corrCol))
							using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near })
								g.DrawString(corrQuality, captionFont, qBrush,
									new RectangleF(trackX + trackW + 6, cy + rowH / 2 + 2, 70, rowH / 2 - 4), sf);'
$idx = $c.IndexOf('"CORRELATION" caption')
Write-Host "Correlation caption idx: $idx"
if ($idx -ge 0) {
	$c = $c -replace [regex]::Escape('// "CORRELATION" caption'), ('// "CORRELATION" caption + quality interpretation' + "`r`n" + '						string corrQuality = !hasCorr ? "" : corr >= 0.95 ? "EXCELLENT" : corr >= 0.80 ? "GOOD" : corr >= 0.60 ? "ACCEPTABLE" : "POOR";')
	Write-Host "Quality label inserted."
}

[System.IO.File]::WriteAllText($file, $c, [System.Text.Encoding]::UTF8)
Write-Host "Written. Len=$($c.Length)"
