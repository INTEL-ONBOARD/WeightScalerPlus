; Inno Setup 6 script for WeightMaster
; Built in CI via ISCC; pass /DAppVersion, /DPublishDir, /DOutputDir from the workflow.

#ifndef AppVersion
  #define AppVersion "0.0.0-dev"
#endif
#ifndef PublishDir
  #define PublishDir "..\publish"
#endif
#ifndef OutputDir
  #define OutputDir "..\dist"
#endif

[Setup]
AppId={{C8D5A6E2-7E1D-4C7F-9F2A-WEIGHTMASTER01}}
AppName=WeightMaster
AppVersion={#AppVersion}
AppPublisher=Tea Coop
DefaultDirName={autopf}\WeightMaster
DefaultGroupName=WeightMaster
DisableProgramGroupPage=yes
PrivilegesRequired=admin
; Let the installer close (and restart) a running WeightMaster.exe so it can be replaced during an update.
CloseApplications=yes
RestartApplications=yes
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
Compression=lzma2/ultra
SolidCompression=yes
OutputDir={#OutputDir}
OutputBaseFilename=WeightMaster-Setup-{#AppVersion}
LicenseFile=LICENSE.txt
WizardStyle=modern
UninstallDisplayIcon={app}\WeightMaster.exe
SetupIconFile=..\WeightMaster\Interfaces\teacoop_logo.ico

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion
Source: "templates\config.json.template";  DestDir: "{tmp}"; Flags: dontcopy
Source: "templates\dbconfig.txt.template"; DestDir: "{tmp}"; Flags: dontcopy

[Icons]
Name: "{group}\WeightMaster";         Filename: "{app}\WeightMaster.exe"
Name: "{commondesktop}\WeightMaster"; Filename: "{app}\WeightMaster.exe"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"

[Code]
var
  BranchPage: TInputQueryWizardPage;
  DbPage:     TInputQueryWizardPage;

function ReadFileToString(const Path: String): String;
var
  Lines: TArrayOfString;
  i: Integer;
begin
  Result := '';
  if FileExists(Path) and LoadStringsFromFile(Path, Lines) then
    for i := 0 to GetArrayLength(Lines) - 1 do
      Result := Result + Lines[i] + #10;
end;

{ Extracts a JSON string value, e.g. the "branch_id" field, from config.json content. }
function ExtractJsonValue(const Content, Key: String): String;
var
  P, Q, R: Integer;
  S: String;
begin
  Result := '';
  P := Pos('"' + Key + '"', Content);
  if P = 0 then Exit;
  S := Copy(Content, P + Length(Key) + 2, Length(Content));
  Q := Pos(':', S);
  if Q = 0 then Exit;
  S := Copy(S, Q + 1, Length(S));
  Q := Pos('"', S);
  if Q = 0 then Exit;
  S := Copy(S, Q + 1, Length(S));
  R := Pos('"', S);
  if R = 0 then Exit;
  Result := Copy(S, 1, R - 1);
end;

{ Extracts a value, e.g. "Server", from a Key=Value;Key=Value connection string. }
function ExtractConnValue(const Content, Key: String): String;
var
  P, Q: Integer;
  S: String;
begin
  Result := '';
  P := Pos(Key + '=', Content);
  if P = 0 then Exit;
  S := Copy(Content, P + Length(Key) + 1, Length(Content));
  Q := Pos(';', S);
  if Q = 0 then Q := Length(S) + 1;
  Result := Copy(S, 1, Q - 1);
end;

{ On an update over an existing install, pre-fill the wizard from the current config so the
  operator does not have to re-enter (and risk wiping) the station's branch/DB settings. }
procedure PrefillFromExistingInstall();
var
  AppDir, CfgJson, DbConf, V: String;
begin
  AppDir := ExpandConstant('{app}');

  CfgJson := ReadFileToString(AppDir + '\config.json');
  if CfgJson <> '' then begin
    V := ExtractJsonValue(CfgJson, 'branch_id');   if V <> '' then BranchPage.Values[0] := V;
    V := ExtractJsonValue(CfgJson, 'branch_name'); if V <> '' then BranchPage.Values[1] := V;
    { build_no is intentionally left as the new app version, not the old value. }
  end;

  DbConf := ReadFileToString(AppDir + '\dbconfig.txt');
  if DbConf <> '' then begin
    V := ExtractConnValue(DbConf, 'Server');   if V <> '' then DbPage.Values[0] := V;
    V := ExtractConnValue(DbConf, 'Port');     if V <> '' then DbPage.Values[1] := V;
    V := ExtractConnValue(DbConf, 'Database'); if V <> '' then DbPage.Values[2] := V;
    V := ExtractConnValue(DbConf, 'Uid');      if V <> '' then DbPage.Values[3] := V;
    V := ExtractConnValue(DbConf, 'Pwd');      if V <> '' then DbPage.Values[4] := V;
  end;
end;

procedure InitializeWizard();
begin
  BranchPage := CreateInputQueryPage(wpSelectDir,
    'Branch settings',
    'Identify this weighing station',
    'These values are written to config.json next to the application. You can edit the file later.');
  BranchPage.Add('Branch ID:',   False);
  BranchPage.Add('Branch name:', False);
  BranchPage.Add('Build no:',    False);
  BranchPage.Values[2] := '{#AppVersion}';

  DbPage := CreateInputQueryPage(BranchPage.ID,
    'Database connection',
    'MySQL server for this station',
    'These values are combined into a connection string in dbconfig.txt.');
  DbPage.Add('Server host (e.g. 10.0.0.5):', False);
  DbPage.Add('Port (default 3306):',         False);
  DbPage.Add('Database name:',               False);
  DbPage.Add('Username:',                    False);
  DbPage.Add('Password:',                    True);
  DbPage.Values[1] := '3306';

  PrefillFromExistingInstall();
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = BranchPage.ID then begin
    if (Trim(BranchPage.Values[0]) = '') or (Trim(BranchPage.Values[1]) = '') then begin
      MsgBox('Branch ID and Branch name are required.', mbError, MB_OK);
      Result := False;
    end;
  end else if CurPageID = DbPage.ID then begin
    if (Trim(DbPage.Values[0]) = '') or (Trim(DbPage.Values[2]) = '')
       or (Trim(DbPage.Values[3]) = '') then begin
      MsgBox('Server, Database and Username are required.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

procedure WriteConfigFiles();
var
  Json, Conn: String;
begin
  Json :=
    '{' + #13#10 +
    '  "branch_id":   "' + BranchPage.Values[0] + '",' + #13#10 +
    '  "branch_name": "' + BranchPage.Values[1] + '",' + #13#10 +
    '  "build_no":    "' + BranchPage.Values[2] + '"' + #13#10 +
    '}' + #13#10;
  SaveStringToFile(ExpandConstant('{app}\config.json'), Json, False);

  Conn :=
    'Server='    + DbPage.Values[0] +
    ';Port='     + DbPage.Values[1] +
    ';Database=' + DbPage.Values[2] +
    ';Uid='      + DbPage.Values[3] +
    ';Pwd='      + DbPage.Values[4] + ';';
  SaveStringToFile(ExpandConstant('{app}\dbconfig.txt'), Conn, False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    WriteConfigFiles();
end;
