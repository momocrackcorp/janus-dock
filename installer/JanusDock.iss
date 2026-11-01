#define MyAppName "JANUS Dock"
#ifndef MyAppVersion
  #define MyAppVersion "6.5.11"
#endif
#ifndef MyBuildFolder
  #define MyBuildFolder "dist-standalone"
#endif
#ifndef MyOutputBase
  #define MyOutputBase "JANUS-Dock-Setup-{#MyAppVersion}-Autonomo"
#endif

[Setup]
AppId={{F65CE0CF-0D45-4AB0-A960-ED5FA44A9153}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=Momocrackcorp
AppComments=JANUS Dock {#MyAppVersion}: accesos, reloj, indicadores, control multimedia y volumen
AppCopyright=Copyright 2026 Momocrackcorp
VersionInfoDescription=JANUS Dock - Barra de accesos para Windows
VersionInfoProductName=JANUS Dock
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
WelcomeLabel1=Bienvenido a JANUS Dock {#MyAppVersion}
WelcomeLabel2=Instala o actualiza JANUS Dock {#MyAppVersion}: accesos y separadores, reloj LCD, indicadores de sistema, controles multimedia y volumen. El complemento de música es opcional y se descarga desde GitHub al activarlo. Conserva tus ajustes personales y puedes iniciar el Dock con Windows.

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
