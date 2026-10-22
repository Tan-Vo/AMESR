Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
Add-Type -AssemblyName System.Drawing

$targetDir = "d:\TÂN\GAME MOI\AMESR\Assets\Sprites\DaveTheDiver_Fish"
if (-not (Test-Path $targetDir)) { New-Item -ItemType Directory -Path $targetDir -Force | Out-Null }

$tempDir = "d:\TÂN\GAME MOI\AMESR\temp_fish_cache"
if (-not (Test-Path $tempDir)) { New-Item -ItemType Directory -Path $tempDir -Force | Out-Null }

Write-Host "1. Fetching fish list from Dave the Diver Wiki API..."
$parseUrl = "https://dave-the-diver.fandom.com/api.php?action=parse&page=Fish&format=json"
$parseRes = Invoke-RestMethod -Uri $parseUrl -UserAgent "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"
$images = $parseRes.parse.images | Where-Object { $_ -match "Thumbnail|Fish|Shark|Eel|Crab|Jellyfish|Seahorse|Ray|Tuna|Squid|Octopus" }

Write-Host "Found $($images.Count) candidate fish images."

# Batch query image URLs (up to 40 titles per batch)
$batchSize = 40
$urlMap = @{}

for ($i = 0; $i -lt $images.Count; $i += $batchSize) {
    $endIdx = [Math]::Min($i + $batchSize - 1, $images.Count - 1)
    $currentBatch = $images[$i..$endIdx]
    $titlesParam = ($currentBatch | ForEach-Object { "File:$_" }) -join "|"
    $queryUrl = "https://dave-the-diver.fandom.com/api.php?action=query&titles=$([System.Uri]::EscapeDataString($titlesParam))&prop=imageinfo&iiprop=url&format=json"
    
    try {
        $queryRes = Invoke-RestMethod -Uri $queryUrl -UserAgent "Mozilla/5.0"
        if ($queryRes.query.pages) {
            foreach ($prop in $queryRes.query.pages.PSObject.Properties) {
                $p = $prop.Value
                if ($p.imageinfo -and $p.imageinfo.Count -gt 0) {
                    $cleanTitle = $p.title -replace "^File:", "" -replace " ", "_"
                    $urlMap[$cleanTitle] = $p.imageinfo[0].url
                }
            }
        }
    } catch {
        Write-Warning "Error querying batch at index $i"
    }
}

Write-Host "Retrieved URLs for $($urlMap.Count) images."

# 2. Download and Convert to 32bpp PNG
$convertedImages = @()
$index = 1

foreach ($imgName in $images) {
    $cleanKey = $imgName -replace " ", "_"
    if (-not $urlMap.ContainsKey($cleanKey)) { continue }
    
    $url = $urlMap[$cleanKey]
    $tempFile = Join-Path $tempDir "temp_$index.bin"
    $pngFile = Join-Path $targetDir ("fish_{0:D3}_{1}.png" -f $index, ($cleanKey -replace "_Thumbnail\.png", "" -replace "\.png", ""))
    
    try {
        Invoke-WebRequest -Uri $url -OutFile $tempFile -UserAgent "Mozilla/5.0" -TimeoutSec 15
        
        # Decode with WIC (supports WebP and PNG seamlessly)
        $uri = New-Object System.Uri($tempFile)
        $decoder = [System.Windows.Media.Imaging.BitmapDecoder]::Create($uri, [System.Windows.Media.Imaging.BitmapCreateOptions]::None, [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
        $frame = $decoder.Frames[0]
        
        # Convert to FormattedBmp in PBgra32
        $convertedBmp = New-Object System.Windows.Media.Imaging.FormatConvertedBitmap($frame, [System.Windows.Media.PixelFormats]::Bgra32, $null, 0)
        
        # Save as standard PNG
        $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
        $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($convertedBmp))
        $fs = [System.IO.File]::OpenWrite($pngFile)
        $encoder.Save($fs)
        $fs.Close()
        
        # Create standard Unity .meta for individual png
        $metaPath = "$pngFile.meta"
        if (-not (Test-Path $metaPath)) {
            $guid = [System.Guid]::NewGuid().ToString("N")
            $metaContent = @"
fileFormatVersion: 2
guid: $guid
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreservesCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 64
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbook: 0
  platformSettings: []
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
            [System.IO.File]::WriteAllText($metaPath, $metaContent)
        }
        
        $convertedImages += $pngFile
        if ($index % 25 -eq 0) {
            Write-Host "Processed $index / $($images.Count) fish..."
        }
        $index++
    } catch {
        Write-Warning "Failed processing $imgName"
    } finally {
        if (Test-Path $tempFile) { Remove-Item $tempFile -Force }
    }
}

Write-Host "Successfully converted $($convertedImages.Count) individual fish PNGs!"

# 3. Assemble Uniform 64x64 Grid Master Spritesheet
Write-Host "Assembling 64x64 Uniform Master Grid Spritesheet..."
$cellSize = 64
$cols = 16
$rows = [Math]::Ceiling($convertedImages.Count / $cols)
$sheetWidth = $cols * $cellSize
$sheetHeight = $rows * $cellSize

$masterBmp = New-Object System.Drawing.Bitmap($sheetWidth, $sheetHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gfx = [System.Drawing.Graphics]::FromImage($masterBmp)
$gfx.Clear([System.Drawing.Color]::Transparent)
$gfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$gfx.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half

for ($i = 0; $i -lt $convertedImages.Count; $i++) {
    $colIdx = $i % $cols
    $rowIdx = [Math]::Floor($i / $cols)
    
    $cellX = $colIdx * $cellSize
    $cellY = $rowIdx * $cellSize
    
    $srcPath = $convertedImages[$i]
    $srcImg = [System.Drawing.Image]::FromFile($srcPath)
    
    # Scale within cellSize while keeping aspect ratio
    $scale = [Math]::Min(($cellSize - 4) / $srcImg.Width, ($cellSize - 4) / $srcImg.Height)
    if ($scale -gt 1.0) { $scale = 1.0 }
    
    $drawW = [int]($srcImg.Width * $scale)
    $drawH = [int]($srcImg.Height * $scale)
    $drawX = $cellX + [int](($cellSize - $drawW) / 2)
    $drawY = $cellY + [int](($cellSize - $drawH) / 2)
    
    $gfx.DrawImage($srcImg, $drawX, $drawY, $drawW, $drawH)
    $srcImg.Dispose()
}

$gfx.Dispose()

$masterSheetPath = "d:\TÂN\GAME MOI\AMESR\Assets\Sprites\DaveTheDiver_Fish\dave_fish_master_spritesheet_64x64.png"
$masterBmp.Save($masterSheetPath, [System.Drawing.Imaging.ImageFormat]::Png)
$masterBmp.Dispose()

# Create Unity Meta for Master Spritesheet (SpriteMode Multiple, 64 PPU)
$masterMetaPath = "$masterSheetPath.meta"
$masterGuid = [System.Guid]::NewGuid().ToString("N")
$masterMetaContent = @"
fileFormatVersion: 2
guid: $masterGuid
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreservesCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 64
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbook: 0
  platformSettings: []
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
[System.IO.File]::WriteAllText($masterMetaPath, $masterMetaContent)

# Create folder .meta
$folderMetaPath = "$targetDir.meta"
if (-not (Test-Path $folderMetaPath)) {
    $folderGuid = [System.Guid]::NewGuid().ToString("N")
    [System.IO.File]::WriteAllText($folderMetaPath, "fileFormatVersion: 2`nguid: $folderGuid`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n")
}

# Cleanup temp
if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }

Write-Host "DONE! All fish saved to: $targetDir"
Write-Host "Master Spritesheet (64x64 grid): $masterSheetPath"
