set "SEVENZ=C:\Program Files\7-Zip\7z.exe"

dotnet publish LastfmDiscordRPC2\LastfmDiscordRPC2.csproj -r win-x64 --self-contained false -c Release -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish LastfmDiscordRPC2.Windows\LastfmDiscordRPC2.Windows.csproj -c Release

set "OUTDIR=%~dp0release"
if not exist "%OUTDIR%" mkdir "%OUTDIR%"

cd LastfmDiscordRPC2\bin\Release\net8.0
del win-x64.zip 2>nul

cd win-x64
copy ..\..\..\..\LastfmDiscordRPC2.Windows\bin\Release\net8.0-windows10.0.19041.0\LastfmDiscordRPC2.Windows.dll .\publish\
"%SEVENZ%" a -tzip ..\win-x64.zip .\publish\LastfmDiscordRPC2.exe .\publish\LastfmDiscordRPC2.Windows.dll

cd ..\..\..\..\..
copy LastfmDiscordRPC2\bin\Release\net8.0\win-x64.zip "%OUTDIR%\"
copy LastfmDiscordRPC2\bin\Release\net8.0\win-x64\publish\LastfmDiscordRPC2.exe "%OUTDIR%\"
copy LastfmDiscordRPC2\bin\Release\net8.0\win-x64\publish\LastfmDiscordRPC2.Windows.dll "%OUTDIR%\"

echo.
echo Build complete. Archives and LastfmDiscordRPC2.exe copied to: %OUTDIR%
pause
