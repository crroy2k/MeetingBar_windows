# Required parameters:
# @raycast.schemaVersion 1
# @raycast.title MeetingBar Settings
# @raycast.mode silent

# Optional parameters:
# @raycast.packageName MeetingBar
# @raycast.icon ../docs/icon.png
# @raycast.description Open MeetingBar settings.

. "$PSScriptRoot\MeetingBar.Common.ps1"
Invoke-MeetingBarCommand -Command settings
