# MeetingBar for Windows

Native Windows companion to MeetingBar. It shows the current or next meeting in the notification area and, optionally, in a taskbar-aligned widget. Every surface opens the same meeting popup.

## Run

The portable build is `dist/MeetingBar-Windows-portable.zip`. Extract it and run `MeetingBar.exe`. No installer or administrator rights are required. Windows 10 or 11 with .NET Framework 4.7.2+ is required.

First launch defaults to Classic Outlook. Right-click the tray icon, choose **Settings**, then select a provider:

- **Outlook** reads local Classic Outlook calendars through its local COM interface. New Outlook does not expose that interface.
- **ICS** accepts one local `.ics` path or HTTPS calendar feed URL per line.
- **Google** uses direct Calendar API access. Enable Google Calendar API, create an OAuth client of type **Desktop app**, enter its client ID in Settings, then connect. OAuth uses PKCE and a temporary loopback callback on `127.0.0.1`; the refresh token is encrypted with Windows Data Protection for the signed-in Windows user.

## Surfaces

- **Tray only**: icon only; Windows controls whether it is directly visible or inside the overflow menu.
- **Near tray**: compact meeting title/time overlay immediately left of the notification area, plus tray icon.
- **Taskbar left**: compact overlay at the far-left edge, plus tray icon.

The taskbar versions are borderless topmost overlays. Windows 11 has no supported third-party text-widget slot inside its taskbar. Width and X/Y offsets are configurable for taskbar size, alignment, display scaling, and third-party taskbar tools.

## Features

- Current/next meeting, today/tomorrow event list, countdowns, calendar colors, dismissals
- Google Meet, Zoom, Teams, Webex, Jitsi, Slack Huddles, and the full MeetingBar provider catalogue; arbitrary links remain joinable
- Meeting-link precedence, HTML cleanup, custom regex model, and nested Outlook SafeLink decoding
- Event filters: all-day, declined, tentative, free/non-busy, linkless, calendar selection
- Preferred browser, create-meeting URL, recurring meeting bookmarks
- Start with Windows, periodic/manual refresh, last-known-good events after provider failure
- Meeting notifications, optional full-screen reminder, end notification, auto-join
- Global `Ctrl+Alt+J` join shortcut
- Local command forwarding for Raycast for Windows, PowerToys Run, scripts, and desktop shortcuts

Mac-only AppleScript, Shortcuts, EventKit, StoreKit, and experimental features are intentionally absent.

## Launcher commands

Commands forward to the already-running instance:

```powershell
MeetingBar.exe --popup
MeetingBar.exe --join
MeetingBar.exe --create
MeetingBar.exe --refresh
MeetingBar.exe --settings
MeetingBar.exe --quit
```

In Raycast for Windows, create Run Commands pointing to the executable plus the desired argument.

## Build and test

Visual Studio 2022 or Build Tools with the .NET Framework 4.7.2 targeting pack is required.

```powershell
.\build.ps1 -Configuration Release -Test -Package
```

Source structure follows the Mac app's feature ownership: calendar providers and sync, meeting detection/opening, notifications, settings, then surface controllers.

## Known platform limits

- The taskbar overlay targets the primary Windows taskbar. Multi-monitor secondary taskbar selection is not implemented yet.
- Outlook support requires Classic Outlook. Google and ICS work without Outlook.
- Notification balloons use the built-in notification-area API; full-screen reminders provide the richer join action.
- The portable binary is unsigned. Production distribution should add Authenticode signing and an MSIX or installer.
