param(
    [ValidateSet("Debug", "Release")][string]$Configuration = "Release",
    [switch]$Test,
    [switch]$Package
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = $null
if (Test-Path $vswhere) {
    $installation = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
    if ($installation) { $msbuild = Join-Path $installation "MSBuild\Current\Bin\MSBuild.exe" }
}
if (!$msbuild -or !(Test-Path $msbuild)) {
    $msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
}
if (!(Test-Path $msbuild)) { throw "MSBuild not found. Install Visual Studio 2022 Build Tools with the .NET Framework 4.7.2 targeting pack." }

& $msbuild (Join-Path $root "MeetingBar.Windows.csproj") /t:Rebuild /p:Configuration=$Configuration /m
if ($LASTEXITCODE -ne 0) { throw "MeetingBar build failed." }

if ($Test) {
    & $msbuild (Join-Path $root "Tests\MeetingBar.Tests.csproj") /t:Rebuild /p:Configuration=$Configuration /m
    if ($LASTEXITCODE -ne 0) { throw "MeetingBar test build failed." }
    & (Join-Path $root "Tests\bin\$Configuration\MeetingBar.Tests.exe")
    if ($LASTEXITCODE -ne 0) { throw "MeetingBar tests failed." }
}

if ($Package) {
    $dist = Join-Path $root "dist"
    $stage = Join-Path $dist "portable"
    New-Item -ItemType Directory -Force $stage | Out-Null
    Copy-Item (Join-Path $root "bin\$Configuration\MeetingBar.exe") (Join-Path $stage "MeetingBar.exe") -Force
    Copy-Item (Join-Path $root "README.md") (Join-Path $stage "README.md") -Force
    Copy-Item (Join-Path $root "..\LICENSE") (Join-Path $stage "LICENSE") -Force
    Compress-Archive -Path (Join-Path $stage "MeetingBar.exe"), (Join-Path $stage "README.md"), (Join-Path $stage "LICENSE") -DestinationPath (Join-Path $dist "MeetingBar-Windows-portable.zip") -Force
    Write-Host "Package: $dist\MeetingBar-Windows-portable.zip"
}
