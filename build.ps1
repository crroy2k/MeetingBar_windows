param(
    [ValidateSet("Debug", "Release")][string]$Configuration = "Release",
    [switch]$Test,
    [switch]$Package
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dotnet = $env:MEETINGBAR_DOTNET
if (!$dotnet) {
    $sdkCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($sdkCommand -and (& $sdkCommand.Source --list-sdks)) { $dotnet = $sdkCommand.Source }
}
if (!$dotnet -and (Test-Path "E:\codex-tools\dotnet8\dotnet.exe")) { $dotnet = "E:\codex-tools\dotnet8\dotnet.exe" }
if (!$dotnet -or !(Test-Path $dotnet)) { throw ".NET 8 SDK not found. Set MEETINGBAR_DOTNET to dotnet.exe." }
& $dotnet build (Join-Path $root "MeetingBar.Windows.csproj") -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "MeetingBar build failed." }
$env:NUGET_PACKAGES = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:LOCALAPPDATA "NuGet\Packages" }
$settingsPublish = Join-Path $root "SettingsApp\bin\$Configuration\publish-folder"
& $dotnet publish (Join-Path $root "SettingsApp\MeetingBar.Settings.csproj") -c $Configuration -r win-x64 --self-contained true -p:Platform=x64 -p:PublishSingleFile=false -p:WindowsAppSDKSelfContained=true -p:EnableMsixTooling=true -p:PublishTrimmed=false -o $settingsPublish
if ($LASTEXITCODE -ne 0) { throw "MeetingBar WinUI settings build failed." }
$settingsOutput = Join-Path $root "bin\$Configuration\net472\Settings"
New-Item -ItemType Directory -Force $settingsOutput | Out-Null
Copy-Item (Join-Path $settingsPublish "*") $settingsOutput -Recurse -Force

$widgetPublish = Join-Path $root "WidgetProvider\bin\$Configuration\publish-folder"
& $dotnet publish (Join-Path $root "WidgetProvider\MeetingBar.WidgetProvider.csproj") -c $Configuration -r win-x64 --self-contained true -p:Platform=x64 -p:WindowsAppSDKSelfContained=true -p:EnableMsixTooling=true -p:PublishTrimmed=false -o $widgetPublish
if ($LASTEXITCODE -ne 0) { throw "MeetingBar Win+W widget provider build failed." }
Copy-Item (Join-Path $root "WidgetProvider\Package.appxmanifest") (Join-Path $widgetPublish "AppxManifest.xml") -Force
$widgetOutput = Join-Path $root "bin\$Configuration\net472\WinWWidget"
New-Item -ItemType Directory -Force $widgetOutput | Out-Null
Copy-Item (Join-Path $widgetPublish "*") $widgetOutput -Recurse -Force

if ($Test) {
    & $dotnet build (Join-Path $root "Tests\MeetingBar.Tests.csproj") -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "MeetingBar test build failed." }
    & (Join-Path $root "Tests\bin\$Configuration\net472\MeetingBar.Tests.exe")
    if ($LASTEXITCODE -ne 0) { throw "MeetingBar tests failed." }
    & $dotnet run --project (Join-Path $root "Tests\SettingsRegression\SettingsRegression.csproj") -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Settings regression tests failed." }
}

if ($Package) {
    $dist = Join-Path $root "dist"
    New-Item -ItemType Directory -Force $dist | Out-Null
    $packageInputs = @(
        (Join-Path $root "bin\$Configuration\net472\MeetingBar.exe")
        (Join-Path $root "bin\$Configuration\net472\Settings")
        (Join-Path $root "bin\$Configuration\net472\WinWWidget")
        (Join-Path $root "raycast")
        (Join-Path $root "docs")
        (Join-Path $root "README.md")
        (Join-Path $root "README.es.md")
        (Join-Path $root "THIRD-PARTY-NOTICES.md")
        (Join-Path $root "LICENSE")
    )
    Compress-Archive -Path $packageInputs -DestinationPath (Join-Path $dist "MeetingBar-Windows-portable.zip") -Force
    Write-Host "Package: $dist\MeetingBar-Windows-portable.zip"
}
