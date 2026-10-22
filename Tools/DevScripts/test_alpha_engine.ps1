$src = Get-Content "AMESR\TransparentAlphaEngine.cs" -Raw
Add-Type -TypeDefinition $src -ReferencedAssemblies System.Drawing

$testFiles = @(
    "AMESR\Assets\Sprites\Characters\player_anim_swim_thrust_01.png",
    "AMESR\Assets\Sprites\Characters\character_npc_001.png",
    "AMESR\Assets\Sprites\Creatures\alien_creature_001.png"
)

$outDir = "AMESR\Assets\Sprites\TestTransparent"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

foreach ($f in $testFiles) {
    $baseName = [System.IO.Path]::GetFileName($f)
    $outPath = Join-Path $outDir $baseName
    [TransparentAlphaEngine]::ProcessAndAlign($f, $outPath, 128, 128, 42, $false, 6)
    
    $bmp = [System.Drawing.Bitmap]::FromFile($outPath)
    $c00 = $bmp.GetPixel(0, 0)
    $cMid = $bmp.GetPixel([int]($bmp.Width / 2), [int]($bmp.Height / 2))
    Write-Output "File: $baseName | Size: $($bmp.Width)x$($bmp.Height) | Corner Alpha: $($c00.A) | Center: R=$($cMid.R),G=$($cMid.G),B=$($cMid.B),A=$($cMid.A)"
    $bmp.Dispose()
}
