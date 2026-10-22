Add-Type -AssemblyName System.Drawing

$p = "AMESR\Assets\Sprites\Characters\player_astronaut_spritesheet.jpg"
if (Test-Path $p) {
    $bmp = [System.Drawing.Bitmap]::FromFile($p)
    Write-Output "player_astronaut_spritesheet.jpg: $($bmp.Width) x $($bmp.Height)"
    $bmp.Dispose()
} else {
    Write-Output "NOT FOUND"
}
