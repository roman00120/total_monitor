#define MyAppName "Total Monitor"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Total Monitor"
#define MyAppExeName "TotalMonitor.exe"
#define MyServerExeName "TotalMonitor.Server.exe"

[Setup]
AppId={{B6B1A8D4-0E4A-4B4E-9E65-6D4C02D0A001}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\TotalMonitor
DefaultGroupName=Total Monitor
OutputDir=..\publish\installer
OutputBaseFilename=TotalMonitor-Setup
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
Uninstallable=yes
UninstallDisplayIcon={app}\client\{#MyAppExeName}
SetupIconFile=..\src\TotalMonitor.App\Assets\Logo\TotalMonitor.ico
WizardStyle=modern
WizardSmallImageFile=assets\HeaderLogo.bmp
DisableWelcomePage=no

[Files]
Source: "..\publish\client\*"; DestDir: "{app}\client"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\src\TotalMonitor.App\Assets\Logo\TotalMonitor.ico"; DestDir: "{app}\client"; Flags: ignoreversion
Source: "..\publish\server\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\docs\*"; DestDir: "{app}\docs"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "assets\WelcomeLogo.bmp"; Flags: dontcopy
Source: "assets\HeaderLogo.bmp"; Flags: dontcopy

[Icons]
Name: "{group}\Total Monitor"; Filename: "{app}\client\{#MyAppExeName}"; IconFilename: "{app}\client\TotalMonitor.ico"
Name: "{commondesktop}\Total Monitor"; Filename: "{app}\client\{#MyAppExeName}"; IconFilename: "{app}\client\TotalMonitor.ico"

[Run]
Filename: "{app}\client\{#MyAppExeName}"; Description: "Iniciar Total Monitor"; Flags: postinstall nowait skipifsilent unchecked

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop TotalMonitor"; Flags: runhidden waituntilterminated; RunOnceId: "StopTotalMonitor"
Filename: "{sys}\sc.exe"; Parameters: "delete TotalMonitor"; Flags: runhidden waituntilterminated; RunOnceId: "DeleteTotalMonitor"

[Code]
var
  ServerPage: TInputQueryWizardPage;
  DatabasePage: TInputQueryWizardPage;
  AdminPage: TInputQueryWizardPage;

function JsonEscape(Value: string): string;
begin
  Result := Value;
  StringChangeEx(Result, Chr(92), Chr(92) + Chr(92), True);
  StringChangeEx(Result, '"', Chr(92) + '"', True);
end;

function GenerateSecret: string;
var
  I: Integer;
  Chars: string;
begin
  Result := '';
  Chars := 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@$%';
  for I := 1 to 48 do
    Result := Result + Chars[Random(Length(Chars)) + 1];
end;

function BuildConnectionString: string;
begin
  Result := 'Server=' + DatabasePage.Values[0] + ';Port=' + DatabasePage.Values[1] +
    ';Database=' + DatabasePage.Values[2] + ';User=' + DatabasePage.Values[3] +
    ';Password=' + DatabasePage.Values[4] + ';';
end;

procedure InitializeWizard;
var
  WelcomeLogoImage: TBitmapImage;
  FinishedLogoImage: TBitmapImage;
  LogoPath: string;
begin
  ExtractTemporaryFile('WelcomeLogo.bmp');
  LogoPath := ExpandConstant('{tmp}\WelcomeLogo.bmp');

  // Hide default side images and clean backgrounds
  WizardForm.WizardBitmapImage.Visible := False;
  WizardForm.WizardBitmapImage.Width := 0;
  WizardForm.WizardBitmapImage2.Visible := False;
  WizardForm.WizardBitmapImage2.Width := 0;
  WizardForm.WelcomePage.Color := clWhite;
  WizardForm.FinishedPage.Color := clWhite;

  // Welcome page - Centered Large Logo
  WelcomeLogoImage := TBitmapImage.Create(WizardForm);
  WelcomeLogoImage.Parent := WizardForm.WelcomePage;
  WelcomeLogoImage.Bitmap.LoadFromFile(LogoPath);
  WelcomeLogoImage.AutoSize := False;
  WelcomeLogoImage.Stretch := True;
  WelcomeLogoImage.ReplaceColor := clNone;
  WelcomeLogoImage.Width := 380;
  WelcomeLogoImage.Height := 56;
  WelcomeLogoImage.Left := (WizardForm.WelcomePage.Width - WelcomeLogoImage.Width) / 2;
  WelcomeLogoImage.Top := 35;

  // Welcome Labels - Centered with proper margins and height
  WizardForm.WelcomeLabel1.Left := 20;
  WizardForm.WelcomeLabel1.Width := WizardForm.WelcomePage.Width - 40;
  WizardForm.WelcomeLabel1.Top := 115;
  WizardForm.WelcomeLabel1.Height := 30;
  WizardForm.WelcomeLabel1.AutoSize := False;
  WizardForm.WelcomeLabel1.Alignment := taCenter;
  WizardForm.WelcomeLabel1.Caption := 'Instalación de TOTAL MONITOR';
  WizardForm.WelcomeLabel1.Font.Size := 13;
  WizardForm.WelcomeLabel1.Font.Style := [fsBold];

  WizardForm.WelcomeLabel2.Left := 30;
  WizardForm.WelcomeLabel2.Width := WizardForm.WelcomePage.Width - 60;
  WizardForm.WelcomeLabel2.Top := 155;
  WizardForm.WelcomeLabel2.Height := 100;
  WizardForm.WelcomeLabel2.AutoSize := False;
  WizardForm.WelcomeLabel2.Alignment := taCenter;
  WizardForm.WelcomeLabel2.Caption := 'Sistema de Monitoreo Eléctrico y Gestión Energética' + #13#10#13#10 +
    'Este asistente instalará el software Total Monitor (Servidor API, Motor de Adquisición RS485 / Modbus RTU y Cliente) en su computadora.' + #13#10#13#10 +
    'Haga clic en Siguiente para continuar con la instalación.';

  // Finished page - Centered Large Logo
  FinishedLogoImage := TBitmapImage.Create(WizardForm);
  FinishedLogoImage.Parent := WizardForm.FinishedPage;
  FinishedLogoImage.Bitmap.LoadFromFile(LogoPath);
  FinishedLogoImage.AutoSize := False;
  FinishedLogoImage.Stretch := True;
  FinishedLogoImage.ReplaceColor := clNone;
  FinishedLogoImage.Width := 380;
  FinishedLogoImage.Height := 56;
  FinishedLogoImage.Left := (WizardForm.FinishedPage.Width - FinishedLogoImage.Width) / 2;
  FinishedLogoImage.Top := 35;

  WizardForm.FinishedHeadingLabel.Left := 20;
  WizardForm.FinishedHeadingLabel.Width := WizardForm.FinishedPage.Width - 40;
  WizardForm.FinishedHeadingLabel.Top := 115;
  WizardForm.FinishedHeadingLabel.Height := 30;
  WizardForm.FinishedHeadingLabel.AutoSize := False;
  WizardForm.FinishedHeadingLabel.Alignment := taCenter;
  WizardForm.FinishedHeadingLabel.Caption := 'Completando la instalación de TOTAL MONITOR';
  WizardForm.FinishedHeadingLabel.Font.Size := 13;
  WizardForm.FinishedHeadingLabel.Font.Style := [fsBold];

  WizardForm.FinishedLabel.Left := 30;
  WizardForm.FinishedLabel.Width := WizardForm.FinishedPage.Width - 60;
  WizardForm.FinishedLabel.Top := 155;
  WizardForm.FinishedLabel.Height := 100;
  WizardForm.FinishedLabel.AutoSize := False;
  WizardForm.FinishedLabel.Alignment := taCenter;

  // Custom configuration wizard pages
  ServerPage := CreateInputQueryPage(wpSelectDir, 'Servidor API', 'Configuración de comunicación',
    'Indica dónde escuchará el servidor y dónde se conectará el cliente.');
  ServerPage.Add('Host o URL sin protocolo:', False);
  ServerPage.Add('Puerto API:', False);
  ServerPage.Values[0] := 'localhost';
  ServerPage.Values[1] := '5080';

  DatabasePage := CreateInputQueryPage(ServerPage.ID, 'Base de datos MySQL', 'Conexión de base de datos',
    'La base debe existir o tener permisos de creación.');
  DatabasePage.Add('Host MySQL:', False);
  DatabasePage.Add('Puerto MySQL:', False);
  DatabasePage.Add('Base de datos:', False);
  DatabasePage.Add('Usuario MySQL:', False);
  DatabasePage.Add('Contraseña MySQL:', True);
  DatabasePage.Values[0] := 'localhost';
  DatabasePage.Values[1] := '3306';
  DatabasePage.Values[2] := 'totalmonitor';
  DatabasePage.Values[3] := 'root';

  AdminPage := CreateInputQueryPage(DatabasePage.ID, 'Administrador inicial', 'Credenciales iniciales',
    'Se crearán una sola vez si todavía no existe un administrador.');
  AdminPage.Add('Usuario administrador:', False);
  AdminPage.Add('Contraseña (mínimo 10 caracteres):', True);
  AdminPage.Add('Nombre mostrado:', False);
  AdminPage.Values[0] := 'admin';
  AdminPage.Values[2] := 'Administrador';
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = ServerPage.ID then
    if (Trim(ServerPage.Values[0]) = '') or (Trim(ServerPage.Values[1]) = '') then begin
      MsgBox('Debes indicar el host y el puerto del API.', mbError, MB_OK);
      Result := False;
    end;
  if CurPageID = DatabasePage.ID then
    if (Trim(DatabasePage.Values[0]) = '') or (Trim(DatabasePage.Values[2]) = '') or
       (Trim(DatabasePage.Values[3]) = '') then begin
      MsgBox('Debes completar la conexión MySQL.', mbError, MB_OK);
      Result := False;
    end;
  if CurPageID = AdminPage.ID then
    if (Trim(AdminPage.Values[0]) = '') or (Length(AdminPage.Values[1]) < 10) then begin
      MsgBox('El usuario es obligatorio y la contraseña debe tener al menos 10 caracteres.', mbError, MB_OK);
      Result := False;
    end;
end;

procedure WriteConfiguration;
var
  ServerConfig, ClientConfig, BaseUrl, Secret: string;
begin
  BaseUrl := 'http://' + Trim(ServerPage.Values[0]) + ':' + Trim(ServerPage.Values[1]) + '/';
  Secret := GenerateSecret;
  ServerConfig :=
    '{' + #13#10 +
    '  "Urls": "' + JsonEscape('http://' + Trim(ServerPage.Values[0]) + ':' + Trim(ServerPage.Values[1])) + '",' + #13#10 +
    '  "ConnectionStrings": { "Default": "' + JsonEscape(BuildConnectionString) + '" },' + #13#10 +
    '  "Authentication": {' + #13#10 +
    '    "InitialAdminUsername": "' + JsonEscape(Trim(AdminPage.Values[0])) + '",' + #13#10 +
    '    "InitialAdminPassword": "' + JsonEscape(AdminPage.Values[1]) + '",' + #13#10 +
    '    "InitialAdminName": "' + JsonEscape(Trim(AdminPage.Values[2])) + '",' + #13#10 +
    '    "Issuer": "TotalMonitor", "Audience": "TotalMonitor.Client", "SecretKey": "' + Secret + '"' + #13#10 +
    '  },' + #13#10 +
    '  "Server": { "Mode": "Real" },' + #13#10 +
    '  "Acquisition": { "Enabled": false },' + #13#10 +
    '  "TOV452RegisterMap": { "Entries": [] }' + #13#10 +
    '}';
  ClientConfig := '{' + #13#10 + '  "Api": { "BaseUrl": "' + JsonEscape(BaseUrl) + '" }' + #13#10 + '}';
  SaveStringToFile(ExpandConstant('{app}\appsettings.Production.json'), ServerConfig, False);
  SaveStringToFile(ExpandConstant('{app}\client\appsettings.Production.json'), ClientConfig, False);
end;

procedure VerifyServiceAndApi; forward;

procedure RegisterAndStartService;
var
  ResultCode: Integer;
  ExePath: string;
begin
  ExePath := ExpandConstant('{app}\{#MyServerExeName}');
  if not FileExists(ExePath) then begin
    MsgBox('No se encontró el ejecutable del servidor en:' + #13#10 + ExePath + #13#10 +
      'La instalación no puede registrar el servicio.', mbError, MB_OK);
    Exit;
  end;
  Exec(ExpandConstant('{sys}\sc.exe'), 'stop TotalMonitor', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec(ExpandConstant('{sys}\sc.exe'), 'delete TotalMonitor', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec(ExpandConstant('{sys}\sc.exe'), 'create TotalMonitor binPath= "' + ExePath +
    '" start= auto DisplayName= "Total Monitor"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if ResultCode <> 0 then begin
    MsgBox('No fue posible registrar el servicio TotalMonitor. Código: ' + IntToStr(ResultCode), mbError, MB_OK);
    Exit;
  end;
  Exec(ExpandConstant('{sys}\sc.exe'), 'failure TotalMonitor reset= 86400 actions= restart/5000/restart/30000/restart/60000',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec(ExpandConstant('{sys}\sc.exe'), 'start TotalMonitor', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if ResultCode <> 0 then begin
    MsgBox('El servicio fue registrado, pero no pudo iniciarse. Código: ' + IntToStr(ResultCode) +
      '. Revisa MySQL, la configuración y el Visor de eventos de Windows.', mbError, MB_OK);
    Exit;
  end;
  VerifyServiceAndApi;
end;

procedure VerifyServiceAndApi;
var
  ResultCode, Attempt: Integer;
begin
  for Attempt := 1 to 30 do begin
    Sleep(1000);
    Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
      '-NoProfile -NonInteractive -Command "if ((Get-Service -Name TotalMonitor -ErrorAction SilentlyContinue).Status -eq ''Running'') { exit 0 } else { exit 1 }"',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    if ResultCode = 0 then begin
      Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
        '-NoProfile -NonInteractive -Command "try { if ((Invoke-WebRequest -UseBasicParsing -TimeoutSec 2 http://localhost:5080/api/v1/health).StatusCode -eq 200) { exit 0 } else { exit 1 } } catch { exit 1 }"',
        '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      if ResultCode = 0 then Exit;
    end;
  end;
  MsgBox('TotalMonitor no pudo completar la verificación automática. Confirma que el servicio esté Running y que http://localhost:5080/api/v1/health responda. Revisa el Visor de eventos de Windows para el error real.', mbError, MB_OK);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then begin
    WriteConfiguration;
    RegisterAndStartService;
  end;
end;
