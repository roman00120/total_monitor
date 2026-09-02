param()

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$sourcePath = Join-Path $root 'Total-Monitor.png'
$installerAssetsDir = Join-Path $root 'installer\assets'

if (-not (Test-Path $installerAssetsDir)) {
    New-Item -ItemType Directory -Force -Path $installerAssetsDir | Out-Null
}

$source = [System.Drawing.Image]::FromFile($sourcePath)
$origW = $source.Width
$origH = $source.Height

Write-Host "Source image dimensions: ${origW}x${origH}"

# 1. Large Centered Welcome Logo (e.g. 420px wide with high quality proportional height)
$targetWidth = 420
$targetHeight = [int][Math]::Round($origH * ($targetWidth / $origW))

$largeBitmap = New-Object System.Drawing.Bitmap($targetWidth, $targetHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($largeBitmap)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.Clear([System.Drawing.Color]::White)
$g.DrawImage($source, 0, 0, $targetWidth, $targetHeight)
$g.Dispose()

$welcomeBmpPath = Join-Path $installerAssetsDir 'WelcomeLogo.bmp'
$largeBitmap.Save($welcomeBmpPath, [System.Drawing.Imaging.ImageFormat]::Bmp)
$largeBitmap.Dispose()
Write-Host "Generated: $welcomeBmpPath (${targetWidth}x${targetHeight})"

# Also save transparent PNG version
$welcomePngPath = Join-Path $installerAssetsDir 'WelcomeLogo.png'
Copy-Item -LiteralPath $sourcePath -Destination $welcomePngPath -Force

# 2. Header Small Logo for subsequent pages (55x55 box, centered on white background)
$headerBoxSize = 58
$hScale = [Math]::Min($headerBoxSize / $origW, $headerBoxSize / $origH)
$hW = [int][Math]::Round($origW * $hScale)
$hH = [int][Math]::Round($origH * $hScale)
$hX = [int](($headerBoxSize - $hW) / 2)
$hY = [int](($headerBoxSize - $hH) / 2)

$headerBitmap = New-Object System.Drawing.Bitmap($headerBoxSize, $headerBoxSize, [System.Drawing.Imaging.PixelFormat]::Format32bppRgb)
$gH = [System.Drawing.Graphics]::FromImage($headerBitmap)
$gH.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gH.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$gH.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$gH.Clear([System.Drawing.Color]::White)
$gH.DrawImage($source, $hX, $hY, $hW, $hH)
$gH.Dispose()

$headerBmpPath = Join-Path $installerAssetsDir 'HeaderLogo.bmp'
$headerBitmap.Save($headerBmpPath, [System.Drawing.Imaging.ImageFormat]::Bmp)
$headerBitmap.Dispose()
Write-Host "Generated: $headerBmpPath (${headerBoxSize}x${headerBoxSize})"

# 3. WizardImageFile banner for modern/classic welcome page (164x314 left banner or full)
$bannerW = 164
$bannerH = 314
$bannerBmp = New-Object System.Drawing.Bitmap($bannerW, $bannerH, [System.Drawing.Imaging.PixelFormat]::Format32bppRgb)
$gB = [System.Drawing.Graphics]::FromImage($bannerBmp)
$gB.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gB.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$gB.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$gB.Clear([System.Drawing.Color]::White)

$bScale = [Math]::Min(($bannerW - 20) / $origW, ($bannerH - 80) / $origH)
$bW = [int][Math]::Round($origW * $bScale)
$bH = [int][Math]::Round($origH * $bScale)
$bX = [int](($bannerW - $bW) / 2)
$bY = [int](($bannerH - $bH) / 2) - 30

$gB.DrawImage($source, $bX, $bY, $bW, $bH)
$gB.Dispose()

$wizardImageBmpPath = Join-Path $installerAssetsDir 'WizardImage.bmp'
$bannerBmp.Save($wizardImageBmpPath, [System.Drawing.Imaging.ImageFormat]::Bmp)
$bannerBmp.Dispose()
Write-Host "Generated: $wizardImageBmpPath (${bannerW}x${bannerH})"

$source.Dispose()
Write-Host "All installer visual assets generated successfully!" -ForegroundColor Green
