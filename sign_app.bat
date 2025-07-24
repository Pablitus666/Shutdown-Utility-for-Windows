@echo off
echo.
echo Intentando firmar la aplicacion...
echo.
"C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe" sign /fd SHA256 /f "I:\ShutdownApp - Publicar\ShutdownApp\ShutdownAppDev.pfx" /p "Condorito123*#*?" /t http://timestamp.digicert.com "I:\ShutdownApp - Publicar\ShutdownApp\bin\Release\net6.0-windows\win-x64\publish\ShutdownApp.exe"

if %errorlevel% equ 0 (
    echo.
    echo ==================================================
    echo  EXITO: El archivo ShutdownApp.exe fue firmado.
    echo ==================================================
) else (
    echo.
    echo =================================================
    echo  ERROR: No se pudo firmar el archivo.
    echo  Codigo de error: %errorlevel%
    echo =================================================
)
echo.
pause
