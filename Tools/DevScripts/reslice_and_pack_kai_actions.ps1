Add-Type -AssemblyName System.Drawing

$srcPath = "d:\TÂN\GAME MOI\AMESR\Assets\Sprites\Characters\player_cosmic_actions_spritesheet.jpg"
if (-not (Test-Path $srcPath)) {
    $srcPath = "C:\Users\TEX\.gemini\antigravity\brain\b1cd6a19-ee1e-47d7-b9e9-8557f65bdb1b\player_cosmic_actions_spritesheet_1792553237423.jpg"
}

$actionsDir = "d:\TÂN\GAME MOI\AMESR\Assets\Sprites\Characters\Kai_Actions"
if (-not (Test-Path $actionsDir)) { New-Item -ItemType Directory -Path $actionsDir -Force | Out-Null }

$srcBmp = [System.Drawing.Bitmap]::FromFile($srcPath)
$srcW = $srcBmp.Width
$srcH = $srcBmp.Height

$cols = 8
$rows = 5
$cellW = $srcW / $cols
$cellH = $srcH / $rows

Write-Host "Source: $srcW x $srcH, Grid: $cols x $rows, Cell: $cellW x $cellH"

# Action names corresponding to the 5 rows
$actionNames = @(
    # Row 0: Idle, Swim, Harpoon Aim/Fire, Specimen, Laser
    "idle_front", "idle_three_quarter", "swim_forward_01", "swim_forward_02", "aim_harpoon", "fire_harpoon_recoil", "hold_specimen_jar", "swing_mining_laser",
    # Row 1: Directional swim, net, knife, heavy carry
    "dive_downward", "swim_upward", "swim_back_view", "net_catch_sweep", "plasma_knife_strike", "carry_heavy_crate", "carry_fish_01", "carry_fish_02",
    # Row 2: Booster, electrocution, hurt, panic
    "jetpack_boost_sprint", "jetpack_thrust_vertical", "electrocuted_shock_xray", "hurt_knockback", "oxygen_panic_gasp", "danger_evade", "shield_block", "wall_climb",
    # Row 3: Waiter, victory, radio, rest
    "waiter_carry_tray_01", "waiter_carry_tray_02", "waiter_serve_dish", "thumbs_up_victory", "radio_comms_call", "sit_rest_bench", "inspect_datapad", "celebrate_wave",
    # Row 4: Extra action poses
    "crawl_low_gravity", "drop_beacon", "place_trap", "taser_thrust", "overheat_vent", "scan_radar", "salute_fleet", "meditate_lumite"
)

# 1. Slice and remove dark background for each cell
$slicedBitmaps = @()

for ($r = 0; $r -lt $rows; $r++) {
    for ($c = 0; $c -lt $cols; $c++) {
        $idx = $r * $cols + $c
        $name = if ($idx -lt $actionNames.Count) { $actionNames[$idx] } else { "action_{0:D2}" -f $idx }
        
        $x0 = [int]($c * $cellW)
        $y0 = [int]($r * $cellH)
        $w = [int]$cellW
        $h = [int]$cellH
        
        # Crop cell
        $cellRect = New-Object System.Drawing.Rectangle($x0, $y0, $w, $h)
        $cellBmp = $srcBmp.Clone($cellRect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        
        # Remove dark background via color key / threshold
        # Sample borders to find background darkness
        for ($py = 0; $py -lt $h; $py++) {
            for ($px = 0; $px -lt $w; $px++) {
                $pixel = $cellBmp.GetPixel($px, $py)
                # Background in this sheet is dark grey/black (R, G, B all < 38)
                if ($pixel.R -lt 36 -and $pixel.G -lt 36 -and $pixel.B -lt 38) {
                    $cellBmp.SetPixel($px, $py, [System.Drawing.Color]::Transparent)
                }
                # Also trim subtle border line at edge of cells
                elseif (($px -lt 2 -or $px -ge $w - 2 -or $py -lt 2 -or $py -ge $h - 2) -and ($pixel.R -lt 55 -and $pixel.G -lt 55 -and $pixel.B -lt 55)) {
                    $cellBmp.SetPixel($px, $py, [System.Drawing.Color]::Transparent)
                }
            }
        }
        
        # Find tight bounding box of character inside cell
        $minX = $w; $maxX = 0; $minY = $h; $maxY = 0; $hasPixels = $false
        for ($py = 0; $py -lt $h; $py++) {
            for ($px = 0; $px -lt $w; $px++) {
                if ($cellBmp.GetPixel($px, $py).A -gt 20) {
                    if ($px -lt $minX) { $minX = $px }
                    if ($px -gt $maxX) { $maxX = $px }
                    if ($py -lt $minY) { $minY = $py }
                    if ($py -gt $maxY) { $maxY = $py }
                    $hasPixels = $true
                }
            }
        }
        
        if ($hasPixels -and ($maxX -ge $minX) -and ($maxY -ge $minY)) {
            $charW = $maxX - $minX + 1
            $charH = $maxY - $minY + 1
            $charRect = New-Object System.Drawing.Rectangle($minX, $minY, $charW, $charH)
            $tightBmp = $cellBmp.Clone($charRect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            $slicedBitmaps += @{ Name = $name; Bitmap = $tightBmp; Index = $idx }
        } else {
            $slicedBitmaps += @{ Name = $name; Bitmap = $cellBmp; Index = $idx }
        }
        $cellBmp.Dispose()
    }
}

Write-Host "Sliced $($slicedBitmaps.Count) action sprites."

# Save individual transparent action sprites
foreach ($item in $slicedBitmaps) {
    $outPath = Join-Path $actionsDir ("kai_{0:D2}_{1}.png" -f $item.Index, $item.Name)
    $item.Bitmap.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    
    # Create Unity .meta
    $metaPath = "$outPath.meta"
    if (-not (Test-Path $metaPath)) {
        $guid = [System.Guid]::NewGuid().ToString("N")
        $meta = @"
fileFormatVersion: 2
guid: $guid
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  isReadable: 0
  textureFormat: 1
  maxTextureSize: 1024
  textureSettings:
    filterMode: 0
  spriteMode: 1
  spritePixelsToUnits: 64
  alphaIsTransparency: 1
  textureType: 8
"@
        [System.IO.File]::WriteAllText($metaPath, $meta)
    }
}

# 2. Build 64x64 Uniform Master Grid Spritesheet
Write-Host "Building 64x64 Uniform Master Grid..."
$outCell64 = 64
$outW64 = $cols * $outCell64
$outH64 = $rows * $outCell64
$master64 = New-Object System.Drawing.Bitmap($outW64, $outH64, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gfx64 = [System.Drawing.Graphics]::FromImage($master64)
$gfx64.Clear([System.Drawing.Color]::Transparent)
$gfx64.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$gfx64.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half

# 3. Build 128x128 Uniform Master Grid Spritesheet (High-Def)
$outCell128 = 128
$outW128 = $cols * $outCell128
$outH128 = $rows * $outCell128
$master128 = New-Object System.Drawing.Bitmap($outW128, $outH128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gfx128 = [System.Drawing.Graphics]::FromImage($master128)
$gfx128.Clear([System.Drawing.Color]::Transparent)
$gfx128.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$gfx128.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half

for ($i = 0; $i -lt $slicedBitmaps.Count; $i++) {
    $cIdx = $i % $cols
    $rIdx = [Math]::Floor($i / $cols)
    $bmp = $slicedBitmaps[$i].Bitmap
    
    # 64x64 placing
    $scale64 = [Math]::Min(($outCell64 - 4) / $bmp.Width, ($outCell64 - 4) / $bmp.Height)
    $dw64 = [int]($bmp.Width * $scale64)
    $dh64 = [int]($bmp.Height * $scale64)
    $dx64 = $cIdx * $outCell64 + [int](($outCell64 - $dw64) / 2)
    $dy64 = $rIdx * $outCell64 + [int](($outCell64 - $dh64) / 2)
    $gfx64.DrawImage($bmp, $dx64, $dy64, $dw64, $dh64)
    
    # 128x128 placing
    $scale128 = [Math]::Min(($outCell128 - 8) / $bmp.Width, ($outCell128 - 8) / $bmp.Height)
    $dw128 = [int]($bmp.Width * $scale128)
    $dh128 = [int]($bmp.Height * $scale128)
    $dx128 = $cIdx * $outCell128 + [int](($outCell128 - $dw128) / 2)
    $dy128 = $rIdx * $outCell128 + [int](($outCell128 - $dh128) / 2)
    $gfx128.DrawImage($bmp, $dx128, $dy128, $dw128, $dh128)
}

$gfx64.Dispose()
$gfx128.Dispose()

$path64 = "d:\TÂN\GAME MOI\AMESR\Assets\Sprites\Characters\kai_actions_master_grid_64x64.png"
$master64.Save($path64, [System.Drawing.Imaging.ImageFormat]::Png)
$master64.Dispose()

$path128 = "d:\TÂN\GAME MOI\AMESR\Assets\Sprites\Characters\kai_actions_master_grid_128x128.png"
$master128.Save($path128, [System.Drawing.Imaging.ImageFormat]::Png)
$master128.Dispose()

# Create Metas for both grid sheets with SpriteMode Multiple
foreach ($p in @($path64, $path128)) {
    $metaP = "$p.meta"
    $g = [System.Guid]::NewGuid().ToString("N")
    $metaTxt = @"
fileFormatVersion: 2
guid: $g
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  isReadable: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    filterMode: 0
  spriteMode: 2
  spritePixelsToUnits: 64
  alphaIsTransparency: 1
  textureType: 8
"@
    [System.IO.File]::WriteAllText($metaP, $metaTxt)
}

# Create folder .meta
$actionsFolderMeta = "$actionsDir.meta"
if (-not (Test-Path $actionsFolderMeta)) {
    $fguid = [System.Guid]::NewGuid().ToString("N")
    [System.IO.File]::WriteAllText($actionsFolderMeta, "fileFormatVersion: 2`nguid: $fguid`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n")
}

$srcBmp.Dispose()
Write-Host "SUCCESS! Generated Kai action sprites:"
Write-Host " - 64x64 Grid Sheet: $path64 (512x320 px, 8x5 cells of 64x64)"
Write-Host " - 128x128 Grid Sheet: $path128 (1024x640 px, 8x5 cells of 128x128)"
Write-Host " - 40 Individual transparent PNGs in: $actionsDir"
