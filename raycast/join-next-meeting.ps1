# Required parameters:
# @raycast.schemaVersion 1
# @raycast.title Join Next Meeting
# @raycast.mode silent

# Optional parameters:
# @raycast.packageName MeetingBar
# @raycast.icon ../docs/icon.png
# @raycast.description Join the current or next meeting found by MeetingBar.

. "$PSScriptRoot\MeetingBar.Common.ps1"
Invoke-MeetingBarCommand -Command join
