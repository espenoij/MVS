$path = "C:\Users\espen\source\repos\espenoij\MVS\MVS\Views\LivoxLidar\LivoxLidarPage.xaml"
$enc  = [System.Text.Encoding]::UTF8
$content = [System.IO.File]::ReadAllText($path, $enc)
$idx = $content.IndexOf("planeVisual")
$snippet = $content.Substring([Math]::Max(0,$idx-150), 300)
# Show hex values of first 300 chars around planeVisual
$bytes = $enc.GetBytes($snippet)
$hex = ($bytes | ForEach-Object { $_.ToString("X2") }) -join " "
Write-Host $hex
Write-Host "---TEXT---"
Write-Host $snippet
