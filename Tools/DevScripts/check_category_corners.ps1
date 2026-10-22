Add-Type -AssemblyName System.Drawing

$testItems = @(
    "AMESR\Assets\Sprites\Dishes\dish_space_001.png",
    "AMESR\Assets\Sprites\Furniture\furniture_prop_001.png",
    "AMESR\Assets\Sprites\Gear\gear_tech_001.png",
    "AMESR\Assets\Sprites\Vehicles\vehicle_craft_001.png",
    "AMESR\Assets\Sprites\Items\quest_artifact_001.png",
    "AMESR\Assets\Sprites\Weapons\weapon_arsenal_001.png",
    "AMESR\Assets\Sprites\VFX\boss_hazard_001.png",
    "AMESR\Assets\Sprites\UI_Icons\ui_icon_001.png",
    "AMESR\Assets\Sprites\Bosses\mini_boss_001.png"
)

foreach ($f in $testItems) {
    if (Test-Path $f) {
        $bmp = [System.Drawing.Bitmap]::FromFile($f)
        $c = $bmp.GetPixel(1, 1)
        Write-Output "$f | Size: $($bmp.Width)x$($bmp.Height) | Corner(1,1): R=$($c.R), G=$($c.G), B=$($c.B), A=$($c.A)"
        $bmp.Dispose()
    }
}
