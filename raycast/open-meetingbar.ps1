# Required parameters:
# @raycast.schemaVersion 1
# @raycast.title Open MeetingBar
# @raycast.mode silent

# Optional parameters:
# @raycast.packageName MeetingBar
# @raycast.icon ../docs/icon.png
# @raycast.description Show the MeetingBar popup.

. "$PSScriptRoot\MeetingBar.Common.ps1"
Invoke-MeetingBarCommand -Command popup
