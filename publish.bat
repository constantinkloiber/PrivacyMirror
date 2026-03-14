@echo off
echo PrivacyMirror - Portable EXE erstellen
echo.
cd /d "%~dp0"
dotnet publish PrivacyMirror.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish\
echo.
if exist "publish\PrivacyMirror.exe" (
    echo FERTIG: publish\PrivacyMirror.exe
    explorer publish\
) else (
    echo FEHLER: .NET SDK 8 pruefen.
)
pause
