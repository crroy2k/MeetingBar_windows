# Required parameters:
# @raycast.schemaVersion 1
# @raycast.title Open Calendar
# @raycast.mode silent

# Optional parameters:
# @raycast.packageName MeetingBar
# @raycast.icon ../docs/icon.png
# @raycast.description Open the calendar configured in MeetingBar.

. "$PSScriptRoot\MeetingBar.Common.ps1"
Invoke-MeetingBarCommand -Command calendar
