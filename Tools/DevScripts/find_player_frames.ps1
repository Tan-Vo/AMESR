Add-Type -AssemblyName System.Drawing

$img = [System.Drawing.Bitmap]::FromFile("AMESR\Assets\Sprites\Characters\player_astronaut_spritesheet.jpg")
$w = $img.Width
$h = $img.Height

# Background color is around pixel(10, 10)
$bg = $img.GetPixel(10, 10)
Write-Output "Image size: $w x $h | BG Color: R=$($bg.R), G=$($bg.G), B=$($bg.B)"

# Find horizontal projections (rows of text or sprites)
$rowHasContent = New-Object bool[] $h
for ($y = 0; $y -lt $h; $y++) {
    $nonBg = 0
    # check from x = 150 to w - 10 (skipping left labels like "Idle (Zero-Gravity)")
    for ($x = 180; $x -lt $w; $x += 4) {
        $c = $img.GetPixel($x, $y)
        $dist = [Math]::Sqrt([Math]::Pow($c.R - $bg.R, 2) + [Math]::Pow($c.G - $bg.G, 2) + [Math]::Pow($c.B - $bg.B, 2))
        if ($dist -gt 35) { $nonBg++ }
    }
    if ($nonBg -gt 5) { $rowHasContent[$y] = $true }
}

# Group into vertical row bands
$inBand = $false
$start = 0
$bands = @()
for ($y = 0; $y -lt $h; $y++) {
    if ($rowHasContent[$y] -and -not $inBand) {
        $inBand = $true
        $start = $y
    } elseif (-not $rowHasContent[$y] -and $inBand) {
        $inBand = $false
        if (($y - $start) -gt 30) {
            $bands += [PSCustomObject]@{ Y1 = $start; Y2 = $y; Height = ($y - $start) }
        }
    }
}

Write-Output "Found bands:"
$bands | Format-Table -AutoSize
$img.Dispose()
