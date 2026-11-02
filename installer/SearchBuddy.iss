#define AppName "SearchBuddy"
#define AppVersion "0.1.0"
#define ServiceName "SearchBuddyIndexer"
#define PublishDir "..\publish"

[Setup]
AppId={{8F3A2C1E-5B7D-4E9A-9C6F-2D1B4A7E3F90}
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={autopf}\{#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
ChangesEnvironment=yes
WizardStyle=modern
OutputDir=..\installer-out
OutputBaseFilename=SearchBuddySetup-{#AppVersion}
Compression=lzma2
SolidCompression=yes

[Tasks]
Name: "ext_txt"; Description: ".txt"; GroupDescription: "Indexed file types:"
Name: "ext_pdf"; Description: ".pdf"; GroupDescription: "Indexed file types:"
Name: "ext_docx"; Description: ".docx"; GroupDescription: "Indexed file types:"
Name: "ext_pptx"; Description: ".pptx"; GroupDescription: "Indexed file types:"
Name: "ext_md"; Description: ".md"; GroupDescription: "Indexed file types:"
Name: "ext_cs"; Description: ".cs"; GroupDescription: "Indexed file types:"

[Files]
Source: "{#PublishDir}\indexer\*"; DestDir: "{app}\indexer"; Flags: recursesubdirs createallsubdirs ignoreversion
Source: "{#PublishDir}\cli\*"; DestDir: "{app}\cli"; Flags: recursesubdirs createallsubdirs ignoreversion

[Registry]
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"; ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}\cli"; Check: NeedsAddPath(ExpandConstant('{app}\cli'))

[Run]
Filename: "{sys}\sc.exe"; Parameters: "create {#ServiceName} binPath= ""{app}\indexer\SearchBuddy.Indexer.exe"" start= delayed-auto"; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "failure {#ServiceName} reset= 86400 actions= restart/10000/restart/10000/restart/60000"; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "failureflag {#ServiceName} 1"; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "start {#ServiceName}"; Flags: runhidden waituntilterminated

[Code]
const
  EnvKey = 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment';

function NeedsAddPath(Dir: string): Boolean;
var
  P: string;
begin
  if not RegQueryStringValue(HKLM, EnvKey, 'Path', P) then
    Result := True
  else
    Result := Pos(';' + Uppercase(Dir) + ';', ';' + Uppercase(P) + ';') = 0;
end;

procedure RemovePath(Dir: string);
var
  P: string;
  I: Integer;
begin
  if RegQueryStringValue(HKLM, EnvKey, 'Path', P) then
  begin
    I := Pos(';' + Uppercase(Dir), Uppercase(P));
    if I > 0 then
    begin
      Delete(P, I, Length(Dir) + 1);
      RegWriteExpandStringValue(HKLM, EnvKey, 'Path', P);
    end;
  end;
end;

procedure RunHidden(Exe, Params: string);
var
  R: Integer;
begin
  Exec(ExpandConstant('{sys}\' + Exe), Params, '', SW_HIDE, ewWaitUntilTerminated, R);
end;

procedure AddExt(var List: string; Task, Ext: string);
begin
  if WizardIsTaskSelected(Task) then
  begin
    if List <> '' then List := List + ', ';
    List := List + '"' + Ext + '"';
  end;
end;

procedure WriteSettings();
var
  Dir, DbPath, Exts: string;
begin
  Dir := ExpandConstant('{commonappdata}\{#AppName}');
  ForceDirectories(Dir);

  Exts := '';
  AddExt(Exts, 'ext_txt', '.txt');
  AddExt(Exts, 'ext_pdf', '.pdf');
  AddExt(Exts, 'ext_docx', '.docx');
  AddExt(Exts, 'ext_pptx', '.pptx');
  AddExt(Exts, 'ext_md', '.md');
  AddExt(Exts, 'ext_cs', '.cs');

  DbPath := Dir + '\index.db';
  StringChangeEx(DbPath, '\', '\\', True);

  SaveStringToFile(Dir + '\watchsettings.json',
    '{' + #13#10 +
    '  "Watch": { "Extensions": [ ' + Exts + ' ] },' + #13#10 +
    '  "Database": { "Path": "' + DbPath + '" }' + #13#10 +
    '}', False);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  RunHidden('net.exe', 'stop {#ServiceName}');
  WriteSettings();
  Result := '';
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    RunHidden('net.exe', 'stop {#ServiceName}');
    RunHidden('sc.exe', 'delete {#ServiceName}');
  end;

  if CurUninstallStep = usPostUninstall then
    RemovePath(ExpandConstant('{app}\cli'));
end;
