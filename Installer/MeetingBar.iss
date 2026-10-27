#define AppVersion "0.1.0.5"
[Setup]
AppId={{304A805D-44D8-4121-A0CC-F323343BE269}
AppName=MeetingBar
AppVersion={#AppVersion}
AppPublisher=MeetingBar contributors - crroy2k fork
AppPublisherURL=https://github.com/crroy2k/MeetingBar_windows
DefaultDirName={localappdata}\Programs\MeetingBar
DefaultGroupName=MeetingBar
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir=..\dist
OutputBaseFilename=MeetingBar-Windows-v{#AppVersion}-Setup
SetupIconFile=..\Resources\MeetingBar.ico
UninstallDisplayIcon={app}\MeetingBar.exe
Compression=lzma2/fast
SolidCompression=yes
CloseApplications=yes
RestartApplications=no
WizardStyle=modern
DisableProgramGroupPage=yes

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; Flags: unchecked

[Files]
Source: "..\bin\Release\net472\MeetingBar.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "ConfigTool\bin\Release\net472\MeetingBar.InstallConfig.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\net472\Settings\*"; DestDir: "{app}\Settings"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\bin\Release\net472\WinWWidget\*"; DestDir: "{app}\WinWWidget"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\LICENSE"; DestDir: "{app}"
Source: "..\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\MeetingBar"; Filename: "{app}\MeetingBar.exe"
Name: "{autodesktop}\MeetingBar"; Filename: "{app}\MeetingBar.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\MeetingBar.exe"; Description: "Abrir MeetingBar"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\MeetingBar.exe"; Parameters: "--quit"; Flags: runhidden waituntilterminated; RunOnceId: "QuitMeetingBar"
Filename: "{app}\MeetingBar.InstallConfig.exe"; Parameters: "--uninstall"; Flags: runhidden waituntilterminated; RunOnceId: "RemoveStartup"

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var ResultCode: Integer;
begin
  if CurStep = ssPostInstall then begin
    if not Exec(ExpandConstant('{app}\MeetingBar.InstallConfig.exe'), '', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      RaiseException('No se pudo configurar MeetingBar.');
    if ResultCode <> 0 then
      RaiseException('No se pudo actualizar la configuracion. La configuracion anterior se ha conservado.');
  end;
end;
