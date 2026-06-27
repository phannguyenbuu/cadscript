# AutoCAD Plugin Deployment Script
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$zipFile = Join-Path $scriptPath "deploy.zip"
$destDir = $scriptPath

Write-Host "Deploying AutoCAD Plugin binaries..." -ForegroundColor Cyan

if (-not (Test-Path $zipFile)) {
    Write-Error "deploy.zip not found in the current directory!"
    exit 1
}

try {
    # Extract zip file, overwriting existing files
    Expand-Archive -Path $zipFile -DestinationPath $destDir -Force
    Write-Host "Deployment completed successfully! Binaries extracted to:" -ForegroundColor Green
    Write-Host "  $destDir\bin\2021" -ForegroundColor Yellow
    Write-Host "  $destDir\bin\assemblies" -ForegroundColor Yellow
} catch {
    Write-Error "Failed to extract binaries: $_"
}
