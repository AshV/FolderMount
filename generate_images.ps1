Add-Type -AssemblyName System.Drawing
$iconPath = 'a:\GitHub\FolderMount\FolderMount\Assets\icon.ico'
$outputDir = 'a:\GitHub\FolderMount\FolderMount.Package\Images'

if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

try {
    $icon = New-Object System.Drawing.Icon($iconPath)
    $bmp = $icon.ToBitmap()
} catch {
    Write-Host "Failed to load icon using ToBitmap. Trying Image.FromFile."
    $bmp = [System.Drawing.Image]::FromFile($iconPath)
}

function GenerateImage($width, $height, $fileName) {
    $canvas = New-Object System.Drawing.Bitmap($width, $height)
    $g = [System.Drawing.Graphics]::FromImage($canvas)
    $g.Clear([System.Drawing.Color]::Transparent)
    
    $ratioX = $width / $bmp.Width
    $ratioY = $height / $bmp.Height
    $ratio = [Math]::Min($ratioX, $ratioY)
    if ($ratio -gt 1) { $ratio = 1 } # Prevent excessive upscaling if not desired, but here we might need to, let's keep it 1 to avoid pixelation, or maybe we just scale it.
    
    $newWidth = [int]($bmp.Width * $ratio)
    $newHeight = [int]($bmp.Height * $ratio)
    
    $posX = ($width - $newWidth) / 2
    $posY = ($height - $newHeight) / 2
    
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($bmp, $posX, $posY, $newWidth, $newHeight)
    
    $outPath = Join-Path $outputDir $fileName
    $canvas.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    
    $g.Dispose()
    $canvas.Dispose()
    Write-Host "Generated $outPath"
}

GenerateImage 50 50 'StoreLogo.png'
GenerateImage 150 150 'Square150x150Logo.png'
GenerateImage 44 44 'Square44x44Logo.png'
GenerateImage 310 150 'Wide310x150Logo.png'
GenerateImage 620 300 'SplashScreen.png'

$bmp.Dispose()
if ($icon) { $icon.Dispose() }
