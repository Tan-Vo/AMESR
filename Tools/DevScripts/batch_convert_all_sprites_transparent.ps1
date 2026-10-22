$srcCode = Get-Content "AMESR\BatchTransparentAndAlign.cs" -Raw
Add-Type -TypeDefinition $srcCode -ReferencedAssemblies System.Drawing

Add-Type -AssemblyName System.Drawing

$assetsDir = "AMESR\Assets\Sprites"

$categories = @(
    @{ Dir = "Characters";  Pattern = "character_npc_*.png"; Threshold = 36; Canvas = 128 },
    @{ Dir = "Characters";  Pattern = "player_suit_*.png";   Threshold = 36; Canvas = 128 },
    @{ Dir = "Creatures";   Pattern = "alien_creature_*.png";Threshold = 36; Canvas = 128 },
    @{ Dir = "Creatures";   Pattern = "creature_action_*.png";Threshold = 36; Canvas = 128 },
    @{ Dir = "Dishes";      Pattern = "dish_space_*.png";    Threshold = 36; Canvas = 96 },
    @{ Dir = "Dishes";      Pattern = "cooking_prep_*.png";  Threshold = 36; Canvas = 96 },
    @{ Dir = "Furniture";   Pattern = "furniture_prop_*.png";Threshold = 36; Canvas = 96 },
    @{ Dir = "Gear";        Pattern = "gear_tech_*.png";     Threshold = 36; Canvas = 96 },
    @{ Dir = "Vehicles";    Pattern = "vehicle_craft_*.png"; Threshold = 36; Canvas = 128 },
    @{ Dir = "Items";       Pattern = "item_*.png";          Threshold = 36; Canvas = 96 },
    @{ Dir = "Items";       Pattern = "quest_artifact_*.png";Threshold = 36; Canvas = 96 },
    @{ Dir = "Weapons";     Pattern = "weapon_*.png";        Threshold = 36; Canvas = 96 },
    @{ Dir = "VFX";         Pattern = "vfx_*.png";           Threshold = 36; Canvas = 96 },
    @{ Dir = "VFX";         Pattern = "boss_hazard_*.png";   Threshold = 36; Canvas = 96 },
    @{ Dir = "UI_Icons";    Pattern = "ui_icon_*.png";       Threshold = 36; Canvas = 96 },
    @{ Dir = "Bosses";      Pattern = "mini_boss_*.png";     Threshold = 36; Canvas = 128 }
)

$totalProcessed = 0

foreach ($cat in $categories) {
    $targetDir = Join-Path $assetsDir $cat.Dir
    if (Test-Path $targetDir) {
        $files = Get-ChildItem -Path $targetDir -Filter $cat.Pattern
        Write-Host "Processing $($files.Count) files in $($cat.Dir) ($($cat.Pattern))..."
        foreach ($f in $files) {
            [BatchTransparentAndAlign]::ProcessSingleSprite($f.FullName, $cat.Threshold, 8, $cat.Canvas)
            $totalProcessed++
        }
    }
}

Write-Host "=========================================================="
Write-Host "SUCCESS: Automatically removed backgrounds and centered"
Write-Host "Total PNG sprites converted to 100% transparent: $totalProcessed"
Write-Host "=========================================================="
