#define AppName "Short Tech"
#define AppVersion "1.0.0"
#define AppPublisher "Techno Sen"
#define AppURL "https://www.techno-sen.com"
#define AppExe "ShortTech.exe"
#define Source "..\publish\ShortTech"

[Setup]
AppId={{43C25CD4-8B63-4FA5-B0E1-1E71422F530D}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
AppCopyright=© 2026 {#AppPublisher}
VersionInfoVersion={#AppVersion}.0
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Setup
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DisableWelcomePage=no
UsePreviousAppDir=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
SetupIconFile=assets\shorttech.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
WizardStyle=modern
WizardImageFile=assets\wizard-164.bmp,assets\wizard-328.bmp
WizardSmallImageFile=assets\wizard-small-55.bmp,assets\wizard-small-110.bmp
ShowLanguageDialog=auto
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes
OutputDir=Output
OutputBaseFilename=ShortTech-Setup-{#AppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[CustomMessages]
english.OptionsGroup=Options:
arabic.OptionsGroup=خيارات:
english.StartupTask=Start {#AppName} when Windows starts
arabic.StartupTask=تشغيل {#AppName} مع بدء تشغيل Windows
english.InstallingWebView2=Installing Microsoft Edge WebView2...
arabic.InstallingWebView2=جارٍ تثبيت Microsoft Edge WebView2...
english.WebView2Failed=Microsoft Edge WebView2 could not be installed (no internet connection?).%n%n{#AppName} will work normally. Only converting documents and spreadsheets to PDF needs WebView2, and you can install it later from Microsoft's website.
arabic.WebView2Failed=تعذّر تثبيت Microsoft Edge WebView2 (ربما لا يوجد اتصال بالإنترنت).%n%nسيعمل {#AppName} بشكل طبيعي، فقط تحويل المستندات والجداول إلى PDF يحتاج WebView2، ويمكنك تثبيته لاحقًا من موقع Microsoft.
english.RemoveSettings=Do you also want to delete your {#AppName} settings?
arabic.RemoveSettings=هل تريد أيضًا حذف إعدادات {#AppName}؟

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:OptionsGroup}"
Name: "startup"; Description: "{cm:StartupTask}"; GroupDescription: "{cm:OptionsGroup}"

[Files]
Source: "{#Source}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#AppName}"; ValueData: """{app}\{#AppExe}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
const
  RunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';
  WebView2Client = 'Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';
  WebView2Url = 'https://go.microsoft.com/fwlink/p/?LinkId=2124703';
  WebView2File = 'MicrosoftEdgeWebview2Setup.exe';

var
  WebView2Ready: Boolean;

procedure CloseShortTech;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM ShortTech.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM shorttech-cli.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

function HasVersion(RootKey: Integer; SubKey: String): Boolean;
var
  Version: String;
begin
  Result := RegQueryStringValue(RootKey, SubKey, 'pv', Version) and (Version <> '') and (Version <> '0.0.0.0');
end;

function WebView2Installed: Boolean;
begin
  Result := HasVersion(HKLM, 'SOFTWARE\WOW6432Node\' + WebView2Client) or HasVersion(HKCU, 'Software\' + WebView2Client);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  CloseShortTech;
  WebView2Ready := False;
  if not WebView2Installed then
  begin
    try
      DownloadTemporaryFile(WebView2Url, WebView2File, '', nil);
      WebView2Ready := True;
    except
      Log('WebView2 download failed: ' + GetExceptionMessage);
    end;
  end;
  Result := '';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    if not WizardIsTaskSelected('startup') then
      RegDeleteValue(HKCU, RunKey, '{#AppName}');
    if not WebView2Installed then
    begin
      if WebView2Ready then
      begin
        WizardForm.StatusLabel.Caption := CustomMessage('InstallingWebView2');
        Exec(ExpandConstant('{tmp}\' + WebView2File), '/silent /install', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      end;
      if not WebView2Installed then
        SuppressibleMsgBox(CustomMessage('WebView2Failed'), mbInformation, MB_OK, IDOK);
    end;
  end;
end;

function InitializeUninstall: Boolean;
begin
  CloseShortTech;
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    RegDeleteValue(HKCU, RunKey, '{#AppName}');
    DelTree(ExpandConstant('{%TEMP}\ShortTech'), True, True, True);
    if not UninstallSilent then
      if MsgBox(CustomMessage('RemoveSettings'), mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
        DelTree(ExpandConstant('{userappdata}\ShortTech'), True, True, True);
  end;
end;
