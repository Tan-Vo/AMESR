$assets = Join-Path $PSScriptRoot "Assets\Sprites"

$report = Get-ChildItem -Directory $assets | ForEach-Object {
    $dir = $_.FullName
    $pngs = (Get-ChildItem -Path $dir -Filter *.png).Count
    $jpgs = (Get-ChildItem -Path $dir -Filter *.jpg).Count
    $metas = (Get-ChildItem -Path $dir -Filter *.meta).Count
    $all = (Get-ChildItem -Path $dir).Count
    
    $missingMetas = 0
    Get-ChildItem -Path $dir -Include *.png, *.jpg -Recurse | ForEach-Object {
        $mf = $_.FullName + ".meta"
        if (-not (Test-Path $mf) -or (Get-Item $mf).Length -eq 0) {
            $missingMetas++
        }
    }

    [PSCustomObject]@{
        Category       = $_.Name
        PNG_Sprites    = $pngs
        Master_Sheets  = $jpgs
        Meta_Files     = $metas
        Missing_Metas  = $missingMetas
        Total_Files    = $all
    }
}

$report | Format-Table -AutoSize

$totalPng = ($report | Measure-Object -Property PNG_Sprites -Sum).Sum
$totalMaster = ($report | Measure-Object -Property Master_Sheets -Sum).Sum
$totalMeta = ($report | Measure-Object -Property Meta_Files -Sum).Sum
$totalFiles = ($report | Measure-Object -Property Total_Files -Sum).Sum
$totalMissing = ($report | Measure-Object -Property Missing_Metas -Sum).Sum

Write-Host "=========================================================="
Write-Host "GRAND TOTAL INDIVIDUAL PNG SPRITES : $totalPng"
Write-Host "MASTER SPRITE SHEETS / MATTES     : $totalMaster"
Write-Host "TOTAL VALID UNITY .META FILES     : $totalMeta"
Write-Host "TOTAL ASSET FILES IN PROJECT      : $totalFiles"
Write-Host "MISSING / CORRUPTED METAS         : $totalMissing"
Write-Host "=========================================================="
