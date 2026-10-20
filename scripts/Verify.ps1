param([string]$DotnetPath = 'dotnet')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$output = Join-Path $projectRoot 'verification'
$restoreConfig = Join-Path $projectRoot 'NuGet.Config'
New-Item -ItemType Directory -Force $output | Out-Null
& $DotnetPath run --project (Join-Path $projectRoot 'tests\TenHours.Tests\TenHours.Tests.csproj') -c Release "-p:RestoreConfigFile=$restoreConfig" -- $output
if ($LASTEXITCODE -ne 0) { throw 'Core regression tests failed' }
& $DotnetPath build (Join-Path $projectRoot 'src\TenHours.App\TenHours.App.csproj') -c Release "-p:RestoreConfigFile=$restoreConfig"
if ($LASTEXITCODE -ne 0) { throw 'WinForms build failed' }
# Running the DLL via this SDK also supports a project-local SDK installation.
& $DotnetPath (Join-Path $projectRoot 'src\TenHours.App\bin\Release\net10.0-windows\TenHours.dll') --verify $output
if ($LASTEXITCODE -ne 0) { throw "UI probe failed; inspect $output" }
Get-Content (Join-Path $output 'ui-report.json')
