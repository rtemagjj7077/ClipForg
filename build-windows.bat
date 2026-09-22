@echo off
echo Building ClipForge for Windows 11 x64...
dotnet build ClipForge.sln -c Release -r win-x64
echo Build completed: ClipForge.Platform.Windows\bin\Release\net8.0-windows\win-x64\ClipForge.exe
