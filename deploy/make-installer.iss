#define appname "PerfectXL.WebDavServer"
#define fileversion GetFileVersion(AddBackslash(SourcePath) + "..\bin\Debug\" + appname + ".exe")

[Setup]
AllowUNCPath=false
AppendDefaultDirName=False
AppId={{f3470b7f-389b-4853-a03d-5ebab4674c9b}
AppName={#appname}
AppPublisher=Infotron B.V.
AppVersion={#fileversion}
Compression=lzma2/ultra64
DefaultDirName=C:\Program Files\{#appname}
DirExistsWarning=no
DisableDirPage=auto
DisableReadyPage=yes
LanguageDetectionMethod=none
OutputBaseFilename=setup-{#appname}-{#fileversion}
OutputDir=deploy
SetupLogging=true
ShowLanguageDialog=no
SolidCompression=true
SourceDir=..\
VersionInfoCopyright=Copyright 2019 Infotron B.V.
VersionInfoVersion={#fileversion}

[Files]
Source: "bin\Debug\*.dll"; DestDir: "{app}"
Source: "bin\Debug\*.exe"; DestDir: "{app}"; Excludes: "*.vshost*"
Source: "bin\Debug\*.exe.config"; DestDir: "{app}"; Flags: onlyifdoesntexist confirmoverwrite uninsneveruninstall; Excludes: "*.vshost*";
Source: "bin\Debug\*.pdb"; DestDir: "{app}"
Source: "bin\Debug\LICENSE"; DestDir: "{app}"
Source: "bin\Debug\README.md"; DestDir: "{app}"

[UninstallRun]
Filename: "{app}\{#appname}.exe"; Parameters: "stop"; WorkingDir: "{app}"; Flags: waituntilterminated
Filename: "{app}\{#appname}.exe"; Parameters: "uninstall"; WorkingDir: "{app}"; Flags: waituntilterminated

[UninstallDelete]
Type: files; Name: "{app}\*.dll"
Type: files; Name: "{app}\*.exe"
Type: files; Name: "{app}\*.pdb"
Type: files; Name: "{app}\license"
Type: files; Name: "{app}\README.md"
Type: dirifempty; Name: "{app}"

[Code]
var
  UserPage: TInputQueryWizardPage;
  DataDirPage: TInputDirWizardPage;

/////////////////////////////////////////////////////////////////////
function GetUninstallString(): String;
var
  sUnInstPath: String;
  sUnInstallString: String;
begin
  sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
  sUnInstallString := '';
  if (not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString)) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;


/////////////////////////////////////////////////////////////////////
function IsUpgrade(): Boolean;
begin
  Result := (GetUninstallString() <> '');
end;


/////////////////////////////////////////////////////////////////////
function UnInstallOldVersion(): Integer;
var
  sUnInstallString: String;
  iResultCode: Integer;
begin
// Return Values:
// 1 - uninstall string is empty
// 2 - error executing the UnInstallString
// 3 - successfully executed the UnInstallString

  // default return value
  Result := 0;

  // get the uninstall string of the old app
  sUnInstallString := GetUninstallString();
  if (sUnInstallString <> '') then
  begin
    sUnInstallString := RemoveQuotes(sUnInstallString);
    if (Exec(sUnInstallString, '/VERYSILENT /NORESTART /SUPPRESSMSGBOXES', '', SW_HIDE, ewWaitUntilTerminated, iResultCode)) then
      Result := 3
    else
      Result := 2;
  end
  else
    Result := 1;
end;

/////////////////////////////////////////////////////////////////////
procedure CurStepChanged(CurStep: TSetupStep);
var
  sAppName: String;
  iResultCode: Integer;
begin
  if (CurStep=ssInstall) then
  begin
    if (IsUpgrade()) then
      UnInstallOldVersion();
  end;
  if (CurStep=ssPostInstall) then
  begin
    sAppName := WizardDirValue() + '\{#appname}.exe';
    Exec(sAppName, '-configure ' + AddQuotes(DataDirPage.Values[0]) + ' ' + AddQuotes(UserPage.Values[0]) + ' ' + AddQuotes(UserPage.Values[1]) + ' ' + AddQuotes(UserPage.Values[2]) + ' ' + AddQuotes(UserPage.Values[3]), '', SW_SHOW, ewWaitUntilTerminated, iResultCode);
    if (Exec(sAppName, 'install --localsystem', '', SW_SHOW, ewWaitUntilTerminated, iResultCode)) then
      Exec(sAppName, 'start', '', SW_SHOW, ewWaitUntilTerminated, iResultCode)
  end;
end;

/////////////////////////////////////////////////////////////////////
procedure InitializeWizard;
begin
  UserPage := CreateInputQueryPage(wpSelectDir,
    'WebDav Server Information', 'Specifiy the host, port and username/password.',
    'The WebDav Server will run with the settings you specify here.');
  UserPage.Add('Host:', False);
  UserPage.Add('Port:', False);
  UserPage.Add('Username:', False);
  UserPage.Add('Password:', False);
  DataDirPage := CreateInputDirPage(wpSelectDir,
    'WebDav Data Directory', 'Where will the files be stored?',
    'Select the folder where WebDav should save the files.',
    False, '');
  DataDirPage.Add('');

  UserPage.Values[0] := GetPreviousData('Host', 'localhost');
  UserPage.Values[1] := GetPreviousData('Port', '52442');
  UserPage.Values[2] := GetPreviousData('Username', 'PerfectXL');
  UserPage.Values[3] := GetPreviousData('Password', 'PerfectXL');
  DataDirPage.Values[0] := GetPreviousData('DataDir', 'C:\ProgramData\PerfectXL.WebDavServer');
end;

procedure RegisterPreviousData(PreviousDataKey: Integer);
var
  UsageMode: String;
begin
  SetPreviousData(PreviousDataKey, 'Host', UserPage.Values[0]);
  SetPreviousData(PreviousDataKey, 'Port', UserPage.Values[1]);
  SetPreviousData(PreviousDataKey, 'Username', UserPage.Values[2]);
  SetPreviousData(PreviousDataKey, 'Password', UserPage.Values[3]);
  SetPreviousData(PreviousDataKey, 'DataDir', DataDirPage.Values[0]);
end;