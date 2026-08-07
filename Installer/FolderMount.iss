; ============================================================
;  FolderMount — Inno Setup Script
;  PrivilegesRequired=lowest  →  NO UAC prompt, NO admin needed
;  Installs to %LOCALAPPDATA%\FolderMount (user profile)
;  Writes only to HKCU (never HKLM)
; ============================================================

#define AppName      "FolderMount"
#define AppVersion   "1.0.0"
#define AppPublisher "FolderMount"
#define AppExeName   "FolderMount.exe"
#define AppId        "{{8F2A3B4C-D5E6-7890-ABCD-EF1234567890}"

[Setup]
SourceDir=..
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://github.com/FolderMount
AppSupportURL=https://github.com/FolderMount
AppUpdatesURL=https://github.com/FolderMount
DefaultDirName={localappdata}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; --- Key: no admin, no UAC ---
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=

OutputDir=Installer\Output
OutputBaseFilename=FolderMountSetup
SetupIconFile=FolderMount\Assets\icon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
WizardImageFile=compiler:WizModernImage.bmp
WizardSmallImageFile=compiler:WizModernSmallImage.bmp

; Use %LOCALAPPDATA% for all user data — no admin access needed
UsedUserAreasWarning=no
DisableWelcomePage=no
DisableDirPage=yes
CreateUninstallRegKey=yes
UninstallDisplayIcon={app}\{#AppExeName}

; Uninstall key goes to HKCU (never HKLM)
[Registry]
Root: HKCU; Subkey: "SOFTWARE\{#AppPublisher}\{#AppName}"; Flags: uninsdeletekeyifempty
Root: HKCU; Subkey: "SOFTWARE\{#AppPublisher}";           Flags: uninsdeletekeyifempty

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startupentry"; Description: "Start FolderMount automatically when I log in (mounts drives silently)"; GroupDescription: "Additional tasks:"; Flags: unchecked

[Files]
Source: "FolderMount\bin\Release\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "FolderMount\bin\Release\*.dll";          DestDir: "{app}"; Flags: ignoreversion recursesubdirs skipifsourcedoesntexist
Source: "FolderMount\Assets\icon.ico";             DestDir: "{app}\Assets"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}";                   Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\Assets\icon.ico"
Name: "{group}\Uninstall {#AppName}";         Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppName}";           Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\Assets\icon.ico"; Tasks: not startupentry

[Registry]
; Startup registry entry (HKCU — no admin needed) — written only if task selected
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "FolderMount"; \
  ValueData: """{app}\{#AppExeName}"" /startup"; \
  Tasks: startupentry; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch FolderMount"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Remove startup entry when uninstalling
Filename: "reg"; Parameters: "delete ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"" /v FolderMount /f"; \
  Flags: runhidden; StatusMsg: "Removing startup entry..."

[Code]
// Show a friendly welcome message on the first page
procedure InitializeWizard();
begin
  WizardForm.WelcomeLabel2.Caption :=
    'FolderMount maps local folders to virtual drive letters using the ' +
    'Windows SUBST command.' + #13#10 + #13#10 +
    'It will be installed to your user profile — no administrator ' +
    'rights are required.' + #13#10 + #13#10 +
    'Click Next to continue.';
end;
