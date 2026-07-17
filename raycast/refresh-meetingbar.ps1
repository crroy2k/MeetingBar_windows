# Required parameters:
# @raycast.schemaVersion 1
# @raycast.title Refresh MeetingBar
# @raycast.mode silent

# Optional parameters:
# @raycast.packageName MeetingBar
# @raycast.icon ../docs/icon.png
# @raycast.description Refresh every enabled MeetingBar calendar source.

. "$PSScriptRoot\MeetingBar.Common.ps1"
Invoke-MeetingBarCommand -Command refresh
