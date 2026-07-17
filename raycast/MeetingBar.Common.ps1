function Invoke-MeetingBarCommand {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("popup", "join", "create", "calendar", "refresh", "settings", "quit")]
        [string]$Command
    )

    $candidates = @(
        $env:MEETINGBAR_EXE
        (Join-Path (Split-Path -Parent $PSScriptRoot) "MeetingBar.exe")
        (Join-Path (Split-Path -Parent $PSScriptRoot) "bin\Release\MeetingBar.exe")
        (Join-Path (Split-Path -Parent $PSScriptRoot) "bin\Debug\MeetingBar.exe")
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    $executable = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
    if (-not $executable) {
        Write-Error "MeetingBar.exe was not found. Keep the raycast folder beside MeetingBar.exe, or set MEETINGBAR_EXE to its full path."
        exit 1
    }

    Start-Process -FilePath $executable -ArgumentList "--$Command"
}
