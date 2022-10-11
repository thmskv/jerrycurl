#Requires -Version 5.0

[CmdletBinding(PositionalBinding=$false)]
param(
    [String] $PackageSource,
    [String] $BuildPath,
    [String] $Verbosity = "minimal"
)

. (Join-Path $PSScriptRoot .\dotfile.ps1)

if (-not $PackageSource) { $PackageSource = Join-Path $PSScriptRoot "..\..\artifacts\packages" }
if (-not $BuildPath)     { $BuildPath     = Join-Path $PSScriptRoot "..\..\artifacts\integration" }

Set-Connection "sqlite" ("DATA SOURCE=" + (Join-Path $BuildPath "sqlite\int.db"))

foreach ($vendor in Get-VendorMonikers)
{
    $connectionString = Get-Connection -Vendor $vendor
    
    Write-Host ""
    Write-Host "   Testing '$vendor'..."
    
    if ($connectionString)
    {
        Test-Integration -Vendor $vendor -Connection $connectionString -PackageSource $PackageSource -Verbosity $Verbosity -TempPath $BuildPath
        
        if (Verify-Integration -Vendor $vendor -TempPath $BuildPath)
        {
            Write-Host "   Integration success." -ForegroundColor Green
        }
        else
        {
            Write-Host "   Result could not be verified." -ForegroundColor Red
            Exit -1
        }
    }
    else
    {
        Write-Host "   Skipped. Connection not configured." -ForegroundColor Yellow
    }
}

Set-Connection "sqlite" $null
