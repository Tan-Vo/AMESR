$srcCode = Get-Content "AMESR\BatchTransparentAndAlign.cs" -Raw
Add-Type -TypeDefinition $srcCode -ReferencedAssemblies System.Drawing

Add-Type -AssemblyName System.Drawing

$sheetPath = "AMESR\Assets\Sprites\Characters\player_astronaut_spritesheet.jpg"
$sheet = [System.Drawing.Bitmap]::FromFile($sheetPath)

# Idle 1: X=390, Y=28, W=85, H=150
$rect = [System.Drawing.Rectangle]::new(390, 28, 85, 150)
$crop = $sheet.Clone($rect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$dest = "AMESR\Assets\Sprites\Characters\test_idle_t18.png"
$crop.Save($dest, [System.Drawing.Imaging.ImageFormat]::Png)
$crop.Dispose()
$sheet.Dispose()

[BatchTransparentAndAlign]::ProcessSingleSprite($dest, 20, 6, 128)
Write-Host "Generated test_idle_t18.png with threshold 20"
