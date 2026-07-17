# Required parameters:
# @raycast.schemaVersion 1
# @raycast.title Quit MeetingBar
# @raycast.mode silent

# Optional parameters:
# @raycast.packageName MeetingBar
# @raycast.icon ../docs/icon.png
# @raycast.description Quit the running MeetingBar instance.
# @raycast.needsConfirmation true

. "$PSScriptRoot\MeetingBar.Common.ps1"
Invoke-MeetingBarCommand -Command quit
