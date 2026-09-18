#define AppName "1 Bullet"
#ifndef ONEBULLET_VERSION
  #define ONEBULLET_VERSION GetEnv("ONEBULLET_VERSION")
#endif
#define AppVersion ONEBULLET_VERSION
#define AppPublisher "Saif"
#define AppExeName "1bullet.exe"

[Setup]
AppId={{E4573C10-739D-4F97-BE87-405C8A928D8C}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\1 Bullet
DefaultGroupName=1 Bullet
OutputDir=dist
OutputBaseFilename=1 Bullet Setup
Compression=lzma
SolidCompression=yes
UninstallDisplayName=1 Bullet
PrivilegesRequired=lowest

[Files]
Source: "dist\1bullet.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "dist\1bullet-backend.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\1 Bullet"; Filename: "{app}\1bullet.exe"
Name: "{autodesktop}\1 Bullet"; Filename: "{app}\1bullet.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[UninstallDelete]
Type: files; Name: "{userappdata}\Microsoft\Windows\Start Menu\Programs\Startup\1 Bullet.lnk"

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', '1 Bullet');
end;
