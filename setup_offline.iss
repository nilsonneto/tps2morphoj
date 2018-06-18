#include <idp.iss>

#define MyAppName "TPS2MorphoJ"
#define MyAppVersion "0.2"
#define MyAppPublisher "nilsonneto"
#define MyAppURL "https://github.com/nilsonneto/tps2morphoj"
#define MyAppExeName "TPS2MorphoJ.exe"
#define DotNetInstallerName "NDP452-KB2901907-x86-x64-AllOS-ENU.exe"

[Setup]
AppId={{2441466C-D611-4F8C-83F0-B7248E5AC056}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\{#MyAppName}
DisableProgramGroupPage=yes
OutputBaseFilename={#MyAppName}_Setup_v{#MyAppVersion}_Offline
OutputDir={#SourcePath}
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#DotNetInstallerName}"; DestDir: "{tmp}"; Check: FrameworkIsNotInstalled; Flags: deleteafterinstall; AfterInstall: InstallFramework
Source: "TPS2MorphoJ\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{commonprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent


[Code]
var
  ShouldRestart: boolean;

// Functions
function GetUninstallString: string;
var
  sUnInstPath: string;
  sUnInstallString: String;
begin
  Result := '';
  sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{{2441466C-D611-4F8C-83F0-B7248E5AC056}_is1');
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;

function IsInstalled: Boolean;
begin
  Result := (GetUninstallString() <> '');
end;

// .NET procedures
// Indicates whether the specified version and service pack of the .NET Framework is installed.
//
// version -- Specify one of these strings for the required .NET Framework version:
//    'v1.1'          .NET Framework 1.1
//    'v2.0'          .NET Framework 2.0
//    'v3.0'          .NET Framework 3.0
//    'v3.5'          .NET Framework 3.5
//    'v4\Client'     .NET Framework 4.0 Client Profile
//    'v4\Full'       .NET Framework 4.0 Full Installation
//    'v4.5'          .NET Framework 4.5
//    'v4.5.1'        .NET Framework 4.5.1
//    'v4.5.2'        .NET Framework 4.5.2
//    'v4.6'          .NET Framework 4.6
//    'v4.6.1'        .NET Framework 4.6.1
//    'v4.7'          .NET Framework 4.7
//
// service -- Specify any non-negative integer for the required service pack level:
//    0               No service packs required
//    1, 2, etc.      Service pack 1, 2, etc. required
function IsDotNetMissing(version: string; service: cardinal): boolean;
var
    key, versionKey: string;
    install, release, serviceCount, versionRelease: cardinal;
    success: boolean;
begin
    versionKey := version;
    versionRelease := 0;

    // .NET 1.1 and 2.0 embed release number in version key
    if version = 'v1.1' then begin
        versionKey := 'v1.1.4322';
    end else if version = 'v2.0' then begin
        versionKey := 'v2.0.50727';
    end

    // .NET 4.5 and newer install as update to .NET 4.0 Full
    else if Pos('v4.', version) = 1 then begin
        versionKey := 'v4\Full';
        case version of
          'v4.5':   versionRelease := 378389;
          'v4.5.1': versionRelease := 378675; // 378758 on Windows 8 and older
          'v4.5.2': versionRelease := 379893;
          'v4.6':   versionRelease := 393295; // 393297 on Windows 8.1 and older
          'v4.6.1': versionRelease := 394254; // 394271 before Win10 November Update
          'v4.6.2': versionRelease := 394802; // 394806 before Win10 Anniversary Update
          'v4.7':   versionRelease := 460798; // 460805 before Win10 Creators Update
        end;
    end;

    // Installation key group for all .NET versions
    key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + versionKey;

    // .NET 3.0 uses value InstallSuccess in subkey Setup
    if Pos('v3.0', version) = 1 then begin
        success := RegQueryDWordValue(HKLM, key + '\Setup', 'InstallSuccess', install);
    end else begin
        success := RegQueryDWordValue(HKLM, key, 'Install', install);
    end;

    // .NET 4.0 and newer use value Servicing instead of SP
    if Pos('v4', version) = 1 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Servicing', serviceCount);
    end else begin
        success := success and RegQueryDWordValue(HKLM, key, 'SP', serviceCount);
    end;

    // .NET 4.5 and newer use additional value Release
    if versionRelease > 0 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Release', release);
        success := success and (release >= versionRelease);
    end;

    result := not(success and (install = 1) and (serviceCount >= service));
end;

function InstallFramework452(shouldDeleteFile: boolean) : boolean;
var
  StatusText: string;
  ResultCode: Integer;
  success: boolean;
  installPath: string;
begin
  success := false;
  StatusText := WizardForm.StatusLabel.Caption;
  WizardForm.StatusLabel.Caption := ExpandConstant('{cm:framework452InstallMsg}');
  WizardForm.ProgressGauge.Style := npbstMarquee;
  installPath := ExpandConstant('{tmp}') + '\' + ExpandConstant('{#DotNetInstallerName}');
  try
    if not Exec(installPath, '/q /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
    begin
      MsgBox(ExpandConstant('{cm:error452CodeMsg}') + ' ' + IntToStr(ResultCode) + '. ' + ExpandConstant('{cm:setupAbortMsg}'), mbError, MB_OK);
      success := false;
      if not shouldDeleteFile then begin
        WizardForm.Close;
      end;
    end else begin
      success := true;
    end;
  finally
    WizardForm.StatusLabel.Caption := StatusText;
    WizardForm.ProgressGauge.Style := npbstNormal;
    if shouldDeleteFile then begin
      DeleteFile(installPath);
    end;
  end;
  Result := success;
end;

procedure InstallFramework;
begin
  InstallFramework452(false);
end;

function FrameworkIsNotInstalled : boolean;
begin
  result := IsDotNetMissing('v4.5.2', 0);
end;

function WillRestart(): Boolean;
begin
  Result := ShouldRestart;
end;

function WontRestart(): Boolean;
begin
  Result := not ShouldRestart;
end;

// Form behaviour procedures
procedure InitializeWizard;
begin
  WizardForm.WizardBitmapImage.Visible := false;
  WizardForm.WizardSmallBitmapImage.Visible := false;

  if IsInstalled() then
  begin
    WizardForm.DirEdit.Enabled := False;
    WizardForm.DirBrowseButton.Enabled := False;
    WizardForm.LicenseAcceptedRadio.Checked := True;
  end;

  ExpandConstant('{#SourcePath}');
end;

procedure KillAppExe;
var
  ResultCode: Integer;
  AppExec: String;
begin
  AppExec := ExpandConstant('{app}\{#MyAppExeName}');
  if FileExists(AppExec) then
  begin
    StringChangeEx(AppExec, '\', '\\', True)
    Exec('cmd.exe', '/c "wmic process where executablepath="' + AppExec + '" delete"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  intResultCode: Integer;
  DumpPath: String;
  DumpCount: Cardinal;
  DumpType: Cardinal;
begin
  case CurStep of
    ssInstall:
      begin
        DumpPath := '%ProgramData%\TPS2MorphoJ\CrashDumps';
        DumpCount := $A;
        DumpType := $1;
        ShouldRestart := False;
        if IsWin64 then
        begin
          RegWriteExpandStringValue(HKLM64, 'SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\TPS2MorphoJ.exe', 'DumpFolder', DumpPath);
          RegWriteDWordValue(HKLM64, 'SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\TPS2MorphoJ.exe', 'DumpCount', DumpCount);
          RegWriteDWordValue(HKLM64, 'SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\TPS2MorphoJ.exe', 'DumpType', DumpType);
        end else
        begin
          RegWriteExpandStringValue(HKLM, 'SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\TPS2MorphoJ.exe', 'DumpFolder', DumpPath);
          RegWriteDWordValue(HKLM, 'SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\TPS2MorphoJ.exe', 'DumpCount', DumpCount);
          RegWriteDWordValue(HKLM, 'SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\TPS2MorphoJ.exe', 'DumpType', DumpType);
        end;
        if FrameworkIsNotInstalled() then
        begin
          ShouldRestart := True;
        end;
        KillAppExe();
      end;
    ssDone:
      begin
        if ShouldRestart then
          if SuppressibleMsgBox(ExpandConstant('{cm:restartMessage,{cm:localRecName}}'), mbConfirmation, MB_YESNO, IDYES) = IDYES then
            Exec('shutdown.exe', '-r -t 0', '', SW_HIDE, ewNoWait, intResultCode);
      end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  case CurUninstallStep of
    usUninstall:
      begin
        KillAppExe();
      end;
  end;
end;
