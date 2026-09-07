param([string]$file)
$enc  = [System.Text.Encoding]::UTF8
$c    = [System.IO.File]::ReadAllText($file, $enc)

# ── Fix WriteOverview ──────────────────────────────────────────────────────
$ovOld = @'
		private static void WriteOverview(RadFixedDocumentEditor editor, VerificationReportModel model)
		{
			editor.InsertPageBreak();
			Heading(editor, "6. Session Overview");

			Paragraph(editor, OverviewSentence(model), 11, ColorText, spacingAfter: 10);

			var rows = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("Project",            model.ProjectName),
				new KeyValuePair<string, string>("Operator",           Dash(model.Operator)),
				new KeyValuePair<string, string>("Vessel",             Dash(model.VesselName)),
				new KeyValuePair<string, string>("Location",           Dash(model.Location)),
				new KeyValuePair<string, string>("Sensor setup",       string.IsNullOrWhiteSpace(model.InputSetup) ? "\u2014" : model.InputSetup),
				new KeyValuePair<string, string>("Capture start",      Dash(model.StartTime)),
				new KeyValuePair<string, string>("Capture end",        Dash(model.EndTime)),
				new KeyValuePair<string, string>("Duration",           Dash(model.Duration)),
				new KeyValuePair<string, string>("Samples averaged",   model.SampleCount.ToString("N0", Ci)),
				new KeyValuePair<string, string>("Correction applied", model.HasCorrectionApplied
					? "Yes" + (string.IsNullOrWhiteSpace(model.CorrectionAppliedAt) ? string.Empty : " (" + model.CorrectionAppliedAt + ")")
					: "Not yet applied"),
			};

			InsertKeyValueTable(editor, rows);

			if (!string.IsNullOrWhiteSpace(model.Comments))
			{
				Paragraph(editor, "Notes", 11, ColorHeading, spacingBefore: 10, spacingAfter: 2, bold: true);
				Paragraph(editor, model.Comments, 10.5, ColorText, spacingAfter: 8);
			}
		}
'@

$ovNew = @'
		private static void WriteOverview(RadFixedDocumentEditor editor, VerificationReportModel model)
		{
			editor.InsertPageBreak();
			Heading(editor, "6. Session Overview");

			// Visual session info cards (900x180 GDI -> 681x136 PDF)
			if (model.SessionOverviewPng != null)
			{
				editor.ParagraphProperties.SpacingAfter = 10;
				InsertImage(editor, model.SessionOverviewPng, 681, 136);
			}

			var rows = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("Location",         Dash(model.Location)),
				new KeyValuePair<string, string>("Sensor setup",     string.IsNullOrWhiteSpace(model.InputSetup) ? "\u2014" : model.InputSetup),
				new KeyValuePair<string, string>("Duration",         Dash(model.Duration)),
				new KeyValuePair<string, string>("Samples averaged", model.SampleCount.ToString("N0", Ci)),
				new KeyValuePair<string, string>("Correction",       model.HasCorrectionApplied
					? "Applied" + (string.IsNullOrWhiteSpace(model.CorrectionAppliedAt) ? string.Empty : " (" + model.CorrectionAppliedAt + ")")
					: "Not yet applied"),
			};
			InsertKeyValueTable(editor, rows);

			if (!string.IsNullOrWhiteSpace(model.Comments))
			{
				Paragraph(editor, "Notes", 11, ColorHeading, spacingBefore: 10, spacingAfter: 2, bold: true);
				Paragraph(editor, model.Comments, 10.5, ColorText, spacingAfter: 8);
			}
		}
'@

if ($c.Contains($ovOld.Trim())) {
	Write-Host "WriteOverview: found"
	$c = $c.Replace($ovOld.Trim(), $ovNew.Trim())
} else {
	Write-Host "WriteOverview: NOT found - trying line-by-line search"
	$idx = $c.IndexOf('Paragraph(editor, OverviewSentence(model), 11, ColorText, spacingAfter: 10);')
	Write-Host "  OverviewSentence line at: $idx"
}

# ── Fix WriteFinalResults ─────────────────────────────────────────────────
$frOld = @'
		private static void WriteFinalResults(RadFixedDocumentEditor editor, VerificationReportModel model)
		{
			Heading(editor, "7. Results \u2014 Recommended Corrections");

			Paragraph(editor,
				"These are the orientation corrections the verification calculated for the vessel unit. " +
				"A correction is the offset that must be applied so the vessel unit matches the trusted reference.",
				10.5, ColorText, spacingAfter: 8);
'@

$frNew = @'
		private static void WriteFinalResults(RadFixedDocumentEditor editor, VerificationReportModel model)
		{
			Heading(editor, "7. Results \u2014 Recommended Corrections");

			// Hero correction cards (900x280 GDI -> 681x212 PDF)
			if (model.CorrectionCardsPng != null)
			{
				editor.ParagraphProperties.SpacingAfter = 12;
				InsertImage(editor, model.CorrectionCardsPng, 681, 212);
			}
'@

if ($c.Contains($frOld.Trim())) {
	Write-Host "WriteFinalResults intro: found"
	$c = $c.Replace($frOld.Trim(), $frNew.Trim())
} else {
	Write-Host "WriteFinalResults intro: NOT found - manual idx search"
	$idx = $c.IndexOf('"These are the orientation corrections')
	Write-Host "  Intro text at: $idx"
}

# ── Fix WriteCompliance ───────────────────────────────────────────────────
# Find the compliance section intro + existing confidence panel block and replace
$coOld = @'
			Paragraph(editor,
				"The table below compares the quality of the captured verification data against the " +
				"built-in thresholds. \u2018Acceptable\u2019 is the minimum required for reliable analysis; " +
				"\u2018Good\u2019 indicates the recommended level for high-confidence results.",
				10.5, ColorText, spacingAfter: 8);
'@

$coNew = @'
			// Compliance scorecards (900x200 GDI -> 681x151 PDF)
			if (model.ComplianceScorecardsPng != null)
			{
				editor.ParagraphProperties.SpacingAfter = 10;
				InsertImage(editor, model.ComplianceScorecardsPng, 681, 151);
			}
'@

if ($c.Contains($coOld.Trim())) {
	Write-Host "WriteCompliance intro: found"
	$c = $c.Replace($coOld.Trim(), $coNew.Trim())
} else {
	Write-Host "WriteCompliance intro: NOT found"
	$idx = $c.IndexOf('"The table below compares the quality')
	Write-Host "  Compliance intro at: $idx"
}

# Also remove/replace the old confidence panel block inside WriteCompliance (already shown separately now)
$cpOld = @'
			// \u2500\u2500 Data quality confidence panel \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
			if (model.ConfidencePanelPng != null)
			{
				editor.ParagraphProperties.SpacingAfter = 10;
				InsertImage(editor, model.ConfidencePanelPng, 681, 136);
			}
'@
$cpNew = @'
			// Data quality confidence panel (681x136)
			if (model.ConfidencePanelPng != null)
			{
				editor.ParagraphProperties.SpacingAfter = 10;
				InsertImage(editor, model.ConfidencePanelPng, 681, 136);
			}
'@
if ($c.Contains("Data quality confidence panel")) {
	Write-Host "ConfidencePanel block: found variant"
}

[System.IO.File]::WriteAllText($file, $c, $enc)
Write-Host "Done. Length: $($c.Length)"
