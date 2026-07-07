# Start the SQLAgent API
$apiPath = "i:\CSE\Projects\Current\WEB\ai\SQLAgent\SQLAgent.API"
$env:ASPNETCORE_ENVIRONMENT = "Development"

Write-Host "Starting SQLAgent API..." -ForegroundColor Cyan
Write-Host "API will run at http://localhost:5000 and https://localhost:5001" -ForegroundColor Yellow
Write-Host "Swagger UI will be available at http://localhost:5000/swagger" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Green
Write-Host ""

Set-Location $apiPath
dotnet run --configuration Debug
