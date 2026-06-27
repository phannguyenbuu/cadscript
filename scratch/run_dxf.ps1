
$detailsDir = "D:\Dropbox\_Documents\_Vlance_2026\SteelHouse\details"
$accoreExe  = "C:\Program Files\Autodesk\AutoCAD 2022\accoreconsole.exe"
$lspFile    = "d:\Dropbox\VS Projects\ACadScript\scratch\conv_dxf_final.lsp"
$scrFile    = "d:\Dropbox\VS Projects\ACadScript\scratch\temp_dxf.scr"

if (-not (Test-Path $accoreExe)) {
    Write-Error "AutoCAD Console not found at $accoreExe"; exit
}

# Pre-write the script file (same LISP for every file)
"(load ""$($lspFile.Replace('\','/'))"")" | Out-File $scrFile -Encoding ASCII

$files   = Get-ChildItem -Path $detailsDir -Filter "*.dwg"
$ok      = 0
$failed  = @()

foreach ($file in $files) {
    if ($file.Name -like "*tmp*" -or $file.Name -like "*probe*") { continue }

    Write-Host "----------------------------------------"
    Write-Host "Processing: $($file.Name)"

    & $accoreExe /i "$($file.FullName)" /s "$scrFile"

    $dxfFile = Join-Path $detailsDir "$($file.BaseName).dxf"
    if (Test-Path $dxfFile) {
        $size = (Get-Item $dxfFile).Length
        Write-Host "  OK  -> $($file.BaseName).dxf  ($size bytes)" -ForegroundColor Green
        $ok++
    } else {
        Write-Warning "  FAIL -> $($file.BaseName).dxf not found"
        $failed += $file.Name
    }
}

Write-Host "========================================"
Write-Host "Done: $ok succeeded, $($failed.Count) failed."
if ($failed.Count -gt 0) { Write-Warning "Failed: $($failed -join ', ')" }
