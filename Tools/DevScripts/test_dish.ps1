$srcCode = Get-Content "AMESR\BatchTransparentAndAlign.cs" -Raw
Add-Type -TypeDefinition $srcCode -ReferencedAssemblies System.Drawing

$testDish = "AMESR\Assets\Sprites\TestTransparent\test_dish_001.png"
Copy-Item "AMESR\Assets\Sprites\Dishes\dish_space_001.png" $testDish -Force

[BatchTransparentAndAlign]::ProcessSingleSprite($testDish, 25, 6, 96)
Write-Host "Processed test_dish_001.png"
