# MeetingBar for Windows

This private repository contains only the native Windows edition of MeetingBar. The application source, tests, build script, and Windows-specific documentation live in [`app_WIN`](app_WIN/README.md).

MeetingBar for Windows shows the current or next calendar meeting in the notification area and can also provide taskbar-aligned and Windows Widgets Board surfaces. It supports Classic Outlook, ICS calendars, and direct Google Calendar access.

## Requirements

- Windows 10 or Windows 11
- Visual Studio 2022 or Build Tools with MSBuild and the .NET Framework 4.7.2 targeting pack
- .NET 8 SDK

## Build and test

Run from the repository root:

```powershell
.\app_WIN\build.ps1 -Configuration Release -Test
```

To also produce `app_WIN/dist/MeetingBar-Windows-portable.zip`:

```powershell
.\app_WIN\build.ps1 -Configuration Release -Test -Package
```

See [`app_WIN/README.md`](app_WIN/README.md) for setup, providers, features, privacy, and troubleshooting. See [`app_WIN/CHANGELOG.md`](app_WIN/CHANGELOG.md) for Windows release notes.

## Repository layout

- `app_WIN/` — WPF tray application, WinUI 3 settings host, Win+W widget provider, tests, resources, and packaging script
- `.github/workflows/ci.yml` — Windows build, test, package, and artifact validation
- `.github/workflows/prerelease.yml` — rolling Windows pre-release publishing from `main`
- `.github/workflows/security.yml` — weekly and change-triggered NuGet vulnerability audit

## License

Licensed under the Apache License 2.0. See [`LICENSE`](LICENSE).
