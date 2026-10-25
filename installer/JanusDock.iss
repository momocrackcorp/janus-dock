#define MyAppName "JANUS Dock"
#ifndef MyAppVersion
  #define MyAppVersion "6.2.0"
#endif
#ifndef MyBuildFolder
  #define MyBuildFolder "dist-standalone"
#endif
#ifndef MyOutputBase
  #define MyOutputBase "JANUS-Dock-Setup-6.2-Autonomo"
#endif

[Setup]
AppId={{F65CE0CF-0D45-4AB0-A960-ED5FA44A9153}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=Momocrackcorp
AppComments=JANUS Dock: accesos rápidos, CPU, RAM, red y discos
AppCopyright=Copyright 2026 Momocrackcorp
VersionInfoDescription=JANUS Dock - Barra de accesos para Windows
VersionInfoProductName=JANUS Dock
WizardImageFile=compiler:WizModernImage-IS.bmp
WizardSmallImageFile=compiler:WizModernSmallImage-IS.bmp
AppPublisherURL=https://github.com/momocrackcorp/janus-dock
AppSupportURL=https://github.com/momocrackcorp/janus-dock/issues
DefaultDirName={localappdata}\Programs\JANUS Dock
DefaultGroupName=JANUS Dock
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
UninstallDisplayIcon={app}\JanusDock.exe
OutputDir=..\installer-output
OutputBaseFilename={#MyOutputBase}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Messages]
WelcomeLabel1=Bienvenido a JANUS Dock 6.2
WelcomeLabel2=Instala una barra de accesos personalizable con monitores de CPU, RAM, discos y red. Al finalizar puedes iniciar JANUS Dock y elegir si arranca con Windows.

[Files]
Source: "..\{#MyBuildFolder}\JanusDock.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\JANUS Dock"; Filename: "{app}\JanusDock.exe"
Name: "{autodesktop}\JANUS Dock"; Filename: "{app}\JanusDock.exe"; Tasks: desktopicon

[Tasks]
Name: "startup"; Description: "Iniciar JANUS Dock al entrar a Windows"; Flags: checkedonce
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; Flags: unchecked

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "JANUS Dock"; ValueData: """{app}\JanusDock.exe"""; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\JanusDock.exe"; Description: "Iniciar JANUS Dock"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{app}\JanusDock.exe"
