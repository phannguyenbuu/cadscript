
$detailsDir  = "D:\Dropbox\_Documents\_Vlance_2026\SteelHouse\details"
$accoreExe   = "C:\Program Files\Autodesk\AutoCAD 2022\accoreconsole.exe"
$lsp1        = "d:\Dropbox\VS Projects\ACadScript\scratch\phase1_export_acad.lsp"
$lsp2        = "d:\Dropbox\VS Projects\ACadScript\scratch\phase2_dxfout.lsp"
$scr1        = "d:\Dropbox\VS Projects\ACadScript\scratch\temp_p1.scr"
$scr2        = "d:\Dropbox\VS Projects\ACadScript\scratch\temp_p2.scr"

"(load ""$($lsp1.Replace('\','/'))"")" | Out-File $scr1 -Encoding ASCII
"(load ""$($lsp2.Replace('\','/'))"")" | Out-File $scr2 -Encoding ASCII

$origFiles = Get-ChildItem -Path $detailsDir -Filter "*.dwg" |
             Where-Object { $_.Name -notlike "*_ACAD*" -and $_.Name -notlike "*tmp*" -and $_.Name -notlike "*probe*" }

# ===== PHASE 1: EXPORTTOAUTOCAD =====
Write-Host "========================================"
Write-Host "PHASE 1: Exporting to clean ACAD DWG..."
Write-Host "========================================"
foreach ($f in $origFiles) {
    Write-Host "  P1: $($f.Name)"
    & $accoreExe /i "$($f.FullName)" /s "$scr1"
    $acadFile = Join-Path $detailsDir "$($f.BaseName)_ACAD.dwg"
    if (Test-Path $acadFile) {
        Write-Host "    -> $($f.BaseName)_ACAD.dwg OK" -ForegroundColor Green
    } else {
        Write-Warning "    -> $($f.BaseName)_ACAD.dwg NOT FOUND"
    }
}

# ===== PHASE 2: DXFOUT on clean files =====
Write-Host ""
Write-Host "========================================"
Write-Host "PHASE 2: Exporting clean DWG -> DXF..."
Write-Host "========================================"
$ok = 0; $failed = @()
$acadFiles = Get-ChildItem -Path $detailsDir -Filter "*_ACAD.dwg"
foreach ($f in $acadFiles) {
    Write-Host "  P2: $($f.Name)"
    & $accoreExe /i "$($f.FullName)" /s "$scr2"
    $cleanBase = $f.BaseName -replace "_ACAD$", ""
    $dxfFile = Join-Path $detailsDir "$cleanBase.dxf"
    if (Test-Path $dxfFile) {
        $sz = (Get-Item $dxfFile).Length
        Write-Host "    -> $cleanBase.dxf OK ($sz bytes)" -ForegroundColor Green
        $ok++
    } else {
        Write-Warning "    -> $cleanBase.dxf FAILED"
        $failed += $f.Name
    }
}

# ===== CLEANUP =====
Write-Host ""
Write-Host "Cleaning up _ACAD.dwg files..."
Get-ChildItem -Path $detailsDir -Filter "*_ACAD.dwg" | Remove-Item -Force

Write-Host "========================================"
Write-Host "Done: $ok / $($acadFiles.Count) DXF files created."
if ($failed.Count -gt 0) { Write-Warning "Failed: $($failed -join ', ')" }
