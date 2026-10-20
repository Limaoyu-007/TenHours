param(
    [ValidateSet('win-x64', 'win-arm64')][string]$Runtime = 'win-x64',
    [string]$DotnetPath = 'dotnet'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$output = Join-Path $projectRoot "artifacts\$Runtime"
$restoreConfig = Join-Path $projectRoot 'NuGet.Config'
& $DotnetPath publish (Join-Path $projectRoot 'src\TenHours.App\TenHours.App.csproj') `
    -c Release -r $Runtime --self-contained true -o $output `
    "-p:RestoreConfigFile=$restoreConfig" `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishReadyToRun=true -p:DebugType=None -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
$binary = Get-Item (Join-Path $output 'TenHours.exe')
[pscustomobject]@{
    Exe = $binary.FullName
    Bytes = $binary.Length
    SHA256 = (Get-FileHash $binary.FullName -Algorithm SHA256).Hash
}
