#define MyAppName "Ærakon x52 driver"
#define MyAppVersion "1.1.7"
#define MyAppPublisher "Ærakon"
#define MyAppURL "https://github.com/aera/x52-custom-driver"
#define MyAppExeName "X52.CustomDriver.App.exe"
#define BuildPath "X52.CustomDriver.App\bin\Release\net9.0-windows\win-x64\publish"
#define ConsoleBuildPath "X52.CustomDriver.Console\bin\Release\net9.0-windows\win-x64\publish"

[Setup]
; (No changes to Setup section)
AppId={{D3F7D5C2-4E8F-4B9A-9C7D-2E5F8A1B3C4D}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=InstallerOutput
OutputBaseFilename=AerakonX52Driver_Setup_v1.1.7
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile=X52.CustomDriver.App\app.ico
UninstallDisplayIcon={app}\app.ico
InfoBeforeFile=INSTRUCTIONS.txt

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#BuildPath}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildPath}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; MANUAL DLL PATCH FIX: Force copy vJoyInterface from x64 folder to ROOT so Wrapper finds it
Source: "{#BuildPath}\x64\vJoyInterface.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#ConsoleBuildPath}\*"; DestDir: "{app}\Diagnostics"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files
Source: "X52.CustomDriver.App\app.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
