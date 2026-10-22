$srcCode = Get-Content "AMESR\SpriteProcessor.cs" -Raw
Add-Type -TypeDefinition $srcCode -ReferencedAssemblies System.Drawing

$testFile = "AMESR\Assets\Sprites\Characters\player_anim_swim_thrust_01.png"
$outFile = "AMESR\Assets\Sprites\Characters\test_processed_swim_01.png"

[SpriteProcessor]::MakeTransparentAndCenter($testFile, $outFile, 128, 40, $false)

$bmp = [System.Drawing.Bitmap]::FromFile($outFile)
$c00 = $bmp.GetPixel(0, 0)
$cMid = $bmp.GetPixel([int]($bmp.Width / 2), [int]($bmp.Height / 2))
Write-Output "Processed: $($bmp.Width)x$($bmp.Height) | Corner Alpha=$($c00.A) | Center: R=$($cMid.R),G=$($cMid.G),B=$($cMid.B),A=$($cMid.A)"
$bmp.Dispose()
