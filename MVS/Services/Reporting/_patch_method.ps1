param(
	[string]$file,
	[string]$methodSignatureStart,
	[string]$replacementFile
)

$content = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)

$start = $content.IndexOf($methodSignatureStart)
if ($start -lt 0) { Write-Error "Signature not found: $methodSignatureStart"; exit 1 }

$afterSig = $content.IndexOf('{', $start)
$depth = 0; $end = $afterSig
for ($i = $afterSig; $i -lt $content.Length; $i++) {
	if ($content[$i] -eq '{') { $depth++ } elseif ($content[$i] -eq '}') { $depth--; if ($depth -eq 0) { $end = $i; break } }
}

$newMethod = [System.IO.File]::ReadAllText($replacementFile, [System.Text.Encoding]::UTF8)

$before = $content.Substring(0, $start)
$after  = $content.Substring($end + 1)
$newContent = $before + $newMethod + $after
[System.IO.File]::WriteAllText($file, $newContent, [System.Text.Encoding]::UTF8)
Write-Host "Patched '$methodSignatureStart'. New size: $($newContent.Length)"
