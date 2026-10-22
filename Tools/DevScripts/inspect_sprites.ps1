Add-Type -AssemblyName System.Drawing

$samples = @(
    "AMESR\Assets\Sprites\Characters\player_anim_swim_thrust_01.png",
    "AMESR\Assets\Sprites\Characters\player_anim_idle_01.png",
    "AMESR\Assets\Sprites\Characters\character_npc_001.png",
    "AMESR\Assets\Sprites\Creatures\alien_creature_001.png",
    "AMESR\Assets\Sprites\Items\quest_artifact_001.png"
)

foreach ($s in $samples) {
    if (Test-Path $s) {
        $bmp = [System.Drawing.Bitmap]::FromFile($s)
        $c00 = $bmp.GetPixel(0, 0)
        $cMid = $bmp.GetPixel([int]($bmp.Width / 2), [int]($bmp.Height / 2))
        Write-Output "$s | Size: $($bmp.Width)x$($bmp.Height) | Corner(0,0): R=$($c00.R),G=$($c00.G),B=$($c00.B),A=$($c00.A) | Mid: R=$($cMid.R),G=$($cMid.G),B=$($cMid.B)"
        $bmp.Dispose()
    }
}
