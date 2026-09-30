#ifndef AppVersion
  #error AppVersion must be supplied by build-installer.ps1
#endif
#ifndef Runtime
  #define Runtime "win-x64"
#endif
#ifndef SourceDir
  #error SourceDir must be supplied by build-installer.ps1
#endif
#ifndef OutputDir
  #error OutputDir must be supplied by build-installer.ps1
#endif

[Setup]
AppId={{8C375AC5-59F2-42C6-AEE6-F9C2C0F90DFD}
AppName=Planar Geometry Studio
AppVersion={#AppVersion}
AppPublisher=Nikola Veselinov
AppPublisherURL=https://nikolaveselinov.github.io/
AppSupportURL=https://github.com/nikolaveselinov/Planar-Geometry-Studio/issues
AppUpdatesURL=https://github.com/nikolaveselinov/Planar-Geometry-Studio/releases/latest
DefaultDirName={autopf}\Planar Geometry Studio
DefaultGroupName=Planar Geometry Studio
AllowNoIcons=yes
DisableProgramGroupPage=no
DisableDirPage=no
DisableWelcomePage=no
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog commandline
#if Runtime == "win-arm64"
ArchitecturesAllowed=arm64 and x64compatible
ArchitecturesInstallIn64BitMode=arm64
MinVersion=10.0.22000
#else
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
#endif
OutputDir={#OutputDir}
OutputBaseFilename=PlanarGeometryStudio-v{#AppVersion}-{#Runtime}-setup
SetupIconFile=..\assets\studio.ico
WizardStyle=modern
WizardSizePercent=110
WizardImageFile=..\assets\wizard.bmp
WizardSmallImageFile=..\assets\wizard-small.bmp
LicenseFile=..\..\LICENSE
InfoBeforeFile=..\..\TERMS.md
Compression=lzma2
SolidCompression=yes
UninstallDisplayIcon={app}\PlanarGeometryStudio.exe
UninstallDisplayName=Planar Geometry Studio
CloseApplications=yes
RestartApplications=no
UsePreviousTasks=yes
SetupLogging=yes

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked
Name: "drawingtools"; Description: "Set up drawing tools after installation (MiKTeX / MetaPost / PDF export)"; GroupDescription: "Optional tools (internet required):"
Name: "autoupdates"; Description: "Check for new stable versions when Studio opens"; GroupDescription: "Updates:"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\assets\studio.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Planar Geometry Studio"; Filename: "{app}\PlanarGeometryStudio.exe"; WorkingDir: "{app}"; IconFilename: "{app}\studio.ico"
Name: "{group}\Studio Setup"; Filename: "{app}\PlanarGeometryStudio.exe"; Parameters: "--setup"; WorkingDir: "{app}"; IconFilename: "{app}\studio.ico"
Name: "{group}\Uninstall Planar Geometry Studio"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Planar Geometry Studio"; Filename: "{app}\PlanarGeometryStudio.exe"; WorkingDir: "{app}"; IconFilename: "{app}\studio.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\PlanarGeometryStudio.exe"; Parameters: "{code:SetupArguments}"; Description: "Open Studio and finish setup"; Flags: nowait postinstall skipifsilent runasoriginaluser

[Code]
function SetupArguments(Param: String): String;
begin
  Result := '--setup';
  if WizardIsTaskSelected('drawingtools') then
    Result := Result + ' --install-drawing-tools';
  if not WizardIsTaskSelected('autoupdates') then
    Result := Result + ' --no-update-checks';
end;

procedure InitializeWizard;
begin
  WizardForm.WelcomeLabel1.Caption := 'From conjecture to figure.';
  WizardForm.WelcomeLabel2.Caption :=
    'Welcome to Planar Geometry Studio.' + #13#10#13#10 +
    'Setup installs the studio, geometry engine, and .NET runtime. Choose the installation folder, shortcuts, and optional drawing tools on the following pages.' + #13#10#13#10 +
    'MiKTeX is installed for your user account after Studio opens. Existing TeX installations are reused. Review the third-party license links in the setup notes before selecting drawing tools.' + #13#10#13#10 +
    'Your configurations and results stay separate from the application and are preserved when upgrading or uninstalling.';
end;
