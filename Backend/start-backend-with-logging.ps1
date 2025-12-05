# Run this script to start the backend with detailed logging
# This will show you exactly what's happening when the favorite toggle fails

Write-Host "Starting backend with detailed logging..." -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

cd D:\iti\Final\airbnb-clone\Backend\PL

# Set environment to Development to enable logging
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Run the backend
dotnet run

# Keep window open if there's an error
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Backend exited with error code: $LASTEXITCODE" -ForegroundColor Red
    Write-Host "Press any key to close..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}
