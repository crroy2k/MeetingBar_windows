# Contributing to MeetingBar for Windows

Keep changes focused on the Windows application and its repository support files.

## Development requirements

- Windows 10 or Windows 11
- Visual Studio 2022 or Build Tools with MSBuild and the .NET Framework 4.7.2 targeting pack
- .NET 8 SDK

## Validation

Before opening a pull request, run:

```powershell
.\build.ps1 -Configuration Release -Test -Package
```

The command builds the .NET Framework tray app, publishes the .NET 8 WinUI settings app and widget provider, runs the Windows test executable, and creates the portable ZIP.

## Pull requests

Describe:

- what changed and why
- user-visible behavior changes
- the Windows version and calendar provider used for manual testing
- validation commands and results
- any dependency, packaging, manifest, OAuth, startup, notification, or widget changes

Update `CHANGELOG.md` for user-visible changes and notable build or release changes. Never commit OAuth credentials, calendar data, logs containing meeting details, or generated `bin`, `obj`, and `dist` output.

## Changelog entries

Add every release note under the `Added` or `Fixed` subsection of `Unreleased`:

- Wrote entries in the past tense, such as `Added calendar filtering` or `Fixed taskbar positioning`.
- Added the related GitHub issue to each fix when one existed, such as `Fixed taskbar positioning (#123)`.
- Used issue numbers only for matching issues; did not substitute pull request numbers.
