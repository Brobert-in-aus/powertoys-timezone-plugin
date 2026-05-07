param (
    [ValidateSet("ARM64", "x64")]
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

$projectName = "Community.PowerToys.Run.Plugin.TimeZone"
$pluginName = "TimeZone"
$solution = Join-Path $PSScriptRoot "TimeZoneConverter.slnx"
$project = Join-Path $PSScriptRoot $projectName
$csproj = Join-Path $project "$projectName.csproj"

[xml]$projectXml = Get-Content -LiteralPath $csproj
$targetFramework = $projectXml.Project.PropertyGroup.TargetFramework

dotnet build $solution -c Release /p:Platform=$Platform

$source = Join-Path $project "bin\$Platform\Release\$targetFramework"
$destination = Join-Path $env:LOCALAPPDATA "Microsoft\PowerToys\PowerToys Run\Plugins\$pluginName"
$hostDependencies = @(
    "PowerToys.Common.UI.*",
    "PowerToys.ManagedCommon.*",
    "PowerToys.Settings.UI.Lib.*",
    "Wox.Infrastructure.*",
    "Wox.Plugin.*"
)

Get-Process -Name "PowerToys.PowerLauncher" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "PowerToys" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

if (Test-Path -LiteralPath $destination) {
    Remove-Item -LiteralPath $destination -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $destination | Out-Null
Copy-Item -Path (Join-Path $source "*") -Destination $destination -Recurse -Force -Exclude $hostDependencies

Write-Output "Deployed $pluginName to $destination"
