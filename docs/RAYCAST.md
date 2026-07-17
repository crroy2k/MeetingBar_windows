# Raycast for Windows

MeetingBar includes ready-to-use Raycast Script Commands. They work with the portable build, forward to the running tray instance, and do not need administrator access.

## Recommended setup: import the commands

1. Extract the complete MeetingBar portable archive to a permanent location. Keep `MeetingBar.exe`, `Settings`, `WinWWidget`, `docs`, and `raycast` together.
2. Run `MeetingBar.exe`. In **MeetingBar Settings > General**, enable **Start MeetingBar with Windows** so **Join Next Meeting** always has a loaded calendar to act on.
3. Open Raycast and go to **Raycast Settings > Script Commands**.
4. Choose **Add Script Directory** and select MeetingBar's extracted `raycast` folder.
5. Return to Raycast Root Search. The following commands now appear under the **MeetingBar** package:

   - **Open MeetingBar**
   - **Join Next Meeting**
   - **Create Meeting**
   - **Open Calendar**
   - **Refresh MeetingBar**
   - **MeetingBar Settings**
   - **Quit MeetingBar** (asks for confirmation)

To assign a global hotkey or a shorter alias, select a MeetingBar command in Raycast, press `Ctrl+K`, and choose **Configure Command**. For example, bind **Join Next Meeting** to a shortcut that does not conflict with MeetingBar's optional global `Ctrl+Alt+J` shortcut.

The scripts locate `MeetingBar.exe` relative to the imported `raycast` folder, so installation paths containing spaces work without extra quoting. If you deliberately store the scripts elsewhere, define a user environment variable named `MEETINGBAR_EXE` whose value is the full path to `MeetingBar.exe`, then restart Raycast.

## PowerShell execution policy

Raycast runs `.ps1` commands using PowerShell from `PATH`. If Raycast reports that scripts are disabled, open PowerShell as your normal user and inspect the effective policies:

```powershell
Get-ExecutionPolicy -List
```

If your machine is not managed by an organization, allow locally created scripts for only your Windows account:

```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

Do not change `MachinePolicy` or `UserPolicy`; contact your administrator if either is enforced. The MeetingBar scripts are plain text and can be reviewed in the `raycast` folder.

## Alternative: Raycast Run Commands

If you do not want to enable PowerShell Script Commands, create aliases with Raycast's built-in **Create Run Command**. Use the system shell and enter the absolute executable path in double quotes, followed by one argument. For example:

```text
"C:\Tools\MeetingBar\MeetingBar.exe" --popup
"C:\Tools\MeetingBar\MeetingBar.exe" --join
"C:\Tools\MeetingBar\MeetingBar.exe" --create
"C:\Tools\MeetingBar\MeetingBar.exe" --calendar
"C:\Tools\MeetingBar\MeetingBar.exe" --refresh
"C:\Tools\MeetingBar\MeetingBar.exe" --settings
"C:\Tools\MeetingBar\MeetingBar.exe" --quit
```

Create one Run Command per action and give each a distinct alias such as `mb`, `join`, or `mbr`. Keep the quotes when the installation path contains spaces. Elevated execution is neither required nor recommended.

## Troubleshooting

- **A command says `MeetingBar.exe was not found`:** re-extract the complete archive and select the `raycast` folder beside `MeetingBar.exe`, or set `MEETINGBAR_EXE` as described above.
- **Join does nothing immediately after sign-in:** launch MeetingBar first and wait for its initial calendar refresh. Enabling **Start MeetingBar with Windows** prevents this in normal use.
- **Raycast does not list the commands:** confirm that the selected Script Directory is the `raycast` folder, not the individual `.ps1` file. Remove and add the directory again after moving the portable installation.
- **The popup opens instead of the requested action:** update MeetingBar from the same archive as the scripts; older builds may not recognize every command.

For Raycast's current behavior, see the official [Script Commands](https://manual.raycast.com/script-commands), [Run Commands](https://manual.raycast.com/run), and [aliases and hotkeys](https://manual.raycast.com/command-aliases-and-hotkeys) documentation.
