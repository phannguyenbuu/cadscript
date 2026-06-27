
$detailsDir = "D:\Dropbox\_Documents\_Vlance_2026\SteelHouse\details"
$accoreExe = "C:\Program Files\Autodesk\AutoCAD 2022\accoreconsole.exe"
$lspFile = "d:\Dropbox\VS Projects\ACadScript\scratch\conv.lsp"

if (-not (Test-Path $accoreExe)) {
    Write-Error "AutoCAD Console not found at $accoreExe"
    exit
}

$files = Get-ChildItem -Path $detailsDir -Filter "*.dwg"
foreach ($file in $files) {
    if ($file.Name -like "*tmp*" -or $file.Name -like "*dxf.dwg*" -or $file.Name -like "*probe*") { continue }
    
    Write-Host "----------------------------------------"
    Write-Host "Processing: $($file.Name)"
    
    # Create a temporary script file to load the LISP
    $scrFile = "d:\Dropbox\VS Projects\ACadScript\scratch\temp_task.scr"
    "(load ""$($lspFile.Replace('\','/'))"")" | Out-File $scrFile -Encoding ASCII
    
    # Run AutoCAD Console
    # /i input file, /s script file
    & $accoreExe /i "$($file.FullName)" /s "$scrFile"
    
    Write-Host "Finished: $($file.Name)"
}

Write-Host "----------------------------------------"
Write-Host "Batch conversion using AutoCAD Console complete."
