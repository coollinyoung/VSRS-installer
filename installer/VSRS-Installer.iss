#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#define MyAppName "VSRS 外接 SSD 安裝工具"
#define MyAppExeName "VSRS.Installer.exe"

[Setup]
AppId={{96C15B23-4609-41F0-B41B-82211B8AA958}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=VSRS
DefaultDirName={autopf}\VSRS Installer
DefaultGroupName=VSRS Installer
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=VSRS-Installer-{#MyAppVersion}-Offline
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupLogging=yes

[Languages]
Name: "chinesetraditional"; MessagesFile: "compiler:Languages\ChineseTraditional.isl"

[Files]
Source: "..\artifacts\app\*"; DestDir: "{app}"; Excludes: "redist\*"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\artifacts\redist\ndp48-x86-x64-allos-enu.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{autoprograms}\VSRS 外接 SSD 安裝工具"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\VSRS 外接 SSD 安裝工具"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "建立桌面捷徑"; GroupDescription: "其他選項："; Flags: unchecked

[Run]
Filename: "{tmp}\ndp48-x86-x64-allos-enu.exe"; Parameters: "/q /norestart"; StatusMsg: "正在安裝 Microsoft .NET Framework 4.8……"; Check: not IsDotNet48Installed; Flags: waituntilterminated
Filename: "{app}\{#MyAppExeName}"; Description: "啟動 VSRS 外接 SSD 安裝工具"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNet48Installed: Boolean;
var
  Release: Cardinal;
begin
  Result := False;
  if IsWin64 then
    Result := RegQueryDWordValue(HKLM64,
      'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release)
  else
    Result := RegQueryDWordValue(HKLM32,
      'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release);

  Result := Result and (Release >= 528040);
end;
