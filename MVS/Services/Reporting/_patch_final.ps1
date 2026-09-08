param([string]$rendererFile, [string]$exporterFile)

# ─────────────────────────────────────────────────────────────────────────────
# 1. ConfidencePanel: replace solid-color verdict badge with tinted badge
# ─────────────────────────────────────────────────────────────────────────────
$c = [System.IO.File]::ReadAllText($rendererFile, [System.Text.Encoding]::UTF8)

$oldBadge = '				// Quality verdict badge
				int badgeY = scoreCardY + scoreCardH - 42;
				using (var badgeBr = new SolidBrush(scoreColor))
					g.FillRectangle(badgeBr, 18, badgeY, scoreCardW - 20, 32);
				using (var vFont    = new Font("Segoe UI", 11f, FontStyle.Bold))
				using (var badgeTxt = new SolidBrush(ContrastText(scoreColor)))
				using (var sf       = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
					g.DrawString("DATA QUALITY:  " + qualityVerdict, vFont, badgeTxt,
						new RectangleF(18, badgeY, scoreCardW - 20, 32), sf);'

$newBadge = '				// Quality verdict badge — tinted per SES brand spec
				int badgeY = scoreCardY + scoreCardH - 42;
				DrawTintedBadge(g, 18, badgeY, scoreCardW - 20, 32, scoreColor, "DATA QUALITY:  " + qualityVerdict);'

$idx = $c.IndexOf("// Quality verdict badge")
Write-Host "Verdict badge found at: $idx"
if ($idx -ge 0) {
	$c = $c.Replace($oldBadge, $newBadge)
	$idx2 = $c.IndexOf($newBadge)
	Write-Host "Replaced: $($idx2 -ge 0)"
}
[System.IO.File]::WriteAllText($rendererFile, $c, [System.Text.Encoding]::UTF8)
Write-Host "Renderer written. Len=$($c.Length)"

# ─────────────────────────────────────────────────────────────────────────────
# 2. Update PDF image heights in VerificationPdfReportExporter.cs
#    GDI->PDF ratio is 681/900 = 0.757
#    ExecutiveDashboard:  900x300 GDI -> 681x227 PDF  (was 681x166)
#    CorrectionCards:     900x340 GDI -> 681x257 PDF  (was 681x212)
#    SessionOverview:     900x220 GDI -> 681x166 PDF  (was 681x136)
#    ComplianceScorecards:900x240 GDI -> 681x181 PDF  (was 681x151)
#    AxisSummaryCard:     900x130 GDI -> 681x98 PDF   (was 681x72)
# ─────────────────────────────────────────────────────────────────────────────
$e = [System.IO.File]::ReadAllText($exporterFile, [System.Text.Encoding]::UTF8)

$e = $e.Replace('// Dashboard image: 900x220 GDI -> 681x166 PDF', '// Dashboard image: 900x300 GDI -> 681x227 PDF')
$e = $e.Replace('InsertImage(editor, model.ExecutiveDashboardPng, 681, 166)', 'InsertImage(editor, model.ExecutiveDashboardPng, 681, 227)')
$e = $e.Replace('InsertImage(editor, model.SessionOverviewPng, 681, 136)',    'InsertImage(editor, model.SessionOverviewPng, 681, 166)')
$e = $e.Replace('InsertImage(editor, model.CorrectionCardsPng, 681, 212)',    'InsertImage(editor, model.CorrectionCardsPng, 681, 257)')
$e = $e.Replace('InsertImage(editor, model.ComplianceScorecardsPng, 681, 151)', 'InsertImage(editor, model.ComplianceScorecardsPng, 681, 181)')
$e = $e.Replace('// Per-axis summary banner (900x96 GDI -> 681x72 PDF)',      '// Per-axis summary banner (900x130 GDI -> 681x98 PDF)')
$e = $e.Replace('InsertImage(editor, summaryPng, 681, 72)',                   'InsertImage(editor, summaryPng, 681, 98)')

[System.IO.File]::WriteAllText($exporterFile, $e, [System.Text.Encoding]::UTF8)
Write-Host "Exporter written. Len=$($e.Length)"
