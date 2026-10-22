Add-Type -AssemblyName System.Drawing

$img = [System.Drawing.Bitmap]::FromFile("AMESR\Assets\Sprites\Characters\player_astronaut_spritesheet.jpg")
# Check pixels around x=430, y=140..170 (astronaut 1 legs)
for ($y = 120; $y -le 180; $y += 5) {
    $c = $img.GetPixel(435, $y)
    Write-Output "Y=$y | R=$($c.R), G=$($c.G), B=$($c.B)"
}
$img.Dispose()
