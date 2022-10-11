function Test-Integration
{
    param(
        [Parameter(Mandatory=$true)]
        [String] $Vendor,
        [Parameter(Mandatory=$true)]
        [String] $ConnectionString,
        [String] $Version,
        [String] $TargetFramework = "netcoreapp3.1",
        [Parameter(Mandatory=$true)]
        [String] $PackageSource,
        [String] $Verbosity = "minimal",
        [Parameter(Mandatory=$true)]
        [String] $TempPath
    )
    
    $allFrameworks = Get-TargetFrameworks
    
    if (-not ($allFrameworks.Contains($TargetFramework)))
    {
        Write-Host "Invalid target framework '$TargetFramework'." -ForegroundColor Red
        Exit -1
    }
    
    if (-not $Version)
    {
        [String[]]$nuget = Get-NuGetVersions -PackageSource $PackageSource
        
        if ($nuget.Length -eq 0)
        {
            Write-Host "No packages found in '$PackagesPath'" -ForegroundColor Red
            Exit -1
        }
        elseif ($nuget.Length -gt 1)
        {
            Write-Host "Multiple packages found in '$PackagesPath'." -ForegroundColor Red
            Write-Host "Please choose one with the -Version argument:" -ForegroundColor Red
            Write-Host $nuget
            Exit -1
        }
        else
        {
            $Version = $nuget[0]
        }
    }
    
    if (-not [System.IO.Path]::IsPathRooted($PackageSource))
    {
        $PackageSource = Resolve-Path $PackageSource
    }
    
    $package = Get-VendorPackage $Vendor
    
    if (-not $package)
    {
        Write-Host "Unknown vendor '$Vendor'"
        Exit -1
    }
    
    foreach ($targetFramework in $allFrameworks)
    {
        Clean-Source $Vendor $targetFramework $TempPath
        Prepare-Source $Vendor $TargetFramework $TempPath $PackageSource
        
        Install-Cli $Vendor $Version $TargetFramework $Verbosity $TempPath $PackageSource
        
        Prepare-Database $Vendor $ConnectionString $TargetFramework $TempPath
        Run-ProjectTest $Vendor $Version $ConnectionString $PackageSource $targetFramework $Verbosity $TempPath
        
        $success = Verify-Integration $Vendor $targetFramework $TempPath
        
        if (-not $success) { break }
    }
}

function Get-TargetFrameworks
{
    $tfm = @("netcoreapp3.1", "net6.0")
    
    if ($IsWindows)
    {
        $tfm += , "net472"
    }
    
    return $tfm
}

function Verify-Integration
{
    param(
        [Parameter(Mandatory=$true)]
        [String] $Vendor,
        [String] $TargetFramework,
        [String] $TempPath
    )
    
    if ($TargetFramework)
    {
        $path = Join-Path (Get-TempPath $Vendor $TargetFramework $TempPath) "results.txt"
    
        if (Test-Path $path) { ((Get-Content $path) -eq "OK") }
        else { $false }
    }
    else
    {
        foreach ($targetFramework in Get-TargetFrameworks)
        {
            $result = (Verify-Integration $Vendor $targetFramework $TempPath)
            
            if (-not $result) { return $false }
        }
        
        return $true
    }
}

function Clean-Source
{
    param(
        [String] $Vendor,
        [String] $TargetFramework,
        [String] $TempPath
    )
    
    Write-Host "  Cleaning source ($TargetFramework)..." -ForegroundColor Cyan
    
    $path = Get-TempPath $Vendor $TargetFramework $TempPath
    
    if (Test-Path $path)
    {
        Remove-Item $path -Force -Recurse
    }
}

function Prepare-Source
{
    param(
        [String] $Vendor,
        [String] $TargetFramework,
        [String] $TempPath,
        [String] $PackageSource
    )
    
    Write-Host "  Preparing source ($TargetFramework)..." -ForegroundColor Cyan
    
    $source = Join-Path $PSScriptRoot ".\src"
    $temp = Join-Path $TempPath "$Vendor\$TargetFramework"
    $configFile = Join-Path $temp "nuget.config"

    New-Item $temp -ItemType Directory | Out-Null
    Copy-Item "$source\*" $temp -Recurse | Out-Null
    
    ((Get-Content -Path $configFile -Raw) -Replace '%PackageSource%', "$PackageSource") | Set-Content -Path $configFile
}

function Get-TempPath
{
    param(
        [String] $Vendor,
        [String] $TargetFramework,
        [String] $TempPath
    )
    
    Join-Path $TempPath "$Vendor\$TargetFramework"
}

function Install-Cli
{
    param(
        [String] $Vendor,
        [String] $Version,
        [String] $TargetFramework,
        [String] $Verbosity,
        [String] $TempPath,
        [String] $PackageSource
    )
    
    Write-Host "  Installing CLI ($TargetFramework)..." -ForegroundColor Cyan
    
    $toolPath = Get-TempPath $Vendor $TargetFramework $TempPath
    
    Push-Location $toolPath
    dotnet tool install --tool-path . dotnet-jerry --version $Version --verbosity $Verbosity --add-source "$PackageSource"
    Pop-Location
}

function Prepare-Database
{
    param(
        [String] $Vendor,
        [String] $ConnectionString,
        [String] $TargetFramework,
        [String] $TempPath
    )
    
    Write-Host "  Preparing database ($TargetFramework)..." -ForegroundColor Cyan

    $sql = Join-Path $PSScriptRoot "sql\prepare.$Vendor.sql"
    $toolPath = Get-TempPath $Vendor $TargetFramework $TempPath

    Push-Location $toolPath
    .\jerry run -v "$Vendor" -c "$ConnectionString" --file "$sql"
    if ($LastExitCode -ne 0) { Pop-Location; throw "Error running 'jerry run'." }
    Pop-Location
}

function Run-ProjectTest
{
    param(
        [String] $Vendor,
        [String] $Version,
        [String] $ConnectionString,
        [String] $PackageSource,
        [String] $TargetFramework,
        [String] $Verbosity,
        [String] $TempPath,
        [Switch] $TranspileWithCli
    )
    
    $projectPath = Join-Path (Get-TempPath $Vendor $TargetFramework $TempPath) "Jerrycurl.Test.Integration"
    $resultsPath = Join-Path (Get-TempPath $Vendor $TargetFramework $TempPath) "results.txt"
    $package = Get-VendorPackage $Vendor
    $constant = Get-VendorConstant $Vendor
    $buildArgs = @(
        "--framework",
        "$TargetFramework",
        "--verbosity",
        "$Verbosity",
        "--configuration",
        "Release",
        "-p:DefineConstants=$constant",
        "-p:DatabaseVendor=$Vendor"
    )
    
    if ($TranspileWithCli)
    {
        $buildArgs += "-p:JerrycurlUseCli=true", "-p:JerrycurlCliPath=..\jerry"
    }
    
    if ($TargetFramework -eq "net472")
    {
        $buildArgs += "-p:PlatformTarget=x64"
    }
    
    Push-Location $projectPath
    Write-Host "  Building project ($TargetFramework)..." -ForegroundColor Cyan
    dotnet add package Jerrycurl --version $Version
    dotnet add package $package --version $Version
    ..\jerry scaffold -v $Vendor -c $ConnectionString -ns "Jerrycurl.Test.Integration.Database" --verbose
    if ($LastExitCode -eq 0)
    {
        dotnet build @buildArgs
    }
    Write-Host "  Running code..." -ForegroundColor Cyan
    if ($LastExitCode -eq 0)
    {
        dotnet run --no-build --framework "$TargetFramework" --verbosity "$Verbosity" --configuration Release "$ConnectionString" "$resultsPath"
    }
    Pop-Location
}

function Get-NuGetVersions
{
    param(
        [String] $PackageSource
    )
    
    $versions = @()
    
    if (Test-Path $PackageSource)
    {
        foreach ($file in (Get-ChildItem "$PackageSource\*.nupkg"))
        {
            $nuget = Parse-NuGetString $file.Name
            
            if ($nuget.Package -eq "Jerrycurl")
            {
                $versions += $nuget.Version
            }
        }
    }

    $versions
}

function Get-VendorConstant
{
    param(
        [String] $Vendor
    )
    
    if ($Vendor -eq "sqlserver") { $constant = "VENDOR_SQLSERVER" }
    if ($Vendor -eq "postgres")  { $constant = "VENDOR_POSTGRES" }
    if ($Vendor -eq "oracle")    { $constant = "VENDOR_ORACLE" }
    if ($Vendor -eq "mysql")     { $constant = "VENDOR_MYSQL" }
    if ($Vendor -eq "sqlite")    { $constant = "VENDOR_SQLITE" }
    
    $constant
}

function Get-VendorPackage
{
    param(
        [String] $Vendor
    )
    
    if ($Vendor -eq "sqlserver") { $package = "Jerrycurl.Vendors.SqlServer" }
    if ($Vendor -eq "postgres")  { $package = "Jerrycurl.Vendors.Postgres" }
    if ($Vendor -eq "oracle")    { $package = "Jerrycurl.Vendors.Oracle" }
    if ($Vendor -eq "mysql")     { $package = "Jerrycurl.Vendors.MySql" }
    if ($Vendor -eq "sqlite")    { $package = "Jerrycurl.Vendors.Sqlite" }
    
    $package
}

function Parse-NuGetString
{
    param(
        [Parameter(Mandatory=$true)]
        [string] $InputString
    )
    

    $match = [Regex]::Match($InputString, '^([^\d]+)\.(\d+\.\d+\.\d+.*?)(\.nupkg)?$')
    
    if ($match.Success)
    {
        $package = $match.Groups[1].Value
        $version = $match.Groups[2].Value
        
        @{
            Package = $package
            Version = $version
        }
    }
}

function Set-Connection
{
    param(
        [Parameter(Mandatory=$true)]
        [String] $Vendor,
        [String] $ConnectionString
    )
    
    $variable = Get-ConnectionVariable -Vendor $Vendor
    
    if ($variable)
    {
        [Environment]::SetEnvironmentVariable($variable, $ConnectionString, "User")
        [Environment]::SetEnvironmentVariable($variable, $ConnectionString)
    }
}

function Get-Connection
{
    param(
        [Parameter(Mandatory=$true)]
        [String] $Vendor
    )
    
    $variable = Get-ConnectionVariable -Vendor $Vendor
    
    if ($variable)
    {
        $value = [Environment]::GetEnvironmentVariable($variable, "Machine")
        
        if (-not $value) { $value = [Environment]::GetEnvironmentVariable($variable, "User") }
        if (-not $value) { $value = [Environment]::GetEnvironmentVariable($variable) }
        
        $value
    }
}

function Get-VendorMonikers
{
    @("sqlite", "sqlserver", "postgres", "oracle", "mysql")
}

function Get-ConnectionVariable
{
    param(
        [String] $Vendor
    )
    
    if ($Vendor -eq "sqlserver") { "JERRY_SQLSERVER_CONN" }
    if ($Vendor -eq "postgres")  { "JERRY_POSTGRES_CONN" }
    if ($Vendor -eq "oracle")    { "JERRY_ORACLE_CONN" }
    if ($Vendor -eq "mysql")     { "JERRY_MYSQL_CONN" }
    if ($Vendor -eq "sqlite")    { "JERRY_SQLITE_CONN" }
}