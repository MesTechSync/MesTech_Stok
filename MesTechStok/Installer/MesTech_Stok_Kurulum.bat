@echo off
title MesTech Stok Takip Sistemi v2.0 - Kurulum
color 0A

echo.
echo ========================================================================
echo                MesTech STOK TAKiP SiSTEMi v2.0 - KURULUM
echo ========================================================================
echo.
echo [INFO] Self-Contained .NET 9 Deployment - Runtime Gerektirmez
echo [INFO] Windows 10/11 x64 Uyumlu
echo.

set "INSTALL_DIR=%ProgramFiles%\MesTech\StokTakip"
set "SOURCE_DIR=%~dp0..\src\MesTechStok.Desktop\bin\Release\Publish"

echo [1/5] Hedef dizin olusturuluyor: %INSTALL_DIR%
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

echo [2/5] Uygulama dosyalari kopyalaniyor...
xcopy "%SOURCE_DIR%\*" "%INSTALL_DIR%\" /E /I /H /Y >nul 2>&1
if errorlevel 1 (
    echo [HATA] Dosya kopyalama basarisiz!
    pause
    exit /b 1
)

echo [3/5] Desktop kisayolu olusturuluyor...
powershell -Command "& { $WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%USERPROFILE%\Desktop\MesTech Stok Takip v2.0.lnk'); $Shortcut.TargetPath = '%INSTALL_DIR%\MesTechStok.Desktop.exe'; $Shortcut.WorkingDirectory = '%INSTALL_DIR%'; $Shortcut.IconLocation = '%INSTALL_DIR%\MesTechStok.Desktop.exe,0'; $Shortcut.Description = 'MesTech Stok Takip Sistemi v2.0'; $Shortcut.Save() }"

echo [4/5] Başlat menüsü kisayolu olusturuluyor...
if not exist "%APPDATA%\Microsoft\Windows\Start Menu\Programs\MesTech" mkdir "%APPDATA%\Microsoft\Windows\Start Menu\Programs\MesTech"
powershell -Command "& { $WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%APPDATA%\Microsoft\Windows\Start Menu\Programs\MesTech\MesTech Stok Takip v2.0.lnk'); $Shortcut.TargetPath = '%INSTALL_DIR%\MesTechStok.Desktop.exe'; $Shortcut.WorkingDirectory = '%INSTALL_DIR%'; $Shortcut.IconLocation = '%INSTALL_DIR%\MesTechStok.Desktop.exe,0'; $Shortcut.Description = 'MesTech Stok Takip Sistemi v2.0'; $Shortcut.Save() }"

echo [5/5] Kurulum tamamlaniyor...

echo.
echo ========================================================================
echo                        KURULUM TAMAMLANDI!
echo ========================================================================
echo.
echo [BASARILI] MesTech Stok Takip Sistemi kuruldu:
echo            %INSTALL_DIR%
echo.
echo [KISAYOLLAR] 
echo            * Desktop: MesTech Stok Takip v2.0
echo            * Start Menu: Programs\MesTech\
echo.
echo [NOT] Sistem .NET Runtime gerektirmez (Self-Contained)
echo.

set /p "LAUNCH=Uygulamayi simdi baslatmak ister misiniz? (E/H): "
if /i "%LAUNCH%"=="E" (
    echo [BASLATILIYOR] MesTech Stok Takip Sistemi...
    start "" "%INSTALL_DIR%\MesTechStok.Desktop.exe"
)

echo.
echo Kurulum tamamlandi. Bu pencereyi kapayabilirsiniz.
pause >nul
