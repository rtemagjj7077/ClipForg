#!/bin/bash
set -e
echo "Building ClipForge for Linux x64..."
dotnet build ClipForge.sln -c Release -r linux-x64
echo "Build successful: ClipForge.Platform.Linux/bin/Release/net8.0/linux-x64/ClipForge"
