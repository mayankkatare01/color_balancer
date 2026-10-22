@echo off
setlocal
echo ========================================================
echo  Building LuminaColor - Windows 11 Monitor Color Balancer
echo ========================================================

taskkill /F /IM LuminaColor.exe >nul 2>&1
ping 127.0.0.1 -n 2 >nul

set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set WPF_LIB="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF"
set NET_LIB="C:\Windows\Microsoft.NET\Framework64\v4.0.30319"

if not exist %CSC% (
    echo [ERROR] csc.exe compiler not found at %CSC%
    exit /b 1
)

%CSC% /nologo /t:winexe /optimize+ /win32icon:app.ico /win32manifest:app.manifest /lib:%WPF_LIB%,%NET_LIB% /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Xaml.dll,WindowsBase.dll,PresentationCore.dll,PresentationFramework.dll /out:LuminaColor.exe src\NativeMethods.cs src\ColorEngine.cs src\DisplayManager.cs src\SettingsManager.cs src\MainWindow.cs src\Program.cs

if %ERRORLEVEL% equ 0 (
    echo [SUCCESS] LuminaColor.exe built successfully!
) else (
    echo [ERROR] Compilation failed.
    exit /b %ERRORLEVEL%
)

endlocal
