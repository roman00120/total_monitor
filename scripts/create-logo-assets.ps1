$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$sourcePath = Join-Path $root 'Total-Monitor.png'
$appLogoDir = Join-Path $root 'src\TotalMonitor.App\Assets\Logo'
$serverLogoDir = Join-Path $root 'src\TotalMonitor.Server\Assets\Logo'
New-Item -ItemType Directory -Force -Path $appLogoDir,$serverLogoDir | Out-Null
Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $appLogoDir 'Total-Monitor.png') -Force

$source = [System.Drawing.Image]::FromFile($sourcePath)
$sizes = @(16,32,48,64,128,256)
$pngFiles = @()
foreach ($size in $sizes) {
    $bitmap = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $scale = [Math]::Min($size / $source.Width, $size / $source.Height)
    $width = [int][Math]::Round($source.Width * $scale)
    $height = [int][Math]::Round($source.Height * $scale)
    $x = [int](($size - $width) / 2)
    $y = [int](($size - $height) / 2)
    $graphics.DrawImage($source, $x, $y, $width, $height)
    $pngPath = Join-Path $appLogoDir ("logo-{0}.png" -f $size)
    $bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()
    $pngFiles += $pngPath
}
$source.Dispose()

$icoPath = Join-Path $appLogoDir 'TotalMonitor.ico'
$stream = [System.IO.File]::Create($icoPath)
$writer = New-Object System.IO.BinaryWriter($stream)
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]$pngFiles.Count)
$offset = 6 + (16 * $pngFiles.Count)
$entries = @()
foreach ($pngPath in $pngFiles) {
    $bytes = [System.IO.File]::ReadAllBytes($pngPath)
    $entries += ,@($pngPath,$bytes,$offset)
    $offset += $bytes.Length
    $pixelSize = [int]([System.IO.Path]::GetFileNameWithoutExtension($pngPath).Replace('logo-',''))
    $widthByte = if ($pixelSize -eq 256) { [byte]0 } else { [byte]$pixelSize }
    $writer.Write($widthByte)
    $writer.Write($widthByte)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]$bytes.Length)
    $writer.Write([UInt32]($offset - $bytes.Length))
}
foreach ($entry in $entries) { $writer.Write([byte[]]$entry[1]) }
$writer.Dispose()
$stream.Dispose()
Copy-Item -LiteralPath $icoPath -Destination (Join-Path $serverLogoDir 'TotalMonitor.ico') -Force
Write-Host "Logo assets generated from $sourcePath"
