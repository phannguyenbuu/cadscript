
$detailsDir = "D:\Dropbox\_Documents\_Vlance_2026\SteelHouse\details"
$accoreExe = "C:\Program Files\Autodesk\AutoCAD 2022\accoreconsole.exe"
$lspFile = "d:\Dropbox\VS Projects\ACadScript\scratch\conv_dwf_final.lsp"

if (-not (Test-Path $accoreExe)) {
    Write-Error "AutoCAD Console not found at $accoreExe"
    exit
}

$files = Get-ChildItem -Path $detailsDir -Filter "*.dwg"
foreach ($file in $files) {
    if ($file.Name -like "*tmp*" -or $file.Name -like "*probe*") { continue }
    
    Write-Host "----------------------------------------"
    Write-Host "Processing: $($file.Name)"
    
    # Create a temporary script file to load the LISP
    $scrFile = "d:\Dropbox\VS Projects\ACadScript\scratch\temp_task.scr"
    "(load ""$($lspFile.Replace('\','/'))"")" | Out-File $scrFile -Encoding ASCII
    
    # Run AutoCAD Console
    # Use -NoProfile to avoid loading unnecessary stuff if possible (though accoreconsole doesn't support it directly)
    & $accoreExe /i "$($file.FullName)" /s "$scrFile"
    
    $dwfFile = Join-Path $detailsDir "$($file.BaseName).dwf"
    if (Test-Path $dwfFile) {
        $size = (Get-Item $dwfFile).Length
        Write-Host "SUCCESS: $($file.BaseName).dwf created ($size bytes)." -ForegroundColor Green
    } else {
        Write-Warning "FAILED: $($file.BaseName).dwf not found."
    }
}

Write-Host "----------------------------------------"
Write-Host "Batch conversion to DWF complete."
