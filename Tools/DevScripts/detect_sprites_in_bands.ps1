Add-Type -AssemblyName System.Drawing

$img = [System.Drawing.Bitmap]::FromFile("AMESR\Assets\Sprites\Characters\player_astronaut_spritesheet.jpg")
$w = $img.Width
$h = $img.Height
$bg = $img.GetPixel(10, 10)

$bands = @(
    @{ Name = "idle"; Y1 = 22; Y2 = 205 },
    @{ Name = "swim"; Y1 = 213; Y2 = 308 },
    @{ Name = "aim";  Y1 = 327; Y2 = 456 },
    @{ Name = "o2";   Y1 = 466; Y2 = 605 },
    @{ Name = "wait"; Y1 = 609; Y2 = 751 }
)

foreach ($b in $bands) {
    $y1 = $b.Y1
    $y2 = $b.Y2
    $colHas = New-Object bool[] $w
    for ($x = 0; $x -lt $w; $x++) {
        $count = 0
        for ($y = $y1; $y -lt $y2; $y += 2) {
            $c = $img.GetPixel($x, $y)
            $dist = [Math]::Sqrt([Math]::Pow($c.R - $bg.R, 2) + [Math]::Pow($c.G - $bg.G, 2) + [Math]::Pow($c.B - $bg.B, 2))
            if ($dist -gt 35) { $count++ }
        }
        if ($count -gt 2) { $colHas[$x] = $true }
    }

    $sprites = @()
    $inS = $false
    $startX = 0
    for ($x = 0; $x -lt $w; $x++) {
        if ($colHas[$x] -and -not $inS) {
            $inS = $true
            $startX = $x
        } elseif (-not $colHas[$x] -and $inS) {
            $inS = $false
            # Skip left text if startX < 240
            if ($startX -ge 240 -and ($x - $startX) -gt 25) {
                $sprites += [PSCustomObject]@{ X1 = $startX; X2 = $x; W = ($x - $startX) }
            }
        }
    }
    Write-Output "=== Band: $($b.Name) (Count: $($sprites.Count)) ==="
    $sprites | Format-Table -AutoSize
}

$img.Dispose()
