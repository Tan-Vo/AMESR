$srcCode = Get-Content "AMESR\BatchTransparentAndAlign.cs" -Raw
Add-Type -TypeDefinition $srcCode -ReferencedAssemblies System.Drawing

$testWep = "AMESR\Assets\Sprites\TestTransparent\test_wep_t36.png"
Copy-Item "AMESR\Assets\Sprites\Weapons\weapon_arsenal_001.png" $testWep -Force

[BatchTransparentAndAlign]::ProcessSingleSprite($testWep, 36, 8, 96)
Write-Host "Processed test_wep_t36.png"
