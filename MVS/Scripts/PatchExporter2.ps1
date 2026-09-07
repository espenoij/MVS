param([string]$file)
$enc = [System.Text.Encoding]::UTF8
$c   = [System.IO.File]::ReadAllText($file, $enc)

# ── WriteOverview ─────────────────────────────────────────────────────────
# Remove OverviewSentence paragraph and expand the key-value table
$ovSentenceOld = 'Paragraph(editor, OverviewSentence(model), 11, ColorText, spacingAfter: 10);'
$ovSentenceNew = @'
// Visual session info cards (900x180 GDI -> 681x136 PDF)
			if (model.SessionOverviewPng != null)
			{
				editor.ParagraphProperties.SpacingAfter = 10;
				InsertImage(editor, model.SessionOverviewPng, 681, 136);
			}
'@

$idx = $c.IndexOf($ovSentenceOld)
if ($idx -ge 0) {
	$c = $c.Substring(0, $idx) + $ovSentenceNew.TrimStart() + $c.Substring($idx + $ovSentenceOld.Length)
	Write-Host "WriteOverview OverviewSentence: replaced"
} else { Write-Host "WriteOverview sentence NOT found" }

# Shrink the key-value table rows (remove project/operator/vessel/start/end rows that are now in the visual card)
$oldRows = @'
				new KeyValuePair<string, string>("Project",            model.ProjectName),
				new KeyValuePair<string, string>("Operator",           Dash(model.Operator)),
				new KeyValuePair<string, string>("Vessel",             Dash(model.VesselName)),
				new KeyValuePair<string, string>("Location",           Dash(model.Location)),
				new KeyValuePair<string, string>("Sensor setup",       string.IsNullOrWhiteSpace(model.InputSetup) ? "
'@
# Use a more targeted find: just find the beginning of the rows list
$rowsStart = $c.IndexOf('new KeyValuePair<string, string>("Project",')
$rowsEnd   = $c.IndexOf('new KeyValuePair<string, string>("Correction applied",')
if ($rowsStart -ge 0 -and $rowsEnd -ge 0) {
	$newRows = @'
				new KeyValuePair<string, string>("Location",         Dash(model.Location)),
				new KeyValuePair<string, string>("Sensor setup",     string.IsNullOrWhiteSpace(model.InputSetup) ? "\u2014" : model.InputSetup),
				new KeyValuePair<string, string>("Duration",         Dash(model.Duration)),
				new KeyValuePair<string, string>("Samples averaged", model.SampleCount.ToString("N0", Ci)),
				new KeyValuePair<string, string>("Correction",       model.HasCorrectionApplied
					? "Applied" + (string.IsNullOrWhiteSpace(model.CorrectionAppliedAt) ? string.Empty : " (" + model.CorrectionAppliedAt + ")")
					: "Not yet applied"),
'@
	# Find the end of the "Correction applied" row
	$corrEnd = $c.IndexOf(': "Not yet applied"),', $rowsEnd) + ': "Not yet applied"),'.Length
	$c = $c.Substring(0, $rowsStart) + $newRows.TrimStart() + $c.Substring($corrEnd)
	Write-Host "WriteOverview rows: replaced (rowsStart=$rowsStart rowsEnd=$rowsEnd corrEnd=$corrEnd)"
} else { Write-Host "WriteOverview rows NOT found (rowsStart=$rowsStart rowsEnd=$rowsEnd)" }

# Also remove the blank line before InsertKeyValueTable that was between rows and InsertKeyValueTable
$c = $c -replace '(?m)^\s*\r?\n(\s*InsertKeyValueTable\(editor, rows\);)', '            InsertKeyValueTable(editor, rows);'

# ── WriteFinalResults ─────────────────────────────────────────────────────
$frOld = '"These are the orientation corrections the verification calculated for the vessel unit. " +'
$frInsert = @'
// Hero correction cards (900x280 GDI -> 681x212 PDF)
			if (model.CorrectionCardsPng != null)
			{
				editor.ParagraphProperties.SpacingAfter = 12;
				InsertImage(editor, model.CorrectionCardsPng, 681, 212);
			}


'@

$idx = $c.IndexOf($frOld)
if ($idx -ge 0) {
	# Go back to the 'Paragraph(' call start
	$paraStart = $c.LastIndexOf('Paragraph(editor,', $idx)
	# Find end of that paragraph call
	$paraEnd = $c.IndexOf(');', $idx) + 2
	$c = $c.Substring(0, $paraStart) + $frInsert.TrimStart() + $c.Substring($paraEnd)
	Write-Host "WriteFinalResults paragraph: replaced"
} else { Write-Host "WriteFinalResults paragraph NOT found" }

# ── WriteCompliance ───────────────────────────────────────────────────────
$compIntroOld = '"The table below compares the quality of the captured verification data against the " +'
$compInsert = @'
// Compliance scorecards (900x200 GDI -> 681x151 PDF)
			if (model.ComplianceScorecardsPng != null)
			{
				editor.ParagraphProperties.SpacingAfter = 10;
				InsertImage(editor, model.ComplianceScorecardsPng, 681, 151);
			}


'@

$idx = $c.IndexOf($compIntroOld)
if ($idx -ge 0) {
	$paraStart = $c.LastIndexOf('Paragraph(editor,', $idx)
	$paraEnd   = $c.IndexOf(');', $idx) + 2
	$c = $c.Substring(0, $paraStart) + $compInsert.TrimStart() + $c.Substring($paraEnd)
	Write-Host "WriteCompliance intro paragraph: replaced"
} else { Write-Host "WriteCompliance intro NOT found" }

[System.IO.File]::WriteAllText($file, $c, $enc)
Write-Host "Done. Length=$($c.Length)"
Write-Host "Has SessionOverviewPng insert: $($c.Contains('SessionOverviewPng'))"
Write-Host "Has CorrectionCardsPng insert: $($c.Contains('CorrectionCardsPng'))"
Write-Host "Has ComplianceScorecardsPng insert: $($c.Contains('ComplianceScorecardsPng'))"
