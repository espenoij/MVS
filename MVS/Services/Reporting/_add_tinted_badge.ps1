param([string]$file)
$c = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)

# Insert DrawTintedBadge right after the DrawPillBadge closing brace
$pbSig   = '        private static void DrawPillBadge'
$pbStart = $c.IndexOf($pbSig)
$pbOpen  = $c.IndexOf('{', $pbStart); $depth=0; $pbEnd=$pbOpen
for ($i=$pbOpen; $i -lt $c.Length; $i++) {
	if ($c[$i] -eq '{') { $depth++ }
	elseif ($c[$i] -eq '}') { $depth--; if ($depth -eq 0) { $pbEnd=$i; break } }
}
Write-Host "DrawPillBadge: $pbStart -> $pbEnd"

$drawTinted = "`r`n`r`n        /// <summary>`r`n        /// Draws a tinted status badge with spec-compliant SES brand background/text colors.`r`n        /// SUCCESS: #DFF8EE bg / #1E7A54 text  WARNING: #FFF3D6 bg / #B87C00 text`r`n        /// FAIL: #FDE0E0 bg / #B42318 text`r`n        /// </summary>`r`n        private static void DrawTintedBadge(Graphics g, int x, int y, int w, int h, Color accentColor, string text)`r`n        {`r`n            double luma = (accentColor.R * 0.299 + accentColor.G * 0.587 + accentColor.B * 0.114) / 255.0;`r`n            bool isGreen  = accentColor.G > 180 && accentColor.R < 100;`r`n            bool isAmber  = accentColor.R > 200 && accentColor.G > 100 && accentColor.B < 80;`r`n            bool isRed    = accentColor.R > 180 && accentColor.G < 80;`r`n`r`n            Color bgColor, fgColor;`r`n            if (isGreen)`r`n            {`r`n                bgColor = Color.FromArgb(0xDF, 0xF8, 0xEE);`r`n                fgColor = Color.FromArgb(0x1E, 0x7A, 0x54);`r`n            }`r`n            else if (isAmber)`r`n            {`r`n                bgColor = Color.FromArgb(0xFF, 0xF3, 0xD6);`r`n                fgColor = Color.FromArgb(0xB8, 0x7C, 0x00);`r`n            }`r`n            else if (isRed)`r`n            {`r`n                bgColor = Color.FromArgb(0xFD, 0xE0, 0xE0);`r`n                fgColor = Color.FromArgb(0xB4, 0x23, 0x18);`r`n            }`r`n            else`r`n            {`r`n                bgColor = Color.FromArgb(0xF2, 0xF5, 0xF7);`r`n                fgColor = ColorNeutralDark;`r`n            }`r`n`r`n            using (var bgBrush = new SolidBrush(bgColor))`r`n                g.FillRectangle(bgBrush, x, y, w, h);`r`n            using (var font    = new Font(""Segoe UI"", 7.5f, FontStyle.Bold))`r`n            using (var fgBrush = new SolidBrush(fgColor))`r`n            using (var sf     = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })`r`n                g.DrawString(text, font, fgBrush, new RectangleF(x, y, w, h), sf);`r`n        }"

$before = $c.Substring(0, $pbEnd + 1)
$after  = $c.Substring($pbEnd + 1)
$c = $before + $drawTinted + $after
Write-Host "DrawTintedBadge added. Len=$($c.Length)"
[System.IO.File]::WriteAllText($file, $c, [System.Text.Encoding]::UTF8)
Write-Host "Written."
