@echo off
echo Publishing self-contained ClipForge for Windows 11 x64...
dotnet publish ClipForge.Platform.Windows\ClipForge.Platform.Windows.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish\win-x64
echo Published executable: publish\win-x64\ClipForge.exe
