# reset-db.ps1
$ErrorActionPreference = "Stop"

Write-Host "Dropping database..." -ForegroundColor Yellow
dotnet ef database drop --force

Write-Host "Deleting Migrations folder..." -ForegroundColor Yellow
if (Test-Path "Migrations") {
    Remove-Item -Recurse -Force "Migrations"
}

Write-Host "Adding InitialCreate migration..." -ForegroundColor Yellow
dotnet ef migrations add InitialCreate

Write-Host "Updating database..." -ForegroundColor Yellow
dotnet ef database update

Write-Host "Done!" -ForegroundColor Green