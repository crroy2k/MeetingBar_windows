# Required parameters:
# @raycast.schemaVersion 1
# @raycast.title Create Meeting
# @raycast.mode silent

# Optional parameters:
# @raycast.packageName MeetingBar
# @raycast.icon ../docs/icon.png
# @raycast.description Open the create-meeting URL configured in MeetingBar.

. "$PSScriptRoot\MeetingBar.Common.ps1"
Invoke-MeetingBarCommand -Command create
