
$detailsDir = "D:\Dropbox\_Documents\_Vlance_2026\SteelHouse\details"
$acadDir = "C:\Program Files\Autodesk\AutoCAD 2022"

# Add AutoCAD dir to path for native DLLs (essential for side-database dependencies)
$env:PATH = "$acadDir;$env:PATH"

$acdbPath = Join-Path $acadDir "acdbmgd.dll"
$accorePath = Join-Path $acadDir "accoremgd.dll"

Write-Host "Loading DLLs from $acadDir..."
try {
    # We must use LoadFrom to ensure dependencies in the same folder are found
    [Reflection.Assembly]::LoadFrom($acdbPath) | Out-Null
    [Reflection.Assembly]::LoadFrom($accorePath) | Out-Null
} catch {
    Write-Error "Failed to load AutoCAD DLLs: $_"
    exit
}

$files = Get-ChildItem -Path $detailsDir -Filter "*.dwg"
foreach ($file in $files) {
    # Skip temporary files, probe files, or existing DXFs
    if ($file.Name -like "*tmp*" -or $file.Name -like "*dxf.dwg*" -or $file.Name -like "*probe*") { continue }
    
    Write-Host "Processing $($file.Name)..."
    try {
        # Initialize Side Database
        $db = New-Object Autodesk.AutoCAD.DatabaseServices.Database($false, $true)
        $db.ReadDwgFile($file.FullName, [System.IO.FileShare]::ReadWrite, $true, "")
        
        $textContent = ""
        $diameter = 0
        
        $tr = $db.TransactionManager.StartTransaction()
        try {
            $bt = $tr.GetObject($db.BlockTableId, [Autodesk.AutoCAD.DatabaseServices.OpenMode]::ForRead)
            $btr = $tr.GetObject($bt[[Autodesk.AutoCAD.DatabaseServices.BlockTableRecord]::ModelSpace], [Autodesk.AutoCAD.DatabaseServices.OpenMode]::ForRead)
            
            # Find Text and Circles
            foreach ($id in $btr) {
                if ($textContent -eq "" -and ($id.ObjectClass.DxfName -eq "TEXT" -or $id.ObjectClass.DxfName -eq "MTEXT")) {
                    $txt = $tr.GetObject($id, [Autodesk.AutoCAD.DatabaseServices.OpenMode]::ForRead)
                    $c = ""
                    if ($id.ObjectClass.DxfName -eq "TEXT") { $c = $txt.TextString } else { $c = $txt.Contents }
                    
                    if (![string]::IsNullOrWhiteSpace($c) -and $c.Length -lt 50 -and !($c -like "*%%*")) {
                        # Basic cleanup for MText formatting codes
                        $textContent = $c -replace "\\P", "_" -replace "\{[^}]*\}", "" -replace "\\[^;]*;", ""
                        $textContent = $textContent.Trim()
                    }
                }
                
                if ($diameter -eq 0 -and $id.ObjectClass.DxfName -eq "CIRCLE") {
                    $cir = $tr.GetObject($id, [Autodesk.AutoCAD.DatabaseServices.OpenMode]::ForRead)
                    $diameter = $cir.Radius * 2
                }
                
                # Stop if both found
                if ($textContent -ne "" -and $diameter -gt 0) { break }
            }
            
            $tr.Commit()
        } catch {
            Write-Warning "Transaction error: $_"
        } finally {
            $tr.Dispose()
        }
        
        # Suffix construction
        $cleanText = $textContent -replace '[\\\/:*?"<>| ]', '_'
        $suffix = ""
        if ($cleanText) { $suffix += "_$cleanText" }
        if ($diameter -gt 0) { $suffix += "_D$([Math]::Round($diameter))" }
        
        if ($suffix -eq "") {
            Write-Host "  ! No text or circle found, skipping rename."
            $newName = Join-Path $detailsDir "$($file.BaseName).dxf"
        } else {
            $newName = Join-Path $detailsDir "$($file.BaseName)$suffix.dxf"
        }
        
        Write-Host "  -> Saving to $newName"
        if (Test-Path $newName) { Remove-Item $newName }
        
        # Save as DXF (AutoCAD 2018 format / AC1032 for 2022 compatibility)
        $db.DxfOut($newName, 16, [Autodesk.AutoCAD.DatabaseServices.DwgVersion]::Current)
        
        $db.Dispose()
    } catch {
        Write-Error "Error processing $($file.Name): $_"
    }
}
Write-Host "Batch conversion complete."
