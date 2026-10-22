$srcCode = Get-Content "AMESR\BatchTransparentAndAlign.cs" -Raw
Add-Type -TypeDefinition $srcCode -ReferencedAssemblies System.Drawing

Add-Type -AssemblyName System.Drawing

$sheetPath = "AMESR\Assets\Sprites\Characters\player_astronaut_spritesheet.jpg"
$outDir = "AMESR\Assets\Sprites\Characters"
$sheet = [System.Drawing.Bitmap]::FromFile($sheetPath)

# 1. Idle (Zero-Gravity) - 4 frames (Y: 26 to 180, H: 154)
$idleXs = @(
    @{ X = 390; W = 85 },
    @{ X = 560; W = 85 },
    @{ X = 725; W = 85 },
    @{ X = 895; W = 85 }
)
$idx = 1
foreach ($item in $idleXs) {
    $rect = [System.Drawing.Rectangle]::new($item.X, 26, $item.W, 154)
    $crop = $sheet.Clone($rect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $dest = "$outDir\player_anim_idle_0$idx.png"
    $crop.Save($dest, [System.Drawing.Imaging.ImageFormat]::Png)
    $crop.Dispose()
    [BatchTransparentAndAlign]::ProcessSingleSprite($dest, 22, 6, 128)
    $idx++
}
Write-Host "Processed 4 flawless transparent Idle frames!"

# 2. Swim Thrusting Forward - 6 frames (Y: 215 to 313, H: 98)
$swimXs = @(
    @{ X = 85;   W = 195 },
    @{ X = 295;  W = 195 },
    @{ X = 505;  W = 195 },
    @{ X = 710;  W = 195 },
    @{ X = 920;  W = 195 },
    @{ X = 1130; W = 195 }
)
$idx = 1
foreach ($item in $swimXs) {
    $rect = [System.Drawing.Rectangle]::new($item.X, 215, $item.W, 98)
    $crop = $sheet.Clone($rect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $dest = "$outDir\player_anim_swim_thrust_0$idx.png"
    $crop.Save($dest, [System.Drawing.Imaging.ImageFormat]::Png)
    $crop.Dispose()
    [BatchTransparentAndAlign]::ProcessSingleSprite($dest, 22, 6, 192)
    $idx++
}
Write-Host "Processed 6 flawless transparent Swim Thrust frames!"

# 3. Aiming & Firing - 5 frames (Y: 325 to 460, H: 135)
$aimXs = @(
    @{ X = 160;  W = 160 },
    @{ X = 350;  W = 160 },
    @{ X = 545;  W = 335 },
    @{ X = 945;  W = 170 },
    @{ X = 1145; W = 170 }
)
$idx = 1
foreach ($item in $aimXs) {
    $rect = [System.Drawing.Rectangle]::new($item.X, 325, $item.W, 135)
    $crop = $sheet.Clone($rect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $dest = "$outDir\player_anim_harpoon_aim_0$idx.png"
    $crop.Save($dest, [System.Drawing.Imaging.ImageFormat]::Png)
    $crop.Dispose()
    [BatchTransparentAndAlign]::ProcessSingleSprite($dest, 22, 6, 200)
    $idx++
}
Write-Host "Processed 5 flawless transparent Aim & Fire frames!"

# 4. Low Oxygen Panic - 4 frames (Y: 480 to 605, H: 125)
$o2Xs = @(
    @{ X = 355; W = 95 },
    @{ X = 535; W = 100 },
    @{ X = 718; W = 100 },
    @{ X = 905; W = 100 }
)
$idx = 1
foreach ($item in $o2Xs) {
    $rect = [System.Drawing.Rectangle]::new($item.X, 480, $item.W, 125)
    $crop = $sheet.Clone($rect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $dest = "$outDir\player_anim_low_o2_panic_0$idx.png"
    $crop.Save($dest, [System.Drawing.Imaging.ImageFormat]::Png)
    $crop.Dispose()
    [BatchTransparentAndAlign]::ProcessSingleSprite($dest, 22, 6, 128)
    $idx++
}
Write-Host "Processed 4 flawless transparent Low O2 frames!"

# 5. Restaurant Mode (Waiter Run) - 7 frames (Y: 615 to 755, H: 140)
$waitXs = @(
    @{ X = 220;  W = 135 },
    @{ X = 385;  W = 135 },
    @{ X = 550;  W = 135 },
    @{ X = 715;  W = 130 },
    @{ X = 865;  W = 140 },
    @{ X = 1035; W = 135 },
    @{ X = 1195; W = 145 }
)
$idx = 1
foreach ($item in $waitXs) {
    $rect = [System.Drawing.Rectangle]::new($item.X, 615, $item.W, 140)
    $crop = $sheet.Clone($rect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $dest = "$outDir\player_anim_waiter_mode_0$idx.png"
    $crop.Save($dest, [System.Drawing.Imaging.ImageFormat]::Png)
    $crop.Dispose()
    [BatchTransparentAndAlign]::ProcessSingleSprite($dest, 22, 6, 144)
    $idx++
}
Write-Host "Processed 7 flawless transparent Restaurant Waiter frames!"

$sheet.Dispose()
