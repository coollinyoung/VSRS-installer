#ifndef MyAppVersion
  #define MyAppVersion "1.0.1"
#endif

#define MyAppName "VSRS 外接 SSD 安裝工具"
#define MyAppExeName "VSRS.Installer.exe"

// Resolve packaging paths relative to this saved script in installer\.
// Copy the complete Release output into artifacts\app before compiling.
#ifndef AppSourceDir
  #define AppSourceDir AddBackslash(SourcePath) + "..\artifacts\app"
#endif
#define RuntimeSource AddBackslash(SourcePath) + "..\artifacts\redist\ndp48-x86-x64-allos-enu.exe"

#if !FileExists(AddBackslash(AppSourceDir) + MyAppExeName)
  #error "Missing VSRS.Installer.exe: copy the complete Release output to artifacts\app, or set AppSourceDir to the complete packaging folder."
#endif
#if !FileExists(AddBackslash(AppSourceDir) + "tools\ventoy\Ventoy2Disk_X64.exe")
  #error "Missing Ventoy2Disk_X64.exe: place the complete Ventoy package in AppSourceDir\tools\ventoy."
#endif
#if !FileExists(RuntimeSource)
  #error "Missing .NET 4.8 offline installer in artifacts\redist. Build the Visual Studio project first."
#endif

[Setup]
DiskSpanning=yes
DiskSliceSize=2000000000
SlicesPerDisk=1
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
SetupIconFile=..\src\VSRS.Installer\Assets\recovery.ico

[Languages]
Name: "chinesetraditional"; MessagesFile: "compiler:Languages\ChineseTraditional.isl"

[Files]
Source: "{#AppSourceDir}\*"; DestDir: "{app}"; Excludes: "\redist\*"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#RuntimeSource}"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{autoprograms}\VSRS 外接 SSD 安裝工具"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0
Name: "{autodesktop}\VSRS 外接 SSD 安裝工具"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "建立桌面捷徑"; GroupDescription: "其他選項："; Flags: unchecked

[Run]
Filename: "{tmp}\ndp48-x86-x64-allos-enu.exe"; Parameters: "/q /norestart"; StatusMsg: "正在安裝 Microsoft .NET Framework 4.8……"; Check: not IsDotNet48Installed; Flags: waituntilterminated
Filename: "{app}\{#MyAppExeName}"; Description: "啟動 VSRS 外接 SSD 安裝工具"; Flags: nowait postinstall skipifsilent runascurrentuser

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
